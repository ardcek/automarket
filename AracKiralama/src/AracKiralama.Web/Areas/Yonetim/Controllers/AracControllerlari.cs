// ============================================================================
// ARAÇ VE BAKIM YÖNETİMİ
//   /Yonetim/Araclar   → filo listesi, ekle, düzenle, detay (kiralama + bakım geçmişi), sil
//   /Yonetim/Bakim     → bakım kayıtları (bakım tarihlerinde araç kiralanamaz)
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

// =============================== ARAÇLAR ===============================
public class AraclarController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly GorselServisi _gorseller;

    public AraclarController(VeriTabani vt, GorselServisi gorseller)
    {
        _vt = vt;
        _gorseller = gorseller;
    }

    public async Task<IActionResult> Index(string? arama, int? kategoriId, AracDurumu? durum, int? subeId, int sayfa = 1)
    {
        var sorgu = _vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori).Include(a => a.Sube).Include(a => a.YakitTuru).Include(a => a.VitesTuru).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(arama))
        {
            var aranan = arama.Trim().ToUpper();
            sorgu = sorgu.Where(a => a.Plaka.Contains(aranan) || (a.Marka!.Ad + " " + a.Model).ToUpper().Contains(aranan));
        }
        if (kategoriId != null) sorgu = sorgu.Where(a => a.KategoriId == kategoriId);
        if (durum != null) sorgu = sorgu.Where(a => a.Durum == durum);
        if (subeId != null) sorgu = sorgu.Where(a => a.SubeId == subeId);

        // Filtre seçimleri view'da kutuları dolu göstermek için ViewBag ile taşınır.
        ViewBag.Arama = arama; ViewBag.KategoriId = kategoriId; ViewBag.Durum = durum; ViewBag.SubeId = subeId;
        ViewBag.Kategoriler = await _vt.Kategoriler.OrderBy(k => k.Sira).ToListAsync();
        ViewBag.Subeler = await _vt.Subeler.OrderBy(s => s.Ad).ToListAsync();
        return View(await SayfaListesi<Arac>.OlusturAsync(sorgu.OrderBy(a => a.Marka!.Ad).ThenBy(a => a.Model), sayfa, 15));
    }

    public async Task<IActionResult> Detay(int id)
    {
        var arac = await _vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori).Include(a => a.Sube).Include(a => a.YakitTuru).Include(a => a.VitesTuru)
            .Include(a => a.Bakimlar).AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (arac == null) return NotFound();

        ViewBag.SonKiralamalar = await _vt.Rezervasyonlar.Include(r => r.Musteri).Where(r => r.AracId == id)
            .OrderByDescending(r => r.BaslangicTarihi).Take(15).AsNoTracking().ToListAsync();
        ViewBag.Gelir = (await _vt.Rezervasyonlar.Where(r => r.AracId == id && r.Durum == RezervasyonDurumu.Tamamlandi).Select(r => r.ToplamTutar).ToListAsync()).Sum();
        return View(arac);
    }

    public async Task<IActionResult> Ekle()
    {
        await ListeleriDoldurAsync();
        return View("Form", new AracFormu());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(AracFormu form)
    {
        var arac = new Arac();
        if (!await KaydetmeyiDeneAsync(form, arac))
        {
            await ListeleriDoldurAsync();
            return View("Form", form);
        }
        _vt.Araclar.Add(arac);
        await _vt.SaveChangesAsync();
        Basarili($"{arac.Plaka} plakalı araç eklendi.");
        return RedirectToAction(nameof(Detay), new { id = arac.Id });
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var a = await _vt.Araclar.FindAsync(id);
        if (a == null) return NotFound();
        await ListeleriDoldurAsync();

        // Araç → form (alan alan kopyalama)
        return View("Form", new AracFormu
        {
            Id = a.Id, Plaka = a.Plaka, MarkaId = a.MarkaId, Model = a.Model, Yil = a.Yil, KategoriId = a.KategoriId,
            YakitTuruId = a.YakitTuruId, VitesTuruId = a.VitesTuruId, KoltukSayisi = a.KoltukSayisi, Renk = a.Renk,
            Kilometre = a.Kilometre, GunlukFiyat = a.GunlukFiyat, Depozito = a.Depozito, Durum = a.Durum, SubeId = a.SubeId,
            OneCikan = a.OneCikan, Aciklama = a.Aciklama, GorselAdresi = a.GorselAdresi,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, AracFormu form)
    {
        var arac = await _vt.Araclar.FindAsync(id);
        if (arac == null) return NotFound();
        if (!await KaydetmeyiDeneAsync(form, arac))
        {
            await ListeleriDoldurAsync();
            return View("Form", form);
        }
        await _vt.SaveChangesAsync();
        Basarili("Araç bilgileri güncellendi.");
        return RedirectToAction(nameof(Detay), new { id });
    }

    /// <summary>Kiralama geçmişi olan araç silinmez (raporlar bozulmasın) → "Pasif" yapılır.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var arac = await _vt.Araclar.FindAsync(id);
        if (arac == null) return NotFound();

        if (await _vt.Rezervasyonlar.AnyAsync(r => r.AracId == id && (r.Durum == RezervasyonDurumu.Onaylandi || r.Durum == RezervasyonDurumu.Kirada)))
        {
            Hata("Aracın onaylı veya devam eden kiralaması var; önce bunları sonuçlandırın.");
            return RedirectToAction(nameof(Detay), new { id });
        }
        if (await _vt.Rezervasyonlar.AnyAsync(r => r.AracId == id))
        {
            arac.Durum = AracDurumu.Pasif;
            Basarili("Aracın kiralama geçmişi olduğu için silinmedi, pasif yapıldı (sitede görünmez).");
        }
        else
        {
            _vt.Araclar.Remove(arac);
            Basarili("Araç silindi.");
        }
        await _vt.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Formu kontrol eder, görseli yükler ve form → araç kopyalamasını yapar. Hata varsa false döner.</summary>
    private async Task<bool> KaydetmeyiDeneAsync(AracFormu form, Arac arac)
    {
        form.Plaka = Plaka.Duzenle(form.Plaka);
        if (await _vt.Araclar.AnyAsync(a => a.Plaka == form.Plaka && a.Id != arac.Id))
            ModelState.AddModelError(nameof(form.Plaka), "Bu plaka ile kayıtlı başka bir araç var.");
        if (!ModelState.IsValid) return false;

        if (form.Gorsel != null)
        {
            try { form.GorselAdresi = await _gorseller.KaydetAsync(form.Gorsel); }
            catch (IsKuraliHatasi hata) { ModelState.AddModelError(nameof(form.Gorsel), hata.Message); return false; }
        }

        arac.Plaka = form.Plaka; arac.MarkaId = form.MarkaId; arac.Model = form.Model.Trim(); arac.Yil = form.Yil;
        arac.KategoriId = form.KategoriId; arac.YakitTuruId = form.YakitTuruId; arac.VitesTuruId = form.VitesTuruId;
        arac.KoltukSayisi = form.KoltukSayisi; arac.Renk = form.Renk.Trim(); arac.Kilometre = form.Kilometre;
        arac.GunlukFiyat = form.GunlukFiyat; arac.Durum = form.Durum; arac.SubeId = form.SubeId;
        // Depozito boş bırakıldıysa kategorinin varsayılan depozitosu (Yönetim → Kategoriler) kullanılır.
        arac.Depozito = form.Depozito ?? (await _vt.Kategoriler.FindAsync(form.KategoriId))?.VarsayilanDepozito ?? 0;
        arac.OneCikan = form.OneCikan; arac.Aciklama = form.Aciklama; arac.GorselAdresi = form.GorselAdresi;
        return true;
    }

    /// <summary>Formdaki açılır listeler (marka, kategori, yakıt, vites, şube).</summary>
    private async Task ListeleriDoldurAsync()
    {
        ViewBag.Markalar = new SelectList(await _vt.Markalar.OrderBy(m => m.Ad).ToListAsync(), "Id", "Ad");
        ViewBag.Kategoriler = new SelectList(await _vt.Kategoriler.OrderBy(k => k.Sira).ToListAsync(), "Id", "Ad");
        ViewBag.Yakitlar = new SelectList(await _vt.YakitTurleri.OrderBy(y => y.Sira).ToListAsync(), "Id", "Ad");
        ViewBag.Vitesler = new SelectList(await _vt.VitesTurleri.OrderBy(v => v.Sira).ToListAsync(), "Id", "Ad");
        ViewBag.Subeler = new SelectList(await _vt.Subeler.OrderBy(s => s.Ad).ToListAsync(), "Id", "Ad");
    }
}

// =============================== BAKIM ===============================
public class BakimController : YonetimController
{
    private readonly VeriTabani _vt;
    public BakimController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index(bool sadeceAcik = false)
    {
        var sorgu = _vt.Bakimlar.Include(b => b.Arac).ThenInclude(a => a!.Marka).AsNoTracking();
        if (sadeceAcik) sorgu = sorgu.Where(b => !b.Tamamlandi);
        ViewBag.SadeceAcik = sadeceAcik;
        ViewBag.ToplamMaliyet = (await _vt.Bakimlar.Select(b => b.Maliyet).ToListAsync()).Sum();
        return View(await sorgu.OrderBy(b => b.Tamamlandi).ThenByDescending(b => b.BaslangicTarihi).Take(200).ToListAsync());
    }

    public async Task<IActionResult> Ekle(int? aracId)
    {
        var form = new BakimFormu { AracId = aracId ?? 0 };
        if (aracId != null) form.Kilometre = (await _vt.Araclar.FindAsync(aracId))?.Kilometre ?? 0;
        form.Araclar = await AracListesiAsync();
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(BakimFormu form)
    {
        if (form.BitisTarihi <= form.BaslangicTarihi) ModelState.AddModelError(nameof(form.BitisTarihi), "Bitiş tarihi başlangıçtan sonra olmalıdır.");

        // Bakım, onaylı / devam eden bir kiralamayla çakışmasın
        var cakisan = await _vt.Rezervasyonlar.Where(r => r.AracId == form.AracId && MusaitlikServisi.KilitleyenDurumlar.Contains(r.Durum)
            && r.BaslangicTarihi < form.BitisTarihi && form.BaslangicTarihi < r.BitisTarihi).Select(r => r.Kod).FirstOrDefaultAsync();
        if (cakisan != null) ModelState.AddModelError("", $"Bu tarihlerde aracın {cakisan} kodlu kiralaması var.");

        if (!ModelState.IsValid) { form.Araclar = await AracListesiAsync(); return View(form); }

        _vt.Bakimlar.Add(new Bakim { AracId = form.AracId, Tur = form.Tur, BaslangicTarihi = form.BaslangicTarihi, BitisTarihi = form.BitisTarihi, Kilometre = form.Kilometre, Maliyet = form.Maliyet, Aciklama = form.Aciklama });

        // Bakım şu an başlıyorsa aracın durumu "Bakımda" olsun
        var arac = await _vt.Araclar.FindAsync(form.AracId);
        if (arac != null && arac.Durum == AracDurumu.Musait && form.BaslangicTarihi <= DateTime.Now && DateTime.Now < form.BitisTarihi)
            arac.Durum = AracDurumu.Bakimda;

        await _vt.SaveChangesAsync();
        Basarili("Bakım kaydı oluşturuldu. Bu tarihlerde araç kiralamaya kapalı.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tamamla(int id)
    {
        var bakim = await _vt.Bakimlar.Include(b => b.Arac).FirstOrDefaultAsync(b => b.Id == id);
        if (bakim == null) return NotFound();
        bakim.Tamamlandi = true;
        if (bakim.BitisTarihi > DateTime.Now) bakim.BitisTarihi = DateTime.Now;
        if (bakim.Arac!.Durum == AracDurumu.Bakimda) bakim.Arac.Durum = AracDurumu.Musait;
        await _vt.SaveChangesAsync();
        Basarili($"{bakim.Arac.Plaka} bakımı tamamlandı, araç tekrar kiralamaya açık.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var bakim = await _vt.Bakimlar.Include(b => b.Arac).FirstOrDefaultAsync(b => b.Id == id);
        if (bakim == null) return NotFound();
        if (!bakim.Tamamlandi && bakim.Arac!.Durum == AracDurumu.Bakimda) bakim.Arac.Durum = AracDurumu.Musait;
        _vt.Bakimlar.Remove(bakim);
        await _vt.SaveChangesAsync();
        Basarili("Bakım kaydı silindi.");
        return RedirectToAction(nameof(Index));
    }

    private Task<List<Arac>> AracListesiAsync() => _vt.Araclar.Include(a => a.Marka).Where(a => a.Durum != AracDurumu.Pasif)
        .OrderBy(a => a.Marka!.Ad).ThenBy(a => a.Model).AsNoTracking().ToListAsync();
}
