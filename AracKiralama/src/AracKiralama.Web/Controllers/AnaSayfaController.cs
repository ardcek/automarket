// ============================================================================
// ANA SAYFA + İÇERİK SAYFALARI
//   /                  → ana sayfa (bölümleri Yönetim → Ayarlar → Ana Sayfa'dan açılıp kapanır)
//   /Sayfa/hakkimizda  → admin panelinde yazılan sayfalar (Yönetim → Sayfalar)
//   /gorsel/5          → veritabanındaki yüklenmiş görsel
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Veri;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Controllers;

public class AnaSayfaController : TemelController
{
    private readonly VeriTabani _vt;
    private readonly SiteAyarlari _ayarlar;

    public AnaSayfaController(VeriTabani vt, SiteAyarlari ayarlar)
    {
        _vt = vt;
        _ayarlar = ayarlar;
    }

    public async Task<IActionResult> Index()
    {
        var araclar = await _vt.Araclar
            .Include(a => a.Marka).Include(a => a.Sube).Include(a => a.YakitTuru).Include(a => a.VitesTuru).Include(a => a.Kategori)
            .Where(a => a.Durum != AracDurumu.Pasif)
            .AsNoTracking().ToListAsync();

        var model = new AnaSayfaModeli
        {
            AracSayisi = araclar.Count,
            TamamlananKiralama = await _vt.Rezervasyonlar.CountAsync(r => r.Durum == RezervasyonDurumu.Tamamlandi),
            Subeler = await _vt.Subeler.OrderBy(s => s.Sehir).AsNoTracking().ToListAsync(),

            // Admin'in "öne çıkar" dediği araçlar (Yönetim → Araçlar → Düzenle)
            OneCikanlar = araclar.Where(a => a.OneCikan).OrderBy(a => a.GunlukFiyat).Take(_ayarlar.OneCikanAracSayisi).ToList(),
        };

        // Her kategori için araç sayısı ve en düşük fiyat
        foreach (var kategori in await _vt.Kategoriler.OrderBy(k => k.Sira).AsNoTracking().ToListAsync())
        {
            var kategoridekiler = araclar.Where(a => a.KategoriId == kategori.Id).ToList();
            if (kategoridekiler.Count == 0) continue;
            model.Kategoriler.Add(new KategoriOzeti
            {
                Kategori = kategori,
                AracSayisi = kategoridekiler.Count,
                EnDusukFiyat = kategoridekiler.Min(a => a.GunlukFiyat),
            });
        }

        return View(model);
    }

    /// <summary>Admin panelinde yazılan bir sayfa (Hakkımızda, KVKK...).</summary>
    [Route("Sayfa/{adres}")]
    public async Task<IActionResult> Sayfa(string adres)
    {
        var sayfa = await _vt.Sayfalar.AsNoTracking().FirstOrDefaultAsync(s => s.Adres == adres && s.Aktif);
        return sayfa == null ? NotFound() : View(sayfa);
    }

    /// <summary>Yüklenen görseli veritabanından okuyup tarayıcıya gönderir.</summary>
    [Route("gorsel/{id:int}")]
    [ResponseCache(Duration = 86400)]   // tarayıcı 1 gün önbellekte tutsun
    public async Task<IActionResult> Gorsel(int id)
    {
        var gorsel = await _vt.Gorseller.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        return gorsel == null ? NotFound() : File(gorsel.Veri, gorsel.IcerikTuru);
    }

    /// <summary>404 gibi hata kodları için özel sayfa (Program.cs → UseStatusCodePagesWithReExecute).</summary>
    public IActionResult Durum(int kod) => View(kod);

    public IActionResult Hata() => View();
}
