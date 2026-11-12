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
using AracKiralama.Servisler;
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

    // ---- Panelden eklenen özellik filtreleri (Yönetim → Filtreler ve Özellikler) ----
    // Anahtar = özelliğin numarası (AracOzellikleri.Id). Adres çubuğunda: ?Oz[3]=Beyaz&OzEnAz[4]=400
    public Dictionary<int, string?>? Oz { get; set; }          // Seçim listesi / Var-Yok / Yazı
    public Dictionary<int, decimal?>? OzEnAz { get; set; }     // Sayı: en az
    public Dictionary<int, decimal?>? OzEnCok { get; set; }    // Sayı: en çok

    // ---- İndirimler (Yönetim → Kampanyalar) ----
    public bool SadeceIndirimli { get; set; }                   // ?SadeceIndirimli=true → sadece kampanyalı araçlar
    // Ziyaretçinin seçtiği müşteri bilgileri (Yönetim → Müşteri Bilgileri). Adres çubuğunda: ?Mu[1]=Öğretmen
    public Dictionary<int, string?>? Mu { get; set; }

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
            if (ozellik.PropertyType.IsGenericType && ozellik.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) continue;   // özellik filtreleri aşağıda
            var deger = ozellik.GetValue(this);
            if (deger == null || deger is false || (deger is string metin && metin.Length == 0)) continue;   // boş ya da işaretsiz olanlar adrese yazılmaz

            sonuc[ozellik.Name] = deger switch
            {
                DateTime tarih => Bicim.Kutu(tarih),
                decimal sayi => sayi.ToString(CultureInfo.InvariantCulture),
                _ => deger.ToString()!,
            };
        }
        // Panelden eklenen özellik filtreleri: "Oz[3]" = "Beyaz" gibi
        foreach (var (id, deger) in Oz ?? new())
            if (!string.IsNullOrWhiteSpace(deger)) sonuc[$"{nameof(Oz)}[{id}]"] = deger;
        foreach (var (id, deger) in OzEnAz ?? new())
            if (deger != null) sonuc[$"{nameof(OzEnAz)}[{id}]"] = deger.Value.ToString(CultureInfo.InvariantCulture);
        foreach (var (id, deger) in OzEnCok ?? new())
            if (deger != null) sonuc[$"{nameof(OzEnCok)}[{id}]"] = deger.Value.ToString(CultureInfo.InvariantCulture);
        // Müşteri bilgisi seçimleri: "Mu[1]" = "Öğretmen" gibi
        foreach (var (id, deger) in Mu ?? new())
            if (!string.IsNullOrWhiteSpace(deger)) sonuc[$"{nameof(Mu)}[{id}]"] = deger;

        if (sayfa > 1) sonuc[nameof(Sayfa)] = sayfa.Value.ToString();
        return sonuc;
    }

    /// <summary>
    /// Tarih, şube ve müşteri bilgisi seçimi (araç kartından detaya geçerken taşınır).
    /// musteriSecimleri = false → "Mu[..]" seçimleri dahil edilmez (filtre formundaki gizli kutular için; formda zaten seçim kutusu var).
    /// </summary>
    public Dictionary<string, string> TarihDegerleri(bool musteriSecimleri = true)
    {
        string[] tarihAlanlari = [nameof(Baslangic), nameof(Bitis), nameof(AlisSubeId), nameof(IadeSubeId)];
        return Degerler().Where(d => tarihAlanlari.Contains(d.Key) || (musteriSecimleri && d.Key.StartsWith(nameof(Mu) + "[")))
            .ToDictionary(d => d.Key, d => d.Value);
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
    public List<AracOzelligi> Ozellikler { get; set; } = new();   // filtre panelinde gösterilecek özel filtreler
    public KampanyaBaglami Kampanya { get; set; } = KampanyaBaglami.Bos;   // kartlardaki indirim rozetleri için
    public List<MusteriAlani> KampanyaAlanlari { get; set; } = new();     // "Mesleğiniz" gibi filtrede sorulacak bilgiler
}

public class AracKartiModeli
{
    public Arac Arac { get; set; } = new();
    public AracFiltresi? Filtre { get; set; }
    public int? GunSayisi { get; set; }
    public KampanyaBaglami? Kampanya { get; set; }   // verilirse kartta indirim rozeti / indirimli fiyat görünür
}

public class AracDetaySayfasi
{
    public Arac Arac { get; set; } = new();
    public AracFiltresi Filtre { get; set; } = new();
    public List<Ekstra> Ekstralar { get; set; } = new();
    public List<Sube> Subeler { get; set; } = new();
    public List<Arac> Benzerler { get; set; } = new();
    public bool? Musait { get; set; }   // tarih seçilmediyse null
    public List<AracOzelligi> Ozellikler { get; set; } = new();   // detayda gösterilecek panel özellikleri
    public KampanyaBaglami Kampanya { get; set; } = KampanyaBaglami.Bos;
    public List<MusteriAlani> KampanyaAlanlari { get; set; } = new();   // fiyat kutusunda "Mesleğiniz" seçimi
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
    public KampanyaBaglami Kampanya { get; set; } = KampanyaBaglami.Bos;   // öne çıkan kartlardaki indirim rozetleri
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
