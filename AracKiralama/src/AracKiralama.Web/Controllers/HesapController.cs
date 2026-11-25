// ============================================================================
// HESAP: giriş, kayıt, çıkış
//   /Hesap/Giris  ·  /Hesap/Kayit  ·  /Hesap/Cikis (POST)
// Şifre kontrolü ve çerez (oturum) işlemleri ASP.NET Identity tarafından yapılır.
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AracKiralama.Controllers;

public class HesapController : TemelController
{
    private readonly SignInManager<Kullanici> _girisYonetici;
    private readonly UserManager<Kullanici> _kullanicilar;
    private readonly SiteAyarlari _ayarlar;
    private readonly IcerikServisi _icerik;
    private readonly MusteriBilgisiServisi _musteriBilgileri;

    public HesapController(SignInManager<Kullanici> girisYonetici, UserManager<Kullanici> kullanicilar, SiteAyarlari ayarlar, IcerikServisi icerik,
        MusteriBilgisiServisi musteriBilgileri)
    {
        _girisYonetici = girisYonetici;
        _kullanicilar = kullanicilar;
        _ayarlar = ayarlar;
        _icerik = icerik;
        _musteriBilgileri = musteriBilgileri;
    }

    [HttpGet]
    public IActionResult Giris(string? donusAdresi) => View(new GirisFormu { DonusAdresi = donusAdresi });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(GirisFormu form)
    {
        if (!ModelState.IsValid) return View(form);
        form.Eposta = form.Eposta.Trim();

        // Sunum modu: üye girişi kapalıyken yalnızca admin ve personel girebilir (yönetim paneli için).
        // Müşteriye (ya da sistemde olmayan bir e-postaya) aynı "yakında açılacak" uyarısı gösterilir.
        if (!_ayarlar.UyeGirisiAcik && !await PersonelMiAsync(form.Eposta))
        {
            ModelState.AddModelError("", _icerik.Metin("hesap.giris-kapali"));
            return View(form);
        }

        var sonuc = await _girisYonetici.PasswordSignInAsync(form.Eposta, form.Sifre, form.BeniHatirla, lockoutOnFailure: false);
        if (sonuc.IsLockedOut)   // Yönetim → Müşteriler → kişi → "Hesabı dondur"
        {
            ModelState.AddModelError("", "Bu hesap dondurulmuş. Bilgi için bizimle iletişime geçin.");
            return View(form);
        }
        if (!sonuc.Succeeded)
        {
            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(form);
        }

        var kullanici = await _kullanicilar.FindByEmailAsync(form.Eposta);
        kullanici!.SonGiris = DateTime.Now;
        await _kullanicilar.UpdateAsync(kullanici);

        // Admin geçici şifre verdiyse önce yeni şifre belirlenir (Yardimcilar/SifreKontrolu.cs)
        if (kullanici.SifreDegistirmeli) return RedirectToAction(nameof(YeniSifre));
        Basarili($"Hoş geldiniz, {kullanici.Ad}!");

        if (!string.IsNullOrEmpty(form.DonusAdresi) && Url.IsLocalUrl(form.DonusAdresi)) return Redirect(form.DonusAdresi);

        // Süper admin, admin ve personel doğrudan yönetim paneline gider
        return await YonetimdeMiAsync(kullanici) ? RedirectToAction("Index", "Panel", new { area = "Yonetim" }) : RedirectToAction("Index", "AnaSayfa");
    }

