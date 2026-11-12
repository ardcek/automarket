// ============================================================================
// KAMPANYA (İNDİRİM KURALI) — Yönetim → Kampanyalar
// Koda dokunmadan indirim tanımlamak için. Örnekler:
//   • "Öğretmen indirimi"  : müşterinin Meslek bilgisi = Öğretmen → %15
//   • "SUV kampanyası"     : aracın kategorisi = SUV → %10
//   • "Yaz fırsatı"        : 01.06 – 31.08 arası alışlarda, 5 gün ve üzeri → %12
//
// Koşulların hepsi isteğe bağlıdır. Boş bırakılan koşul "fark etmez" demektir;
// doldurulan koşulların HEPSİ sağlanırsa indirim uygulanır (VE mantığı).
// Hangi kampanyanın uygulanacağına Servisler/KampanyaServisi.cs karar verir:
// birden fazla kampanya uyarsa müşteriye en avantajlı (oranı en yüksek) olan seçilir.
// ============================================================================
using System.ComponentModel.DataAnnotations;
using AracKiralama.Yardimcilar;

namespace AracKiralama.Modeller;

public class Kampanya
{
    public int Id { get; set; }

    [Display(Name = "Kampanya adı"), Required(ErrorMessage = "Kampanya adı zorunludur."), MaxLength(80)]
    public string Ad { get; set; } = "";             // fiyat dökümünde görünür: "Öğretmen indirimi"

    [Display(Name = "Rozet yazısı"), MaxLength(60)]
    public string? RozetYazisi { get; set; }         // araç kartında görünür: "Öğretmenlere %15"

    [Display(Name = "İndirim oranı"), Range(0.01, 0.9, ErrorMessage = "İndirim oranı %1 ile %90 arasında olmalıdır.")]
    public decimal IndirimOrani { get; set; } = 0.10m;   // 0.15 = %15

    // ---- KOŞULLAR (boş = fark etmez) ----

    /// <summary>Müşteri bilgisi koşulu: bu alandaki cevabı MusteriDegeri olan müşteriler (örn. Meslek = Öğretmen).</summary>
    public int? MusteriAlanId { get; set; }
    public MusteriAlani? MusteriAlani { get; set; }
    [MaxLength(200)] public string? MusteriDegeri { get; set; }

    /// <summary>Araç koşulları: sadece bu kategori / marka.</summary>
    public int? KategoriId { get; set; }
    public Kategori? Kategori { get; set; }
    public int? MarkaId { get; set; }
    public Marka? Marka { get; set; }

    /// <summary>Panelden eklenen bir araç özelliği koşulu (örn. Yakıt tasarrufu = A sınıfı).</summary>
    public int? AracOzellikId { get; set; }
    public AracOzelligi? AracOzelligi { get; set; }
    [MaxLength(200)] public string? AracOzellikDegeri { get; set; }

    /// <summary>En az kaç günlük kiralamada geçerli (boşsa her süre).</summary>
    [Range(1, 365)] public int? EnAzGun { get; set; }

    /// <summary>Alış tarihi bu aralıktaysa geçerli (boşsa her zaman).</summary>
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }

    [Display(Name = "Yayında")] public bool Aktif { get; set; } = true;
    [Display(Name = "Sıra")] public int Sira { get; set; }

    /// <summary>"%15" gibi okunur oran.</summary>
    public string OranYazisi => "%" + (IndirimOrani * 100).ToString("0.#", Bicim.Tr);

    /// <summary>Rozet boş bırakıldıysa kullanılacak yazı: "Öğretmen indirimi %15".</summary>
    public string Rozet => string.IsNullOrWhiteSpace(RozetYazisi) ? $"{Ad} {OranYazisi}" : RozetYazisi;

    /// <summary>
    /// Koşulların okunur özeti (panel listesinde görünür): "Meslek: Öğretmen · Kategori: SUV · 5+ gün".
    /// İlişkili tablolar (MusteriAlani, Kategori, Marka, AracOzelligi) Include ile yüklenmiş olmalı.
    /// </summary>
    public string KosulOzeti()
    {
        var parcalar = new List<string>();
        if (MusteriAlanId != null) parcalar.Add($"{MusteriAlani?.Ad ?? "Müşteri bilgisi"}: {MusteriDegeri}");
        if (KategoriId != null) parcalar.Add($"Kategori: {Kategori?.Ad}");
        if (MarkaId != null) parcalar.Add($"Marka: {Marka?.Ad}");
        if (AracOzellikId != null) parcalar.Add($"{AracOzelligi?.Ad ?? "Özellik"}: {(AracOzellikDegeri == "true" ? "Var" : AracOzellikDegeri)}");
        if (EnAzGun != null) parcalar.Add($"{EnAzGun}+ gün");
        if (BaslangicTarihi != null || BitisTarihi != null)
            parcalar.Add($"{BaslangicTarihi?.ToString("dd.MM.yyyy") ?? "…"} – {BitisTarihi?.ToString("dd.MM.yyyy") ?? "…"}");
        return parcalar.Count == 0 ? "Herkese, bütün araçlarda" : string.Join(" · ", parcalar);
    }
}
