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
/// Buradan sadece Admin / Personel yetkisi verilir (Roller.PaneldenVerilebilir). Süper Admin hesapları
/// listede görünür ama değiştirilemez; Süper Admin yetkisi sadece veritabanından verilir (README → "Süper Admin").
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
        if (!Roller.PaneldenVerilebilir.Contains(form.Rol)) ModelState.AddModelError(nameof(form.Rol), "Geçersiz rol.");
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

    /// <summary>Sitede zaten üye olan birine e-postasıyla Admin ya da Personel yetkisi verir.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YetkiVer(string eposta, string rol)
    {
        if (!Roller.PaneldenVerilebilir.Contains(rol)) { Hata("Panelden sadece Admin ya da Personel yetkisi verilebilir."); return RedirectToAction(nameof(Index)); }
        var kullanici = string.IsNullOrWhiteSpace(eposta) ? null : await _kullanicilar.FindByEmailAsync(eposta.Trim());
        if (kullanici == null) { Hata($"\"{eposta}\" e-postasıyla üye bulunamadı. Önce sitede üye olması gerekir."); return RedirectToAction(nameof(Index)); }
        if (kullanici.Id == KullaniciId) { Hata("Kendi yetkinizi değiştiremezsiniz."); return RedirectToAction(nameof(Index)); }
        if (await _kullanicilar.IsInRoleAsync(kullanici, Roller.SuperAdmin)) { Hata("Süper Admin hesapları panelden değiştirilemez."); return RedirectToAction(nameof(Index)); }

        await RolleriAyarlaAsync(kullanici, rol);
        Basarili($"{kullanici.TamAd} artık {rol}. Yetkinin görünmesi için bir kez çıkış yapıp tekrar girmesi gerekir.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Admin ↔ Personel. Kişi kendi rolünü, kimse Süper Admin'in rolünü değiştiremez.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RolDegistir(string id)
    {
        var (kullanici, hata) = await DegistirilebilirMiAsync(id);
        if (kullanici == null) { Hata(hata!); return RedirectToAction(nameof(Index)); }

        var yeni = await _kullanicilar.IsInRoleAsync(kullanici, Roller.Admin) ? Roller.Personel : Roller.Admin;
        await RolleriAyarlaAsync(kullanici, yeni);
        Basarili($"{kullanici.TamAd} artık {yeni}.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Yetkiyi geri alır: kişi normal müşteri olur (hesabı silinmez).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YetkiAl(string id)
    {
        var (kullanici, hata) = await DegistirilebilirMiAsync(id);
        if (kullanici == null) { Hata(hata!); return RedirectToAction(nameof(Index)); }

        await RolleriAyarlaAsync(kullanici, Roller.Musteri);
        Basarili($"{kullanici.TamAd} artık panele giremez (müşteri).");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Kendisi ya da Süper Admin ise değiştirilemez.</summary>
    private async Task<(Kullanici?, string?)> DegistirilebilirMiAsync(string id)
    {
        if (id == KullaniciId) return (null, "Kendi rolünüzü değiştiremezsiniz.");
        var kullanici = await _kullanicilar.FindByIdAsync(id);
        if (kullanici == null) return (null, "Kullanıcı bulunamadı.");
        if (await _kullanicilar.IsInRoleAsync(kullanici, Roller.SuperAdmin)) return (null, "Süper Admin hesapları panelden değiştirilemez.");
        return (kullanici, null);
    }

    /// <summary>Müşteri / Personel / Admin rollerinden sadece verileni bırakır (Süper Admin'e asla dokunmaz).</summary>
    private async Task RolleriAyarlaAsync(Kullanici kullanici, string rol)
    {
        foreach (var eski in new[] { Roller.Musteri, Roller.Personel, Roller.Admin })
            if (eski != rol && await _kullanicilar.IsInRoleAsync(kullanici, eski)) await _kullanicilar.RemoveFromRoleAsync(kullanici, eski);
        if (!await _kullanicilar.IsInRoleAsync(kullanici, rol)) await _kullanicilar.AddToRoleAsync(kullanici, rol);
    }
}
