// ============================================================================
// SİTE İÇERİĞİ YÖNETİMİ
//   Ayarlar   → firma bilgileri, renkler, ana sayfa, fiyat kuralları, filtreler (SiteAyarlari tablosu)
//   Sayfalar  → Hakkımızda, KVKK gibi serbest metin sayfaları
//   Metinler  → sitedeki her başlık / buton yazısı
//   Gorseller → resim yükleme (adresini kopyalayıp ayarlarda / araçlarda kullanırsınız)
// Kaydedilen her şeyden sonra _icerik.Yenile() çağrılır → değişiklik sitede hemen görünür.
// ============================================================================
using System.Globalization;
using System.Reflection;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

/// <summary>Ayarlar formundaki tek bir alan (view bunu okuyup uygun kutuyu çizer).</summary>
public class AyarAlani
{
    public string Ad { get; set; } = "";          // özellik adı → formdaki name
    public AyarAttribute Bilgi { get; set; } = null!;
    public Type Tur { get; set; } = typeof(string);
    public string Deger { get; set; } = "";
}

// =============================== AYARLAR ===============================
public class AyarlarController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly IcerikServisi _icerik;

    public AyarlarController(VeriTabani vt, IcerikServisi icerik)
    {
        _vt = vt;
        _icerik = icerik;
    }

    public async Task<IActionResult> Index()
    {
        var ayarlar = await _vt.SiteAyarlari.AsNoTracking().FirstAsync();
        return View(AlanlariOlustur(ayarlar));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteAyarlari form)
    {
        var mevcut = await _vt.SiteAyarlari.FirstAsync();
        form.Id = mevcut.Id;
        _vt.Entry(mevcut).CurrentValues.SetValues(form);   // formdaki bütün değerleri mevcut kayda kopyala
        await _vt.SaveChangesAsync();
        _icerik.Yenile();
        Basarili("Ayarlar kaydedildi. Değişiklikler sitede hemen geçerli.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Bütün ayarları ilk haline döndürür.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VarsayilanaDon()
    {
        var mevcut = await _vt.SiteAyarlari.FirstAsync();
        _vt.Entry(mevcut).CurrentValues.SetValues(new SiteAyarlari { Id = mevcut.Id });
        await _vt.SaveChangesAsync();
        _icerik.Yenile();
        Basarili("Bütün ayarlar varsayılan değerlere döndürüldü.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Araç, kullanıcı ve rezervasyonları silip örnek verileri yeniden yükler (sadece Admin).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> DemoSifirla([FromServices] UserManager<Kullanici> kullanicilar, [FromServices] SignInManager<Kullanici> giris)
    {
        await OrnekVeriler.DemoSifirlaAsync(_vt, kullanicilar);
        await giris.SignOutAsync();   // kullanıcılar yeniden oluşturulduğu için oturumu kapatıyoruz
        TempData["Basarili"] = "Demo verileri yeniden yüklendi. Lütfen tekrar giriş yapın.";
        return RedirectToAction("Giris", "Hesap", new { area = "" });
    }

    /// <summary>SiteAyarlari'ndaki [Ayar] etiketli özellikleri form alanı listesine çevirir.</summary>
    private static List<AyarAlani> AlanlariOlustur(SiteAyarlari ayarlar)
    {
        var alanlar = new List<AyarAlani>();
        foreach (var ozellik in typeof(SiteAyarlari).GetProperties())
        {
            var bilgi = ozellik.GetCustomAttribute<AyarAttribute>();
            if (bilgi == null) continue;
            var deger = ozellik.GetValue(ayarlar);
            alanlar.Add(new AyarAlani
            {
                Ad = ozellik.Name,
                Bilgi = bilgi,
                Tur = ozellik.PropertyType,
                Deger = deger is decimal d ? d.ToString(CultureInfo.InvariantCulture) : deger?.ToString() ?? "",
            });
        }
        return alanlar;
    }
}

// =============================== SAYFALAR ===============================
public class SayfalarController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly IcerikServisi _icerik;

    public SayfalarController(VeriTabani vt, IcerikServisi icerik)
    {
        _vt = vt;
        _icerik = icerik;
    }

    public async Task<IActionResult> Index() => View(await _vt.Sayfalar.OrderBy(s => s.Sira).ToListAsync());

    public IActionResult Ekle() => View("Form", new Sayfa());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(Sayfa sayfa)
    {
        if (await _vt.Sayfalar.AnyAsync(s => s.Adres == sayfa.Adres)) ModelState.AddModelError(nameof(sayfa.Adres), "Bu adres başka bir sayfada kullanılıyor.");
        if (!ModelState.IsValid) return View("Form", sayfa);
        _vt.Sayfalar.Add(sayfa);
        await _vt.SaveChangesAsync();
        _icerik.Yenile();
        Basarili($"\"{sayfa.Baslik}\" sayfası eklendi: /Sayfa/{sayfa.Adres}");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var sayfa = await _vt.Sayfalar.FindAsync(id);
        return sayfa == null ? NotFound() : View("Form", sayfa);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(Sayfa sayfa)
    {
        if (await _vt.Sayfalar.AnyAsync(s => s.Adres == sayfa.Adres && s.Id != sayfa.Id)) ModelState.AddModelError(nameof(sayfa.Adres), "Bu adres başka bir sayfada kullanılıyor.");
        if (!ModelState.IsValid) return View("Form", sayfa);
        _vt.Sayfalar.Update(sayfa);
        await _vt.SaveChangesAsync();
        _icerik.Yenile();
        Basarili("Sayfa güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        await _vt.Sayfalar.Where(s => s.Id == id).ExecuteDeleteAsync();
        _icerik.Yenile();
        Basarili("Sayfa silindi.");
        return RedirectToAction(nameof(Index));
    }
}

// =============================== SİTE METİNLERİ ===============================
public class MetinlerController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly IcerikServisi _icerik;

    public MetinlerController(VeriTabani vt, IcerikServisi icerik)
    {
        _vt = vt;
        _icerik = icerik;
    }

    public IActionResult Index() => View();   // liste doğrudan VarsayilanMetinler + IcerikServisi'nden okunur

    /// <summary>Formdaki bütün metinleri kaydeder. Boş bırakılan veya varsayılanla aynı olan → varsayılana döner.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Dictionary<string, string?> metinler)
    {
        var kayitlilar = await _vt.Metinler.ToDictionaryAsync(m => m.Anahtar);
        foreach (var (anahtar, yeniDeger) in metinler)
        {
            if (!VarsayilanMetinler.Liste.TryGetValue(anahtar, out var varsayilan)) continue;   // tanımsız anahtarı yok say
            var degisti = !string.IsNullOrWhiteSpace(yeniDeger) && yeniDeger.Trim() != varsayilan;
            kayitlilar.TryGetValue(anahtar, out var kayit);

            if (!degisti && kayit != null) _vt.Metinler.Remove(kayit);
            else if (degisti && kayit == null) _vt.Metinler.Add(new Metin { Anahtar = anahtar, Deger = yeniDeger!.Trim() });
            else if (degisti && kayit != null) kayit.Deger = yeniDeger!.Trim();
        }
        await _vt.SaveChangesAsync();
        _icerik.Yenile();
        Basarili("Metinler kaydedildi.");
        return RedirectToAction(nameof(Index));
    }
}

// =============================== GÖRSELLER ===============================
public class GorsellerController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly GorselServisi _gorseller;

    public GorsellerController(VeriTabani vt, GorselServisi gorseller)
    {
        _vt = vt;
        _gorseller = gorseller;
    }

    public async Task<IActionResult> Index()
    {
        // Görsel baytlarını (Veri) listede çekmiyoruz; sadece bilgileri.
        var liste = await _vt.Gorseller.OrderByDescending(g => g.Id)
            .Select(g => new Gorsel { Id = g.Id, DosyaAdi = g.DosyaAdi, YuklenmeTarihi = g.YuklenmeTarihi }).ToListAsync();
        return View(liste);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yukle(IFormFile? dosya)
    {
        if (dosya == null) Hata("Lütfen bir dosya seçin.");
        else
        {
            try { Basarili($"Görsel yüklendi. Adresi: {await _gorseller.KaydetAsync(dosya)}"); }
            catch (IsKuraliHatasi hata) { Hata(hata.Message); }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        await _vt.Gorseller.Where(g => g.Id == id).ExecuteDeleteAsync();
        Basarili("Görsel silindi. (Kullanıldığı yerlerde artık görünmez.)");
        return RedirectToAction(nameof(Index));
    }
}
