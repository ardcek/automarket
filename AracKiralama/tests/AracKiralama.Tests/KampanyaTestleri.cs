// ============================================================================
// KAMPANYA TESTLERİ: Servisler/KampanyaServisi.cs (KampanyaBaglami) ve
// FiyatServisi'nin kampanyalı hesabı. Veritabanı gerekmez, sadece karar ve hesap.
// Senaryo: "Meslek" bilgisi (numarası 1), Öğretmen %15, Doktor %20, SUV'larda %10.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Servisler;

namespace AracKiralama.Testler;

public class KampanyaTestleri
{
    private const int Meslek = 1;
    private const int SuvKategorisi = 7;
    private static readonly DateTime Bugun = new(2026, 11, 11);

    private static readonly Kampanya Ogretmen = new() { Id = 1, Ad = "Öğretmen indirimi", IndirimOrani = 0.15m, MusteriAlanId = Meslek, MusteriDegeri = "Öğretmen" };
    private static readonly Kampanya Doktor = new() { Id = 2, Ad = "Doktor indirimi", IndirimOrani = 0.20m, MusteriAlanId = Meslek, MusteriDegeri = "Doktor" };
    private static readonly Kampanya Suv = new() { Id = 3, Ad = "SUV kampanyası", IndirimOrani = 0.10m, KategoriId = SuvKategorisi };

    private static readonly Arac Sedan = new() { Id = 10, KategoriId = 1, MarkaId = 1, GunlukFiyat = 1000 };
    private static readonly Arac SuvArac = new() { Id = 11, KategoriId = SuvKategorisi, MarkaId = 1, GunlukFiyat = 2000 };

    private static KampanyaBaglami Baglam(string? meslek, params Kampanya[] kampanyalar)
    {
        var degerler = meslek == null ? new Dictionary<int, string>() : new Dictionary<int, string> { [Meslek] = meslek };
        return new KampanyaBaglami(kampanyalar.ToList(), degerler, Bugun);
    }

    [Fact]
    public void Meslegi_tutan_musteriye_indirim_uygulanir()
    {
        var durum = Baglam("Öğretmen", Ogretmen, Doktor).Durum(Sedan);
        Assert.Same(Ogretmen, durum.Uygulanan);
        Assert.Null(durum.Olasi);   // Doktor kampanyası bu müşteriye uymaz, rozet olarak bile gösterilmez
    }

    [Fact]
    public void Meslek_buyuk_kucuk_harf_farkini_onemsemez()
        => Assert.Same(Ogretmen, Baglam("ÖĞRETMEN", Ogretmen).Durum(Sedan).Uygulanan);

    [Fact]
    public void Meslek_secilmemisse_indirim_uygulanmaz_ama_olasi_kampanya_gorunur()
    {
        var durum = Baglam(null, Ogretmen, Doktor).Durum(Sedan);
        Assert.Null(durum.Uygulanan);
        Assert.Same(Doktor, durum.Olasi);   // en yüksek oranlı olası kampanya rozet olur
        Assert.True(durum.IndirimliMi);     // "sadece indirimli araçlar" filtresinde çıkar
    }

    [Fact]
    public void Baska_meslekte_kampanya_yoksa_arac_indirimli_sayilmaz()
        => Assert.False(Baglam("Mühendis", Ogretmen, Doktor).Durum(Sedan).IndirimliMi);

    [Fact]
    public void Kategori_kosulu_sadece_o_kategorideki_araclara_uygulanir()
    {
        var baglam = Baglam(null, Suv);
        Assert.Same(Suv, baglam.Durum(SuvArac).Uygulanan);
        Assert.False(baglam.Durum(Sedan).IndirimliMi);
    }

    [Fact]
    public void Birden_fazla_kampanya_uyarsa_en_avantajlisi_secilir()
        => Assert.Same(Ogretmen, Baglam("Öğretmen", Ogretmen, Suv).Durum(SuvArac).Uygulanan);   // %15 > %10

