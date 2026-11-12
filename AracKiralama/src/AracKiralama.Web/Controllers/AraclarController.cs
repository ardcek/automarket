// ============================================================================
// ARAÇLAR (müşteri tarafı): liste + filtreler + detay
//   /Araclar?KategoriId=2&Siralama=yeni  → filtreli liste
//   /Araclar/Detay/5                      → araç detayı ve canlı fiyat
// Yeni filtre eklemek: FormModelleri/SiteModelleri.cs dosyasının başındaki tarife bakın.
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Controllers;

public class AraclarController : TemelController
{
    private readonly VeriTabani _vt;
    private readonly MusaitlikServisi _musaitlik;
    private readonly FiyatServisi _fiyat;
    private readonly SiteAyarlari _ayarlar;
    private readonly KampanyaServisi _kampanyalar;

    public AraclarController(VeriTabani vt, MusaitlikServisi musaitlik, FiyatServisi fiyat, SiteAyarlari ayarlar, KampanyaServisi kampanyalar)
    {
        _vt = vt;
        _musaitlik = musaitlik;
        _fiyat = fiyat;
        _ayarlar = ayarlar;
        _kampanyalar = kampanyalar;
    }

    public async Task<IActionResult> Index(AracFiltresi filtre)
    {
        // Panelden eklenmiş, yayındaki özellikler (filtre panelinde ve kartlarda kullanılır)
        // SQL: SELECT * FROM "AracOzellikleri" WHERE "Aktif" ORDER BY "Sira", "Ad"
        var ozellikler = await _vt.AracOzellikleri.Where(o => o.Aktif).OrderBy(o => o.Sira).ThenBy(o => o.Ad).AsNoTracking().ToListAsync();

        var sorgu = _vt.Araclar
            .Include(a => a.Marka).Include(a => a.Sube).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru)
            .Include(a => a.OzellikDegerleri).ThenInclude(d => d.Ozellik)
            .Where(a => a.Durum != AracDurumu.Pasif)
            .AsNoTracking();

