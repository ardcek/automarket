// ============================================================================
// PANELDEN EKLENEN ARAÇ ÖZELLİKLERİ (Yönetim → Filtreler ve Özellikler)
// Koda dokunmadan araçlara yeni bir bilgi eklemek için: örneğin "Bagaj hacmi (lt)",
// "Klima", "Renk". Eklenen özellik araç listesinde filtre, kartta ve detayda bilgi olarak görünür.
//
// İki tablo:
//   AracOzellikleri       → özelliğin tanımı (adı, türü, seçenekleri, nerede görüneceği)
//   AracOzellikDegerleri  → hangi aracın o özellikte hangi değere sahip olduğu
//                           (bir araç × bir özellik = bir satır)
// ============================================================================
using System.ComponentModel.DataAnnotations;

namespace AracKiralama.Modeller;

/// <summary>Özelliğin türü: filtrede ve formda hangi kutunun çıkacağını belirler.</summary>
public enum OzellikTuru
{
    [Display(Name = "Seçim listesi")] Secim,     // örn. Renk: Beyaz | Siyah | Kırmızı → açılır liste
    [Display(Name = "Sayı")] Sayi,               // örn. Bagaj hacmi: 450 → en az / en çok kutuları
    [Display(Name = "Var / Yok")] EvetHayir,     // örn. Klima → var mı yok mu
    [Display(Name = "Serbest yazı")] Yazi,       // örn. Lastik markası → içinde arama
}

/// <summary>Bir özelliğin tanımı (AracOzellikleri tablosu).</summary>
public class AracOzelligi
{
    public int Id { get; set; }

    [Display(Name = "Özellik adı"), Required(ErrorMessage = "Özellik adı zorunludur."), MaxLength(60)]
    public string Ad { get; set; } = "";

    [Display(Name = "Türü")]
    public OzellikTuru Tur { get; set; } = OzellikTuru.Secim;

    [Display(Name = "Seçenekler"), MaxLength(1000)]
    public string? Secenekler { get; set; }          // "Beyaz|Siyah|Kırmızı" (sadece Seçim listesi için)

    [Display(Name = "Birim"), MaxLength(20)]
    public string? Birim { get; set; }               // "lt", "kg", "km" (sadece Sayı için)

    [Display(Name = "İkon"), MaxLength(40)]
    public string Ikon { get; set; } = "bi-tag";     // Bootstrap ikon adı

    [Display(Name = "Araç listesinde filtre olarak göster")] public bool FiltredeGoster { get; set; } = true;
    [Display(Name = "Araç kartında göster")] public bool KarttaGoster { get; set; }
    [Display(Name = "Araç detayında göster")] public bool DetaydaGoster { get; set; } = true;
    [Display(Name = "Sıra")] public int Sira { get; set; }
    [Display(Name = "Yayında")] public bool Aktif { get; set; } = true;

    public List<AracOzellikDegeri> Degerler { get; set; } = new();

    /// <summary>Seçenekleri liste olarak verir: "Beyaz|Siyah" → ["Beyaz", "Siyah"] (veritabanında sütun değildir).</summary>
    public string[] SecenekListesi => (Secenekler ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>Bir aracın bir özellikteki değeri (AracOzellikDegerleri tablosu). Anahtarı: AracId + OzellikId.</summary>
public class AracOzellikDegeri
{
    public int AracId { get; set; }
    public Arac? Arac { get; set; }

    public int OzellikId { get; set; }
    public AracOzelligi? Ozellik { get; set; }

    [MaxLength(200)]
    public string Deger { get; set; } = "";          // ekranda görünen hali: "Beyaz", "450", "true"

    public decimal? SayiDeger { get; set; }          // Sayı türündeyse sayı hali (en az / en çok filtresi bununla çalışır)

    /// <summary>Kartta ve detayda görünecek yazı. "Var/Yok" özelliğinde yoksa null döner (gösterilmez).</summary>
    public string? Gosterim => Ozellik?.Tur switch
    {
        OzellikTuru.EvetHayir => Deger == "true" ? Ozellik!.Ad : null,
        OzellikTuru.Sayi => $"{Deger} {Ozellik!.Birim}".Trim(),
        _ => Deger,
    };

    /// <summary>
    /// Formdan gelen ham değeri özelliğin türüne göre temizler. Boş ya da geçersizse null döner (= değer yok).
    ///   Sayı      : "1.250,5" ya da "1250.5" → "1250.5"
    ///   Var / Yok : "true" → "true", diğer her şey → null (yok)
    ///   Diğerleri : baştaki / sondaki boşluklar silinir
    /// </summary>
    public static string? Temizle(AracOzelligi ozellik, string? ham)
    {
        if (string.IsNullOrWhiteSpace(ham)) return null;
        ham = ham.Trim();
        switch (ozellik.Tur)
        {
            case OzellikTuru.EvetHayir:
                return ham == "true" ? "true" : null;
            case OzellikTuru.Sayi:
                var duzgun = ham.Contains(',') ? ham.Replace(".", "").Replace(',', '.') : ham;   // Türkçe yazım: 1.250,5
                return decimal.TryParse(duzgun, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var sayi)
                    ? sayi.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            default:
                return ham.Length > 200 ? ham[..200] : ham;
        }
    }
}
