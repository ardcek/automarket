// ============================================================================
// KAMPANYALAR ve MÜŞTERİ BİLGİLERİ (koda dokunmadan indirim tanımlamak için)
//   /Yonetim/Kampanyalar              → indirim kuralları (Öğretmen %15, SUV %10 ...)
//   /Yonetim/Kampanyalar/Ekle         → yeni kampanya
//   /Yonetim/MusteriAlanlari          → müşterilere sorulan ek bilgiler (Meslek, Öğrenci mi? ...)
//   /Yonetim/MusteriAlanlari/Ekle     → yeni bilgi
//
// Örnek ("meslek gruplarına göre indirim yapın, filtrede de görünsün"):
//   1) Müşteri Bilgileri → "Meslek" ekle (seçenekler: Öğrenci, Öğretmen, Doktor ...)
//   2) Kampanyalar → "Öğretmen indirimi": Müşteri bilgisi = Meslek: Öğretmen, %15
//   3) Araç listesinde solda "İndirimler" bölümü çıkar: Mesleğiniz + "Sadece indirimli araçlar"
// Kuralın nasıl uygulandığı: Servisler/KampanyaServisi.cs
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

// =============================== KAMPANYALAR ===============================
public class KampanyalarController : YonetimController
{
    private readonly VeriTabani _vt;
    public KampanyalarController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index()
    {
        // Koşul özetinde adlar görünsün diye ilişkili tablolar da okunur (JOIN)
        // SQL: SELECT * FROM "Kampanyalar" LEFT JOIN "MusteriAlanlari" ... ORDER BY "Sira", "Id"
        var kampanyalar = await _vt.Kampanyalar.Include(k => k.MusteriAlani).Include(k => k.Kategori).Include(k => k.Marka).Include(k => k.AracOzelligi)
            .OrderBy(k => k.Sira).ThenBy(k => k.Id).AsNoTracking().ToListAsync();
        ViewBag.AlanSayisi = await _vt.MusteriAlanlari.CountAsync();
        return View(kampanyalar);
    }

