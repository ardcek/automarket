// ============================================================================
// GÖRSEL SERVİSİ: admin panelinden yüklenen resimleri VERİTABANINA kaydeder.
// Dosya sistemine değil veritabanına yazdığımız için sunucu yeniden başlasa da
// görseller kaybolmaz. Tarayıcı resmi /gorsel/{id} adresinden alır (GorselController).
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;

namespace AracKiralama.Servisler;

public class GorselServisi
{
    private const long EnBuyukBoyut = 3 * 1024 * 1024;   // 3 MB
    // SVG bilerek yok: içine script gömülebildiği için güvenlik riski taşır.
    private static readonly string[] IzinliUzantilar = [".jpg", ".jpeg", ".png", ".webp"];
    private readonly VeriTabani _vt;

    public GorselServisi(VeriTabani vt)
    {
        _vt = vt;
    }

    /// <summary>Görseli kaydeder ve sitede kullanılacak adresini ("/gorsel/12") döndürür.</summary>
    public async Task<string> KaydetAsync(IFormFile dosya)
    {
        if (dosya.Length == 0) throw new IsKuraliHatasi("Boş dosya yüklenemez.");
        if (dosya.Length > EnBuyukBoyut) throw new IsKuraliHatasi("Görsel en fazla 3 MB olabilir.");

        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        if (!IzinliUzantilar.Contains(uzanti) || !dosya.ContentType.StartsWith("image/"))
            throw new IsKuraliHatasi("Sadece JPG, PNG veya WEBP görsel yükleyebilirsiniz.");

        using var bellek = new MemoryStream();
        await dosya.CopyToAsync(bellek);
        var veri = bellek.ToArray();

        // Uzantı ve tarayıcının söylediği tür taklit edilebilir; dosyanın ilk baytlarına (imzasına) bakarız.
        var gercekTur = ResimTuru(veri) ?? throw new IsKuraliHatasi("Dosya geçerli bir JPG, PNG veya WEBP görseli değil.");

        var gorsel = new Gorsel { DosyaAdi = dosya.FileName, IcerikTuru = gercekTur, Veri = veri };
        _vt.Gorseller.Add(gorsel);
        await _vt.SaveChangesAsync();
        return $"/gorsel/{gorsel.Id}";
    }

    /// <summary>Her resim biçimi belirli baytlarla başlar. Tanınmazsa null döner.</summary>
    public static string? ResimTuru(byte[] veri)
    {
        if (veri.Length >= 3 && veri[0] == 0xFF && veri[1] == 0xD8 && veri[2] == 0xFF) return "image/jpeg";              // JPG
        if (veri.Length >= 4 && veri[0] == 0x89 && veri[1] == 'P' && veri[2] == 'N' && veri[3] == 'G') return "image/png";  // PNG
        if (veri.Length >= 12 && veri[0] == 'R' && veri[1] == 'I' && veri[2] == 'F' && veri[3] == 'F'
            && veri[8] == 'W' && veri[9] == 'E' && veri[10] == 'B' && veri[11] == 'P') return "image/webp";                // WEBP
        return null;
    }
}