    [HttpGet]
    public async Task<IActionResult> Kayit(string? donusAdresi)
        => View(new KayitFormu { DonusAdresi = donusAdresi, Alanlar = await _musteriBilgileri.AlanlarAsync(sadeceKayit: true) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Kayit(KayitFormu form)
    {
        // Panelden eklenen sorular (Meslek vb.): ekranda tekrar gösterilmek ve kontrol edilmek için okunur
        form.Alanlar = await _musteriBilgileri.AlanlarAsync(sadeceKayit: true);

        // Sunum modu: kayıt kapalıyken form görünür ama gönderilince kayıt yapılmaz.
        if (!_ayarlar.UyeKaydiAcik)
        {
            ModelState.AddModelError("", _icerik.Metin("hesap.kayit-kapali"));
            return View(form);
        }
        foreach (var hata in MusteriBilgisiServisi.Hatalar(form.Alanlar, form.Alan)) ModelState.AddModelError("", hata);
        if (!ModelState.IsValid) return View(form);

        var eposta = form.Eposta.Trim().ToLowerInvariant();   // "Ayse@Ornek.com " ile "ayse@ornek.com" aynı hesap sayılsın
        var kullanici = new Kullanici { UserName = eposta, Email = eposta, Ad = form.Ad.Trim(), Soyad = form.Soyad.Trim(), PhoneNumber = form.Telefon?.Trim() };
        var sonuc = await _kullanicilar.CreateAsync(kullanici, form.Sifre);
        if (!sonuc.Succeeded)
        {
            // Hata, ilgili kutunun altında görünsün (e-posta zaten kayıtlı → e-posta kutusu, şifre kuralları → şifre kutusu)
            foreach (var hata in sonuc.Errors)
            {
                if (hata.Code == "DuplicateUserName") continue;   // e-posta = kullanıcı adı; aynı hata iki kez yazılmasın
                var alan = hata.Code.StartsWith("Password") ? nameof(form.Sifre) : hata.Code.Contains("Email") ? nameof(form.Eposta) : "";
                ModelState.AddModelError(alan, hata.Description);
            }
            return View(form);
        }

        // Cevaplar MusteriAlanDegerleri tablosuna yazılır (INSERT)
        await _musteriBilgileri.KaydetAsync(kullanici.Id, form.Alanlar, form.Alan);
        await _kullanicilar.AddToRoleAsync(kullanici, Roller.Musteri);
        await _girisYonetici.SignInAsync(kullanici, isPersistent: true);

        // Rezervasyon için doğum tarihi / ehliyet bilgisi gerekli → profili tamamlamaya yönlendir
        Basarili("Hesabınız oluşturuldu! Rezervasyon yapabilmek için ehliyet bilgilerinizi tamamlayın.");
        return RedirectToAction("Duzenle", "Profil", new { donusAdresi = form.DonusAdresi });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Cikis()
    {
        await _girisYonetici.SignOutAsync();
        Basarili("Çıkış yaptınız.");
        return RedirectToAction("Index", "AnaSayfa");
    }

    public IActionResult ErisimEngellendi() => View();

    // =====================================================================
    // ŞİFRE İŞLEMLERİ
    //   /Hesap/SifremiUnuttum → e-posta yazılır, 2 saat geçerli link gönderilir (Servisler/EpostaServisi.cs)
    //   /Hesap/SifreSifirla   → linkten gelinir, yeni şifre belirlenir
    //   /Hesap/YeniSifre      → admin geçici şifre verdiyse ilk girişte zorunlu
    // =====================================================================

    [HttpGet]
    public IActionResult SifremiUnuttum() => View(new SifremiUnuttumFormu());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SifremiUnuttum(SifremiUnuttumFormu form, [FromServices] EpostaServisi eposta,
        [FromServices] IWebHostEnvironment ortam, [FromServices] ILogger<HesapController> log)
    {
        if (!ModelState.IsValid) return View(form);

        // Güvenlik: e-posta kayıtlı olsa da olmasa da AYNI mesaj gösterilir (kimin üye olduğu öğrenilemesin)
        const string mesaj = "Bu e-posta adresi kayıtlıysa şifre sıfırlama linki gönderildi. Gelen kutunuzu (ve gereksiz klasörünü) kontrol edin; link 2 saat geçerlidir.";
        var kullanici = await _kullanicilar.FindByEmailAsync(form.Eposta.Trim());
        if (kullanici != null)
        {
            var kod = await _kullanicilar.GeneratePasswordResetTokenAsync(kullanici);
            var link = Url.Action(nameof(SifreSifirla), "Hesap", new { eposta = kullanici.Email, kod }, Request.Scheme)!;
            var gonderildi = await eposta.GonderAsync(kullanici.Email!, $"{_ayarlar.FirmaAdi} · Şifre sıfırlama",
                $"<p>Merhaba {System.Net.WebUtility.HtmlEncode(kullanici.Ad)},</p>" +
                $"<p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın. Bağlantı 2 saat geçerlidir.</p>" +
                $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Yeni şifre belirle</a></p>" +
                $"<p>Bu isteği siz yapmadıysanız bu e-postayı görmezden gelebilirsiniz.</p>");
            if (!gonderildi)
            {
                log.LogWarning("Şifre sıfırlama linki (e-posta gönderilmedi) {Eposta}: {Link}", kullanici.Email, link);
                // Bilgisayarda (Development) e-posta ayarı yoksa link ekranda gösterilir; canlıda asla gösterilmez
                if (ortam.IsDevelopment()) TempData["SifirlamaLinki"] = link;
            }
        }
        Basarili(mesaj);
        return RedirectToAction(nameof(SifremiUnuttum));
    }

    [HttpGet]
    public IActionResult SifreSifirla(string? eposta, string? kod)
    {
        if (string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(kod)) { Hata("Bağlantı eksik. Lütfen e-postadaki bağlantıyı yeniden açın."); return RedirectToAction(nameof(SifremiUnuttum)); }
        return View(new SifreSifirlaFormu { Eposta = eposta, Kod = kod });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SifreSifirla(SifreSifirlaFormu form)
    {
        if (!ModelState.IsValid) return View(form);
        var kullanici = await _kullanicilar.FindByEmailAsync(form.Eposta);
        var sonuc = kullanici == null ? IdentityResult.Failed() : await _kullanicilar.ResetPasswordAsync(kullanici, form.Kod, form.Sifre);
        if (!sonuc.Succeeded)
        {
            // Süresi dolmuş / kullanılmış link ya da şifre kuralı hatası
            var sifreHatalari = sonuc.Errors.Where(h => h.Code.StartsWith("Password")).ToList();
            if (sifreHatalari.Count > 0) { foreach (var h in sifreHatalari) ModelState.AddModelError(nameof(form.Sifre), h.Description); return View(form); }
            Hata("Bu bağlantının süresi dolmuş ya da daha önce kullanılmış. Yeni bir bağlantı isteyin.");
            return RedirectToAction(nameof(SifremiUnuttum));
        }
        kullanici!.SifreDegistirmeli = false;
        await _kullanicilar.UpdateAsync(kullanici);
        Basarili("Şifreniz değiştirildi. Yeni şifrenizle giriş yapabilirsiniz.");
        return RedirectToAction(nameof(Giris));
    }

    [HttpGet, Authorize]
    public IActionResult YeniSifre() => View(new YeniSifreFormu());

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> YeniSifre(YeniSifreFormu form)
    {
        if (!ModelState.IsValid) return View(form);
        var kullanici = await _kullanicilar.GetUserAsync(User);
        if (kullanici == null) return Challenge();

        var kod = await _kullanicilar.GeneratePasswordResetTokenAsync(kullanici);
        var sonuc = await _kullanicilar.ResetPasswordAsync(kullanici, kod, form.Sifre);
        if (!sonuc.Succeeded) { foreach (var h in sonuc.Errors) ModelState.AddModelError(nameof(form.Sifre), h.Description); return View(form); }

        kullanici.SifreDegistirmeli = false;
        await _kullanicilar.UpdateAsync(kullanici);
        await _girisYonetici.RefreshSignInAsync(kullanici);   // "şifresini değiştirmeli" bilgisi çerezden de silinsin
        Basarili($"Şifreniz kaydedildi. Hoş geldiniz, {kullanici.Ad}!");
        return await YonetimdeMiAsync(kullanici) ? RedirectToAction("Index", "Panel", new { area = "Yonetim" }) : RedirectToAction("Index", "AnaSayfa");
    }

    /// <summary>Bu e-posta yönetim paneline girebilen birine mi ait? (Sunum modunda giriş izni için)</summary>
    private async Task<bool> PersonelMiAsync(string eposta)
    {
        var kullanici = await _kullanicilar.FindByEmailAsync(eposta);
        return kullanici != null && await YonetimdeMiAsync(kullanici);
    }

    /// <summary>Süper admin, admin ya da personel mi? (veritabanındaki rollerine bakar)</summary>
    private async Task<bool> YonetimdeMiAsync(Kullanici kullanici)
        => (await _kullanicilar.GetRolesAsync(kullanici)).Any(r => r is Roller.SuperAdmin or Roller.Admin or Roller.Personel);
}