    public async Task<IActionResult> Ekle()
    {
        await SecenekleriDoldurAsync(null);
        return View("Form", new KampanyaFormu());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(KampanyaFormu form)
    {
        Denetle(form);
        if (!ModelState.IsValid) { await SecenekleriDoldurAsync(form); return View("Form", form); }

        var kampanya = new Kampanya();
        form.Aktar(kampanya);
        _vt.Kampanyalar.Add(kampanya);   // INSERT INTO "Kampanyalar" ...
        await _vt.SaveChangesAsync();
        Basarili($"\"{kampanya.Ad}\" eklendi. Araç listesinde indirim rozetleri ve \"İndirimler\" filtresi görünür.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var kampanya = await _vt.Kampanyalar.FindAsync(id);
        if (kampanya == null) return NotFound();
        var form = KampanyaFormu.Olustur(kampanya);
        await SecenekleriDoldurAsync(form);
        return View("Form", form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, KampanyaFormu form)
    {
        var kampanya = await _vt.Kampanyalar.FindAsync(id);
        if (kampanya == null) return NotFound();
        Denetle(form);
        if (!ModelState.IsValid) { await SecenekleriDoldurAsync(form); return View("Form", form); }

        form.Aktar(kampanya);   // UPDATE "Kampanyalar" SET ... WHERE "Id" = id
        await _vt.SaveChangesAsync();
        Basarili($"\"{kampanya.Ad}\" güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Yayına al / yayından kaldır (tek tık).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcKapat(int id)
    {
        var kampanya = await _vt.Kampanyalar.FindAsync(id);
        if (kampanya == null) return NotFound();
        kampanya.Aktif = !kampanya.Aktif;   // UPDATE "Kampanyalar" SET "Aktif" = ... WHERE "Id" = id
        await _vt.SaveChangesAsync();
        Basarili(kampanya.Aktif ? $"\"{kampanya.Ad}\" yayında." : $"\"{kampanya.Ad}\" yayından kaldırıldı.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        // Geçmiş rezervasyonlar etkilenmez: indirim tutarı rezervasyona kopyalanmıştı.
        await _vt.Kampanyalar.Where(k => k.Id == id).ExecuteDeleteAsync();   // DELETE FROM "Kampanyalar" WHERE "Id" = id
        Basarili("Kampanya silindi.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Formdaki kuralları kontrol eder.</summary>
    private void Denetle(KampanyaFormu form)
    {
        if (form.BaslangicTarihi != null && form.BitisTarihi != null && form.BitisTarihi < form.BaslangicTarihi)
            ModelState.AddModelError(nameof(form.BitisTarihi), "Bitiş tarihi başlangıçtan önce olamaz.");
        if (!string.IsNullOrEmpty(form.MusteriKosulu) && KampanyaFormu.Ayir(form.MusteriKosulu).Id == null)
            ModelState.AddModelError(nameof(form.MusteriKosulu), "Geçerli bir müşteri bilgisi seçin.");
        if (!string.IsNullOrEmpty(form.AracKosulu) && KampanyaFormu.Ayir(form.AracKosulu).Id == null)
            ModelState.AddModelError(nameof(form.AracKosulu), "Geçerli bir araç özelliği seçin.");
    }

    /// <summary>
    /// Formdaki açılır listelerin seçenekleri. Müşteri bilgisi ve araç özelliği koşulları "numara|değer" biçimindedir:
    /// "Meslek: Öğretmen" → "3|Öğretmen". Sadece seçim listesi ve var/yok türleri koşul olabilir.
    /// </summary>
    private async Task SecenekleriDoldurAsync(KampanyaFormu? form)
    {
        var musteriSecenekleri = new List<(string Deger, string Yazi)>();
        foreach (var alan in await _vt.MusteriAlanlari.OrderBy(a => a.Sira).ThenBy(a => a.Ad).AsNoTracking().ToListAsync())
        {
            if (alan.Tur == OzellikTuru.Secim) musteriSecenekleri.AddRange(alan.SecenekListesi.Select(s => ($"{alan.Id}|{s}", $"{alan.Ad}: {s}")));
            else if (alan.Tur == OzellikTuru.EvetHayir) musteriSecenekleri.Add(($"{alan.Id}|true", $"{alan.Ad}: Evet"));
        }

        var aracSecenekleri = new List<(string Deger, string Yazi)>();
        foreach (var oz in await _vt.AracOzellikleri.OrderBy(o => o.Sira).ThenBy(o => o.Ad).AsNoTracking().ToListAsync())
        {
            if (oz.Tur == OzellikTuru.Secim) aracSecenekleri.AddRange(oz.SecenekListesi.Select(s => ($"{oz.Id}|{s}", $"{oz.Ad}: {s}")));
            else if (oz.Tur == OzellikTuru.EvetHayir) aracSecenekleri.Add(($"{oz.Id}|true", $"{oz.Ad}: Var"));
        }

        // Düzenlenen kampanyanın değeri listede yoksa (seçenek sonradan silinmiş) yine de görünsün
        if (!string.IsNullOrEmpty(form?.MusteriKosulu) && musteriSecenekleri.All(s => s.Deger != form.MusteriKosulu))
            musteriSecenekleri.Add((form.MusteriKosulu, form.MusteriKosulu.Split('|').Last() + " (artık listede yok)"));
        if (!string.IsNullOrEmpty(form?.AracKosulu) && aracSecenekleri.All(s => s.Deger != form.AracKosulu))
            aracSecenekleri.Add((form.AracKosulu, form.AracKosulu.Split('|').Last() + " (artık listede yok)"));

        ViewBag.MusteriSecenekleri = musteriSecenekleri;
        ViewBag.AracSecenekleri = aracSecenekleri;
        ViewBag.Kategoriler = await _vt.Kategoriler.OrderBy(k => k.Sira).AsNoTracking().ToListAsync();
        ViewBag.Markalar = await _vt.Markalar.OrderBy(m => m.Sira).ThenBy(m => m.Ad).AsNoTracking().ToListAsync();
    }
}

// =============================== MÜŞTERİ BİLGİLERİ ===============================
public class MusteriAlanlariController : YonetimController
{
    private readonly VeriTabani _vt;
    public MusteriAlanlariController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index()
    {
        // Her bilgiyi kaç müşteri doldurmuş: SELECT "AlanId", COUNT(*) FROM "MusteriAlanDegerleri" GROUP BY "AlanId"
        ViewBag.CevapSayilari = await _vt.MusteriAlanDegerleri.GroupBy(d => d.AlanId)
            .Select(g => new { g.Key, Sayi = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Sayi);
        // Hangi alan kaç kampanyada koşul: kampanyası olan alan silinemez
        ViewBag.KampanyaSayilari = await _vt.Kampanyalar.Where(k => k.MusteriAlanId != null).GroupBy(k => k.MusteriAlanId!.Value)
            .Select(g => new { g.Key, Sayi = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Sayi);
        return View(await _vt.MusteriAlanlari.OrderBy(a => a.Sira).ThenBy(a => a.Ad).AsNoTracking().ToListAsync());
    }

    public IActionResult Ekle() => View("Form", new MusteriAlaniFormu());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(MusteriAlaniFormu form)
    {
        Denetle(form);
        if (!ModelState.IsValid) return View("Form", form);

        var alan = new MusteriAlani();
        form.Aktar(alan);
        _vt.MusteriAlanlari.Add(alan);   // INSERT INTO "MusteriAlanlari" ...
        await _vt.SaveChangesAsync();
        Basarili($"\"{alan.Ad}\" eklendi. Kayıt formunda ve profilde sorulur; kampanyalarda koşul olarak seçilebilir.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var alan = await _vt.MusteriAlanlari.FindAsync(id);
        return alan == null ? NotFound() : View("Form", MusteriAlaniFormu.Olustur(alan));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, MusteriAlaniFormu form)
    {
        var alan = await _vt.MusteriAlanlari.FindAsync(id);
        if (alan == null) return NotFound();
        Denetle(form);
        if (!ModelState.IsValid) return View("Form", form);

        form.Aktar(alan);   // UPDATE "MusteriAlanlari" SET ... WHERE "Id" = id
        await _vt.SaveChangesAsync();
        Basarili($"\"{alan.Ad}\" güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        // Bir kampanyanın koşuluysa silinmez (yoksa "Öğretmen indirimi" herkese uygulanmaya başlardı)
        if (await _vt.Kampanyalar.AnyAsync(k => k.MusteriAlanId == id))
        {
            Hata("Bu bilgi bir kampanyada koşul olarak kullanılıyor. Önce Kampanyalar'dan o kampanyayı değiştirin ya da silin.");
            return RedirectToAction(nameof(Index));
        }
        await _vt.MusteriAlanDegerleri.Where(d => d.AlanId == id).ExecuteDeleteAsync();   // DELETE FROM "MusteriAlanDegerleri" WHERE "AlanId" = id
        await _vt.MusteriAlanlari.Where(a => a.Id == id).ExecuteDeleteAsync();            // DELETE FROM "MusteriAlanlari" WHERE "Id" = id
        Basarili("Bilgi ve müşterilerin cevapları silindi.");
        return RedirectToAction(nameof(Index));
    }

    private void Denetle(MusteriAlaniFormu form)
    {
        if (form.Tur == OzellikTuru.Secim && form.SecenekSatirlari().Count == 0)
            ModelState.AddModelError(nameof(form.SeceneklerMetni), "Seçim listesi için en az bir seçenek yazın (her satıra bir tane).");
    }
}
