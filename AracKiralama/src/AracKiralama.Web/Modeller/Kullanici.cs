// ============================================================================
// KULLANICI (müşteri, personel ve admin)
// IdentityUser'dan türer: e-posta (Email), telefon (PhoneNumber), kullanıcı adı
// (UserName) ve şifre hash'i ASP.NET Identity'den gelir; o yüzden adları İngilizcedir.
// Rol (SuperAdmin / Admin / Personel / Musteri) ayrı tabloda tutulur (bkz. Yardimcilar/Genel.cs → Roller).
// ============================================================================
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AracKiralama.Modeller;

public class Kullanici : IdentityUser
{
    public string Ad { get; set; } = "";
    public string Soyad { get; set; } = "";
    public string? TcNo { get; set; }
    public DateTime? DogumTarihi { get; set; }
    public string? EhliyetNo { get; set; }
    public DateTime? EhliyetTarihi { get; set; }

    public bool KaraListede { get; set; }            // true ise rezervasyon yapamaz
    public string? KaraListeSebebi { get; set; }

    public DateTime KayitTarihi { get; set; } = DateTime.Now;
    public DateTime? SonGiris { get; set; }          // en son ne zaman giriş yaptı (yönetim → müşteri detayı)

    /// <summary>
    /// Admin geçici şifre verdiyse true: kişi ilk girişte yeni şifresini belirlemeden siteyi kullanamaz
    /// (Yardimcilar/SifreKontrolu.cs → /Hesap/YeniSifre'ye yönlendirir).
    /// </summary>
    public bool SifreDegistirmeli { get; set; }

    public List<Rezervasyon> Rezervasyonlar { get; set; } = new();

    [NotMapped] public string TamAd => $"{Ad} {Soyad}";
}
