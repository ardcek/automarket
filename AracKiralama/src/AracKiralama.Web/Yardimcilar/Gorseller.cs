// ============================================================================
// KÜÇÜK GÖRSELLER (araç kartları, kategoriler, rezervasyon özeti)
// wwwroot/img/cars/ altındaki her fotoğrafın 720 px genişliğinde hafif bir kopyası
// wwwroot/img/cars/k/ klasöründe .webp olarak durur (büyük fotoğrafın ~onda biri).
// 1300 px'ten geniş fotoğrafların ayrıca 1200 px'lik orta boy kopyası wwwroot/img/cars/o/ klasöründedir
// (yüksek çözünürlüklü ekranlar büyük JPG yerine bunu alır).
// Kartlar küçük kopyayı gösterir, sayfa hızlı açılır; detay sayfası büyük fotoğrafı gösterir.
// Küçük kopyası olmayan görsel (örn. panelden yüklenen) olduğu gibi kullanılır, hiçbir şey bozulmaz.
//
// View'da:  <img src="@Gorseller.Kucuk(adres)" srcset="@Gorseller.Secenekler(adres)" sizes="..." />
// sizes = görselin ekranda kapladığı GERÇEK genişlik; büyük yazılırsa tarayıcı gereksiz büyük dosya indirir.
// ============================================================================
namespace AracKiralama.Yardimcilar;

public static class Gorseller
{
    private const string Klasor = "/img/cars/";
    private static HashSet<string> _kucukler = new(StringComparer.OrdinalIgnoreCase);
    private static HashSet<string> _ortalar = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, int> _genislikler = new(StringComparer.OrdinalIgnoreCase);   // özgün fotoğrafın piksel genişliği

    /// <summary>Program.cs'te uygulama açılırken bir kez çağrılır: hangi küçük / orta kopyalar var, özgünler kaç piksel?</summary>
    public static void Yukle(string? wwwroot)
    {
        if (string.IsNullOrEmpty(wwwroot)) return;
        var klasor = Path.Combine(wwwroot, "img", "cars");
        if (!Directory.Exists(Path.Combine(klasor, "k"))) return;
        _kucukler = Adlar(Path.Combine(klasor, "k"));
        _ortalar = Adlar(Path.Combine(klasor, "o"));
        // srcset'e özgünün gerçek genişliği yazılır (çoğu 960 px; hepsine 1600 yazmak tarayıcıyı yanıltıyordu)
        _genislikler = new(StringComparer.OrdinalIgnoreCase);
        foreach (var dosya in Directory.GetFiles(klasor))
        {
            var ad = Path.GetFileNameWithoutExtension(dosya);
            if (_kucukler.Contains(ad) && GenislikOku(dosya) is { } genislik) _genislikler[ad] = genislik;
        }
    }

    /// <summary>"/img/cars/Audi_A3.jpg" → "/img/cars/k/Audi_A3.webp" (kopyası varsa), yoksa aynı adres.</summary>
    public static string Kucuk(string? adres)
    {
        var ad = KucukAdi(adres);
        return ad == null ? adres ?? "/img/placeholder.svg" : $"{Klasor}k/{ad}.webp";
    }

    /// <summary>img etiketinin srcset değeri: tarayıcı ekrana göre küçük, orta ya da büyük olanı seçer. Kopya yoksa null (yazılmaz).</summary>
    public static string? Secenekler(string? adres)
    {
        if (KucukAdi(adres) is not { } ad) return null;
        var secenekler = new List<string> { $"{Klasor}k/{ad}.webp 720w" };
        if (_ortalar.Contains(ad)) secenekler.Add($"{Klasor}o/{ad}.webp 1200w");
        secenekler.Add($"{adres} {(_genislikler.TryGetValue(ad, out var genislik) ? genislik : 1600)}w");
        return string.Join(", ", secenekler);
    }

    private static string? KucukAdi(string? adres)
    {
        if (string.IsNullOrEmpty(adres) || !adres.StartsWith(Klasor, StringComparison.OrdinalIgnoreCase)) return null;
        var ad = Path.GetFileNameWithoutExtension(adres);
        return _kucukler.Contains(ad) ? ad : null;
    }

    private static HashSet<string> Adlar(string klasor) => Directory.Exists(klasor)
        ? new(Directory.GetFiles(klasor, "*.webp").Select(d => Path.GetFileNameWithoutExtension(d)), StringComparer.OrdinalIgnoreCase)
        : new(StringComparer.OrdinalIgnoreCase);

    /// <summary>JPEG ya da PNG dosyasının piksel genişliği (dosyanın başlığından okunur, resim açılmaz). Okunamazsa null.</summary>
    public static int? GenislikOku(string dosya)
    {
        try
        {
            var b = File.ReadAllBytes(dosya);
            // PNG: 8 baytlık imza, ardından IHDR bölümünde genişlik (16. bayttan itibaren 4 bayt)
            if (b.Length > 24 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
                return (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
            // JPEG: işaretçileri (FF xx) sırayla atla; SOF işaretçisinde (FF C0–CF, C4/C8/CC hariç) yükseklik ve genişlik yazar
            if (b.Length > 4 && b[0] == 0xFF && b[1] == 0xD8)
            {
                var i = 2;
                while (i + 8 < b.Length)
                {
                    if (b[i] != 0xFF) { i++; continue; }
                    var isaret = b[i + 1];
                    if (isaret == 0xFF) { i++; continue; }
                    if (isaret is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC)) return (b[i + 7] << 8) | b[i + 8];
                    if (isaret is 0x01 or >= 0xD0 and <= 0xD8) { i += 2; continue; }   // uzunluğu olmayan işaretçiler
                    i += 2 + ((b[i + 2] << 8) | b[i + 3]);
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return null;
    }
}
