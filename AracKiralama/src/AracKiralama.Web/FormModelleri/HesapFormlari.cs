// ============================================================================
// GİRİŞ, KAYIT VE PROFİL FORMLARI
// [Required] zorunlu alan, [Display] etiket, ErrorMessage ekranda görünen hata.
// ============================================================================
using System.ComponentModel.DataAnnotations;
using AracKiralama.Modeller;
using AracKiralama.Yardimcilar;

namespace AracKiralama.FormModelleri;

public class GirisFormu
{
    [Display(Name = "E-posta"), Required(ErrorMessage = "E-posta zorunludur."), EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    public string Eposta { get; set; } = "";

    [Display(Name = "Şifre"), Required(ErrorMessage = "Şifre zorunludur."), DataType(DataType.Password)]
    public string Sifre { get; set; } = "";

    [Display(Name = "Beni hatırla")] public bool BeniHatirla { get; set; } = true;
    public string? DonusAdresi { get; set; }
}

public class KayitFormu
{
    [Display(Name = "Ad"), Required(ErrorMessage = "Ad zorunludur."), MaxLength(50)] public string Ad { get; set; } = "";
    [Display(Name = "Soyad"), Required(ErrorMessage = "Soyad zorunludur."), MaxLength(50)] public string Soyad { get; set; } = "";

    [Display(Name = "E-posta"), Required(ErrorMessage = "E-posta zorunludur."), EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    public string Eposta { get; set; } = "";

    [Display(Name = "Telefon"), Phone(ErrorMessage = "Geçerli bir telefon giriniz.")] public string? Telefon { get; set; }

    [Display(Name = "Şifre"), Required(ErrorMessage = "Şifre zorunludur."), DataType(DataType.Password), MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string Sifre { get; set; } = "";

    [Display(Name = "Şifre (tekrar)"), DataType(DataType.Password), Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string SifreTekrar { get; set; } = "";

    [Range(typeof(bool), "true", "true", ErrorMessage = "KVKK aydınlatma metnini onaylamalısınız.")]
    public bool KvkkOnay { get; set; }

    public string? DonusAdresi { get; set; }
}

public class ProfilFormu
{
    [Display(Name = "Ad"), Required(ErrorMessage = "Ad zorunludur.")] public string Ad { get; set; } = "";
    [Display(Name = "Soyad"), Required(ErrorMessage = "Soyad zorunludur.")] public string Soyad { get; set; } = "";
    [Display(Name = "E-posta")] public string? Eposta { get; set; }
    [Display(Name = "Telefon"), Phone(ErrorMessage = "Geçerli bir telefon giriniz.")] public string? Telefon { get; set; }
    [Display(Name = "T.C. Kimlik No"), TcKimlik] public string? TcNo { get; set; }
    [Display(Name = "Doğum tarihi"), DataType(DataType.Date), Required(ErrorMessage = "Doğum tarihi zorunludur.")] public DateTime? DogumTarihi { get; set; }
    [Display(Name = "Ehliyet no")] public string? EhliyetNo { get; set; }
    [Display(Name = "Ehliyet veriliş tarihi"), DataType(DataType.Date), Required(ErrorMessage = "Ehliyet tarihi zorunludur.")] public DateTime? EhliyetTarihi { get; set; }
    public string? DonusAdresi { get; set; }
}

/// <summary>"Rezervasyonlarım" sayfası.</summary>
public class ProfilSayfasi
{
    public Kullanici Kullanici { get; set; } = new();
    public List<Rezervasyon> Aktifler { get; set; } = new();
    public List<Rezervasyon> Gecmis { get; set; } = new();
    public bool ProfilTamam => Kullanici.DogumTarihi.HasValue && Kullanici.EhliyetTarihi.HasValue;
}
