// ============================================================================
// GİRİŞ, KAYIT VE PROFİL FORMLARI
// [Required] zorunlu alan, [Display] etiket, ErrorMessage ekranda görünen hata.
// ============================================================================
using System.ComponentModel.DataAnnotations;
using AracKiralama.Modeller;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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

    // "Şifre (tekrar)" kutusu yok: yerine şifreyi göster/gizle düğmesi var (yazım hatası görülerek düzeltilir).
    [Display(Name = "Şifre"), Required(ErrorMessage = "Şifre zorunludur."), DataType(DataType.Password), MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string Sifre { get; set; } = "";

    [Range(typeof(bool), "true", "true", ErrorMessage = "KVKK aydınlatma metnini onaylamalısınız.")]
    public bool KvkkOnay { get; set; }

    public string? DonusAdresi { get; set; }

    // ---- Panelden eklenen müşteri bilgileri (Yönetim → Müşteri Bilgileri), örn. Meslek ----
    public Dictionary<int, string?>? Alan { get; set; }                   // formdan gelen cevaplar: Alan[3] = "Öğretmen"
    [ValidateNever] public List<MusteriAlani> Alanlar { get; set; } = new();   // sadece ekranda göstermek için
}

// ---- ŞİFRE İŞLEMLERİ (HesapController ve ProfilController) ----

/// <summary>"Şifremi unuttum": e-posta yazılır, sıfırlama linki gönderilir.</summary>
public class SifremiUnuttumFormu
{
    [Display(Name = "E-posta"), Required(ErrorMessage = "E-posta zorunludur."), EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    public string Eposta { get; set; } = "";
}

/// <summary>E-postadaki linkten gelinir: yeni şifre belirlenir. Kod, linkteki tek kullanımlık şifre sıfırlama kodudur.</summary>
public class SifreSifirlaFormu
{
    public string Eposta { get; set; } = "";
    public string Kod { get; set; } = "";
    [Display(Name = "Yeni şifre"), Required(ErrorMessage = "Şifre zorunludur."), DataType(DataType.Password), MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string Sifre { get; set; } = "";
}

/// <summary>Geçici şifreyle ilk giriş: kişi kendi şifresini belirler (mevcut şifre sorulmaz, zaten az önce girdi).</summary>
public class YeniSifreFormu
{
    [Display(Name = "Yeni şifre"), Required(ErrorMessage = "Şifre zorunludur."), DataType(DataType.Password), MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string Sifre { get; set; } = "";
}

/// <summary>Profil → Şifremi değiştir.</summary>
public class SifreDegistirFormu
{
    [Display(Name = "Şu anki şifre"), Required(ErrorMessage = "Şu anki şifrenizi giriniz."), DataType(DataType.Password)]
    public string MevcutSifre { get; set; } = "";
    [Display(Name = "Yeni şifre"), Required(ErrorMessage = "Yeni şifre zorunludur."), DataType(DataType.Password), MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string YeniSifre { get; set; } = "";
}

public class ProfilFormu
{
    [Display(Name = "Ad"), Required(ErrorMessage = "Ad zorunludur.")] public string Ad { get; set; } = "";
    [Display(Name = "Soyad"), Required(ErrorMessage = "Soyad zorunludur.")] public string Soyad { get; set; } = "";
    [Display(Name = "E-posta")] public string? Eposta { get; set; }
    // Bu sayfadaki bütün alanlar zorunlu (eksikse rezervasyon yapılamaz: Kullanici.EksikProfilBilgileri)
    [Display(Name = "Telefon"), Required(ErrorMessage = "Telefon zorunludur."), Phone(ErrorMessage = "Geçerli bir telefon giriniz.")] public string? Telefon { get; set; }
    [Display(Name = "T.C. Kimlik No"), Required(ErrorMessage = "T.C. Kimlik No zorunludur."), TcKimlik] public string? TcNo { get; set; }
    [Display(Name = "Doğum tarihi"), DataType(DataType.Date), Required(ErrorMessage = "Doğum tarihi zorunludur.")] public DateTime? DogumTarihi { get; set; }
    [Display(Name = "Ehliyet no"), Required(ErrorMessage = "Ehliyet no zorunludur."), StringLength(20, MinimumLength = 5, ErrorMessage = "Ehliyet no 5–20 karakter olmalıdır.")] public string? EhliyetNo { get; set; }
    [Display(Name = "Ehliyet veriliş tarihi"), DataType(DataType.Date), Required(ErrorMessage = "Ehliyet tarihi zorunludur.")] public DateTime? EhliyetTarihi { get; set; }
    public string? DonusAdresi { get; set; }

    // ---- Panelden eklenen müşteri bilgileri (Yönetim → Müşteri Bilgileri), örn. Meslek ----
    public Dictionary<int, string?>? Alan { get; set; }
    [ValidateNever] public List<MusteriAlani> Alanlar { get; set; } = new();
}

/// <summary>"Rezervasyonlarım" sayfası.</summary>
public class ProfilSayfasi
{
    public Kullanici Kullanici { get; set; } = new();
    public List<Rezervasyon> Aktifler { get; set; } = new();
    public List<Rezervasyon> Gecmis { get; set; } = new();
    public bool ProfilTamam => Kullanici.EksikProfilBilgileri().Count == 0;
}
