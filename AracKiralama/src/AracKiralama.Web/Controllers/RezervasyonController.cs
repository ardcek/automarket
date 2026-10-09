// ============================================================================
// REZERVASYON (müşteri): 3 adımlı rezervasyon formu
//   /Rezervasyon/Yeni?aracId=5     → form
//   /Rezervasyon/Tamamlandi/12     → "talebiniz alındı" sayfası
// Kurallar Servisler/RezervasyonServisi.cs içinde; burası sadece formu alır ve servisi çağırır.
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Controllers;

[Authorize]   // giriş yapmamış kullanıcı giriş sayfasına yönlendirilir
public class RezervasyonController : TemelController
{
    private readonly VeriTabani _vt;
    private readonly RezervasyonServisi _rezervasyonlar;

    public RezervasyonController(VeriTabani vt, RezervasyonServisi rezervasyonlar)
    {
        _vt = vt;
        _rezervasyonlar = rezervasyonlar;
    }

    [HttpGet]
    public async Task<IActionResult> Yeni(int aracId, DateTime? baslangic, DateTime? bitis, int? alisSubeId, int? iadeSubeId, List<int>? ekstraIdleri)
    {
        var arac = await _vt.Araclar.FindAsync(aracId);
        if (arac == null || arac.Durum == AracDurumu.Pasif) return NotFound();

        // Varsayılan: yarın 10:00'da al, 3 gün sonra iade et
        var varsayilanBaslangic = DateTime.Today.AddDays(1).AddHours(10);
        var form = new RezervasyonFormu
        {
            AracId = aracId,
            Baslangic = baslangic ?? varsayilanBaslangic,
            Bitis = bitis ?? (baslangic ?? varsayilanBaslangic).AddDays(3),
            AlisSubeId = alisSubeId ?? arac.SubeId,
            IadeSubeId = iadeSubeId ?? alisSubeId ?? arac.SubeId,
            EkstraIdleri = ekstraIdleri ?? new(),
        };
        await FormuDoldurAsync(form);
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yeni(RezervasyonFormu form)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var rezervasyon = await _rezervasyonlar.OlusturAsync(KullaniciId, form.AracId, form.AlisSubeId, form.IadeSubeId,
                    form.Baslangic, form.Bitis, form.EkstraIdleri, form.Not);
                return RedirectToAction(nameof(Tamamlandi), new { id = rezervasyon.Id });
            }
            catch (IsKuraliHatasi hata)
            {
                ModelState.AddModelError("", hata.Message);   // formun üstünde kırmızı kutuda görünür
            }
        }
        await FormuDoldurAsync(form);
        if (form.Arac == null) return NotFound();   // araç silinmiş veya numara yanlış
        return View(form);
    }

    public async Task<IActionResult> Tamamlandi(int id)
    {
        var rezervasyon = await _vt.Rezervasyonlar
            .Include(r => r.Arac).ThenInclude(a => a!.Marka).Include(r => r.AlisSube).Include(r => r.IadeSube)
            .Include(r => r.Ekstralar).ThenInclude(e => e.Ekstra)
            .AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.MusteriId == KullaniciId);
        return rezervasyon == null ? NotFound() : View(rezervasyon);
    }

    /// <summary>Formda gösterilecek araç, şube, ekstra ve sürücü uygunluğu bilgilerini doldurur.</summary>
    private async Task FormuDoldurAsync(RezervasyonFormu form)
    {
        form.Arac = await _vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru)
            .AsNoTracking().FirstOrDefaultAsync(a => a.Id == form.AracId);
        form.Subeler = await _vt.Subeler.OrderBy(s => s.Sehir).ToListAsync();
        form.Ekstralar = await _vt.Ekstralar.Where(e => e.Aktif).OrderBy(e => e.GunlukFiyat).ToListAsync();
        form.Musteri = await _vt.Users.AsNoTracking().FirstOrDefaultAsync(k => k.Id == KullaniciId);
        if (form.Musteri != null && form.Arac != null)
            form.SurucuHatalari = RezervasyonServisi.SurucuHatalari(form.Musteri, form.Arac.Kategori!, form.Baslangic);
    }
}
