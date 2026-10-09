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
        return kullanici == null ? NotFound() : View(kullanici);
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

[Authorize(Roles = Roller.Admin)]
public class PersonelController : YonetimController
{
    private readonly UserManager<Kullanici> _kullanicilar;
    public PersonelController(UserManager<Kullanici> kullanicilar) { _kullanicilar = kullanicilar; }

    public async Task<IActionResult> Index()
    {
        var liste = new List<(Kullanici Kullanici, string Rol)>();
        foreach (var rol in new[] { Roller.Admin, Roller.Personel })
            foreach (var k in await _kullanicilar.GetUsersInRoleAsync(rol)) liste.Add((k, rol));
        return View(liste);
    }

    public IActionResult Ekle() => View(new PersonelFormu());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(PersonelFormu form)
    {
        if (form.Rol != Roller.Admin && form.Rol != Roller.Personel) ModelState.AddModelError(nameof(form.Rol), "Geçersiz rol.");
        if (!ModelState.IsValid) return View(form);

        var kullanici = new Kullanici { UserName = form.Eposta, Email = form.Eposta, EmailConfirmed = true, Ad = form.Ad.Trim(), Soyad = form.Soyad.Trim() };
        var sonuc = await _kullanicilar.CreateAsync(kullanici, form.Sifre);
        if (!sonuc.Succeeded)
        {
            foreach (var hata in sonuc.Errors) ModelState.AddModelError("", hata.Description);
            return View(form);
        }
        await _kullanicilar.AddToRoleAsync(kullanici, form.Rol);
        Basarili($"{kullanici.TamAd} ({form.Rol}) hesabı oluşturuldu.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Admin ↔ Personel. Kişi kendi rolünü değiştiremez.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RolDegistir(string id)
    {
        if (id == KullaniciId) { Hata("Kendi rolünüzü değiştiremezsiniz."); return RedirectToAction(nameof(Index)); }
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        if (kullanici == null) return NotFound();

        var adminMi = await _kullanicilar.IsInRoleAsync(kullanici, Roller.Admin);
        await _kullanicilar.RemoveFromRoleAsync(kullanici, adminMi ? Roller.Admin : Roller.Personel);
        await _kullanicilar.AddToRoleAsync(kullanici, adminMi ? Roller.Personel : Roller.Admin);
        Basarili($"{kullanici.TamAd} artık {(adminMi ? Roller.Personel : Roller.Admin)}.");
        return RedirectToAction(nameof(Index));
    }
}
