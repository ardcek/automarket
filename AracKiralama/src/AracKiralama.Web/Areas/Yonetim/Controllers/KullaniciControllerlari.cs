// ============================================================================
// MÜŞTERİLER ve PERSONEL
//   /Yonetim/Musteriler  → müşteri listesi, detay, kara liste
//   /Yonetim/Personel    → personel / admin hesapları ve roller (SADECE Admin görür)
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
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

    public async Task<IActionResult> Index(string? arama, bool karaListe = false)
    {
        var musteriler = (await _kullanicilar.GetUsersInRoleAsync(Roller.Musteri)).AsEnumerable();
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
            KiralamaSayisi = tamamlananlar.Count(r => r.MusteriId == k.Id),
            ToplamHarcama = tamamlananlar.Where(r => r.MusteriId == k.Id).Sum(r => r.ToplamTutar),
        }).OrderByDescending(s => s.ToplamHarcama).ToList();

        ViewBag.Arama = arama;
        ViewBag.KaraListe = karaListe;
        return View(satirlar);
    }

    public async Task<IActionResult> Detay(string id)
    {
        var kullanici = await _vt.Users.Include(k => k.Rezervasyonlar).ThenInclude(r => r.Arac).ThenInclude(a => a!.Marka)
            .AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
        if (kullanici == null) return NotFound();

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
/// Admin, Süper Admin hesaplarını değiştiremez. Kimse kendi yetkisini değiştiremez (kendini kilitlemesin diye).
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
        if (!Roller.Verebilecegi(User).Contains(form.Rol)) ModelState.AddModelError(nameof(form.Rol), "Bu yetkiyi veremezsiniz.");
        if (!ModelState.IsValid) return View(form);

        var kullanici = new Kullanici { UserName = form.Eposta, Email = form.Eposta, EmailConfirmed = true, Ad = form.Ad.Trim(), Soyad = form.Soyad.Trim() };
        var sonuc = await _kullanicilar.CreateAsync(kullanici, form.Sifre);
        if (!sonuc.Succeeded)
        {
            foreach (var hata in sonuc.Errors) ModelState.AddModelError("", hata.Description);
            return View(form);
        }
        await _kullanicilar.AddToRoleAsync(kullanici, form.Rol);
        Basarili($"{kullanici.TamAd} ({Roller.Ad(form.Rol)}) hesabı oluşturuldu.");
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
        if (kullanici == null) { Hata("Kullanıcı bulunamadı."); return RedirectToAction(nameof(Index)); }
        return await RolVerAsync(kullanici, rol);
    }

    /// <summary>Yetkiyi geri alır: kişi normal müşteri olur (hesabı silinmez).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YetkiAl(string id)
    {
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        var engel = kullanici == null ? "Kullanıcı bulunamadı." : await EngelAsync(kullanici);
        if (engel != null) { Hata(engel); return RedirectToAction(nameof(Index)); }

        await RolleriAyarlaAsync(kullanici!, Roller.Musteri);
        Basarili($"{kullanici!.TamAd} artık panele giremez (müşteri).");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Ortak kontroller + rol verme. Verilen rol, verenin verebileceklerinden olmalı.</summary>
    private async Task<IActionResult> RolVerAsync(Kullanici kullanici, string rol)
    {
        var engel = !Roller.Verebilecegi(User).Contains(rol)
            ? (User.IsInRole(Roller.SuperAdmin) ? "Geçersiz yetki." : "Admin sadece Admin ya da Personel yetkisi verebilir.")
            : await EngelAsync(kullanici);
        if (engel != null) { Hata(engel); return RedirectToAction(nameof(Index)); }

        await RolleriAyarlaAsync(kullanici, rol);
        Basarili($"{kullanici.TamAd} artık {Roller.Ad(rol)}. Yetkinin görünmesi için bir kez çıkış yapıp tekrar girmesi gerekir.");
        return RedirectToAction(nameof(Index));
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
