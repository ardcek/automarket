// ============================================================================
// PANELDEN EKLENEN MÜŞTERİ BİLGİLERİ (Yönetim → Müşteri Bilgileri)
// Koda dokunmadan müşterilere yeni bir bilgi eklemek için: örneğin "Meslek",
// "Öğrenci misiniz?", "Nereden duydunuz?". Eklenen bilgi kayıt formunda ve
// profilde sorulur; kampanyalarda koşul olarak kullanılabilir
// (örn. "Mesleği Öğretmen olana %15 indirim").
//
// Araç özellikleriyle (Modeller/AracOzelligi.cs) aynı mantık, iki tablo:
//   MusteriAlanlari       → bilginin tanımı (adı, türü, seçenekleri)
//   MusteriAlanDegerleri  → hangi müşterinin o bilgide ne cevap verdiği
//                           (bir müşteri × bir alan = bir satır)
// ============================================================================
using System.ComponentModel.DataAnnotations;

namespace AracKiralama.Modeller;

/// <summary>Bir müşteri bilgisinin tanımı (MusteriAlanlari tablosu). Türleri araç özellikleriyle aynıdır.</summary>
public class MusteriAlani
{
    public int Id { get; set; }

    [Display(Name = "Bilgi adı"), Required(ErrorMessage = "Bilgi adı zorunludur."), MaxLength(60)]
    public string Ad { get; set; } = "";             // "Meslek"

    [Display(Name = "Türü")]
    public OzellikTuru Tur { get; set; } = OzellikTuru.Secim;

    [Display(Name = "Seçenekler"), MaxLength(1000)]
    public string? Secenekler { get; set; }          // "Öğrenci|Öğretmen|Doktor" (sadece Seçim listesi için)

    [Display(Name = "Kayıt formunda sor")] public bool KayittaGoster { get; set; } = true;
    [Display(Name = "Zorunlu")] public bool Zorunlu { get; set; }
    [Display(Name = "Sıra")] public int Sira { get; set; }
    [Display(Name = "Yayında")] public bool Aktif { get; set; } = true;

    public List<MusteriAlanDegeri> Degerler { get; set; } = new();

    /// <summary>Seçenekleri liste olarak verir: "Öğrenci|Öğretmen" → ["Öğrenci", "Öğretmen"] (veritabanında sütun değildir).</summary>
    public string[] SecenekListesi => (Secenekler ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Formdan gelen ham değeri bu alanın türüne göre temizler; boşsa ya da geçersizse null döner.
    /// Araç özelliklerindeki kuralın aynısıdır (AracOzellikDegeri.Temizle), ayrıca seçim listesinde
    /// listede olmayan bir değer gelirse kabul edilmez.
    /// </summary>
    public string? Temizle(string? ham)
    {
        var temiz = AracOzellikDegeri.Temizle(new AracOzelligi { Tur = Tur }, ham);
        if (temiz != null && Tur == OzellikTuru.Secim && !SecenekListesi.Contains(temiz)) return null;
        return temiz;
    }
}

/// <summary>Bir müşterinin bir bilgideki cevabı (MusteriAlanDegerleri tablosu). Anahtarı: KullaniciId + AlanId.</summary>
public class MusteriAlanDegeri
{
    public string KullaniciId { get; set; } = "";
    public Kullanici? Kullanici { get; set; }

    public int AlanId { get; set; }
    public MusteriAlani? Alan { get; set; }

    [MaxLength(200)]
    public string Deger { get; set; } = "";          // "Öğretmen", "true", "25"
}
