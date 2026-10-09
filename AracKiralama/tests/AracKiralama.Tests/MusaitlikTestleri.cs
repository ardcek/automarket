// ============================================================================
// MÜSAİTLİK TESTLERİ: "Araç bu tarihlerde boş mu?" kuralları (Servisler/MusaitlikServisi.cs)
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Servisler;

namespace AracKiralama.Testler;

public class MusaitlikTestleri : IDisposable
{
    private readonly TestVeriTabani _t = new();
    private readonly MusaitlikServisi _musaitlik;

    public MusaitlikTestleri()
    {
        _musaitlik = new MusaitlikServisi(_t.Vt);
    }

    public void Dispose() => _t.Dispose();

    [Fact]
    public void Uc_uca_eklenen_araliklar_cakismaz()
    {
        var a = TestVeriTabani.Ileride(5);
        Assert.False(MusaitlikServisi.Cakisiyor(a, a.AddDays(2), a.AddDays(2), a.AddDays(4)));
        Assert.True(MusaitlikServisi.Cakisiyor(a, a.AddDays(2), a.AddDays(1), a.AddDays(4)));
        Assert.True(MusaitlikServisi.Cakisiyor(a, a.AddDays(10), a.AddDays(2), a.AddDays(3)));   // içinde kalan
    }

    [Fact]
    public async Task Bos_arac_musaittir()
        => Assert.True(await _musaitlik.MusaitMiAsync(_t.Ekonomik.Id, TestVeriTabani.Ileride(5), TestVeriTabani.Ileride(8)));

    [Theory]
    [InlineData(RezervasyonDurumu.Onaylandi, false)]
    [InlineData(RezervasyonDurumu.Kirada, false)]
    [InlineData(RezervasyonDurumu.OnayBekliyor, true)]     // bekleyen talep aracı kilitlemez
    [InlineData(RezervasyonDurumu.IptalEdildi, true)]
    [InlineData(RezervasyonDurumu.Reddedildi, true)]
    public async Task Sadece_onayli_ve_kiradaki_rezervasyonlar_araci_kilitler(RezervasyonDurumu durum, bool beklenen)
    {
        _t.RezervasyonEkle(_t.Ekonomik, _t.DigerMusteri, TestVeriTabani.Ileride(5), TestVeriTabani.Ileride(8), durum);
        Assert.Equal(beklenen, await _musaitlik.MusaitMiAsync(_t.Ekonomik.Id, TestVeriTabani.Ileride(6), TestVeriTabani.Ileride(7)));
    }

    [Fact]
    public async Task Bir_kiralama_biterken_digeri_baslayabilir()
    {
        _t.RezervasyonEkle(_t.Ekonomik, _t.DigerMusteri, TestVeriTabani.Ileride(5), TestVeriTabani.Ileride(8), RezervasyonDurumu.Onaylandi);
        Assert.True(await _musaitlik.MusaitMiAsync(_t.Ekonomik.Id, TestVeriTabani.Ileride(8), TestVeriTabani.Ileride(10)));
    }

    [Fact]
    public async Task Tamamlanmamis_bakim_araci_kilitler_tamamlanmis_kilitlemez()
    {
        var bakim = new Bakim { Arac = _t.Ekonomik, BaslangicTarihi = TestVeriTabani.Ileride(5), BitisTarihi = TestVeriTabani.Ileride(7) };
        _t.Vt.Bakimlar.Add(bakim);
        await _t.Vt.SaveChangesAsync();
        Assert.False(await _musaitlik.MusaitMiAsync(_t.Ekonomik.Id, TestVeriTabani.Ileride(6), TestVeriTabani.Ileride(9)));

        bakim.Tamamlandi = true;
        await _t.Vt.SaveChangesAsync();
        Assert.True(await _musaitlik.MusaitMiAsync(_t.Ekonomik.Id, TestVeriTabani.Ileride(6), TestVeriTabani.Ileride(9)));
    }

    [Fact]
    public async Task Pasif_arac_hicbir_tarihte_musait_degildir()
    {
        _t.Ekonomik.Durum = AracDurumu.Pasif;
        await _t.Vt.SaveChangesAsync();
        Assert.False(await _musaitlik.MusaitMiAsync(_t.Ekonomik.Id, TestVeriTabani.Ileride(30), TestVeriTabani.Ileride(31)));
    }

    [Fact]
    public void Liste_filtresi_dolu_araci_cikarir()
    {
        _t.RezervasyonEkle(_t.Luks, _t.DigerMusteri, TestVeriTabani.Ileride(5), TestVeriTabani.Ileride(8), RezervasyonDurumu.Onaylandi);

        var musaitler = _musaitlik.SadeceMusaitler(_t.Vt.Araclar, TestVeriTabani.Ileride(6), TestVeriTabani.Ileride(7))
            .Select(a => a.Id).ToList();

        Assert.Contains(_t.Ekonomik.Id, musaitler);
        Assert.DoesNotContain(_t.Luks.Id, musaitler);
    }
}
