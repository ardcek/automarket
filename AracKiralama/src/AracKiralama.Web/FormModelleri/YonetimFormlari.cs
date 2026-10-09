// ============================================================================
// YÖNETİM PANELİ FORMLARI VE SAYFA MODELLERİ
// Basit tanım tabloları (Şube, Marka, Ekstra...) formda doğrudan kendi sınıflarıyla
// kullanılır; burada sadece ayrı bir form gerektirenler var.
// ============================================================================
using System.ComponentModel.DataAnnotations;
using AracKiralama.Modeller;
using AracKiralama.Yardimcilar;

namespace AracKiralama.FormModelleri;

/// <summary>Araç ekleme / düzenleme formu. Araca yeni alan eklerseniz buraya ve AraclarController'daki eşlemeye de ekleyin.</summary>
public class AracFormu
{
    public int? Id { get; set; }
    [Display(Name = "Plaka"), Required(ErrorMessage = "Plaka zorunludur."), PlakaKontrol] public string Plaka { get; set; } = "";
    [Display(Name = "Marka")] public int MarkaId { get; set; }
    [Display(Name = "Model"), Required(ErrorMessage = "Model zorunludur.")] public string Model { get; set; } = "";
    [Display(Name = "Model yılı"), Range(1990, 2035, ErrorMessage = "Geçerli bir yıl giriniz.")] public int Yil { get; set; } = DateTime.Today.Year;
    [Display(Name = "Kategori")] public int KategoriId { get; set; }
    [Display(Name = "Yakıt")] public int YakitTuruId { get; set; }
    [Display(Name = "Vites")] public int VitesTuruId { get; set; }
    [Display(Name = "Koltuk sayısı"), Range(1, 20)] public int KoltukSayisi { get; set; } = 5;
    [Display(Name = "Renk"), Required(ErrorMessage = "Renk zorunludur.")] public string Renk { get; set; } = "";
    [Display(Name = "Kilometre"), Range(0, 3_000_000)] public int Kilometre { get; set; }
    [Display(Name = "Günlük fiyat (₺)"), Range(1, 1_000_000, ErrorMessage = "Geçerli bir fiyat giriniz.")] public decimal GunlukFiyat { get; set; } = 1500;
    [Display(Name = "Depozito (₺)"), Range(0, 1_000_000)] public decimal? Depozito { get; set; }   // boşsa kategorinin varsayılan depozitosu
    [Display(Name = "Durum")] public AracDurumu Durum { get; set; }
    [Display(Name = "Bulunduğu şube")] public int SubeId { get; set; }
    [Display(Name = "Ana sayfada öne çıkar")] public bool OneCikan { get; set; }
    [Display(Name = "Açıklama")] public string? Aciklama { get; set; }
    [Display(Name = "Görsel adresi")] public string? GorselAdresi { get; set; }
    [Display(Name = "Yeni görsel yükle")] public IFormFile? Gorsel { get; set; }
}

public class TeslimFormu
{
    public int RezervasyonId { get; set; }
    [Display(Name = "Teslim kilometresi"), Range(0, 3_000_000)] public int TeslimKm { get; set; }
    [Display(Name = "Yakıt seviyesi")] public YakitSeviyesi TeslimYakit { get; set; } = YakitSeviyesi.Dolu;
    public Rezervasyon? Rezervasyon { get; set; }
}

public class IadeFormu
{
    public int RezervasyonId { get; set; }
    [Display(Name = "İade kilometresi"), Range(0, 3_000_000)] public int IadeKm { get; set; }
    [Display(Name = "Yakıt seviyesi")] public YakitSeviyesi IadeYakit { get; set; } = YakitSeviyesi.Dolu;
    [Display(Name = "İade zamanı")] public DateTime IadeTarihi { get; set; } = DateTime.Now;
    [Display(Name = "Hasar açıklaması")] public string? HasarAciklamasi { get; set; }
    [Display(Name = "Hasar bedeli (₺)"), Range(0, 10_000_000)] public decimal HasarTutari { get; set; }
    [Display(Name = "Not")] public string? Not { get; set; }
    public Rezervasyon? Rezervasyon { get; set; }
    public decimal TahminiGecikmeUcreti { get; set; }
}

