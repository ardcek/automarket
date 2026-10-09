// ============================================================================
// TEST VERİTABANI: her test için bellekte (RAM) çalışan, boş bir SQLite veritabanı.
// Gerçek uygulamayla aynı tabloları kullanır ama diskte dosya oluşturmaz.
// İçinde 2 şube, 2 kategori, 2 araç, 4 kullanıcı ve 1 ekstra hazır gelir.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Testler;

public class TestVeriTabani : IDisposable
{
    private readonly SqliteConnection _baglanti;
    public VeriTabani Vt { get; }

    /// <summary>Varsayılan ayarlar (admin panelinde hiçbir şey değiştirilmemiş hali).</summary>
    public SiteAyarlari Ayarlar { get; } = new();

    public Sube Istanbul { get; }
    public Sube Ankara { get; }
    public Arac Ekonomik { get; }
    public Arac Luks { get; }
    public Kullanici Musteri { get; }
    public Kullanici DigerMusteri { get; }
    public Kullanici GencMusteri { get; }
    public Kullanici Personel { get; }
    public Ekstra Kasko { get; }

    /// <summary>Bugünden "gun" gün sonra, saat "saat"te.</summary>
    public static DateTime Ileride(int gun, int saat = 10) => DateTime.Today.AddDays(gun).AddHours(saat);

    public TestVeriTabani()
    {
        _baglanti = new SqliteConnection("DataSource=:memory:");
        _baglanti.Open();
        Vt = new VeriTabani(new DbContextOptionsBuilder<VeriTabani>().UseSqlite(_baglanti).Options);
        Vt.Database.EnsureCreated();

        Istanbul = new Sube { Ad = "İstanbul", Sehir = "İstanbul", Adres = "-", Telefon = "-" };
        Ankara = new Sube { Ad = "Ankara", Sehir = "Ankara", Adres = "-", Telefon = "-" };
        var marka = new Marka { Ad = "Renault" };
        var benzin = new YakitTuru { Ad = "Benzin" };
        var manuel = new VitesTuru { Ad = "Manuel" };
        var ekonomikSinif = new Kategori { Ad = "Ekonomik", MinYas = 21, MinEhliyetYili = 1 };
        var luksSinif = new Kategori { Ad = "Lüks", MinYas = 27, MinEhliyetYili = 5 };

        Ekonomik = new Arac { Plaka = "34 AB 100", Marka = marka, Model = "Clio", Yil = 2023, Kategori = ekonomikSinif,
            YakitTuru = benzin, VitesTuru = manuel, GunlukFiyat = 1000, Depozito = 3000, Sube = Istanbul, Kilometre = 20_000 };
        Luks = new Arac { Plaka = "34 AB 200", Marka = marka, Model = "Talisman", Yil = 2024, Kategori = luksSinif,
            YakitTuru = benzin, VitesTuru = manuel, GunlukFiyat = 3000, Depozito = 12000, Sube = Istanbul, Kilometre = 10_000 };

        Musteri = KullaniciOlustur("ayse@test.com", new DateTime(1990, 1, 1), new DateTime(2010, 1, 1));
        DigerMusteri = KullaniciOlustur("mehmet@test.com", new DateTime(1985, 1, 1), new DateTime(2005, 1, 1));
        GencMusteri = KullaniciOlustur("genc@test.com", DateTime.Today.AddYears(-20), DateTime.Today.AddYears(-1));
        Personel = KullaniciOlustur("personel@test.com", new DateTime(1980, 1, 1), new DateTime(2000, 1, 1));

        Kasko = new Ekstra { Ad = "Tam Kasko", GunlukFiyat = 300 };

        Vt.AddRange(Istanbul, Ankara, Ekonomik, Luks, Musteri, DigerMusteri, GencMusteri, Personel, Kasko);
        Vt.SaveChanges();
    }

    private static Kullanici KullaniciOlustur(string eposta, DateTime dogum, DateTime ehliyet)
    {
        return new Kullanici
        {
            UserName = eposta, Email = eposta, Ad = "Test", Soyad = eposta[..3],
            DogumTarihi = dogum, EhliyetTarihi = ehliyet,
        };
    }

    public RezervasyonServisi RezervasyonServisi()
        => new(Vt, new FiyatServisi(Ayarlar), new MusaitlikServisi(Vt), Ayarlar);

    /// <summary>Kurallara bakmadan, doğrudan veritabanına istenen durumda bir rezervasyon ekler.</summary>
    public Rezervasyon RezervasyonEkle(Arac arac, Kullanici musteri, DateTime baslangic, DateTime bitis, RezervasyonDurumu durum)
    {
        var r = new Rezervasyon
        {
            Kod = $"T-{Guid.NewGuid():N}"[..12], Arac = arac, Musteri = musteri, AlisSube = Istanbul, IadeSube = Istanbul,
            BaslangicTarihi = baslangic, BitisTarihi = bitis, Durum = durum, GunlukFiyat = arac.GunlukFiyat, GunSayisi = 1,
        };
        Vt.Rezervasyonlar.Add(r);
        Vt.SaveChanges();
        return r;
    }

    public void Dispose()
    {
        Vt.Dispose();
        _baglanti.Dispose();
    }
}
