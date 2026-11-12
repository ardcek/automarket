// ============================================================================
// FİLTRELER VE ÖZELLİKLER (Yönetim → Filtreler ve Özellikler)
// Koda dokunmadan araç listesine yeni filtre eklemek için.
//   /Yonetim/Ozellikler             → hazır filtrelerin aç/kapa düğmeleri + panelden eklenen özellikler
//   /Yonetim/Ozellikler/Ekle        → yeni özellik (örn. "Bagaj hacmi", "Klima", "Renk")
//   /Yonetim/Ozellikler/Duzenle/5   → özelliği değiştir
//   /Yonetim/Ozellikler/Degerler/5  → bütün araçlara bu özelliğin değerini tek ekranda gir
//   POST /Yonetim/Ozellikler/Sil/5  → özelliği ve araçlardaki değerlerini sil
// Kullandığı tablolar: AracOzellikleri, AracOzellikDegerleri, Araclar, SiteAyarlari
// ============================================================================
using System.Globalization;
using System.Reflection;
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

public class OzelliklerController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly IcerikServisi _icerik;

    public OzelliklerController(VeriTabani vt, IcerikServisi icerik)
    {
        _vt = vt;
        _icerik = icerik;
    }

    public async Task<IActionResult> Index()
    {
        // 1) Hazır filtreler: SiteAyarlari'nda "Araç Listesi" grubundaki aç/kapa ayarları (Filtre: Kategori, Marka...)
        var ayarlar = await _vt.SiteAyarlari.AsNoTracking().FirstAsync();
        ViewBag.HazirFiltreler = typeof(SiteAyarlari).GetProperties()
            .Select(o => (Ozellik: o, Bilgi: o.GetCustomAttribute<AyarAttribute>()))
            .Where(x => x.Bilgi?.Grup == "Araç Listesi" && x.Ozellik.PropertyType == typeof(bool))
            .Select(x => new DuzenleyiciAlani { AyarMi = true, Anahtar = x.Ozellik.Name, Etiket = x.Bilgi!.Ad, Deger = AyarCevirici.Yazi(x.Ozellik.GetValue(ayarlar)) })
            .ToList();

        // 2) Her özellik kaç araçta doldurulmuş?
        // SQL: SELECT "OzellikId", COUNT(*) FROM "AracOzellikDegerleri" GROUP BY "OzellikId"
        ViewBag.DegerSayilari = await _vt.AracOzellikDegerleri.GroupBy(d => d.OzellikId)
            .Select(g => new { g.Key, Sayi = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Sayi);
        ViewBag.AracSayisi = await _vt.Araclar.CountAsync(a => a.Durum != AracDurumu.Pasif);

        // 3) Panelden eklenen özellikler: SELECT * FROM "AracOzellikleri" ORDER BY "Sira", "Ad"
        return View(await _vt.AracOzellikleri.OrderBy(o => o.Sira).ThenBy(o => o.Ad).AsNoTracking().ToListAsync());
    }

    /// <summary>Hazır filtrelerin aç / kapa düğmelerini kaydeder.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HazirFiltreler()
    {
        var ayarlar = await _vt.SiteAyarlari.FirstAsync();
        foreach (var (ad, degerler) in Request.Form)
            if (ad.StartsWith("ayar[") && ad.EndsWith(']')) AyarCevirici.Uygula(ayarlar, ad[5..^1], degerler.FirstOrDefault());   // ilk değer: işaretliyse "true"
        await _vt.SaveChangesAsync();
        _icerik.Yenile();
        Basarili("Hazır filtreler kaydedildi. Araç listesinde hemen geçerli.");
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Ekle() => View("Form", new OzellikFormu());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(OzellikFormu form)
    {
        Denetle(form);
        if (!ModelState.IsValid) return View("Form", form);

        var ozellik = new AracOzelligi();
        form.Aktar(ozellik);
        _vt.AracOzellikleri.Add(ozellik);   // INSERT INTO "AracOzellikleri" ...
        await _vt.SaveChangesAsync();
        Basarili($"\"{ozellik.Ad}\" eklendi. Şimdi araçlara değerlerini gir; boş bırakılan araç bu filtrede çıkmaz.");
        return RedirectToAction(nameof(Degerler), new { id = ozellik.Id });
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var ozellik = await _vt.AracOzellikleri.FindAsync(id);
        return ozellik == null ? NotFound() : View("Form", OzellikFormu.Olustur(ozellik));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, OzellikFormu form)
    {
        var ozellik = await _vt.AracOzellikleri.FindAsync(id);
        if (ozellik == null) return NotFound();
        Denetle(form);
        if (!ModelState.IsValid) return View("Form", form);

        form.Aktar(ozellik);   // UPDATE "AracOzellikleri" SET ... WHERE "Id" = id
        await _vt.SaveChangesAsync();
        Basarili($"\"{ozellik.Ad}\" güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        // Bir kampanyanın koşuluysa silinmez (yoksa kampanya bütün araçlara uygulanmaya başlardı)
        if (await _vt.Kampanyalar.AnyAsync(k => k.AracOzellikId == id))
        {
            Hata("Bu özellik bir kampanyada koşul olarak kullanılıyor. Önce Yönetim → Kampanyalar'dan kampanyayı değiştirin ya da silin.");
            return RedirectToAction(nameof(Index));
        }
        // Önce araçlardaki değerleri, sonra özelliğin kendisini sil
        await _vt.AracOzellikDegerleri.Where(d => d.OzellikId == id).ExecuteDeleteAsync();   // DELETE FROM "AracOzellikDegerleri" WHERE "OzellikId" = id
        await _vt.AracOzellikleri.Where(o => o.Id == id).ExecuteDeleteAsync();              // DELETE FROM "AracOzellikleri" WHERE "Id" = id
        Basarili("Özellik ve araçlardaki değerleri silindi.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Bütün araçların bu özellikteki değerlerini tek ekranda gösterir.</summary>
    public async Task<IActionResult> Degerler(int id)
    {
        var ozellik = await _vt.AracOzellikleri.FindAsync(id);
        if (ozellik == null) return NotFound();
        return View(new OzellikDegerleriSayfasi
        {
            Ozellik = ozellik,
            Araclar = await _vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori)
                .Where(a => a.Durum != AracDurumu.Pasif).OrderBy(a => a.Marka!.Ad).ThenBy(a => a.Model).AsNoTracking().ToListAsync(),
            Degerler = await _vt.AracOzellikDegerleri.Where(d => d.OzellikId == id).ToDictionaryAsync(d => d.AracId, d => d.Deger),
        });
    }

    /// <summary>Tablodaki her aracın değerini kaydeder (boş bırakılanın değeri silinir).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Degerler(int id, IFormCollection form)
    {
        var ozellik = await _vt.AracOzellikleri.FindAsync(id);
        if (ozellik == null) return NotFound();

        var mevcut = await _vt.AracOzellikDegerleri.Where(d => d.OzellikId == id).ToDictionaryAsync(d => d.AracId);
        var aracIdleri = await _vt.Araclar.Select(a => a.Id).ToListAsync();
        var sayi = 0;
        foreach (var aracId in aracIdleri)
        {
            if (!form.TryGetValue($"deger[{aracId}]", out var ham)) continue;   // tabloda olmayan araca dokunma
            var temiz = AracOzellikDegeri.Temizle(ozellik, ham.FirstOrDefault());
            mevcut.TryGetValue(aracId, out var kayit);

            if (temiz == null)
            {
                if (kayit != null) _vt.AracOzellikDegerleri.Remove(kayit);   // DELETE
                continue;
            }
            if (kayit == null)
            {
                kayit = new AracOzellikDegeri { AracId = aracId, OzellikId = id };
                _vt.AracOzellikDegerleri.Add(kayit);                          // INSERT
            }
            kayit.Deger = temiz;                                              // UPDATE
            kayit.SayiDeger = ozellik.Tur == OzellikTuru.Sayi ? decimal.Parse(temiz, CultureInfo.InvariantCulture) : null;
            sayi++;
        }
        await _vt.SaveChangesAsync();
        Basarili($"{sayi} aracın \"{ozellik.Ad}\" değeri kaydedildi. Araç listesinde filtreyi deneyebilirsin.");
        return RedirectToAction(nameof(Degerler), new { id });
    }

    /// <summary>Formun kurallarını kontrol eder (Seçim listesinde en az bir seçenek olmalı).</summary>
    private void Denetle(OzellikFormu form)
    {
        if (form.Tur == OzellikTuru.Secim && form.SecenekSatirlari().Count == 0)
            ModelState.AddModelError(nameof(form.SeceneklerMetni), "Seçim listesi için en az bir seçenek yazın (her satıra bir tane).");
    }
}
