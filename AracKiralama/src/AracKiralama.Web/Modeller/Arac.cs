// ============================================================================
// ARAÇ
// Filodaki her araç. Marka, kategori, yakıt ve vites ayrı tablolardan seçilir
// (bkz. Tanimlar.cs). Yeni bir alan eklemek için: buraya bir özellik ekleyin,
// sonra FormModelleri/YonetimFormlari.cs → AracFormu ve araç formu view'ı.
// ============================================================================
using System.ComponentModel.DataAnnotations.Schema;

namespace AracKiralama.Modeller;

public class Arac
{
    public int Id { get; set; }
    public string Plaka { get; set; } = "";          // "34 ABC 123" (benzersiz)

    public int MarkaId { get; set; }
    public Marka? Marka { get; set; }
    public string Model { get; set; } = "";
    public int Yil { get; set; }

    public int KategoriId { get; set; }
    public Kategori? Kategori { get; set; }
    public int YakitTuruId { get; set; }
    public YakitTuru? YakitTuru { get; set; }
    public int VitesTuruId { get; set; }
    public VitesTuru? VitesTuru { get; set; }

    public int KoltukSayisi { get; set; } = 5;
    public string Renk { get; set; } = "";
    public int Kilometre { get; set; }

    public decimal GunlukFiyat { get; set; }         // ₺
    public decimal Depozito { get; set; }            // teslimde alınır, iadede geri verilir

    public AracDurumu Durum { get; set; } = AracDurumu.Musait;

    public int SubeId { get; set; }                  // aracın şu an bulunduğu şube
    public Sube? Sube { get; set; }

    public string? GorselAdresi { get; set; }
    public string? Aciklama { get; set; }

    /// <summary>İşaretliyse ana sayfadaki "öne çıkanlar" bölümünde gösterilir.</summary>
    public bool OneCikan { get; set; }

    public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

    public List<Rezervasyon> Rezervasyonlar { get; set; } = new();
    public List<Bakim> Bakimlar { get; set; } = new();

    /// <summary>"Renault Clio" gibi ekranda gösterilen ad (veritabanına yazılmaz).</summary>
    [NotMapped] public string TamAd => $"{Marka?.Ad} {Model}".Trim();
}
