// ============================================================================
// GARAJ TESTLERİ: Yardimcilar/GarajSecimi.cs — ana sayfadaki "Garaj" bölümüne hangi araçlar, hangi sırayla girer?
// Veritabanı gerekmez: araç listesi + kiralama sayıları → seçim.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Yardimcilar;

namespace AracKiralama.Testler;

public class GarajTestleri
{
    private static Arac Arac(int id, string model, decimal fiyat = 1000, bool oneCikan = false, int markaId = 1,
        string? gorsel = "/img/cars/x.jpg", AracDurumu durum = AracDurumu.Musait)
        => new() { Id = id, MarkaId = markaId, Model = model, GunlukFiyat = fiyat, OneCikan = oneCikan, GorselAdresi = gorsel, Durum = durum };

    private static readonly Dictionary<int, int> KiralamaYok = new();

    private static List<int> Sirasi(List<GarajAraci> secilen) => secilen.Select(g => g.Arac.Id).ToList();

    [Fact]
    public void En_cok_kiralanan_once_gelir_ve_sayisi_tasinir()
    {
        var araclar = new[] { Arac(1, "Clio"), Arac(2, "Polo"), Arac(3, "Egea") };
        var secilen = GarajSecimi.Sec(araclar, new Dictionary<int, int> { [2] = 9, [3] = 4 }, 3);

        Assert.Equal([2, 3, 1], Sirasi(secilen));
        Assert.Equal(9, secilen[0].KiralamaSayisi);
        Assert.Equal(0, secilen[2].KiralamaSayisi);   // hiç kiralanmamış araç da tamamlamak için girebilir
    }

    [Fact]
    public void Kiralama_yoksa_once_one_cikanlar_sonra_pahali_olanlar_tamamlar()
    {
        var araclar = new[] { Arac(1, "Clio", 900), Arac(2, "A7", 6000), Arac(3, "Corolla", 1500, oneCikan: true) };
        Assert.Equal([3, 2, 1], Sirasi(GarajSecimi.Sec(araclar, KiralamaYok, 3)));
    }

    [Fact]
    public void Adet_sinirina_uyar_ve_en_fazla_sekiz_arac_alir()
    {
        var araclar = Enumerable.Range(1, 12).Select(i => Arac(i, "Model " + i)).ToList();
        Assert.Equal(2, GarajSecimi.Sec(araclar, KiralamaYok, 2).Count);
        Assert.Equal(GarajSecimi.EnFazla, GarajSecimi.Sec(araclar, KiralamaYok, 50).Count);
        Assert.Empty(GarajSecimi.Sec(araclar, KiralamaYok, 0));
        Assert.Empty(GarajSecimi.Sec(araclar, KiralamaYok, -3));
    }

    [Fact]
    public void Fotografsiz_ve_pasif_arac_garaja_girmez()
    {
        var araclar = new[] { Arac(1, "Clio", gorsel: null), Arac(2, "Polo", gorsel: " "), Arac(3, "Egea", durum: AracDurumu.Pasif), Arac(4, "Golf") };
        var secilen = GarajSecimi.Sec(araclar, new Dictionary<int, int> { [1] = 50, [3] = 50 }, 4);
        Assert.Equal([4], Sirasi(secilen));
    }

    [Fact]
    public void Ayni_marka_ve_model_iki_kez_cikmaz_en_cok_kiralanan_kalir()
    {
        var araclar = new[] { Arac(1, "Clio"), Arac(2, "clio "), Arac(3, "Clio", markaId: 2), Arac(4, "Polo") };
        var secilen = GarajSecimi.Sec(araclar, new Dictionary<int, int> { [2] = 5, [1] = 1 }, 4);
        Assert.Equal([2, 3, 4], Sirasi(secilen));   // 1 ile 2 aynı model (büyük/küçük harf ve boşluk önemsiz); başka markanın "Clio"su ayrı
    }
}
