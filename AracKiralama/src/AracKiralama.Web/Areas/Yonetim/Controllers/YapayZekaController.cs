// ============================================================================
// YAPAY ZEKÂ ASİSTANI (Yönetim → Yapay Zekâ Asistanı) — internetsiz, bu bilgisayarda çalışır
//   GET  /Yonetim/YapayZeka                → sayfa (açılınca motor da arka planda açılır)
//   GET  /Yonetim/YapayZeka/Durum          → motor hazır mı? (sayfa 2 saniyede bir sorar)
//   POST /Yonetim/YapayZeka/Baslat         → motoru (seçilen modelle) aç
//   POST /Yonetim/YapayZeka/Durdur         → motoru kapat (belleği boşaltır)
//   POST /Yonetim/YapayZeka/Sor            → isteği modele sor, işlem kartlarını döndür
//   POST /Yonetim/YapayZeka/Kontrol        → kullanıcının düzenlediği kartları yeniden kontrol et
//   POST /Yonetim/YapayZeka/Onizle         → yazı / ayar değişikliklerini taslak olarak göster
//   POST /Yonetim/YapayZeka/Uygula         → işlemleri uygula, geçmişe yaz
//   POST /Yonetim/YapayZeka/GeriAl/5       → bir kaydı geri al
// Akış: Servisler/YapayZeka/ (YapayZekaServisi → PanelIslemleri). Kod modu: KodModu.cs
// ============================================================================
using System.Text.Json.Nodes;
using AracKiralama.FormModelleri;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

