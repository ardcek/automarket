// ============================================================================
// MESLEK GRUBU KAMPANYASI ve ARAÇ KATALOĞU TESTLERİ
//   - Bir kampanya birden çok meslek tutabilir ("Sağlık çalışanı|Emekli"): herhangi biri yeter.
//   - Hazır özellik (motor hacmi, kasa...) eklenince araçların değeri Yardimcilar/AracKatalogu.cs'ten dolar.
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Testler;

public class KatalogVeGrupTestleri
{
    private const int Meslek = 1;
    private static readonly Arac Araba = new() { Id = 10, KategoriId = 1, MarkaId = 1, GunlukFiyat = 1000 };
    private static readonly Kampanya Grup = new() { Id = 1, Ad = "Meslek grubu", IndirimOrani = 0.10m, MusteriAlanId = Meslek, MusteriDegeri = "Sağlık çalışanı|Kamu çalışanı|Emekli" };
    private static readonly Kampanya Ogretmen = new() { Id = 2, Ad = "Öğretmen", IndirimOrani = 0.50m, MusteriAlanId = Meslek, MusteriDegeri = "Öğretmen" };

    private static KampanyaBaglami Baglam(string meslek)
        => new([Grup, Ogretmen], new Dictionary<int, string> { [Meslek] = meslek }, new DateTime(2026, 11, 26));

    [Theory]
    [InlineData("Emekli", 1)]
    [InlineData("kamu çalışanı", 1)]      // büyük-küçük harf önemsiz
    [InlineData("Öğretmen", 2)]           // öğretmen %50'yi alır, grubun %10'u üst üste binmez
    public void Meslek_grubundaki_herhangi_bir_meslek_indirimi_alir(string meslek, int beklenenKampanya)
        => Assert.Equal(beklenenKampanya, Baglam(meslek).Durum(Araba).Uygulanan!.Id);

    [Fact]
    public void Grupta_olmayan_meslek_indirim_almaz()
        => Assert.Null(Baglam("Mühendis").Durum(Araba).Uygulanan);

    [Fact]
    public void Formda_isaretlenen_meslekler_tek_kampanyada_birlesir_ve_geri_okunur()
    {
        var form = new KampanyaFormu { Ad = "Grup", MusteriKosullari = ["3|Sağlık çalışanı", "3|Emekli"] };
        var k = new Kampanya();
        form.Aktar(k);
        Assert.Equal(3, k.MusteriAlanId);
        Assert.Equal("Sağlık çalışanı|Emekli", k.MusteriDegeri);
        Assert.Equal("Meslek: Sağlık çalışanı, Emekli", k.KosulOzeti().Replace("Müşteri bilgisi", "Meslek"));
        Assert.Equal(form.MusteriKosullari, KampanyaFormu.Olustur(k).MusteriKosullari);
    }

    [Fact]
    public async Task Hazir_ozellik_eklenince_araclarin_degeri_katalogdan_dolar()
    {
        using var t = new TestVeriTabani();   // araçlar: Renault Clio (Ekonomi) ve Renault Talisman (Lüks)
        var motor = new AracOzelligi { Ad = "Motor hacmi", Tur = OzellikTuru.Sayi, Birim = "cc", SablonKod = "motor-hacmi" };
        var kasa = new AracOzelligi { Ad = "Kasa tipi", Tur = OzellikTuru.Secim, Secenekler = OzellikSablonlari.Bul("kasa")!.Secenekler, SablonKod = "kasa" };
        var evcil = new AracOzelligi { Ad = "Evcil hayvan kabul edilir", Tur = OzellikTuru.EvetHayir, SablonKod = "evcil-hayvan" };
        t.Vt.AracOzellikleri.AddRange(motor, kasa, evcil);
        await t.Vt.SaveChangesAsync();

        Assert.Equal(2, (await AracKatalogu.DoldurAsync(t.Vt, motor)).Doldurulan);
        await AracKatalogu.DoldurAsync(t.Vt, kasa);
        Assert.Equal(0, (await AracKatalogu.DoldurAsync(t.Vt, evcil)).Doldurulan);   // işletme kararı: katalogdan dolmaz

        var degerler = await t.Vt.AracOzellikDegerleri.Where(d => d.AracId == t.Ekonomik.Id).ToDictionaryAsync(d => d.OzellikId, d => d.Deger);
        Assert.Equal("999", degerler[motor.Id]);          // Renault Clio
        Assert.Equal("Hatchback", degerler[kasa.Id]);
        Assert.Equal(1598m, (await t.Vt.AracOzellikDegerleri.SingleAsync(d => d.AracId == t.Luks.Id && d.OzellikId == motor.Id)).SayiDeger);   // Talisman, filtre sayıyla çalışır
    }

    [Fact]
    public async Task Elle_girilen_deger_katalogla_ezilmez()
    {
        using var t = new TestVeriTabani();
        var bagaj = new AracOzelligi { Ad = "Bagaj hacmi", Tur = OzellikTuru.Sayi, Birim = "lt" };   // şablon kodu yok: adından tanınır
        t.Vt.AracOzellikleri.Add(bagaj);
        t.Vt.AracOzellikDegerleri.Add(new AracOzellikDegeri { Arac = t.Ekonomik, Ozellik = bagaj, Deger = "400", SayiDeger = 400 });
        await t.Vt.SaveChangesAsync();

        var (dolan, _) = await AracKatalogu.DoldurAsync(t.Vt, bagaj);

        Assert.Equal(1, dolan);   // sadece boş olan Talisman
        Assert.Equal("400", (await t.Vt.AracOzellikDegerleri.SingleAsync(d => d.AracId == t.Ekonomik.Id)).Deger);
    }

    [Fact]
    public void Katalogdaki_secim_degerleri_sablon_seceneklerinde_var()
    {
        var kasalar = OzellikSablonlari.Bul("kasa")!.Secenekler!.Split('|');
        var cekisler = OzellikSablonlari.Bul("cekis")!.Secenekler!.Split('|');
        Assert.All(AracKatalogu.Modeller.Values, m => { Assert.Contains(m.Kasa, kasalar); Assert.Contains(m.Cekis, cekisler); });
    }
}
