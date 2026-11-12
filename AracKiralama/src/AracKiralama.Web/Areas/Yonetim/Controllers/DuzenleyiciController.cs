// ============================================================================
// SİTE DÜZENLEYİCİ (Yönetim → Site Düzenleyici)
//   GET  /Yonetim/Duzenleyici?sayfa=anasayfa  → solda kutular, sağda canlı önizleme
//   POST /Yonetim/Duzenleyici/Taslak          → yazılanları taslak olarak sakla (önizleme için, siteyi değiştirmez)
//   POST /Yonetim/Duzenleyici/Kaydet          → taslağı gerçek siteye yaz (Metinler + SiteAyarlari + Sayfalar tabloları)
//   POST /Yonetim/Duzenleyici/Vazgec          → taslağı sil
// Hangi sekmede hangi yazı / ayar olduğu: Yardimcilar/DuzenleyiciTanimlari.cs
// ============================================================================
using System.Reflection;
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

public class DuzenleyiciController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly IcerikServisi _icerik;
    private readonly TaslakServisi _taslaklar;

    public DuzenleyiciController(VeriTabani vt, IcerikServisi icerik, TaslakServisi taslaklar)
    {
        _vt = vt;
        _icerik = icerik;
        _taslaklar = taslaklar;
    }

    public async Task<IActionResult> Index(string? sayfa)
    {
        var sekme = DuzenleyiciTanimlari.Bul(sayfa);
        _taslaklar.Sil(KullaniciId);   // sayfa her açılışta temiz başlar: önizleme kayıtlı hali gösterir

        // Kayıtlı değerler: SELECT * FROM "SiteAyarlari" LIMIT 1  ve  SELECT * FROM "Metinler"
        var ayarlar = await _vt.SiteAyarlari.AsNoTracking().FirstAsync();
        var kayitliMetinler = await _vt.Metinler.AsNoTracking().ToDictionaryAsync(m => m.Anahtar, m => m.Deger);
        var fabrikaAyarlari = new SiteAyarlari();   // "varsayılana dön" için ilk değerler

        var alanlar = new List<DuzenleyiciAlani>();

        // 1) Bu sekmenin yazıları
        foreach (var anahtar in sekme.Metinler.Distinct())
        {
            if (!VarsayilanMetinler.Liste.TryGetValue(anahtar, out var varsayilan)) continue;
            var (bolum, ad) = DuzenleyiciTanimlari.Aciklama(anahtar);
            alanlar.Add(new DuzenleyiciAlani
            {
                Anahtar = anahtar, Bolum = bolum, Etiket = ad, Varsayilan = varsayilan,
                Deger = kayitliMetinler.GetValueOrDefault(anahtar, varsayilan),
            });
        }

        // 2) Bu sekmenin ayarları (SiteAyarlari'nda [Ayar] etiketi olup grubu bu sekmede olanlar)
        foreach (var ozellik in typeof(SiteAyarlari).GetProperties())
        {
            var bilgi = ozellik.GetCustomAttribute<AyarAttribute>();
            if (bilgi == null || !sekme.AyarGruplari.Contains(bilgi.Grup)) continue;
            alanlar.Add(new DuzenleyiciAlani
            {
                AyarMi = true, Anahtar = ozellik.Name, Bolum = "Ayarlar · " + bilgi.Grup, Etiket = bilgi.Ad, Aciklama = bilgi.Aciklama,
                AyarTuru = ozellik.PropertyType, AyarBilgisi = bilgi,
                Deger = AyarCevirici.Yazi(ozellik.GetValue(ayarlar)),
                Varsayilan = AyarCevirici.Yazi(ozellik.GetValue(fabrikaAyarlari)),
            });
        }

        // Araç detayı önizlemesi için ilk aracın numarası: SELECT "Id" FROM "Araclar" WHERE "Durum" <> 'Pasif' ORDER BY "Id" LIMIT 1
        var ilkArac = await _vt.Araclar.Where(a => a.Durum != AracDurumu.Pasif).OrderBy(a => a.Id).Select(a => a.Id).FirstOrDefaultAsync();

        return View(new DuzenleyiciModeli
        {
            Sekme = sekme,
            Alanlar = alanlar,
            KurumsalSayfalar = sekme.KurumsalSayfalar ? await _vt.Sayfalar.OrderBy(s => s.Sira).AsNoTracking().ToListAsync() : new(),
            Onizlemeler = sekme.Onizlemeler.Select(o => (o.Ad, o.Adres.Replace("{arac}", ilkArac.ToString()))).ToList(),
        });
    }

    /// <summary>Kutulara yazılanları taslak olarak saklar; sadece önizleme bunları görür.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Taslak()
    {
        _taslaklar.Kaydet(KullaniciId, FormdanTaslak());
        return NoContent();   // 204: "aldım, gösterilecek sayfa yok"
    }

    /// <summary>Formdaki her şeyi gerçek siteye kaydeder.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(string? sayfa)
    {
        var taslak = FormdanTaslak();

        // 1) Yazılar → Metinler tablosu. Varsayılanla aynı ya da boş bırakılan yazının kaydı silinir.
        var kayitlilar = await _vt.Metinler.ToDictionaryAsync(m => m.Anahtar);
        foreach (var (anahtar, yeniDeger) in taslak.Metinler)
        {
            if (!VarsayilanMetinler.Liste.TryGetValue(anahtar, out var varsayilan)) continue;
            var deger = yeniDeger.Trim();
            var degisti = deger.Length > 0 && deger != varsayilan;
            kayitlilar.TryGetValue(anahtar, out var kayit);

            if (!degisti && kayit != null) _vt.Metinler.Remove(kayit);                                  // DELETE
            else if (degisti && kayit == null) _vt.Metinler.Add(new Metin { Anahtar = anahtar, Deger = deger });   // INSERT
            else if (degisti && kayit != null) kayit.Deger = deger;                                        // UPDATE
        }

        // 2) Ayarlar → SiteAyarlari tablosundaki tek satır
        var ayarlar = await _vt.SiteAyarlari.FirstAsync();
        var hatalilar = new List<string>();
        foreach (var (ad, deger) in taslak.Ayarlar)
            if (!AyarCevirici.Uygula(ayarlar, ad, deger)) hatalilar.Add(AyarCevirici.AyarOzelligi(ad)?.GetCustomAttribute<AyarAttribute>()?.Ad ?? ad);

        // 3) Kurumsal sayfaların aç / kapa düğmeleri → Sayfalar tablosundaki "Aktif" sütunu
        foreach (var anahtar in Request.Form.Keys.Where(k => k.StartsWith("sayfaAktif[") && k.EndsWith(']')))
        {
            if (!int.TryParse(anahtar["sayfaAktif[".Length..^1], out var sayfaId)) continue;
            var kurumsal = await _vt.Sayfalar.FindAsync(sayfaId);
            if (kurumsal != null) kurumsal.Aktif = Request.Form[anahtar].FirstOrDefault() == "true";
        }

        await _vt.SaveChangesAsync();   // hepsi tek seferde veritabanına yazılır
        _icerik.Yenile();               // bellekteki kopya tazelenir → değişiklik sitede hemen görünür
        _taslaklar.Sil(KullaniciId);

        if (hatalilar.Count > 0) Hata("Şu ayarlar geçersiz olduğu için kaydedilmedi: " + string.Join(", ", hatalilar) + ". Diğerleri kaydedildi.");
        else Basarili("Kaydedildi. Değişiklikler sitede şimdi geçerli.");
        return RedirectToAction(nameof(Index), new { sayfa });
    }

    /// <summary>Kaydedilmemiş değişiklikleri atar.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Vazgec(string? sayfa)
    {
        _taslaklar.Sil(KullaniciId);
        Basarili("Kaydedilmemiş değişiklikler geri alındı.");
        return RedirectToAction(nameof(Index), new { sayfa });
    }

    /// <summary>Formdaki "metin[...]" ve "ayar[...]" kutularını taslağa çevirir.</summary>
    private Taslak FormdanTaslak()
    {
        var taslak = new Taslak();
        foreach (var (ad, degerler) in Request.Form)
        {
            // Aç/kapa düğmesinde iki değer gelir: işaretliyse "true" (düğme), ardından gizli kutudaki "false".
            // İlk değer geçerlidir.
            var deger = degerler.FirstOrDefault() ?? "";
            if (ad.StartsWith("metin[") && ad.EndsWith(']')) taslak.Metinler[ad[6..^1]] = deger;
            else if (ad.StartsWith("ayar[") && ad.EndsWith(']')) taslak.Ayarlar[ad[5..^1]] = deger;
        }
        return taslak;
    }
}
