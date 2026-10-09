// ============================================================================
// DURUMLAR (enum)
// İş akışını belirleyen sabit seçenekler. Bunlar admin panelinden değiştirilmez,
// çünkü kod bu değerlere göre karar verir (ör. "Kirada" olan araç teslim edilemez).
// [Display(Name = "...")] → ekranda görünen Türkçe ad. Kodda: durum.Ad()
// ============================================================================
using System.ComponentModel.DataAnnotations;

namespace AracKiralama.Modeller;

/// <summary>Aracın şu anki durumu.</summary>
public enum AracDurumu
{
    [Display(Name = "Müsait")] Musait,
    [Display(Name = "Kirada")] Kirada,
    [Display(Name = "Bakımda")] Bakimda,
    [Display(Name = "Pasif")] Pasif          // sitede görünmez
}

/// <summary>Rezervasyonun yaşam döngüsü. Geçiş kuralları: Servisler/RezervasyonServisi.cs</summary>
public enum RezervasyonDurumu
{
    [Display(Name = "Onay Bekliyor")] OnayBekliyor,
    [Display(Name = "Onaylandı")] Onaylandi,
    [Display(Name = "Kirada")] Kirada,
    [Display(Name = "Tamamlandı")] Tamamlandi,
    [Display(Name = "İptal Edildi")] IptalEdildi,
    [Display(Name = "Reddedildi")] Reddedildi
}

/// <summary>Teslim / iade anındaki yakıt seviyesi (çeyrek depo cinsinden).</summary>
public enum YakitSeviyesi
{
    [Display(Name = "Boş")] Bos = 0,
    [Display(Name = "1/4")] Ceyrek = 1,
    [Display(Name = "1/2")] Yarim = 2,
    [Display(Name = "3/4")] UcCeyrek = 3,
    [Display(Name = "Dolu")] Dolu = 4
}

public enum OdemeYontemi
{
    [Display(Name = "Nakit")] Nakit,
    [Display(Name = "Kredi Kartı")] KrediKarti,
    [Display(Name = "Havale / EFT")] Havale
}

public enum OdemeTuru
{
    [Display(Name = "Kira Bedeli")] KiraBedeli,
    [Display(Name = "Depozito")] Depozito,
    [Display(Name = "Ceza / Ek Ücret")] Ceza,
    [Display(Name = "Depozito İadesi")] DepozitoIadesi
}

public enum BakimTuru
{
    [Display(Name = "Periyodik Bakım")] Periyodik,
    [Display(Name = "Onarım")] Onarim,
    [Display(Name = "Lastik Değişimi")] Lastik,
    [Display(Name = "Muayene")] Muayene,
    [Display(Name = "Temizlik")] Temizlik
}
