// ============================================================================
// MÜŞTERİLER ve PERSONEL
//   /Yonetim/Musteriler  → bütün kullanıcılar (müşteri + personel), detay (geçmiş, ödemeler, hasarlar), kara liste
//   /Yonetim/Personel    → yetkiler, yeni kullanıcı, geçici şifre, sıfırlama e-postası, hesabı dondur / sil,
//                          e-posta gönderimi durumu + test (SADECE Admin ve Süper Admin)
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

public class MusterilerController : YonetimController
{
    private readonly VeriTabani _vt;
    private readonly UserManager<Kullanici> _kullanicilar;

    public MusterilerController(VeriTabani vt, UserManager<Kullanici> kullanicilar)
    {
        _vt = vt;
        _kullanicilar = kullanicilar;
    }

    public async Task<IActionResult> Index(string? arama, bool karaListe = false, string? rol = null)
    {
        // Her kullanıcının en yüksek rolü (Süper Admin > Admin > Personel > Müşteri)
        var roller = await RolleriAsync();
        var musteriler = (await _vt.Users.AsNoTracking().ToListAsync()).AsEnumerable();
        if (SuperAdminGizli) musteriler = musteriler.Where(k => roller.GetValueOrDefault(k.Id) != Roller.SuperAdmin);
        if (!string.IsNullOrEmpty(rol)) musteriler = musteriler.Where(k => roller.GetValueOrDefault(k.Id, Roller.Musteri) == rol);
        if (!string.IsNullOrWhiteSpace(arama))
            musteriler = musteriler.Where(k => k.TamAd.Contains(arama, StringComparison.CurrentCultureIgnoreCase)
                                           || (k.Email ?? "").Contains(arama, StringComparison.OrdinalIgnoreCase) || (k.PhoneNumber ?? "").Contains(arama));
        if (karaListe) musteriler = musteriler.Where(k => k.KaraListede);

        // Müşteri başına tamamlanan kiralama sayısı ve harcama
        var tamamlananlar = await _vt.Rezervasyonlar.Where(r => r.Durum == RezervasyonDurumu.Tamamlandi)
            .Select(r => new { r.MusteriId, r.ToplamTutar }).ToListAsync();
        var satirlar = musteriler.Select(k => new MusteriSatiri
        {
            Kullanici = k,
            Rol = roller.GetValueOrDefault(k.Id, Roller.Musteri),
            KiralamaSayisi = tamamlananlar.Count(r => r.MusteriId == k.Id),
            ToplamHarcama = tamamlananlar.Where(r => r.MusteriId == k.Id).Sum(r => r.ToplamTutar),
        }).OrderByDescending(s => s.ToplamHarcama).ToList();

        ViewBag.Arama = arama;
        ViewBag.KaraListe = karaListe;
        ViewBag.Rol = rol;
        return View(satirlar);
    }

    /// <summary>
    /// Sadece Personel olan biri Süper Admin hesaplarını görmez (listede çıkmaz, detayı açılmaz).
    /// Admin ve Süper Admin herkesi görür (Süper Admin, Program.cs'teki dönüşümle Admin de sayılır).
    /// </summary>
    private bool SuperAdminGizli => !User.IsInRole(Roller.Admin);

    /// <summary>Kullanıcı numarası → en yüksek rolü.</summary>
    private async Task<Dictionary<string, string>> RolleriAsync()
    {
        var sira = new[] { Roller.SuperAdmin, Roller.Admin, Roller.Personel, Roller.Musteri };
        var satirlar = await (from ur in _vt.UserRoles join r in _vt.Roles on ur.RoleId equals r.Id select new { ur.UserId, r.Name }).ToListAsync();
        return satirlar.GroupBy(s => s.UserId)
            .ToDictionary(g => g.Key, g => sira.FirstOrDefault(s => g.Any(x => x.Name == s)) ?? Roller.Musteri);
    }

