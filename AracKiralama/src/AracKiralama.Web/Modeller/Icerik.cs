// ============================================================================
// SİTE İÇERİĞİ: Sayfalar, Metinler, Görseller
// - Sayfa  : Hakkımızda, KVKK gibi serbest metin sayfaları (Yönetim → Sayfalar)
// - Metin  : Sitedeki her başlık / buton / açıklama yazısı (Yönetim → Site Metinleri)
//            Varsayılan halleri Yardimcilar/VarsayilanMetinler.cs dosyasında.
// - Gorsel : Panelden yüklenen resimler. Veritabanında saklanır → sunucu
//            yeniden başlasa da kaybolmaz. Adresi: /gorsel/{Id}
// ============================================================================
using System.ComponentModel.DataAnnotations;

namespace AracKiralama.Modeller;

public class Sayfa
{
    public int Id { get; set; }

    [Display(Name = "Başlık"), Required(ErrorMessage = "Başlık zorunludur."), MaxLength(100)]
    public string Baslik { get; set; } = "";

    /// <summary>Adres çubuğunda görünen kısım: /Sayfa/{Adres}. Sadece küçük harf, rakam ve tire.</summary>
    [Display(Name = "Adres (örn: hakkimizda)"), Required(ErrorMessage = "Adres zorunludur."), MaxLength(60)]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Sadece küçük harf, rakam ve tire kullanın.")]
    public string Adres { get; set; } = "";

    /// <summary>Boş satır = yeni paragraf, "- " ile başlayan satır = madde, "## " = ara başlık.</summary>
    [Display(Name = "İçerik")]
    public string Icerik { get; set; } = "";

    [Display(Name = "Üst menüde göster")] public bool MenudeGoster { get; set; }
    [Display(Name = "Alt bilgide göster")] public bool AltBilgideGoster { get; set; } = true;
    [Display(Name = "İletişim kartlarını göster (telefon, e-posta, adres)")] public bool IletisimKartlari { get; set; }
    [Display(Name = "Sıra")] public int Sira { get; set; }
    [Display(Name = "Yayında")] public bool Aktif { get; set; } = true;
}

/// <summary>Sitedeki tek bir yazı. Anahtar örn: "anasayfa.baslik"</summary>
public class Metin
{
    [Key, MaxLength(100)] public string Anahtar { get; set; } = "";
    public string Deger { get; set; } = "";
}

public class Gorsel
{
    public int Id { get; set; }
    public string DosyaAdi { get; set; } = "";
    public string IcerikTuru { get; set; } = "image/jpeg";   // tarayıcıya "bu bir jpeg" demek için
    public byte[] Veri { get; set; } = [];
    public DateTime YuklenmeTarihi { get; set; } = DateTime.Now;
}
