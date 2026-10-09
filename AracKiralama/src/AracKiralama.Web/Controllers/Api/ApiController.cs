// ============================================================================
// REST API (JSON). Swagger arayüzünden denenebilir: /swagger
//   GET  /api/araclar?baslangic=..&bitis=..&kategoriId=..  → araç listesi
//   GET  /api/araclar/5                                    → tek araç
//   POST /api/fiyat                                        → canlı fiyat hesabı
// Araç detay sayfasındaki ve rezervasyon formundaki canlı fiyat, /api/fiyat'ı kullanır
// (wwwroot/js/fiyat-hesapla.js).
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Controllers.Api;

/// <summary>API'nin dışarıya verdiği sade araç bilgisi.</summary>
public class AracBilgisi
{
    public int Id { get; set; }
    public string Marka { get; set; } = "";
    public string Model { get; set; } = "";
    public int Yil { get; set; }
    public string Kategori { get; set; } = "";
    public string Yakit { get; set; } = "";
    public string Vites { get; set; } = "";
    public int KoltukSayisi { get; set; }
    public decimal GunlukFiyat { get; set; }
    public string Sube { get; set; } = "";
    public string? GorselAdresi { get; set; }
}

public class FiyatIstegi
{
    public int AracId { get; set; }
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public int AlisSubeId { get; set; }
    public int IadeSubeId { get; set; }
    public List<int>? EkstraIdleri { get; set; }
}

public class FiyatCevabi
{
    public bool Musait { get; set; }
    public List<string> Hatalar { get; set; } = new();
    public FiyatTeklifi? Teklif { get; set; }
    public string? ToplamYazi { get; set; }     // "4.560 ₺" (ekranda doğrudan gösterilir)
    public string? DepozitoYazi { get; set; }
}

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly VeriTabani _vt;
    private readonly MusaitlikServisi _musaitlik;
    private readonly RezervasyonServisi _rezervasyonlar;
    private readonly SiteAyarlari _ayarlar;

    public ApiController(VeriTabani vt, MusaitlikServisi musaitlik, RezervasyonServisi rezervasyonlar, SiteAyarlari ayarlar)
    {
        _vt = vt;
        _musaitlik = musaitlik;
        _rezervasyonlar = rezervasyonlar;
        _ayarlar = ayarlar;
    }

    [HttpGet("araclar")]
    public async Task<List<AracBilgisi>> Araclar(DateTime? baslangic, DateTime? bitis, int? kategoriId, int? subeId)
    {
        var sorgu = _vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru).Include(a => a.Sube)
            .Where(a => a.Durum != AracDurumu.Pasif).AsNoTracking();
        if (kategoriId != null) sorgu = sorgu.Where(a => a.KategoriId == kategoriId);
        if (subeId != null) sorgu = sorgu.Where(a => a.SubeId == subeId);
        if (baslangic != null && bitis != null && bitis > baslangic) sorgu = _musaitlik.SadeceMusaitler(sorgu, baslangic.Value, bitis.Value);

        var araclar = await sorgu.OrderBy(a => a.GunlukFiyat).ToListAsync();
        return araclar.Select(Donustur).ToList();
    }

    [HttpGet("araclar/{id:int}")]
    public async Task<ActionResult<AracBilgisi>> Arac(int id)
    {
        var arac = await _vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori).Include(a => a.YakitTuru).Include(a => a.VitesTuru).Include(a => a.Sube)
            .AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.Durum != AracDurumu.Pasif);
        return arac == null ? NotFound() : Donustur(arac);
    }

    [HttpPost("fiyat")]
    public async Task<FiyatCevabi> Fiyat(FiyatIstegi istek)
    {
        var cevap = new FiyatCevabi { Hatalar = RezervasyonServisi.TarihHatalari(istek.Baslangic, istek.Bitis, DateTime.Now, _ayarlar.EnUzunKiralama) };
        if (cevap.Hatalar.Count > 0) return cevap;

        try
        {
            var (_, teklif) = await _rezervasyonlar.TeklifAsync(istek.AracId, istek.Baslangic, istek.Bitis, istek.EkstraIdleri ?? new(), istek.AlisSubeId, istek.IadeSubeId);
            cevap.Teklif = teklif;
            cevap.ToplamYazi = Bicim.Para(teklif.Toplam);
            cevap.DepozitoYazi = Bicim.Para(teklif.Depozito);
            cevap.Musait = await _musaitlik.MusaitMiAsync(istek.AracId, istek.Baslangic, istek.Bitis);
            if (!cevap.Musait) cevap.Hatalar.Add("Araç seçtiğiniz tarihlerde müsait değil. Lütfen farklı tarih deneyin.");
            if (!_ayarlar.RezervasyonaAcik) { cevap.Musait = false; cevap.Hatalar.Add("Şu anda yeni rezervasyon alınmamaktadır."); }
        }
        catch (IsKuraliHatasi hata)
        {
            cevap.Hatalar.Add(hata.Message);
        }
        return cevap;
    }

    private static AracBilgisi Donustur(Arac a) => new()
    {
        Id = a.Id, Marka = a.Marka!.Ad, Model = a.Model, Yil = a.Yil, Kategori = a.Kategori!.Ad,
        Yakit = a.YakitTuru!.Ad, Vites = a.VitesTuru!.Ad, KoltukSayisi = a.KoltukSayisi,
        GunlukFiyat = a.GunlukFiyat, Sube = a.Sube!.Ad, GorselAdresi = a.GorselAdresi,
    };
}
