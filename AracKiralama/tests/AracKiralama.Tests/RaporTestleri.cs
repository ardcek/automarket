// ============================================================================
// RAPOR TESTLERİ: Yönetim → Raporlar (Servisler/RaporServisi.cs)
// Kiralı gün, aracın GERÇEKTE dışarıda olduğu süreye (teslim → iade) göre hesaplanır.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Servisler;

namespace AracKiralama.Testler;

public class RaporTestleri
{
    [Fact]
    public async Task Planlanandan_erken_teslim_ve_iade_edilen_kiralama_gun_ve_gelir_olarak_sayilir()
    {
        using var t = new TestVeriTabani();
        // Rezervasyon ileride (3-6 gün sonra) planlanmış ama araç bugün verilip bugün geri alınmış
        var r = t.RezervasyonEkle(t.Luks, t.Musteri, TestVeriTabani.Ileride(3), TestVeriTabani.Ileride(6), RezervasyonDurumu.Tamamlandi);
        r.TeslimTarihi = DateTime.Today.AddHours(9);
        r.IadeTarihi = DateTime.Today.AddHours(11);
        r.ToplamTutar = 18_530;
        await t.Vt.SaveChangesAsync();

        var rapor = await new RaporServisi(t.Vt).RaporAsync(DateTime.Today.AddDays(-29), DateTime.Today);
        var satir = rapor.Araclar.Single(a => a.AracId == t.Luks.Id);

        Assert.Equal(1, satir.KiralamaSayisi);
        Assert.Equal(1, satir.KiraliGun);                 // 2 saat = başlamış 1 gün
        Assert.True(satir.KullanimOrani > 0);
        Assert.Equal(18_530, rapor.ToplamGelir);
        Assert.Equal(t.Istanbul.Ad, Assert.Single(rapor.Subeler).Sube);
    }

    [Fact]
    public async Task Geciken_kiralama_bugune_kadar_sayilir()
    {
        using var t = new TestVeriTabani();
        var r = t.RezervasyonEkle(t.Ekonomik, t.Musteri, DateTime.Today.AddDays(-5), DateTime.Today.AddDays(-2), RezervasyonDurumu.Kirada);
        r.TeslimTarihi = DateTime.Today.AddDays(-5);
        await t.Vt.SaveChangesAsync();

        var rapor = await new RaporServisi(t.Vt).RaporAsync(DateTime.Today.AddDays(-29), DateTime.Today);

        Assert.True(rapor.Araclar.Single(a => a.AracId == t.Ekonomik.Id).KiraliGun >= 5);   // hâlâ dışarıda
    }
}
