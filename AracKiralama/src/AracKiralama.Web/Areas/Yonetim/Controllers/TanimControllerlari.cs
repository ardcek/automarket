// ============================================================================
// TANIM SAYFALARI: Şubeler, Ek Hizmetler, Markalar, Kategoriler, Yakıt/Vites
// Hepsi aynı 5 adımlı kalıp (birini anlayan hepsini anlar):
//   Index (liste) → Ekle (boş form) → Ekle POST (kaydet) → Duzenle → Sil
// Formlar Areas/Yonetim/Views/{Controller}/Form.cshtml dosyasındadır.
// Kullanımda olan bir kayıt (ör. araçları olan marka) silinemez; uyarı gösterilir.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Veri;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

// =============================== ŞUBELER ===============================
public class SubelerController : YonetimController
{
    private readonly VeriTabani _vt;
    public SubelerController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index() => View(await _vt.Subeler.OrderBy(s => s.Sehir).ThenBy(s => s.Ad).ToListAsync());

    public IActionResult Ekle() => View("Form", new Sube());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(Sube sube)
    {
        if (!ModelState.IsValid) return View("Form", sube);
        _vt.Subeler.Add(sube);
        await _vt.SaveChangesAsync();
        Basarili($"\"{sube.Ad}\" eklendi.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var sube = await _vt.Subeler.FindAsync(id);
        return sube == null ? NotFound() : View("Form", sube);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(Sube sube)
    {
        if (!ModelState.IsValid) return View("Form", sube);
        _vt.Subeler.Update(sube);
        await _vt.SaveChangesAsync();
        Basarili("Şube güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var kullanimda = await _vt.Araclar.AnyAsync(a => a.SubeId == id) || await _vt.Rezervasyonlar.AnyAsync(r => r.AlisSubeId == id || r.IadeSubeId == id);
        if (kullanimda) Hata("Bu şubeye bağlı araç veya rezervasyon olduğu için silinemez.");
        else { await _vt.Subeler.Where(s => s.Id == id).ExecuteDeleteAsync(); Basarili("Şube silindi."); }
        return RedirectToAction(nameof(Index));
    }
}

// =============================== EK HİZMETLER ===============================
public class EkstralarController : YonetimController
{
    private readonly VeriTabani _vt;
    public EkstralarController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index() => View(await _vt.Ekstralar.OrderBy(e => e.Ad).ToListAsync());

    public IActionResult Ekle() => View("Form", new Ekstra());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(Ekstra ekstra)
    {
        if (!ModelState.IsValid) return View("Form", ekstra);
        _vt.Ekstralar.Add(ekstra);
        await _vt.SaveChangesAsync();
        Basarili($"\"{ekstra.Ad}\" eklendi.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var ekstra = await _vt.Ekstralar.FindAsync(id);
        return ekstra == null ? NotFound() : View("Form", ekstra);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(Ekstra ekstra)
    {
        if (!ModelState.IsValid) return View("Form", ekstra);
        _vt.Ekstralar.Update(ekstra);
        await _vt.SaveChangesAsync();
        Basarili("Ek hizmet güncellendi (mevcut rezervasyonların fiyatı değişmez).");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        // Geçmiş rezervasyonlarda kullanıldıysa silinmez, pasif yapılır.
        if (await _vt.RezervasyonEkstralari.AnyAsync(x => x.EkstraId == id))
        {
            await _vt.Ekstralar.Where(e => e.Id == id).ExecuteUpdateAsync(x => x.SetProperty(e => e.Aktif, false));
            Basarili("Bu hizmet geçmiş rezervasyonlarda kullanıldığı için silinmedi, pasif yapıldı.");
        }
        else { await _vt.Ekstralar.Where(e => e.Id == id).ExecuteDeleteAsync(); Basarili("Ek hizmet silindi."); }
        return RedirectToAction(nameof(Index));
    }
}

// =============================== MARKALAR ===============================
public class MarkalarController : YonetimController
{
    private readonly VeriTabani _vt;
    public MarkalarController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index() => View(await _vt.Markalar.OrderBy(m => m.Sira).ThenBy(m => m.Ad).ToListAsync());

    public IActionResult Ekle() => View("Form", new Marka());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(Marka marka)
    {
        if (!ModelState.IsValid) return View("Form", marka);
        _vt.Markalar.Add(marka);
        await _vt.SaveChangesAsync();
        Basarili($"\"{marka.Ad}\" eklendi.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var marka = await _vt.Markalar.FindAsync(id);
        return marka == null ? NotFound() : View("Form", marka);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(Marka marka)
    {
        if (!ModelState.IsValid) return View("Form", marka);
        _vt.Markalar.Update(marka);
        await _vt.SaveChangesAsync();
        Basarili("Marka güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        if (await _vt.Araclar.AnyAsync(a => a.MarkaId == id)) Hata("Bu markaya ait araçlar olduğu için silinemez.");
        else { await _vt.Markalar.Where(m => m.Id == id).ExecuteDeleteAsync(); Basarili("Marka silindi."); }
        return RedirectToAction(nameof(Index));
    }
}

// =============================== KATEGORİLER ===============================
public class KategorilerController : YonetimController
{
    private readonly VeriTabani _vt;
    public KategorilerController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index() => View(await _vt.Kategoriler.OrderBy(k => k.Sira).ToListAsync());

    public IActionResult Ekle() => View("Form", new Kategori());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(Kategori kategori)
    {
        if (!ModelState.IsValid) return View("Form", kategori);
        _vt.Kategoriler.Add(kategori);
        await _vt.SaveChangesAsync();
        Basarili($"\"{kategori.Ad}\" eklendi. Artık filtrelerde ve araç formunda görünür.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var kategori = await _vt.Kategoriler.FindAsync(id);
        return kategori == null ? NotFound() : View("Form", kategori);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(Kategori kategori)
    {
        if (!ModelState.IsValid) return View("Form", kategori);
        _vt.Kategoriler.Update(kategori);
        await _vt.SaveChangesAsync();
        Basarili("Kategori güncellendi. Yeni yaş / ehliyet şartları bundan sonraki rezervasyonlarda geçerli.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        if (await _vt.Araclar.AnyAsync(a => a.KategoriId == id)) Hata("Bu kategoride araçlar olduğu için silinemez.");
        else { await _vt.Kategoriler.Where(k => k.Id == id).ExecuteDeleteAsync(); Basarili("Kategori silindi."); }
        return RedirectToAction(nameof(Index));
    }
}

// =============================== YAKIT VE VİTES TÜRLERİ ===============================
/// <summary>İki küçük liste tek sayfada: ekle, adını değiştir, sil.</summary>
public class SeceneklerController : YonetimController
{
    private readonly VeriTabani _vt;
    public SeceneklerController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index()
    {
        ViewBag.Yakitlar = await _vt.YakitTurleri.OrderBy(y => y.Sira).ToListAsync();
        ViewBag.Vitesler = await _vt.VitesTurleri.OrderBy(v => v.Sira).ToListAsync();
        return View();
    }

    /// <summary>tur: "yakit" veya "vites"</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(string tur, string ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) { Hata("Ad boş olamaz."); return RedirectToAction(nameof(Index)); }
        if (tur == "yakit") _vt.YakitTurleri.Add(new YakitTuru { Ad = ad.Trim(), Sira = 99 });
        else _vt.VitesTurleri.Add(new VitesTuru { Ad = ad.Trim(), Sira = 99 });
        await _vt.SaveChangesAsync();
        Basarili($"\"{ad}\" eklendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdDegistir(string tur, int id, string ad)
    {
        if (tur == "yakit") await _vt.YakitTurleri.Where(y => y.Id == id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Ad, ad.Trim()));
        else await _vt.VitesTurleri.Where(v => v.Id == id).ExecuteUpdateAsync(x => x.SetProperty(v => v.Ad, ad.Trim()));
        Basarili("Güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(string tur, int id)
    {
        var kullanimda = tur == "yakit" ? await _vt.Araclar.AnyAsync(a => a.YakitTuruId == id) : await _vt.Araclar.AnyAsync(a => a.VitesTuruId == id);
        if (kullanimda) Hata("Bu seçeneği kullanan araçlar olduğu için silinemez.");
        else if (tur == "yakit") { await _vt.YakitTurleri.Where(y => y.Id == id).ExecuteDeleteAsync(); Basarili("Silindi."); }
        else { await _vt.VitesTurleri.Where(v => v.Id == id).ExecuteDeleteAsync(); Basarili("Silindi."); }
        return RedirectToAction(nameof(Index));
    }
}