    public async Task<IActionResult> Detay(string id)
    {
        // Kişinin bütün geçmişi: rezervasyonlar + araç, şubeler, ödemeler, hasarlar, ek hizmetler
        var kullanici = await _vt.Users
            .Include(k => k.Rezervasyonlar).ThenInclude(r => r.Arac).ThenInclude(a => a!.Marka)
            .Include(k => k.Rezervasyonlar).ThenInclude(r => r.AlisSube)
            .Include(k => k.Rezervasyonlar).ThenInclude(r => r.IadeSube)
            .Include(k => k.Rezervasyonlar).ThenInclude(r => r.Odemeler)
            .Include(k => k.Rezervasyonlar).ThenInclude(r => r.Hasarlar)
            .Include(k => k.Rezervasyonlar).ThenInclude(r => r.Ekstralar).ThenInclude(e => e.Ekstra)
            .AsSplitQuery().AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
        if (kullanici == null) return NotFound();
        ViewBag.Rol = (await RolleriAsync()).GetValueOrDefault(id, Roller.Musteri);
        if (SuperAdminGizli && ViewBag.Rol == Roller.SuperAdmin) return NotFound();

        // Panelden eklenen bilgiler (Meslek vb.): SELECT a."Ad", d."Deger" FROM "MusteriAlanDegerleri" d JOIN "MusteriAlanlari" a ... WHERE d."KullaniciId" = @id
        ViewBag.MusteriBilgileri = await _vt.MusteriAlanDegerleri.Where(d => d.KullaniciId == id)
            .OrderBy(d => d.Alan!.Sira).Select(d => new KeyValuePair<string, string>(d.Alan!.Ad, d.Deger)).ToListAsync();
        return View(kullanici);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KaraListe(string id, string? sebep)
    {
        var kullanici = await _vt.Users.FindAsync(id);
        if (kullanici == null) return NotFound();
        if (SuperAdminGizli && (await RolleriAsync()).GetValueOrDefault(id) == Roller.SuperAdmin) return NotFound();
        kullanici.KaraListede = !kullanici.KaraListede;
        kullanici.KaraListeSebebi = kullanici.KaraListede ? (string.IsNullOrWhiteSpace(sebep) ? "Belirtilmedi" : sebep.Trim()) : null;
        await _vt.SaveChangesAsync();
        Basarili(kullanici.KaraListede ? $"{kullanici.TamAd} kara listeye alındı." : $"{kullanici.TamAd} kara listeden çıkarıldı.");
        return RedirectToAction(nameof(Detay), new { id });
    }
}

/// <summary>
/// PERSONEL VE ROLLER (Admin ve Süper Admin görür).
/// Kim ne verebilir: Roller.Verebilecegi → Süper Admin: Süper Admin / Admin / Personel · Admin: Admin / Personel.
/// Herkesi Müşteri'ye (yetkisiz) çevirebilir. Admin, Süper Admin hesaplarını değiştiremez. Kimse kendi hesabına dokunamaz.
/// Hesap işlemleri: yeni kullanıcı (geçici şifreyle), geçici şifre ver, hesabı dondur / aç, hesabı sil.
/// İşlem sonrası "donus" alanı varsa oraya (örn. müşteri detayı), yoksa bu listeye dönülür.
/// </summary>
[Authorize(Roles = Roller.Admin)]
public class PersonelController : YonetimController
{
    private readonly UserManager<Kullanici> _kullanicilar;
    public PersonelController(UserManager<Kullanici> kullanicilar) { _kullanicilar = kullanicilar; }

    public async Task<IActionResult> Index()
    {
        // Her kişi bir kez, en yüksek rolüyle listelenir (Süper Admin > Admin > Personel)
        var liste = new List<(Kullanici Kullanici, string Rol)>();
        foreach (var rol in new[] { Roller.SuperAdmin, Roller.Admin, Roller.Personel })
            foreach (var k in await _kullanicilar.GetUsersInRoleAsync(rol))
                if (liste.All(x => x.Kullanici.Id != k.Id)) liste.Add((k, rol));
        return View(liste);
    }

