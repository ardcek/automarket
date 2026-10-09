// ============================================================================
// TANIM TABLOLARI: Marka, Kategori, Yakıt türü, Vites türü, Şube, Ekstra
// Hepsi admin panelinden eklenip düzenlenir (Yönetim → Tanımlar menüsü).
// Yeni eklenen bir kategori / marka / yakıt türü filtrelerde ve formlarda
// otomatik görünür; koda dokunmak gerekmez.
// [Display] → formdaki etiket, [Required] → zorunlu alan.
// ============================================================================
using System.ComponentModel.DataAnnotations;

namespace AracKiralama.Modeller;

public class Marka
{
    public int Id { get; set; }

    [Display(Name = "Marka adı"), Required(ErrorMessage = "Marka adı zorunludur."), MaxLength(50)]
    public string Ad { get; set; } = "";

    [Display(Name = "Logo adresi"), MaxLength(250)]
    public string? LogoAdresi { get; set; }

    [Display(Name = "Sıra")]
    public int Sira { get; set; }
}

/// <summary>Araç sınıfı. Sürücü şartları (yaş, ehliyet) kategoriye göre belirlenir.</summary>
public class Kategori
{
    public int Id { get; set; }

    [Display(Name = "Kategori adı"), Required(ErrorMessage = "Kategori adı zorunludur."), MaxLength(50)]
    public string Ad { get; set; } = "";

    [Display(Name = "İkon"), MaxLength(40)]
    public string Ikon { get; set; } = "bi-car-front";

    [Display(Name = "Kapak görseli"), MaxLength(250)]
    public string? GorselAdresi { get; set; }

    [Display(Name = "En az sürücü yaşı"), Range(18, 80)]
    public int MinYas { get; set; } = 21;

    [Display(Name = "En az ehliyet yılı"), Range(0, 30)]
    public int MinEhliyetYili { get; set; } = 1;

    [Display(Name = "Varsayılan depozito (₺)"), Range(0, 1_000_000)]
    public decimal VarsayilanDepozito { get; set; } = 3000;

    [Display(Name = "Sıra")]
    public int Sira { get; set; }
}

public class YakitTuru
{
    public int Id { get; set; }
    [Display(Name = "Yakıt türü"), Required, MaxLength(30)] public string Ad { get; set; } = "";
    [Display(Name = "Sıra")] public int Sira { get; set; }
}

public class VitesTuru
{
    public int Id { get; set; }
    [Display(Name = "Vites türü"), Required, MaxLength(30)] public string Ad { get; set; } = "";
    [Display(Name = "Sıra")] public int Sira { get; set; }
}

/// <summary>Araç alma / iade noktası.</summary>
public class Sube
{
    public int Id { get; set; }

    [Display(Name = "Şube adı"), Required(ErrorMessage = "Şube adı zorunludur."), MaxLength(100)]
    public string Ad { get; set; } = "";

    [Display(Name = "Şehir"), Required(ErrorMessage = "Şehir zorunludur."), MaxLength(50)]
    public string Sehir { get; set; } = "";

    [Display(Name = "Adres"), Required(ErrorMessage = "Adres zorunludur."), MaxLength(250)]
    public string Adres { get; set; } = "";

    [Display(Name = "Telefon"), Required(ErrorMessage = "Telefon zorunludur."), MaxLength(20)]
    public string Telefon { get; set; } = "";
}

/// <summary>Kiralamaya eklenebilen ek hizmet (tam kasko, bebek koltuğu...).</summary>
public class Ekstra
{
    public int Id { get; set; }

    [Display(Name = "Hizmet adı"), Required(ErrorMessage = "Ad zorunludur."), MaxLength(60)]
    public string Ad { get; set; } = "";

    [Display(Name = "Açıklama"), MaxLength(250)]
    public string? Aciklama { get; set; }

    /// <summary>Bootstrap ikon adı, örn: "bi-shield-check". Liste: icons.getbootstrap.com</summary>
    [Display(Name = "İkon"), MaxLength(40)]
    public string Ikon { get; set; } = "bi-plus-circle";

    [Display(Name = "Günlük fiyat (₺)"), Range(0, 100_000)]
    public decimal GunlukFiyat { get; set; }

    [Display(Name = "Aktif")]
    public bool Aktif { get; set; } = true;
}
