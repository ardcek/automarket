// ============================================================================
// REZERVASYON OPERASYONLARI + ÖDEMELER
//   /Yonetim/Rezervasyonlar          → liste, arama, durum sekmeleri
//   /Yonetim/Rezervasyonlar/Detay/5  → onayla, reddet, teslim et, iade al, ödeme al, sözleşme
//   /Yonetim/Odemeler                → kasa hareketleri
// Controller sadece formu alıp RezervasyonServisi'ni çağırır; kurallar serviste.
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

public class RezervasyonlarController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly RezervasyonServisi _rezervasyonlar;
    private readonly FiyatServisi _fiyat;
    private readonly SozlesmeServisi _sozlesme;

    public RezervasyonlarController(VeriTabani vt, RezervasyonServisi rezervasyonlar, FiyatServisi fiyat, SozlesmeServisi sozlesme)
    {
        _vt = vt;
        _rezervasyonlar = rezervasyonlar;
        _fiyat = fiyat;
        _sozlesme = sozlesme;
    }

    public async Task<IActionResult> Index(RezervasyonFiltresi filtre)
    {
        var sorgu = DetayliSorgu().AsNoTracking();
        if (filtre.Durum != null) sorgu = sorgu.Where(r => r.Durum == filtre.Durum);
        if (filtre.SadeceGeciken) sorgu = sorgu.Where(r => r.Durum == RezervasyonDurumu.Kirada && r.BitisTarihi < DateTime.Now);
        if (filtre.SubeId != null) sorgu = sorgu.Where(r => r.AlisSubeId == filtre.SubeId || r.IadeSubeId == filtre.SubeId);
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var aranan = filtre.Arama.Trim();
            var buyuk = aranan.ToUpperInvariant();
            sorgu = sorgu.Where(r => r.Kod.Contains(buyuk) || r.Arac!.Plaka.Contains(buyuk)
                || r.Musteri!.Ad.Contains(aranan) || r.Musteri.Soyad.Contains(aranan) || r.Musteri.Email!.Contains(aranan));
        }
        // Bekleyenlerde en eski talep önce, diğerlerinde en yeni alış tarihi önce
        sorgu = filtre.Durum == RezervasyonDurumu.OnayBekliyor ? sorgu.OrderBy(r => r.OlusturmaTarihi) : sorgu.OrderByDescending(r => r.BaslangicTarihi);

        ViewBag.Filtre = filtre;
        ViewBag.Subeler = await _vt.Subeler.OrderBy(s => s.Ad).ToListAsync();
        ViewBag.DurumSayilari = await _vt.Rezervasyonlar.GroupBy(r => r.Durum).Select(g => new { g.Key, Sayi = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Sayi);
        return View(await SayfaListesi<Rezervasyon>.OlusturAsync(sorgu, filtre.Sayfa, 20));
    }

    public async Task<IActionResult> Detay(int id)
    {
        var r = await DetayliSorgu().Include(x => x.Ekstralar).ThenInclude(e => e.Ekstra).Include(x => x.Odemeler).ThenInclude(o => o.AlanKullanici)
            .Include(x => x.Hasarlar).Include(x => x.IslemYapan).AsSplitQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        ViewBag.TahminiGecikme = r.Durum == RezervasyonDurumu.Kirada ? _fiyat.GecikmeUcreti(r.GunlukFiyat, r.BitisTarihi, DateTime.Now) : 0m;
        return View(r);
    }

    // ---- Tek tıklık işlemler: hepsi aynı kalıp → servisi çağır, sonucu bildir, detaya dön ----
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Onayla(int id) => CalistirAsync(id, () => _rezervasyonlar.OnaylaAsync(id, KullaniciId), "Rezervasyon onaylandı. Çakışan bekleyen talepler varsa otomatik reddedildi.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reddet(int id, string? sebep) => CalistirAsync(id, () => _rezervasyonlar.ReddetAsync(id, KullaniciId, sebep), "Talep reddedildi.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> IptalEt(int id) => CalistirAsync(id, () => _rezervasyonlar.IptalEtAsync(id, KullaniciId, personelMi: true), "Rezervasyon iptal edildi.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> OdemeEkle(int id, decimal tutar, OdemeYontemi yontem, OdemeTuru tur, string? not)
        => CalistirAsync(id, () => _rezervasyonlar.OdemeEkleAsync(id, KullaniciId, tutar, yontem, tur, not), $"{Bicim.Para(tutar)} tutarında ödeme kaydedildi.");

    // ---- Teslim ----
    public async Task<IActionResult> TeslimEt(int id)
    {
        var r = await DetayliSorgu().Include(x => x.Ekstralar).ThenInclude(e => e.Ekstra).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        return View(new TeslimFormu { RezervasyonId = id, TeslimKm = r.Arac!.Kilometre, Rezervasyon = r });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeslimEt(TeslimFormu form)
    {
        try
        {
            await _rezervasyonlar.TeslimEtAsync(form.RezervasyonId, KullaniciId, form.TeslimKm, form.TeslimYakit);
            Basarili("Araç müşteriye teslim edildi. İyi yolculuklar!");
            return RedirectToAction(nameof(Detay), new { id = form.RezervasyonId });
        }
        catch (IsKuraliHatasi hata)
        {
            ModelState.AddModelError("", hata.Message);
            form.Rezervasyon = await DetayliSorgu().AsNoTracking().FirstOrDefaultAsync(x => x.Id == form.RezervasyonId);
            if (form.Rezervasyon == null) return NotFound();
            return View(form);
        }
    }

    // ---- İade ----
    public async Task<IActionResult> IadeAl(int id)
    {
        var r = await DetayliSorgu().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        var simdi = DateTime.Now;
        return View(new IadeFormu
        {
            RezervasyonId = id, Rezervasyon = r,
            IadeKm = (r.TeslimKm ?? 0) + 150 * r.GunSayisi,
            IadeTarihi = new DateTime(simdi.Year, simdi.Month, simdi.Day, simdi.Hour, simdi.Minute, 0),
            IadeYakit = r.TeslimYakit ?? YakitSeviyesi.Dolu,
            TahminiGecikmeUcreti = _fiyat.GecikmeUcreti(r.GunlukFiyat, r.BitisTarihi, simdi),
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IadeAl(IadeFormu form)
    {
        try
        {
            var r = await _rezervasyonlar.IadeAlAsync(form.RezervasyonId, KullaniciId, form.IadeKm, form.IadeYakit, form.IadeTarihi, form.HasarAciklamasi, form.HasarTutari, form.Not);
            var ekUcret = r.GecikmeUcreti + r.YakitUcreti + r.HasarUcreti;
            Basarili(ekUcret > 0 ? $"İade alındı. Ek ücretler: {Bicim.Para(ekUcret)} — yeni toplam {Bicim.Para(r.ToplamTutar)}." : $"İade alındı. Toplam {Bicim.Para(r.ToplamTutar)}.");
            return RedirectToAction(nameof(Detay), new { id = form.RezervasyonId });
        }
        catch (IsKuraliHatasi hata)
        {
            ModelState.AddModelError("", hata.Message);
            form.Rezervasyon = await DetayliSorgu().AsNoTracking().FirstOrDefaultAsync(x => x.Id == form.RezervasyonId);
            if (form.Rezervasyon == null) return NotFound();
            return View(form);
        }
    }

    public async Task<IActionResult> Sozlesme(int id)
    {
        var pdf = await _sozlesme.OlusturAsync(id);
        return pdf == null ? NotFound() : PdfGoster(pdf, $"Sozlesme-{id}.pdf");
    }

    // ---- Yardımcılar ----
    private IQueryable<Rezervasyon> DetayliSorgu() => _vt.Rezervasyonlar
        .Include(r => r.Musteri).Include(r => r.Arac).ThenInclude(a => a!.Marka)
        .Include(r => r.AlisSube).Include(r => r.IadeSube);

    /// <summary>Servis işlemini çalıştırır; kural hatası olursa mesajı kırmızı bildirimle gösterir.</summary>
    private async Task<IActionResult> CalistirAsync(int id, Func<Task> islem, string basariMesaji)
    {
        try { await islem(); Basarili(basariMesaji); }
        catch (IsKuraliHatasi hata) { Hata(hata.Message); }
        return RedirectToAction(nameof(Detay), new { id });
    }
}

// =============================== ÖDEMELER ===============================
public class OdemelerController : YonetimController
{
    private readonly VeriTabani _vt;
    public OdemelerController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index(DateTime? baslangic, DateTime? bitis, OdemeYontemi? yontem, int sayfa = 1)
    {
        var bas = (baslangic ?? DateTime.Today.AddDays(-30)).Date;
        var son = (bitis ?? DateTime.Today).Date.AddDays(1);
        var sorgu = _vt.Odemeler.Include(o => o.Rezervasyon).ThenInclude(r => r!.Musteri).Include(o => o.AlanKullanici)
            .Where(o => o.Tarih >= bas && o.Tarih < son).AsNoTracking();
        if (yontem != null) sorgu = sorgu.Where(o => o.Yontem == yontem);

        var tutarlar = await sorgu.Select(o => new { o.Yontem, o.Tutar }).ToListAsync();
        ViewBag.Baslangic = bas; ViewBag.Bitis = son.AddDays(-1); ViewBag.Yontem = yontem;
        ViewBag.Toplam = tutarlar.Sum(x => x.Tutar);
        ViewBag.YontemToplamlari = tutarlar.GroupBy(x => x.Yontem).ToDictionary(g => g.Key, g => g.Sum(x => x.Tutar));
        return View(await SayfaListesi<Odeme>.OlusturAsync(sorgu.OrderByDescending(o => o.Tarih), sayfa, 25));
    }
}
