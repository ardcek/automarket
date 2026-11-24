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
        if (!sonuc.Succeeded)
        {
            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(form);
        }

        var kullanici = await _kullanicilar.FindByEmailAsync(form.Eposta);
        Basarili($"Hoş geldiniz, {kullanici!.Ad}!");

        if (!string.IsNullOrEmpty(form.DonusAdresi) && Url.IsLocalUrl(form.DonusAdresi)) return Redirect(form.DonusAdresi);

        // Personel ve admin doğrudan yönetim paneline gider
        var personelMi = await _kullanicilar.IsInRoleAsync(kullanici, Roller.Admin) || await _kullanicilar.IsInRoleAsync(kullanici, Roller.Personel);
        return personelMi ? RedirectToAction("Index", "Panel", new { area = "Yonetim" }) : RedirectToAction("Index", "AnaSayfa");
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

    /// <summary>Bu e-posta bir admin ya da personele mi ait? (Sunum modunda giriş izni için)</summary>
    private async Task<bool> PersonelMiAsync(string eposta)
    {
        var kullanici = await _kullanicilar.FindByEmailAsync(eposta);
        if (kullanici == null) return false;
        return await _kullanicilar.IsInRoleAsync(kullanici, Roller.Admin) || await _kullanicilar.IsInRoleAsync(kullanici, Roller.Personel);
    }
}
