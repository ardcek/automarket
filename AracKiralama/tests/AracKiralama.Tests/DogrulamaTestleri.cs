// ============================================================================
// DOĞRULAMA TESTLERİ: T.C. Kimlik No, plaka, görsel türü ve sayfa metni güvenliği
// ============================================================================
using AracKiralama.Servisler;
using AracKiralama.Yardimcilar;

namespace AracKiralama.Testler;

public class DogrulamaTestleri
{
    [Theory]
    [InlineData("10000000146", true)]     // algoritmaya uygun örnek numara
    [InlineData("10000000147", false)]    // son hane yanlış
    [InlineData("01234567890", false)]    // 0 ile başlayamaz
    [InlineData("1234567890", false)]     // 10 hane
    [InlineData("1234567890a", false)]
    [InlineData(null, false)]
    public void Tc_kimlik_algoritmasi(string? no, bool beklenen) => Assert.Equal(beklenen, TcKimlikNo.GecerliMi(no));

    [Fact]
    public void Uretilen_tc_numaralari_gecerlidir()
    {
        var rastgele = new Random(1);
        for (var i = 0; i < 200; i++) Assert.True(TcKimlikNo.GecerliMi(TcKimlikNo.Uret(rastgele)));
    }

    [Theory]
    [InlineData("34 ABC 123", true)]
    [InlineData("06 A 1234", true)]
    [InlineData("35abc12", true)]         // boşluksuz ve küçük harf de kabul
    [InlineData("82 ABC 123", false)]     // il kodu 01-81
    [InlineData("34 ABCD 12", false)]     // en fazla 3 harf
    [InlineData("ABC 123", false)]
    public void Plaka_bicimi(string plaka, bool beklenen) => Assert.Equal(beklenen, Plaka.GecerliMi(plaka));

    [Fact]
    public void Plaka_standart_bicime_donusturulur() => Assert.Equal("34 ABC 123", Plaka.Duzenle("34abc123"));

    [Fact]
    public void Gorsel_turu_uzantiya_degil_dosya_imzasina_gore_belirlenir()
    {
        Assert.Equal("image/png", GorselServisi.ResimTuru(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A }));
        Assert.Equal("image/jpeg", GorselServisi.ResimTuru(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));
        Assert.Null(GorselServisi.ResimTuru("<svg onload=alert(1)>"u8.ToArray()));   // adı .png olsa bile reddedilir
    }

    [Fact]
    public void Sayfa_metni_paragraf_ve_maddeye_donusur_ama_html_calistirmaz()
    {
        var html = Bicim.Paragraflar("## Şartlar\nİlk paragraf\n\n- Madde <b>bir</b>\n- <script>alert(1)</script>").ToString();

        Assert.Equal("<h2 class=\"h5 mt-4\">Şartlar</h2><p>İlk paragraf</p>"
                     + "<ul><li>Madde &lt;b&gt;bir&lt;/b&gt;</li><li>&lt;script&gt;alert(1)&lt;/script&gt;</li></ul>", html);
    }

    [Theory]   // İade / teslim formundaki km kutusu: Türkçe binlik nokta ile yazılsa da doğru okunur
    [InlineData("82.020", 82020)]
    [InlineData("82020", 82020)]
    [InlineData("1.250.000", 1250000)]
    [InlineData(" 82 450 km", 82450)]
    [InlineData("", null)]
    public void Km_binlik_noktayla_yazilsa_da_okunur(string yazi, int? beklenen)
        => Assert.Equal(beklenen, KmBaglayici.Oku(yazi));

    [Fact]
    public void Profildeki_butun_alanlar_dolmadan_eksik_sayilir()
    {
        var k = new AracKiralama.Modeller.Kullanici { DogumTarihi = new DateTime(1990, 1, 1), EhliyetTarihi = new DateTime(2010, 1, 1) };
        Assert.Equal(["telefon", "T.C. Kimlik No", "ehliyet no"], k.EksikProfilBilgileri());
        k.PhoneNumber = "0532 123 45 67"; k.TcNo = "10000000146"; k.EhliyetNo = "123456";
        Assert.Empty(k.EksikProfilBilgileri());
    }
}
