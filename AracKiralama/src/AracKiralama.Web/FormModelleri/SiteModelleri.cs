// ============================================================================
// MÜŞTERİ SİTESİ SAYFA MODELLERİ
// "Sayfa modeli" = bir view'ın ihtiyaç duyduğu bütün veriyi taşıyan basit sınıf.
//
// ► ARAÇ FİLTRESİ: yeni bir filtre eklemek 3 adımdır
//   1) Aşağıdaki AracFiltresi sınıfına bir özellik ekleyin       (örn: public string? Renk { get; set; })
//   2) Controllers/AraclarController.cs → Index içinde bir "if" ekleyin
//        if (!string.IsNullOrEmpty(filtre.Renk)) sorgu = sorgu.Where(a => a.Renk == filtre.Renk);
//   3) Views/Araclar/Index.cshtml → filtre paneline bir kutu ekleyin (name="Renk")
//   Adres çubuğu (?Renk=Beyaz), sayfalama ve "temizle" linki kendiliğinden çalışır.
// ============================================================================
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using AracKiralama.Modeller;
using AracKiralama.Yardimcilar;

namespace AracKiralama.FormModelleri;

/// <summary>Araç arama / filtreleme seçimleri. Adres çubuğundaki ?Kategori=2&amp;Siralama=yeni gibi değerlerden dolar.</summary>
public class AracFiltresi
{
    // ---- Tarih ve şube (müsaitlik) ----
    public DateTime? Baslangic { get; set; }
    public DateTime? Bitis { get; set; }
    public int? AlisSubeId { get; set; }
    public int? IadeSubeId { get; set; }

    // ---- Filtreler ----
    public string? Arama { get; set; }
    public int? KategoriId { get; set; }
    public int? MarkaId { get; set; }
    public int? VitesTuruId { get; set; }
    public int? YakitTuruId { get; set; }
    public int? EnAzKoltuk { get; set; }
    public decimal? EnDusukFiyat { get; set; }
    public decimal? EnYuksekFiyat { get; set; }
    public int? EnAzYil { get; set; }

    // ---- Sıralama ve sayfa ----
    public string? Siralama { get; set; }
    public int Sayfa { get; set; } = 1;

    public bool TarihSecili => Baslangic.HasValue && Bitis.HasValue && Bitis > Baslangic;

    /// <summary>Dolu olan bütün seçimleri link oluşturmak için döndürür (yeni eklenen filtreler de otomatik dahil olur).</summary>
    public Dictionary<string, string> Degerler(int? sayfa = null)
    {
        var sonuc = new Dictionary<string, string>();
        foreach (var ozellik in GetType().GetProperties())
        {
            if (!ozellik.CanWrite || ozellik.Name == nameof(Sayfa)) continue;
            var deger = ozellik.GetValue(this);
            if (deger == null || (deger is string metin && metin.Length == 0)) continue;

            sonuc[ozellik.Name] = deger switch
            {
                DateTime tarih => Bicim.Kutu(tarih),
                decimal sayi => sayi.ToString(CultureInfo.InvariantCulture),
                _ => deger.ToString()!,
            };
        }
        if (sayfa > 1) sonuc[nameof(Sayfa)] = sayfa.Value.ToString();
        return sonuc;
    }

    /// <summary>Sadece tarih ve şube seçimi (araç kartından detaya geçerken taşınır).</summary>
    public Dictionary<string, string> TarihDegerleri()
    {
        string[] tarihAlanlari = [nameof(Baslangic), nameof(Bitis), nameof(AlisSubeId), nameof(IadeSubeId)];
        return Degerler().Where(d => tarihAlanlari.Contains(d.Key)).ToDictionary(d => d.Key, d => d.Value);
    }
}

public class AracListesiSayfasi
{
    public AracFiltresi Filtre { get; set; } = new();
    public SayfaListesi<Arac> Araclar { get; set; } = new();
    public int? GunSayisi { get; set; }
    public List<Kategori> Kategoriler { get; set; } = new();
    public List<Marka> Markalar { get; set; } = new();
    public List<YakitTuru> Yakitlar { get; set; } = new();
    public List<VitesTuru> Vitesler { get; set; } = new();
    public List<Sube> Subeler { get; set; } = new();
}

public class AracKartiModeli
{
    public Arac Arac { get; set; } = new();
    public AracFiltresi? Filtre { get; set; }
    public int? GunSayisi { get; set; }
}

public class AracDetaySayfasi
{
    public Arac Arac { get; set; } = new();
    public AracFiltresi Filtre { get; set; } = new();
    public List<Ekstra> Ekstralar { get; set; } = new();
    public List<Sube> Subeler { get; set; } = new();
    public List<Arac> Benzerler { get; set; } = new();
    public bool? Musait { get; set; }   // tarih seçilmediyse null
}

public class KategoriOzeti
{
    public Kategori Kategori { get; set; } = new();
    public int AracSayisi { get; set; }
    public decimal EnDusukFiyat { get; set; }
}

public class AnaSayfaModeli
{
    public List<Arac> OneCikanlar { get; set; } = new();
    public List<KategoriOzeti> Kategoriler { get; set; } = new();
    public List<Sube> Subeler { get; set; } = new();
    public int AracSayisi { get; set; }
    public int TamamlananKiralama { get; set; }
}

/// <summary>3 adımlı rezervasyon formu (Views/Rezervasyon/Yeni.cshtml).</summary>
public class RezervasyonFormu
{
    public int AracId { get; set; }
    [Display(Name = "Alış şubesi")] public int AlisSubeId { get; set; }
    [Display(Name = "İade şubesi")] public int IadeSubeId { get; set; }
    [Display(Name = "Alış tarihi")] public DateTime Baslangic { get; set; }
    [Display(Name = "İade tarihi")] public DateTime Bitis { get; set; }
    public List<int> EkstraIdleri { get; set; } = new();
    [Display(Name = "Notunuz"), MaxLength(500)] public string? Not { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Devam etmek için kiralama koşullarını kabul etmelisiniz.")]
    public bool KosullariKabul { get; set; }

    // ---- Sadece ekranda göstermek için ----
    public Arac? Arac { get; set; }
    public Kullanici? Musteri { get; set; }
    public List<Sube> Subeler { get; set; } = new();
    public List<Ekstra> Ekstralar { get; set; } = new();
    public List<string> SurucuHatalari { get; set; } = new();
}