    public IActionResult Ekle() => View(new PersonelFormu());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(PersonelFormu form)
    {
        if (form.Rol != Roller.Musteri && !Roller.Verebilecegi(User).Contains(form.Rol)) ModelState.AddModelError(nameof(form.Rol), "Bu yetkiyi veremezsiniz.");
        if (!ModelState.IsValid) return View(form);

        // Geçici şifre: kişi ilk girişte kendi şifresini belirler (Yardimcilar/SifreKontrolu.cs)
        var eposta = form.Eposta.Trim().ToLowerInvariant();
        var kullanici = new Kullanici { UserName = eposta, Email = eposta, EmailConfirmed = true, Ad = form.Ad.Trim(), Soyad = form.Soyad.Trim(), SifreDegistirmeli = true };
        var sonuc = await _kullanicilar.CreateAsync(kullanici, form.Sifre);
        if (!sonuc.Succeeded)
        {
            foreach (var hata in sonuc.Errors) ModelState.AddModelError("", hata.Description);
            return View(form);
        }
        await _kullanicilar.AddToRoleAsync(kullanici, form.Rol);
        Basarili($"{kullanici.TamAd} ({Roller.Ad(form.Rol)}) hesabı oluşturuldu. İlk girişte geçici şifreyle girip kendi şifresini belirleyecek.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sitede zaten üye olan birine e-postasıyla yetki verir.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YetkiVer(string eposta, string rol)
    {
        var kullanici = string.IsNullOrWhiteSpace(eposta) ? null : await _kullanicilar.FindByEmailAsync(eposta.Trim());
        if (kullanici == null) { Hata($"\"{eposta}\" e-postasıyla üye bulunamadı. Önce sitede üye olması gerekir."); return RedirectToAction(nameof(Index)); }
        return await RolVerAsync(kullanici, rol);
    }

    /// <summary>Listedeki kişinin yetkisini seçilen role çevirir.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RolDegistir(string id, string rol)
    {
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        if (kullanici == null) { Hata("Kullanıcı bulunamadı."); return Geri(); }
        return await RolVerAsync(kullanici, rol);
    }

    /// <summary>Yetkiyi geri alır: kişi normal müşteri olur (hesabı silinmez).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YetkiAl(string id)
    {
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        var engel = kullanici == null ? "Kullanıcı bulunamadı." : await EngelAsync(kullanici);
        if (engel != null) { Hata(engel); return Geri(); }

        await RolleriAyarlaAsync(kullanici!, Roller.Musteri);
        Basarili($"{kullanici!.TamAd} artık panele giremez (müşteri).");
        return Geri();
    }

    /// <summary>Ortak kontroller + rol verme. Verilen rol, verenin verebileceklerinden (ya da Müşteri) olmalı.</summary>
    private async Task<IActionResult> RolVerAsync(Kullanici kullanici, string rol)
    {
        var engel = rol != Roller.Musteri && !Roller.Verebilecegi(User).Contains(rol)
            ? (User.IsInRole(Roller.SuperAdmin) ? "Geçersiz yetki." : "Admin sadece Admin ya da Personel yetkisi verebilir.")
            : await EngelAsync(kullanici);
        if (engel != null) { Hata(engel); return Geri(); }

        await RolleriAyarlaAsync(kullanici, rol);
        Basarili($"{kullanici.TamAd} artık {Roller.Ad(rol)}. Yetkinin görünmesi için bir kez çıkış yapıp tekrar girmesi gerekir.");
        return Geri();
    }

    /// <summary>
    /// Geçici şifre verir: rastgele bir şifre üretilir, ekranda BİR KEZ gösterilir (admin kişiye iletir).
    /// Kişi bu şifreyle girince kendi şifresini belirlemeden devam edemez. Açık oturumları kapanır.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GeciciSifre(string id)
    {
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        var engel = kullanici == null ? "Kullanıcı bulunamadı." : await EngelAsync(kullanici);
        if (engel != null) { Hata(engel); return Geri(); }

        var sifre = GeciciSifreUret();
        var kod = await _kullanicilar.GeneratePasswordResetTokenAsync(kullanici!);
        var sonuc = await _kullanicilar.ResetPasswordAsync(kullanici!, kod, sifre);   // güvenlik damgası da yenilenir → açık oturumlar kapanır
        if (!sonuc.Succeeded) { Hata(string.Join(" ", sonuc.Errors.Select(h => h.Description))); return Geri(); }
        kullanici!.SifreDegistirmeli = true;
        await _kullanicilar.UpdateAsync(kullanici);
        TempData["GeciciSifre"] = sifre;
        TempData["GeciciSifreKime"] = $"{kullanici.TamAd} ({kullanici.Email})";
        return Geri();
    }

    /// <summary>
    /// Kişiye "şifre sıfırlama" e-postası gönderir (Müşteri detayı → Hesap işlemleri). Kişi şifresini kendisi belirler;
    /// geçici şifreden farkı: admin şifreyi hiç görmez. E-posta ayarı yoksa uyarır (o zaman "Geçici şifre ver" kullanılır).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SifirlamaGonder(string id, [FromServices] EpostaServisi eposta, [FromServices] SiteAyarlari ayarlar)
    {
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        var engel = kullanici == null ? "Kullanıcı bulunamadı." : await EngelAsync(kullanici);
        if (engel != null) { Hata(engel); return Geri(); }
        if (!eposta.Ayarli) { Hata("E-posta gönderimi ayarlı değil (Personel ve Roller → E-posta gönderimi). Bunun yerine geçici şifre verin."); return Geri(); }

        var kod = await _kullanicilar.GeneratePasswordResetTokenAsync(kullanici!);
        var link = Url.Action("SifreSifirla", "Hesap", new { area = "", eposta = kullanici!.Email, kod }, Request.Scheme)!;
        if (await eposta.SifirlamaLinkiGonderAsync(kullanici.Email!, kullanici.Ad, link, ayarlar.FirmaAdi))
            Basarili($"Şifre sıfırlama bağlantısı {kullanici.Email} adresine gönderildi (2 saat geçerli).");
        else Hata($"E-posta gönderilemedi: {EpostaServisi.SonDeneme?.Mesaj}");
        return Geri();
    }

    /// <summary>E-posta ayarını denemek için yazılan adrese test e-postası gönderir (Personel ve Roller → E-posta gönderimi).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EpostaDene(string? alici, [FromServices] EpostaServisi eposta, [FromServices] SiteAyarlari ayarlar)
    {
        alici = alici?.Trim() ?? "";
        if (alici.Length == 0 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(alici))
        {
            Hata("Geçerli bir e-posta adresi yazın.");
            return RedirectToAction(nameof(Index));
        }
        var html = $"<p>Bu bir test e-postasıdır. \"Şifremi unuttum\" e-postaları bu ayarla gönderilir.</p><p>{System.Net.WebUtility.HtmlEncode(eposta.Yontem)}</p>";
        if (await eposta.GonderAsync(alici, $"{ayarlar.FirmaAdi} · Test e-postası", html, "Bu bir test e-postasıdır."))
            Basarili($"Test e-postası {alici} adresine gönderildi. Gelen kutusunu (ve gereksiz klasörünü) kontrol edin.");
        else Hata($"Gönderilemedi: {EpostaServisi.SonDeneme?.Mesaj}");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Hesabı dondurur (giriş yapamaz) ya da çözer. Kayıtları silinmez.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Dondur(string id)
    {
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        var engel = kullanici == null ? "Kullanıcı bulunamadı." : await EngelAsync(kullanici);
        if (engel != null) { Hata(engel); return Geri(); }

        var donukMu = await _kullanicilar.IsLockedOutAsync(kullanici!);
        await _kullanicilar.SetLockoutEnabledAsync(kullanici!, true);
        await _kullanicilar.SetLockoutEndDateAsync(kullanici!, donukMu ? null : DateTimeOffset.MaxValue);
        if (!donukMu) await _kullanicilar.UpdateSecurityStampAsync(kullanici!);   // açık oturumu da kapansın
        Basarili(donukMu ? $"{kullanici!.TamAd} hesabı yeniden açıldı." : $"{kullanici!.TamAd} hesabı donduruldu; giriş yapamaz.");
        return Geri();
    }

    /// <summary>
    /// Hesabı tamamen siler. Rezervasyonu olan hesap silinemez (kiralama ve ödeme kayıtları kaybolmasın):
    /// onun yerine dondurun.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(string id, [FromServices] VeriTabani vt)
    {
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        var engel = kullanici == null ? "Kullanıcı bulunamadı." : await EngelAsync(kullanici);
        if (engel == null && await vt.Rezervasyonlar.AnyAsync(r => r.MusteriId == id))
            engel = "Bu kişinin rezervasyon kayıtları var; silinirse kiralama geçmişi kaybolur. Bunun yerine hesabı dondurun.";
        if (engel != null) { Hata(engel); return Geri(); }

        // Kişiye bağlı küçük kayıtlar: panelden eklenen bilgileri, personelse yaptığı işlemlerdeki adı
        await vt.MusteriAlanDegerleri.Where(d => d.KullaniciId == id).ExecuteDeleteAsync();
        await vt.Rezervasyonlar.Where(r => r.IslemYapanId == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.IslemYapanId, (string?)null));
        await vt.Odemeler.Where(o => o.AlanKullaniciId == id).ExecuteUpdateAsync(s => s.SetProperty(o => o.AlanKullaniciId, (string?)null));
        await _kullanicilar.DeleteAsync(kullanici!);
        Basarili($"{kullanici!.TamAd} ({kullanici.Email}) hesabı silindi.");
        // Silinen kişinin detay sayfasına geri dönülmez
        return Request.Form["donus"].ToString().Contains("/Musteriler/Detay/") ? Redirect("/Yonetim/Musteriler") : Geri();
    }

    /// <summary>İşlemden sonra formdaki "donus" adresine (örn. müşteri detayı) ya da Personel listesine döner.</summary>
    private IActionResult Geri()
    {
        var donus = Request.HasFormContentType ? Request.Form["donus"].ToString() : "";
        return !string.IsNullOrEmpty(donus) && Url.IsLocalUrl(donus) ? Redirect(donus) : RedirectToAction(nameof(Index));
    }

    /// <summary>Okunması kolay geçici şifre: "kemr-4827" gibi (harf + rakam; şifre kurallarına uyar).</summary>
    private static string GeciciSifreUret()
    {
        const string harfler = "abcdefghjkmnprstuvyz";   // birbirine benzeyen harfler (l, o, i) yok
        var harf = string.Concat(Enumerable.Range(0, 4).Select(_ => harfler[System.Security.Cryptography.RandomNumberGenerator.GetInt32(harfler.Length)]));
        return harf + "-" + System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 10000);
    }

    /// <summary>Bu kişinin yetkisi değiştirilebilir mi? Değilse sebebini döndürür.</summary>
    private async Task<string?> EngelAsync(Kullanici kullanici)
    {
        if (kullanici.Id == KullaniciId) return "Kendi yetkinizi değiştiremezsiniz.";
        if (!User.IsInRole(Roller.SuperAdmin) && await _kullanicilar.IsInRoleAsync(kullanici, Roller.SuperAdmin))
            return "Süper Admin hesaplarını sadece bir Süper Admin değiştirebilir.";
        return null;
    }

    /// <summary>Müşteri / Personel / Admin / Süper Admin rollerinden sadece verileni bırakır.</summary>
    private async Task RolleriAyarlaAsync(Kullanici kullanici, string rol)
    {
        foreach (var eski in new[] { Roller.Musteri, Roller.Personel, Roller.Admin, Roller.SuperAdmin })
            if (eski != rol && await _kullanicilar.IsInRoleAsync(kullanici, eski)) await _kullanicilar.RemoveFromRoleAsync(kullanici, eski);
        if (!await _kullanicilar.IsInRoleAsync(kullanici, rol)) await _kullanicilar.AddToRoleAsync(kullanici, rol);
    }
}