public class YapayZekaController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly YapayZekaSunucusu _sunucu;
    private readonly YapayZekaServisi _yapayZeka;
    private readonly YapayZekaBaglami _baglam;
    private readonly PanelIslemleri _panel;
    private readonly TaslakServisi _taslaklar;
    private readonly KodModu _kodModu;

    public YapayZekaController(VeriTabani vt, YapayZekaSunucusu sunucu, YapayZekaServisi yapayZeka, YapayZekaBaglami baglam,
        PanelIslemleri panel, TaslakServisi taslaklar, KodModu kodModu)
    {
        _vt = vt;
        _sunucu = sunucu;
        _yapayZeka = yapayZeka;
        _baglam = baglam;
        _panel = panel;
        _taslaklar = taslaklar;
        _kodModu = kodModu;
    }

    public async Task<IActionResult> Index()
    {
        // Sayfa açılınca motor arka planda açılmaya başlar; kullanıcı yazarken model belleğe yüklenir.
        if (_sunucu.Kurulu && _sunucu.Durum is SunucuDurumu.Kapali or SunucuDurumu.Hata) _ = Task.Run(() => _sunucu.BaslatAsync());

        var (kodAcik, kodNeden) = _kodModu.AcikMi(User);
        return View(new YapayZekaSayfasi
        {
            Durum = _sunucu.Durum, Mesaj = _sunucu.Mesaj, Kurulu = _sunucu.Kurulu, ModelAdi = _sunucu.ModelAdi ?? _sunucu.OtomatikModel()?.Name,
            Modeller = _sunucu.Modeller().Select(m => (m.Name, Math.Round(m.Length / 1024d / 1024 / 1024, 1))).ToList(),
            RamGb = YapayZekaSunucusu.RamGb, KodModuAcik = kodAcik, KodModuNedenKapali = kodNeden,
            // SQL: SELECT * FROM "YapayZekaKayitlari" ORDER BY "Tarih" DESC LIMIT 20
            Gecmis = await _vt.YapayZekaKayitlari.OrderByDescending(k => k.Tarih).Take(20).AsNoTracking().ToListAsync(),
        });
    }

    /// <summary>Motorun durumu (JSON). Sayfadaki durum çubuğu bunu gösterir.</summary>
    [HttpGet]
    public IActionResult Durum() => Json(new
    {
        durum = _sunucu.Durum.ToString(),
        mesaj = _sunucu.Mesaj,
        model = _sunucu.ModelAdi,
        motor = _sunucu.Motor,
        kurulu = _sunucu.Kurulu,
        sonCikti = _sunucu.Durum == SunucuDurumu.Hata ? _sunucu.SonCikti : null,
    });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Baslat([FromBody] YapayZekaIstegi? istek)
    {
        _ = Task.Run(() => _sunucu.BaslatAsync(istek?.Model));   // beklemeden döner; sayfa durumu yoklar
        return Durum();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Durdur()
    {
        _sunucu.Durdur();
        return Durum();
    }

    /// <summary>İsteği modele sorar. Kartlarda hata varsa modelden bir kez düzeltmesini ister.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sor([FromBody] YapayZekaIstegi istek, CancellationToken iptal)
    {
        if (string.IsNullOrWhiteSpace(istek.Istek)) return Hatali("Ne yapmak istediğinizi yazın.");
        try
        {
            var sistem = await _baglam.PanelSistemiAsync();
            var sema = PanelIslemleri.Sema();
            var mesajlar = new List<(string, string)> { ("user", istek.Istek.Trim()) };

            var cevap = await _yapayZeka.SorAsync(sistem, mesajlar, sema, 3000, iptal);
            var json = YapayZekaServisi.JsonOku(cevap.Icerik);
            var islemler = json["islemler"] as JsonArray ?? new JsonArray();
            var kartlar = await _panel.DogrulaAsync(islemler);
            var sure = cevap.Sure;
            var duzeltildi = false;

            // Hata varsa (örn. olmayan bir kategori adı) modele hataları gösterip bir kez düzelttir
            var hatalar = kartlar.Where(k => k.Uygulanabilir).SelectMany(k => k.Hatalar.Select(h => $"- {k.Tur}: {h}")).ToList();
            if (hatalar.Count > 0)
            {
                mesajlar.Add(("assistant", cevap.Icerik));
                mesajlar.Add(("user", "Cevabında şu hatalar var:\n" + string.Join("\n", hatalar) + "\nSİTENİN DURUMU bölümündeki adları kullanarak düzelt ve cevabın tamamını yeniden yaz."));
                var ikinci = await _yapayZeka.SorAsync(sistem, mesajlar, sema, 3000, iptal);
                sure += ikinci.Sure;
                var ikinciJson = YapayZekaServisi.JsonOku(ikinci.Icerik);
                var ikinciIslemler = ikinciJson["islemler"] as JsonArray ?? new JsonArray();
                var ikinciKartlar = await _panel.DogrulaAsync(ikinciIslemler);
                if (ikinciKartlar.Sum(k => k.Hatalar.Count) < kartlar.Sum(k => k.Hatalar.Count))
                    (json, kartlar, cevap, duzeltildi) = (ikinciJson, ikinciKartlar, ikinci, true);
            }

            return Json(new
            {
                anladigim = json["anladigim"]?.ToString(),
                kartlar,
                sure = Math.Round(sure, 1),
                tokenHizi = cevap.TokenHizi,
                model = _sunucu.ModelAdi,
                duzeltildi,
            });
        }
        catch (IsKuraliHatasi hata) { return Hatali(hata.Message); }
        catch (HttpRequestException hata) { return Hatali("Yapay zekâ motoruna ulaşılamadı: " + hata.Message); }
        catch (OperationCanceledException) when (iptal.IsCancellationRequested) { return Hatali("İptal edildi."); }
        catch (TaskCanceledException) { return Hatali("Yapay zekâ çok uzun sürdü (zaman aşımı). Daha kısa bir istek deneyin ya da küçük modeli seçin."); }
    }

    /// <summary>Kullanıcının düzenlediği işlemleri yeniden kontrol eder.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Kontrol([FromBody] YapayZekaIstegi istek)
        => Json(new { kartlar = await _panel.DogrulaAsync(istek.Islemler ?? new JsonArray()) });

    /// <summary>Yazı ve ayar değişikliklerini taslağa koyar; sayfa ?onizleme=1 ile açılınca görünür (gerçek site değişmez).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Onizle([FromBody] YapayZekaIstegi istek)
    {
        var taslak = new Taslak();
        string? adres = null;
        foreach (var p in (istek.Islemler ?? new JsonArray()).OfType<JsonObject>())
        {
            var tur = p["tur"]?.ToString();
            if (tur == "metin_degistir" && p["anahtar"]?.ToString() is string anahtar) taslak.Metinler[anahtar] = p["yeni_metin"]?.ToString() ?? "";
            else if (tur == "ana_baslik_degistir") foreach (var (a, d) in AnaBaslikDegistir.Parcalar(p)) taslak.Metinler[a] = d;
            else if (tur == "ayar_degistir" && p["ayar"]?.ToString() is string ayar) taslak.Ayarlar[ayar] = p["deger"]?.ToString() ?? "";
            else continue;
            adres ??= PanelIslemleri.Bul(tur)!.Link(p);
        }
        _taslaklar.Kaydet(KullaniciId, taslak);
        return Json(new { adres = (adres ?? "/") + ((adres ?? "/").Contains('?') ? "&" : "?") + "onizleme=1" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Uygula([FromBody] YapayZekaIstegi istek)
    {
        try
        {
            var kayit = await _panel.UygulaAsync(istek.Islemler ?? new JsonArray(), istek.Istek ?? "", istek.Anladigim,
                User.Identity?.Name ?? "", _sunucu.ModelAdi, istek.Sure);
            _taslaklar.Sil(KullaniciId);
            var linkler = (istek.Islemler ?? new JsonArray()).OfType<JsonObject>()
                .Select(p => PanelIslemleri.Bul(p["tur"]?.ToString())?.Link(p)).Where(l => l != null).Distinct().ToList();
            return Json(new { kayitId = kayit.Id, ozet = kayit.Ozet, linkler });
        }
        catch (IsKuraliHatasi hata) { return Hatali(hata.Message); }
    }

    // ===================== KOD MODU (deneysel, sadece Admin) =====================

    /// <summary>1) Modelden, istek için hangi dosyaların değişeceğini seçmesini ister.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KodDosyaSec([FromBody] YapayZekaIstegi istek, CancellationToken iptal)
    {
        if (KodModuKapali() is { } kapali) return kapali;
        if (string.IsNullOrWhiteSpace(istek.Istek)) return Hatali("Ne yapmak istediğinizi yazın.");
        return await Dene(async () =>
        {
            var (anladigim, dosyalar, sure) = await _kodModu.DosyaSecAsync(istek.Istek, iptal);
            return Json(new { anladigim, dosyalar, sure, tumDosyalar = _kodModu.ProjeHaritasi().Select(h => h.Yol) });
        }, iptal);
    }

    /// <summary>2) Seçilen dosyalarla değişiklik önerisi ister (OncekiHata doluysa düzeltme ister).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KodOner([FromBody] YapayZekaIstegi istek, CancellationToken iptal)
    {
        if (KodModuKapali() is { } kapali) return kapali;
        if (string.IsNullOrWhiteSpace(istek.Istek) || istek.Dosyalar is not { Count: > 0 }) return Hatali("İstek ve en az bir dosya gerekli.");
        return await Dene(async () =>
        {
            var (anladigim, degisiklikler, sure) = await _kodModu.OnerAsync(istek.Istek, istek.Dosyalar, istek.Degisiklikler, istek.OncekiHata, iptal);
            return Json(new { anladigim, degisiklikler, sure });
        }, iptal);
    }

    /// <summary>3) Değişiklikleri geçici kopyada derleyerek dener (gerçek dosyalar değişmez).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KodDene([FromBody] YapayZekaIstegi istek, CancellationToken iptal)
    {
        if (KodModuKapali() is { } kapali) return kapali;
        var degisiklikler = istek.Degisiklikler ?? new();
        var (basarili, hatalar, sure) = await _kodModu.DeneAsync(degisiklikler, iptal);
        return Json(new { basarili, hatalar, sure, degisiklikler });
    }

    /// <summary>4) Değişiklikleri yedekleyip gerçek dosyalara yazar.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KodUygula([FromBody] YapayZekaIstegi istek)
    {
        if (KodModuKapali() is { } kapali) return kapali;
        try
        {
            var (kayit, veritabani) = await _kodModu.UygulaAsync(istek.Degisiklikler ?? new(), istek.Istek ?? "", istek.Anladigim,
                User.Identity?.Name ?? "", _sunucu.ModelAdi, istek.Sure);
            return Json(new { kayitId = kayit.Id, ozet = kayit.Ozet, veritabaniYenilenecek = veritabani, yenidenBaslatilabilir = KodModu.BaslaticidanMi });
        }
        catch (IsKuraliHatasi hata) { return Hatali(hata.Message); }
    }

    /// <summary>5) Uygulamayı yeniden başlatır (sadece BASLAT.bat ile açıldıysa).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult YenidenBaslat()
    {
        if (KodModuKapali() is { } kapali) return kapali;
        return _kodModu.YenidenBaslat()
            ? Json(new { tamam = true })
            : Hatali("Uygulama BASLAT.bat ile açılmadığı için kendini yeniden başlatamıyor. Kapatıp yeniden açın (Visual Studio / VS Code'da durdurup tekrar çalıştırın).");
    }

    private IActionResult? KodModuKapali()
    {
        var (acik, neden) = _kodModu.AcikMi(User);
        return acik ? null : Hatali(neden!);
    }

    /// <summary>Yapay zekâ çağrılarındaki ortak hata yakalama.</summary>
    private async Task<IActionResult> Dene(Func<Task<IActionResult>> islem, CancellationToken iptal)
    {
        try { return await islem(); }
        catch (IsKuraliHatasi hata) { return Hatali(hata.Message); }
        catch (HttpRequestException hata) { return Hatali("Yapay zekâ motoruna ulaşılamadı: " + hata.Message); }
        catch (OperationCanceledException) when (iptal.IsCancellationRequested) { return Hatali("İptal edildi."); }
        catch (TaskCanceledException) { return Hatali("Yapay zekâ çok uzun sürdü (zaman aşımı)."); }
    }

    /// <summary>Geçmişteki bir değişikliği geri alır (panel ya da kod).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GeriAl(int id)
    {
        var kayit = await _vt.YapayZekaKayitlari.FindAsync(id);
        if (kayit == null) return NotFound();
        try
        {
            if (kayit.Tur == "Kod") await _kodModu.GeriAlAsync(kayit);
            else await _panel.GeriAlAsync(kayit);
            Basarili(kayit.Tur == "Kod" ? "Kod değişikliği geri alındı. Uygulama yeniden başlatılınca geçerli olur." : "Değişiklik geri alındı.");
        }
        catch (IsKuraliHatasi hata) { Hata(hata.Message); }
        return RedirectToAction(nameof(Index));
    }

    private JsonResult Hatali(string mesaj)
    {
        Response.StatusCode = 400;
        return Json(new { hata = mesaj });
    }
}