public class BakimFormu
{
    [Display(Name = "Araç")] public int AracId { get; set; }
    [Display(Name = "Bakım türü")] public BakimTuru Tur { get; set; }
    [Display(Name = "Başlangıç")] public DateTime BaslangicTarihi { get; set; } = DateTime.Today.AddHours(9);
    [Display(Name = "Bitiş")] public DateTime BitisTarihi { get; set; } = DateTime.Today.AddDays(1).AddHours(18);
    [Display(Name = "Kilometre"), Range(0, 3_000_000)] public int Kilometre { get; set; }
    [Display(Name = "Maliyet (₺)"), Range(0, 10_000_000)] public decimal Maliyet { get; set; }
    [Display(Name = "Açıklama")] public string? Aciklama { get; set; }
    public List<Arac> Araclar { get; set; } = new();
}

public class PersonelFormu
{
    [Display(Name = "Ad"), Required(ErrorMessage = "Ad zorunludur.")] public string Ad { get; set; } = "";
    [Display(Name = "Soyad"), Required(ErrorMessage = "Soyad zorunludur.")] public string Soyad { get; set; } = "";
    [Display(Name = "E-posta"), Required(ErrorMessage = "E-posta zorunludur."), EmailAddress] public string Eposta { get; set; } = "";
    [Display(Name = "Geçici şifre"), Required(ErrorMessage = "Şifre zorunludur."), MinLength(6, ErrorMessage = "En az 6 karakter.")] public string Sifre { get; set; } = "";
    [Display(Name = "Rol")] public string Rol { get; set; } = Roller.Personel;
}

/// <summary>Rezervasyon listesindeki arama / filtre seçimleri.</summary>
public class RezervasyonFiltresi
{
    public RezervasyonDurumu? Durum { get; set; }
    public string? Arama { get; set; }
    public int? SubeId { get; set; }
    public bool SadeceGeciken { get; set; }
    public int Sayfa { get; set; } = 1;
}

/// <summary>Gösterge panelindeki küçük bilgi kartı (Views/Shared/_GostergeKarti.cshtml).</summary>
public class GostergeKarti
{
    public string Baslik { get; set; } = "";
    public string Deger { get; set; } = "";
    public string Ikon { get; set; } = "bi-info-circle";
    public string Renk { get; set; } = "ana";          // ana | yesil | turuncu | kirmizi | mavi
    public string? Aciklama { get; set; }
    public string? Link { get; set; }
}

// ---- Doluluk takvimi ----
public class TakvimBloku
{
    public int BaslangicSutunu { get; set; }   // 1 = ilk gün
    public int GunSayisi { get; set; }
    public string Yazi { get; set; } = "";
    public string Ipucu { get; set; } = "";    // fareyle üstüne gelince görünen açıklama
    public string Renk { get; set; } = "";     // CSS sınıfı: blok-onaylandi, blok-kirada...
    public string? Link { get; set; }
}

public class TakvimSatiri
{
    public Arac Arac { get; set; } = new();
    public List<TakvimBloku> Bloklar { get; set; } = new();
}

public class TakvimSayfasi
{
    public DateTime Baslangic { get; set; }
    public int GunSayisi { get; set; }
    public int? SubeId { get; set; }
    public int? KategoriId { get; set; }
    public List<DateTime> Gunler { get; set; } = new();
    public List<TakvimSatiri> Satirlar { get; set; } = new();
    public List<Sube> Subeler { get; set; } = new();
    public List<Kategori> Kategoriler { get; set; } = new();
}

public class MusteriSatiri
{
    public Kullanici Kullanici { get; set; } = new();
    public int KiralamaSayisi { get; set; }
    public decimal ToplamHarcama { get; set; }
}