        // ---- FİLTRELER: her biri tek satır. Yeni filtre = yeni bir "if" ----
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var aranan = filtre.Arama.Trim().ToUpper();
            sorgu = sorgu.Where(a => (a.Marka!.Ad + " " + a.Model).ToUpper().Contains(aranan));
        }
        if (filtre.AlisSubeId != null) sorgu = sorgu.Where(a => a.SubeId == filtre.AlisSubeId);
        if (filtre.KategoriId != null) sorgu = sorgu.Where(a => a.KategoriId == filtre.KategoriId);
        if (filtre.MarkaId != null) sorgu = sorgu.Where(a => a.MarkaId == filtre.MarkaId);
        if (filtre.VitesTuruId != null) sorgu = sorgu.Where(a => a.VitesTuruId == filtre.VitesTuruId);
        if (filtre.YakitTuruId != null) sorgu = sorgu.Where(a => a.YakitTuruId == filtre.YakitTuruId);
        if (filtre.EnAzKoltuk != null) sorgu = sorgu.Where(a => a.KoltukSayisi >= filtre.EnAzKoltuk);
        if (filtre.EnDusukFiyat != null) sorgu = sorgu.Where(a => a.GunlukFiyat >= filtre.EnDusukFiyat);
        if (filtre.EnYuksekFiyat != null) sorgu = sorgu.Where(a => a.GunlukFiyat <= filtre.EnYuksekFiyat);
        if (filtre.EnAzYil != null) sorgu = sorgu.Where(a => a.Yil >= filtre.EnAzYil);

        // ---- PANELDEN EKLENEN ÖZELLİK FİLTRELERİ (Yönetim → Filtreler ve Özellikler) ----
        // Her biri: "bu özellikte şu değere sahip bir satırı olan araçlar"
        // SQL: ... WHERE EXISTS (SELECT 1 FROM "AracOzellikDegerleri" d WHERE d."AracId" = a."Id" AND d."OzellikId" = 3 AND d."Deger" = 'Beyaz')
        var turler = ozellikler.ToDictionary(o => o.Id, o => o.Tur);
        foreach (var (ozellikId, ham) in filtre.Oz ?? new())
        {
            if (string.IsNullOrWhiteSpace(ham) || !turler.TryGetValue(ozellikId, out var tur)) continue;
            var deger = ham.Trim();
            if (tur == OzellikTuru.EvetHayir && deger == "false")
                sorgu = sorgu.Where(a => !a.OzellikDegerleri.Any(d => d.OzellikId == ozellikId && d.Deger == "true"));   // "Yok" seçildiyse
            else if (tur == OzellikTuru.Yazi)
            {
                var aranan = deger.ToUpper();
                sorgu = sorgu.Where(a => a.OzellikDegerleri.Any(d => d.OzellikId == ozellikId && d.Deger.ToUpper().Contains(aranan)));
            }
            else
                sorgu = sorgu.Where(a => a.OzellikDegerleri.Any(d => d.OzellikId == ozellikId && d.Deger == deger));
        }
        foreach (var (ozellikId, enAz) in filtre.OzEnAz ?? new())
            if (enAz != null) sorgu = sorgu.Where(a => a.OzellikDegerleri.Any(d => d.OzellikId == ozellikId && d.SayiDeger >= enAz));
        foreach (var (ozellikId, enCok) in filtre.OzEnCok ?? new())
            if (enCok != null) sorgu = sorgu.Where(a => a.OzellikDegerleri.Any(d => d.OzellikId == ozellikId && d.SayiDeger <= enCok));

        // Tarih seçildiyse sadece o tarihlerde boş olanlar
        if (filtre.TarihSecili) sorgu = _musaitlik.SadeceMusaitler(sorgu, filtre.Baslangic!.Value, filtre.Bitis!.Value);
        int? gunSayisi = filtre.TarihSecili ? _fiyat.GunSayisi(filtre.Baslangic!.Value, filtre.Bitis!.Value) : null;

        // ---- İNDİRİMLER (Yönetim → Kampanyalar) ----
        // Yayındaki kampanyalar + ziyaretçinin seçtiği "Mesleğiniz" gibi bilgiler (giriş yapmış müşteride profili)
        var kampanya = await _kampanyalar.BaglamAsync(User, filtre.Mu, filtre.Baslangic);
        if (filtre.SadeceIndirimli)
        {
            // Kampanya koşulları C# ile kontrol edilir: önce süzülmüş araçlar okunur, kampanyası olanların numaraları alınır.
            // (Araç sayısı az olduğu için bu yöntem hızlıdır ve kural tek yerde kalır: KampanyaServisi.)
            var indirimliler = (await sorgu.ToListAsync()).Where(a => kampanya.Durum(a, gunSayisi).IndirimliMi).Select(a => a.Id).ToList();
            // SQL: ... AND a."Id" IN (3, 7, 12 ...)
            sorgu = sorgu.Where(a => indirimliler.Contains(a.Id));
        }

        // ---- SIRALAMA: yeni sıralama = yeni bir satır (ve Ayarlar'daki seçenek listesine ekleyin) ----
        filtre.Siralama ??= _ayarlar.VarsayilanSiralama;
        sorgu = filtre.Siralama switch
        {
            "fiyat-azalan" => sorgu.OrderByDescending(a => a.GunlukFiyat),
            "yeni" => sorgu.OrderByDescending(a => a.Yil).ThenBy(a => a.GunlukFiyat),
            "km" => sorgu.OrderBy(a => a.Kilometre),
            "ad" => sorgu.OrderBy(a => a.Marka!.Ad).ThenBy(a => a.Model),
            _ => sorgu.OrderBy(a => a.GunlukFiyat),
        };

        var model = new AracListesiSayfasi
        {
            Filtre = filtre,
            Araclar = await SayfaListesi<Arac>.OlusturAsync(sorgu, filtre.Sayfa, _ayarlar.SayfaBasinaArac),
            GunSayisi = gunSayisi,
            Kategoriler = await _vt.Kategoriler.OrderBy(k => k.Sira).ToListAsync(),
            Markalar = await _vt.Markalar.OrderBy(m => m.Sira).ThenBy(m => m.Ad).ToListAsync(),
            Yakitlar = await _vt.YakitTurleri.OrderBy(y => y.Sira).ToListAsync(),
            Vitesler = await _vt.VitesTurleri.OrderBy(v => v.Sira).ToListAsync(),
            Subeler = await _vt.Subeler.OrderBy(s => s.Sehir).ToListAsync(),
            Ozellikler = ozellikler.Where(o => o.FiltredeGoster).ToList(),
            Kampanya = kampanya,
            KampanyaAlanlari = await _kampanyalar.FiltreAlanlariAsync(kampanya),
        };
        return View(model);
    }

    public async Task<IActionResult> Detay(int id, AracFiltresi filtre)
    {
        var arac = await _vt.Araclar
            .Include(a => a.Marka).Include(a => a.Sube).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru)
            .Include(a => a.OzellikDegerleri).ThenInclude(d => d.Ozellik)
            .AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.Durum != AracDurumu.Pasif);
        if (arac == null) return NotFound();

        var model = new AracDetaySayfasi
        {
            Arac = arac,
            Filtre = filtre,
            Ekstralar = await _vt.Ekstralar.Where(e => e.Aktif).OrderBy(e => e.GunlukFiyat).ToListAsync(),
            Subeler = await _vt.Subeler.OrderBy(s => s.Sehir).ToListAsync(),
            Benzerler = await _vt.Araclar.Include(a => a.Marka).Include(a => a.Sube).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru)
                .Include(a => a.OzellikDegerleri).ThenInclude(d => d.Ozellik)
                .Where(a => a.KategoriId == arac.KategoriId && a.Id != id && a.Durum != AracDurumu.Pasif)
                .OrderBy(a => a.GunlukFiyat).Take(4).AsNoTracking().ToListAsync(),
            Musait = filtre.TarihSecili ? await _musaitlik.MusaitMiAsync(id, filtre.Baslangic!.Value, filtre.Bitis!.Value) : null,
            // SQL: SELECT * FROM "AracOzellikleri" WHERE "Aktif" AND "DetaydaGoster" ORDER BY "Sira", "Ad"
            Ozellikler = await _vt.AracOzellikleri.Where(o => o.Aktif && o.DetaydaGoster).OrderBy(o => o.Sira).ThenBy(o => o.Ad).AsNoTracking().ToListAsync(),
        };
        // Kampanyalar: fiyat kutusunda "Mesleğiniz" seçimi ve benzer araç kartlarındaki rozetler için
        model.Kampanya = await _kampanyalar.BaglamAsync(User, filtre.Mu, filtre.Baslangic);
        model.KampanyaAlanlari = await _kampanyalar.FiltreAlanlariAsync(model.Kampanya);
        return View(model);
    }
}