    [Fact]
    public void Tarih_araligi_disinda_kampanya_uymaz()
    {
        var yaz = new Kampanya { Ad = "Yaz", IndirimOrani = 0.12m, BaslangicTarihi = new(2026, 6, 1), BitisTarihi = new(2026, 8, 31) };
        var baglam = Baglam(null, yaz);
        Assert.False(baglam.Durum(Sedan, alisTarihi: new DateTime(2026, 11, 11)).IndirimliMi);
        Assert.Same(yaz, baglam.Durum(Sedan, alisTarihi: new DateTime(2026, 7, 15)).Uygulanan);
    }

    [Fact]
    public void En_az_gun_yetmiyorsa_olasi_yetiyorsa_uygulanir()
    {
        var uzun = new Kampanya { Ad = "5 gün fırsatı", IndirimOrani = 0.12m, EnAzGun = 5 };
        var baglam = Baglam(null, uzun);
        Assert.Same(uzun, baglam.Durum(Sedan, gunSayisi: 3).Olasi);
        Assert.Same(uzun, baglam.Durum(Sedan, gunSayisi: 5).Uygulanan);
    }

    [Fact]
    public void Arac_ozelligi_kosulu_kontrol_edilir()
    {
        var klimali = new Kampanya { Ad = "Klimalı", IndirimOrani = 0.05m, AracOzellikId = 4, AracOzellikDegeri = "true" };
        var klimaliArac = new Arac { Id = 12, OzellikDegerleri = [new AracOzellikDegeri { OzellikId = 4, Deger = "true" }] };
        Assert.Same(klimali, Baglam(null, klimali).Durum(klimaliArac).Uygulanan);
        Assert.False(Baglam(null, klimali).Durum(Sedan).IndirimliMi);
    }

    // ---------------- FİYAT SERVİSİ İLE BİRLİKTE ----------------
    private static readonly DateTime Alis = new(2026, 11, 20, 10, 0, 0);

    [Fact]
    public void Fiyat_dokumunde_kampanya_adi_ve_indirimi_gorunur()
    {
        var t = new FiyatServisi(new SiteAyarlari()).Hesapla(Sedan, Alis, Alis.AddDays(3), new(), false, Baglam("Öğretmen", Ogretmen));
        Assert.Equal(0.15m, t.IndirimOrani);
        Assert.Equal(450, t.Indirim);               // 3 gün × 1000 × %15
        Assert.Equal(2550, t.Toplam);
        Assert.Equal("Öğretmen indirimi", t.IndirimAciklamasi);
    }

    [Fact]
    public void Uzun_donem_ile_kampanyadan_avantajli_olan_uygulanir()
    {
        var fiyat = new FiyatServisi(new SiteAyarlari());   // 30+ gün %20
        var t = fiyat.Hesapla(Sedan, Alis, Alis.AddDays(30), new(), false, Baglam("Öğretmen", Ogretmen));
        Assert.Equal(0.20m, t.IndirimOrani);
        Assert.Equal("Uzun dönem indirimi", t.IndirimAciklamasi);
    }

    [Fact]
    public void Ayar_aciksa_uzun_donem_ve_kampanya_toplanir()
    {
        var fiyat = new FiyatServisi(new SiteAyarlari { KampanyaUzunDonemleBirlesir = true });   // 7+ gün %10
        var t = fiyat.Hesapla(Sedan, Alis, Alis.AddDays(7), new(), false, Baglam("Öğretmen", Ogretmen));
        Assert.Equal(0.25m, t.IndirimOrani);        // %10 + %15
        Assert.StartsWith("Uzun dönem + ", t.IndirimAciklamasi);
    }

    [Fact]
    public void Kampanya_yoksa_eski_hesap_aynen_calisir()
    {
        var t = new FiyatServisi(new SiteAyarlari()).Hesapla(Sedan, Alis, Alis.AddDays(3), new(), false, KampanyaBaglami.Bos);
        Assert.Equal(0, t.Indirim);
        Assert.Null(t.IndirimAciklamasi);
    }
}
