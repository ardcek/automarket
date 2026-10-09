// ============================================================================
// FİYAT TESTLERİ: Servisler/FiyatServisi.cs kuralları. Veritabanı gerekmez, sadece hesap.
// Oranlar SiteAyarlari'nın varsayılan değerleridir (%10 haftalık, %20 aylık, 750 ₺ tek yön...).
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Servisler;

namespace AracKiralama.Testler;

public class FiyatTestleri
{
    private readonly FiyatServisi _fiyat = new(new SiteAyarlari());
    private static readonly Arac Araba = new() { GunlukFiyat = 1000, Depozito = 3000 };
    private static readonly DateTime Baslangic = new(2026, 11, 1, 10, 0, 0);

    [Theory]
    [InlineData(24, 1)]     // tam 1 gün
    [InlineData(25, 2)]     // 1 saat aşım → 2 gün
    [InlineData(1, 1)]      // 1 saatlik kiralama bile 1 gün
    [InlineData(72, 3)]
    public void Gun_sayisi_baslayan_her_24_saat_icin_artar(int saat, int beklenenGun)
        => Assert.Equal(beklenenGun, _fiyat.GunSayisi(Baslangic, Baslangic.AddHours(saat)));

    [Fact]
    public void Kisa_kiralamada_indirim_yoktur()
    {
        var t = _fiyat.Hesapla(Araba, Baslangic, Baslangic.AddDays(3), new(), farkliSubeyeIade: false);
        Assert.Equal(3, t.GunSayisi);
        Assert.Equal(3000, t.KiraTutari);
        Assert.Equal(0, t.Indirim);
        Assert.Equal(3000, t.Toplam);
        Assert.Equal(3000, t.Depozito);
    }

    [Fact]
    public void Yedi_gun_ve_uzeri_yuzde_10_indirim()
    {
        var t = _fiyat.Hesapla(Araba, Baslangic, Baslangic.AddDays(7), new(), false);
        Assert.Equal(0.10m, t.IndirimOrani);
        Assert.Equal(700, t.Indirim);
        Assert.Equal(6300, t.Toplam);
    }

    [Fact]
    public void Otuz_gun_ve_uzeri_yuzde_20_indirim()
    {
        var t = _fiyat.Hesapla(Araba, Baslangic, Baslangic.AddDays(30), new(), false);
        Assert.Equal(0.20m, t.IndirimOrani);
        Assert.Equal(24_000, t.Toplam);
    }

    [Fact]
    public void Admin_panelinde_degistirilen_indirim_orani_hemen_gecerli_olur()
    {
        // Yönetim → Ayarlar → Fiyatlar'da "haftalık indirim" 5 gün ve %15 yapılmış gibi
        var ayarlar = new SiteAyarlari { HaftalikIndirimGunu = 5, HaftalikIndirimOrani = 0.15m, TekYonUcreti = 1000 };
        var t = new FiyatServisi(ayarlar).Hesapla(Araba, Baslangic, Baslangic.AddDays(5), new(), farkliSubeyeIade: true);

        Assert.Equal(750, t.Indirim);                  // 5000 × %15
        Assert.Equal(1000, t.TekYonUcreti);
        Assert.Equal(5000 - 750 + 1000, t.Toplam);
    }

    [Fact]
    public void Ekstralar_gun_ile_carpilir_ve_indirime_girmez()
    {
        var ekstralar = new List<Ekstra>
        {
            new() { Id = 1, Ad = "Kasko", GunlukFiyat = 300 },
            new() { Id = 2, Ad = "GPS", GunlukFiyat = 100 },
        };
        var t = _fiyat.Hesapla(Araba, Baslangic, Baslangic.AddDays(7), ekstralar, false);

        Assert.Equal(2800, t.EkstraTutari);                // (300 + 100) × 7
        Assert.Equal(7000 - 700 + 2800, t.Toplam);         // indirim sadece kira bedeline
        Assert.Equal(2, t.Ekstralar.Count);
    }

    [Fact]
    public void Farkli_subeye_iade_tek_yon_ucreti_ekler()
    {
        var t = _fiyat.Hesapla(Araba, Baslangic, Baslangic.AddDays(2), new(), farkliSubeyeIade: true);
        Assert.Equal(750, t.TekYonUcreti);
        Assert.Equal(2750, t.Toplam);
    }

    [Fact]
    public void Tolerans_icindeki_gecikme_ucretsizdir()
        => Assert.Equal(0, _fiyat.GecikmeUcreti(1000, Baslangic, Baslangic.AddMinutes(45)));

    [Fact]
    public void Gec_iade_her_gun_icin_bir_bucuk_kat_ucretlenir()
    {
        Assert.Equal(1500, _fiyat.GecikmeUcreti(1000, Baslangic, Baslangic.AddHours(3)));    // 1 gün geç
        Assert.Equal(3000, _fiyat.GecikmeUcreti(1000, Baslangic, Baslangic.AddHours(30)));   // 2 gün geç
    }

    [Theory]
    [InlineData(YakitSeviyesi.Dolu, YakitSeviyesi.Dolu, 0)]
    [InlineData(YakitSeviyesi.Dolu, YakitSeviyesi.Yarim, 900)]     // 2 çeyrek eksik × 450
    [InlineData(YakitSeviyesi.Yarim, YakitSeviyesi.Dolu, 0)]       // fazla yakıt ücretlendirilmez
    public void Eksik_yakit_ceyrek_depo_basina_ucretlenir(YakitSeviyesi teslimde, YakitSeviyesi iadede, decimal beklenen)
        => Assert.Equal(beklenen, _fiyat.YakitUcreti(teslimde, iadede));

    [Fact]
    public void Son_24_saatte_iptal_bir_gunluk_ucret_keser()
    {
        var r = new Rezervasyon { Durum = RezervasyonDurumu.Onaylandi, GunlukFiyat = 1000, BaslangicTarihi = Baslangic };
        Assert.Equal(1000, _fiyat.IptalUcreti(r, Baslangic.AddHours(-5)));
        Assert.Equal(0, _fiyat.IptalUcreti(r, Baslangic.AddDays(-3)));
    }
}
