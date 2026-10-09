// ============================================================================
// PROFİL ("Hesabım"): rezervasyonlarım, profil/ehliyet bilgileri, iptal, sözleşme
//   /Profil  ·  /Profil/Duzenle  ·  /Profil/IptalEt/5 (POST)  ·  /Profil/Sozlesme/5
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

namespace AracKiralama.Controllers;

[Authorize]
public class ProfilController : TemelController
{
    private readonly VeriTabani _vt;
    private readonly UserManager<Kullanici> _kullanicilar;
    private readonly RezervasyonServisi _rezervasyonlar;
    private readonly SozlesmeServisi _sozlesme;

    public ProfilController(VeriTabani vt, UserManager<Kullanici> kullanicilar, RezervasyonServisi rezervasyonlar, SozlesmeServisi sozlesme)
    {
        _vt = vt;
        _kullanicilar = kullanicilar;
        _rezervasyonlar = rezervasyonlar;
        _sozlesme = sozlesme;
    }

    public async Task<IActionResult> Index()
    {
        var kullanici = await _kullanicilar.GetUserAsync(User);
        if (kullanici == null) return Challenge();

        var hepsi = await _vt.Rezervasyonlar.Where(r => r.MusteriId == kullanici.Id)
            .Include(r => r.Arac).ThenInclude(a => a!.Marka).Include(r => r.AlisSube).Include(r => r.IadeSube)
            .OrderByDescending(r => r.BaslangicTarihi).AsNoTracking().ToListAsync();

        var aktifDurumlar = new[] { RezervasyonDurumu.OnayBekliyor, RezervasyonDurumu.Onaylandi, RezervasyonDurumu.Kirada };
        return View(new ProfilSayfasi
        {
            Kullanici = kullanici,
            Aktifler = hepsi.Where(r => aktifDurumlar.Contains(r.Durum)).OrderBy(r => r.BaslangicTarihi).ToList(),
            Gecmis = hepsi.Where(r => !aktifDurumlar.Contains(r.Durum)).ToList(),
        });
    }

    [HttpGet]
    public async Task<IActionResult> Duzenle(string? donusAdresi)
    {
        var k = await _kullanicilar.GetUserAsync(User);
        if (k == null) return Challenge();
        return View(new ProfilFormu
        {
            Ad = k.Ad, Soyad = k.Soyad, Eposta = k.Email, Telefon = k.PhoneNumber, TcNo = k.TcNo,
            DogumTarihi = k.DogumTarihi, EhliyetNo = k.EhliyetNo, EhliyetTarihi = k.EhliyetTarihi, DonusAdresi = donusAdresi,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(ProfilFormu form)
    {
        if (form.DogumTarihi > DateTime.Today.AddYears(-18)) ModelState.AddModelError(nameof(form.DogumTarihi), "18 yaşından küçükler kayıt olamaz.");
        if (form.EhliyetTarihi > DateTime.Today) ModelState.AddModelError(nameof(form.EhliyetTarihi), "Ehliyet tarihi gelecekte olamaz.");

        var k = await _kullanicilar.GetUserAsync(User);
        if (k == null) return Challenge();
        if (!ModelState.IsValid) { form.Eposta = k.Email; return View(form); }

        k.Ad = form.Ad.Trim();
        k.Soyad = form.Soyad.Trim();
        k.PhoneNumber = form.Telefon;
        k.TcNo = string.IsNullOrWhiteSpace(form.TcNo) ? null : form.TcNo;
        k.DogumTarihi = form.DogumTarihi;
        k.EhliyetNo = form.EhliyetNo;
        k.EhliyetTarihi = form.EhliyetTarihi;
        await _kullanicilar.UpdateAsync(k);

        Basarili("Profil bilgileriniz güncellendi.");
        if (!string.IsNullOrEmpty(form.DonusAdresi) && Url.IsLocalUrl(form.DonusAdresi)) return Redirect(form.DonusAdresi);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IptalEt(int id)
    {
        try
        {
            await _rezervasyonlar.IptalEtAsync(id, KullaniciId, personelMi: false);
            var ucret = await _vt.Rezervasyonlar.Where(r => r.Id == id).Select(r => r.IptalUcreti).FirstAsync();
            Basarili(ucret > 0 ? $"Rezervasyonunuz iptal edildi. Alış saatine az kaldığı için {Bicim.Para(ucret)} iptal ücreti uygulanır." : "Rezervasyonunuz ücretsiz olarak iptal edildi.");
        }
        catch (IsKuraliHatasi hata)
        {
            Hata(hata.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Müşteri sadece KENDİ rezervasyonunun sözleşmesini açabilir.</summary>
    public async Task<IActionResult> Sozlesme(int id)
    {
        var rezervasyon = await _vt.Rezervasyonlar.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.MusteriId == KullaniciId);
        if (rezervasyon == null) return NotFound();
        var pdf = await _sozlesme.OlusturAsync(id);
        return pdf == null ? NotFound() : PdfGoster(pdf, $"Sozlesme-{rezervasyon.Kod}.pdf");
    }
}
