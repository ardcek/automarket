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

    public AraclarController(VeriTabani vt, MusaitlikServisi musaitlik, FiyatServisi fiyat, SiteAyarlari ayarlar)
    {
        _vt = vt;
        _musaitlik = musaitlik;
        _fiyat = fiyat;
        _ayarlar = ayarlar;
    }

    public async Task<IActionResult> Index(AracFiltresi filtre)
    {
        var sorgu = _vt.Araclar
            .Include(a => a.Marka).Include(a => a.Sube).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru)
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

        // Tarih seçildiyse sadece o tarihlerde boş olanlar
        if (filtre.TarihSecili) sorgu = _musaitlik.SadeceMusaitler(sorgu, filtre.Baslangic!.Value, filtre.Bitis!.Value);

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
            GunSayisi = filtre.TarihSecili ? _fiyat.GunSayisi(filtre.Baslangic!.Value, filtre.Bitis!.Value) : null,
            Kategoriler = await _vt.Kategoriler.OrderBy(k => k.Sira).ToListAsync(),
            Markalar = await _vt.Markalar.OrderBy(m => m.Sira).ThenBy(m => m.Ad).ToListAsync(),
            Yakitlar = await _vt.YakitTurleri.OrderBy(y => y.Sira).ToListAsync(),
            Vitesler = await _vt.VitesTurleri.OrderBy(v => v.Sira).ToListAsync(),
            Subeler = await _vt.Subeler.OrderBy(s => s.Sehir).ToListAsync(),
        };
        return View(model);
    }

    public async Task<IActionResult> Detay(int id, AracFiltresi filtre)
    {
        var arac = await _vt.Araclar
            .Include(a => a.Marka).Include(a => a.Sube).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru)
            .AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.Durum != AracDurumu.Pasif);
        if (arac == null) return NotFound();

        var model = new AracDetaySayfasi
        {
            Arac = arac,
            Filtre = filtre,
            Ekstralar = await _vt.Ekstralar.Where(e => e.Aktif).OrderBy(e => e.GunlukFiyat).ToListAsync(),
            Subeler = await _vt.Subeler.OrderBy(s => s.Sehir).ToListAsync(),
            Benzerler = await _vt.Araclar.Include(a => a.Marka).Include(a => a.Sube).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru)
                .Where(a => a.KategoriId == arac.KategoriId && a.Id != id && a.Durum != AracDurumu.Pasif)
                .OrderBy(a => a.GunlukFiyat).Take(4).AsNoTracking().ToListAsync(),
            Musait = filtre.TarihSecili ? await _musaitlik.MusaitMiAsync(id, filtre.Baslangic!.Value, filtre.Bitis!.Value) : null,
        };
        return View(model);
    }
}
