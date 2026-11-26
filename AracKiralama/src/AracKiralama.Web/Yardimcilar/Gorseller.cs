// ============================================================================
// KÜÇÜK GÖRSELLER (araç kartları, kategoriler, rezervasyon özeti)
// wwwroot/img/cars/ altındaki her fotoğrafın 720 px genişliğinde hafif bir kopyası
// wwwroot/img/cars/k/ klasöründe .webp olarak durur (büyük fotoğrafın ~onda biri).
// Kartlar küçük kopyayı gösterir, sayfa hızlı açılır; detay sayfası büyük fotoğrafı gösterir.
// Küçük kopyası olmayan görsel (örn. panelden yüklenen) olduğu gibi kullanılır, hiçbir şey bozulmaz.
//
// View'da:  <img src="@Gorseller.Kucuk(adres)" srcset="@Gorseller.Secenekler(adres)" sizes="..." />
// ============================================================================
namespace AracKiralama.Yardimcilar;

public static class Gorseller
{
    private const string Klasor = "/img/cars/";
    private static HashSet<string> _kucukler = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Program.cs'te uygulama açılırken bir kez çağrılır: hangi küçük kopyalar var?</summary>
    public static void Yukle(string? wwwroot)
    {
        if (string.IsNullOrEmpty(wwwroot)) return;
        var klasor = Path.Combine(wwwroot, "img", "cars", "k");
        if (!Directory.Exists(klasor)) return;
        _kucukler = new(Directory.GetFiles(klasor, "*.webp").Select(d => Path.GetFileNameWithoutExtension(d)), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>"/img/cars/Audi_A3.jpg" → "/img/cars/k/Audi_A3.webp" (kopyası varsa), yoksa aynı adres.</summary>
    public static string Kucuk(string? adres)
    {
        var ad = KucukAdi(adres);
        return ad == null ? adres ?? "/img/placeholder.svg" : $"{Klasor}k/{ad}.webp";
    }

    /// <summary>img etiketinin srcset değeri: tarayıcı ekrana göre küçük ya da büyük olanı seçer. Kopya yoksa null (yazılmaz).</summary>
    public static string? Secenekler(string? adres) => KucukAdi(adres) is { } ad ? $"{Klasor}k/{ad}.webp 720w, {adres} 1600w" : null;

    private static string? KucukAdi(string? adres)
    {
        if (string.IsNullOrEmpty(adres) || !adres.StartsWith(Klasor, StringComparison.OrdinalIgnoreCase)) return null;
        var ad = Path.GetFileNameWithoutExtension(adres);
        return _kucukler.Contains(ad) ? ad : null;
    }
}
