// ============================================================================
// REZERVASYON TESTLERİ: yaşam döngüsü (durum geçişleri) ve iş kuralları
// (Servisler/RezervasyonServisi.cs)
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Testler;

public class RezervasyonTestleri : IDisposable
{
    private readonly TestVeriTabani _t = new();
    private readonly RezervasyonServisi _servis;

    public RezervasyonTestleri()
    {
        _servis = _t.RezervasyonServisi();
    }

    public void Dispose() => _t.Dispose();

    /// <summary>Kısa yol: "baslangicGunu" gün sonra başlayan, "gun" gün süren bir talep oluşturur.</summary>
    private Task<Rezervasyon> TalepOlustur(Kullanici musteri, Arac arac, int baslangicGunu = 10, int gun = 3,
        Sube? iadeSubesi = null, List<int>? ekstralar = null)
    {
        return _servis.OlusturAsync(musteri.Id, arac.Id, _t.Istanbul.Id, (iadeSubesi ?? _t.Istanbul).Id,
            TestVeriTabani.Ileride(baslangicGunu), TestVeriTabani.Ileride(baslangicGunu + gun), ekstralar ?? new(), null);
    }

    [Fact]
    public async Task Rezervasyon_olusturulur_kod_ve_fiyat_kaydedilir()
    {
        var r = await TalepOlustur(_t.Musteri, _t.Ekonomik, ekstralar: new() { _t.Kasko.Id }, iadeSubesi: _t.Ankara);

        Assert.Equal(RezervasyonDurumu.OnayBekliyor, r.Durum);
        Assert.Matches(@"^RZ-\d{4}-0001$", r.Kod);
        Assert.Equal(3, r.GunSayisi);
        Assert.Equal(3000 + 900 + 750, r.ToplamTutar);    // kira + kasko + tek yön
        Assert.Single(r.Ekstralar);
    }

    [Fact]
    public async Task Kodlar_sirayla_artar()
    {
        await TalepOlustur(_t.Musteri, _t.Ekonomik, baslangicGunu: 10);
        var ikinci = await TalepOlustur(_t.Musteri, _t.Ekonomik, baslangicGunu: 20);
        Assert.EndsWith("-0002", ikinci.Kod);
    }

    [Fact]
    public async Task Otomatik_onay_acikken_talep_dogrudan_onaylanir()
    {
        _t.Ayarlar.OtomatikOnay = true;    // Yönetim → Ayarlar → Rezervasyon
        var r = await TalepOlustur(_t.Musteri, _t.Ekonomik);
        Assert.Equal(RezervasyonDurumu.Onaylandi, r.Durum);
    }

    [Fact]
    public async Task Gecmis_tarihe_rezervasyon_yapilamaz()
    {
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => _servis.OlusturAsync(_t.Musteri.Id, _t.Ekonomik.Id,
            _t.Istanbul.Id, _t.Istanbul.Id, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(1), new(), null));
        Assert.Contains("geçmişte", hata.Message);
    }

    [Fact]
    public async Task Olmayan_subeyle_rezervasyon_yapilamaz()
    {
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => _servis.OlusturAsync(_t.Musteri.Id, _t.Ekonomik.Id,
            9999, _t.Istanbul.Id, TestVeriTabani.Ileride(10), TestVeriTabani.Ileride(12), new(), null));
        Assert.Contains("şube", hata.Message);
    }

    [Fact]
    public async Task Genc_surucu_luks_arac_kiralayamaz()
    {
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => TalepOlustur(_t.GencMusteri, _t.Luks));
        Assert.Contains("27 yaşında", hata.Message);
    }

    [Fact]
    public async Task Kara_listedeki_musteri_rezervasyon_yapamaz()
    {
        _t.Musteri.KaraListede = true;
        await _t.Vt.SaveChangesAsync();
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => TalepOlustur(_t.Musteri, _t.Ekonomik));
    }

    [Fact]
    public async Task Onayli_rezervasyonla_cakisan_talep_reddedilir()
    {
        _t.RezervasyonEkle(_t.Ekonomik, _t.DigerMusteri, TestVeriTabani.Ileride(9), TestVeriTabani.Ileride(12), RezervasyonDurumu.Onaylandi);
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => TalepOlustur(_t.Musteri, _t.Ekonomik));
        Assert.Contains("müsait değil", hata.Message);
    }

    [Fact]
    public async Task Onaylaninca_cakisan_bekleyen_talepler_otomatik_reddedilir()
    {
        var benim = await TalepOlustur(_t.Musteri, _t.Ekonomik);
        var cakisan = await TalepOlustur(_t.DigerMusteri, _t.Ekonomik, baslangicGunu: 11);
        var cakismayan = await TalepOlustur(_t.DigerMusteri, _t.Ekonomik, baslangicGunu: 30);

        await _servis.OnaylaAsync(benim.Id, _t.Personel.Id);

        _t.Vt.ChangeTracker.Clear();
        Assert.Equal(RezervasyonDurumu.Onaylandi, (await _t.Vt.Rezervasyonlar.FindAsync(benim.Id))!.Durum);
        Assert.Equal(RezervasyonDurumu.Reddedildi, (await _t.Vt.Rezervasyonlar.FindAsync(cakisan.Id))!.Durum);
        Assert.Equal(RezervasyonDurumu.OnayBekliyor, (await _t.Vt.Rezervasyonlar.FindAsync(cakismayan.Id))!.Durum);
    }

    [Fact]
    public async Task Gecersiz_durum_gecisi_engellenir()
    {
        var r = await TalepOlustur(_t.Musteri, _t.Ekonomik);

        // Onaylanmamış araç teslim edilemez
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => _servis.TeslimEtAsync(r.Id, _t.Personel.Id, 20_000, YakitSeviyesi.Dolu));
        // Teslim edilmemiş araç iade alınamaz
        await Assert.ThrowsAsync<IsKuraliHatasi>(() =>
            _servis.IadeAlAsync(r.Id, _t.Personel.Id, 21_000, YakitSeviyesi.Dolu, DateTime.Now, null, 0, null));
    }

    [Theory]
    [InlineData(RezervasyonDurumu.OnayBekliyor, RezervasyonDurumu.Onaylandi, true)]
    [InlineData(RezervasyonDurumu.Onaylandi, RezervasyonDurumu.Kirada, true)]
    [InlineData(RezervasyonDurumu.Kirada, RezervasyonDurumu.Tamamlandi, true)]
    [InlineData(RezervasyonDurumu.Tamamlandi, RezervasyonDurumu.IptalEdildi, false)]
    [InlineData(RezervasyonDurumu.Kirada, RezervasyonDurumu.IptalEdildi, false)]
    [InlineData(RezervasyonDurumu.Reddedildi, RezervasyonDurumu.Onaylandi, false)]
    public void Durum_gecis_tablosu(RezervasyonDurumu simdiki, RezervasyonDurumu yeni, bool izinli)
        => Assert.Equal(izinli, RezervasyonServisi.GecisMumkun(simdiki, yeni));

    [Fact]
    public async Task Tam_kiralama_dongusu_gec_iade_ve_yakit_ucretiyle()
    {
        var r = await TalepOlustur(_t.Musteri, _t.Ekonomik, iadeSubesi: _t.Ankara);
        await _servis.OnaylaAsync(r.Id, _t.Personel.Id);
        await _servis.TeslimEtAsync(r.Id, _t.Personel.Id, teslimKm: 20_000, YakitSeviyesi.Dolu);

        var arac = await _t.Vt.Araclar.FindAsync(_t.Ekonomik.Id);
        Assert.Equal(AracDurumu.Kirada, arac!.Durum);

        // Planlanandan 5 saat geç, yarım depo ve 500 ₺ hasarla iade
        var iade = await _servis.IadeAlAsync(r.Id, _t.Personel.Id, 20_850, YakitSeviyesi.Yarim, r.BitisTarihi.AddHours(5), "Çizik", 500, null);

        Assert.Equal(RezervasyonDurumu.Tamamlandi, iade.Durum);
        Assert.Equal(1500, iade.GecikmeUcreti);     // 1 gün × 1000 × 1.5
        Assert.Equal(900, iade.YakitUcreti);        // 2 çeyrek × 450
        Assert.Equal(500, iade.HasarUcreti);
        Assert.Equal(3000 + 750 + 1500 + 900 + 500, iade.ToplamTutar);
        Assert.Single(iade.Hasarlar);

        // Araç iade şubesinde, yeni kilometresiyle tekrar müsait
        Assert.Equal(AracDurumu.Musait, arac.Durum);
        Assert.Equal(20_850, arac.Kilometre);
        Assert.Equal(_t.Ankara.Id, arac.SubeId);
    }

    [Fact]
    public async Task Iade_kilometresi_teslimden_kucuk_olamaz()
    {
        var r = await TalepOlustur(_t.Musteri, _t.Ekonomik);
        await _servis.OnaylaAsync(r.Id, _t.Personel.Id);
        await _servis.TeslimEtAsync(r.Id, _t.Personel.Id, 20_000, YakitSeviyesi.Dolu);

        await Assert.ThrowsAsync<IsKuraliHatasi>(() =>
            _servis.IadeAlAsync(r.Id, _t.Personel.Id, 19_000, YakitSeviyesi.Dolu, r.BitisTarihi, null, 0, null));
    }

    [Fact]
    public async Task Musteri_baskasinin_rezervasyonunu_iptal_edemez()
    {
        var r = await TalepOlustur(_t.Musteri, _t.Ekonomik);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => _servis.IptalEtAsync(r.Id, _t.DigerMusteri.Id, personelMi: false));

        await _servis.IptalEtAsync(r.Id, _t.Musteri.Id, personelMi: false);
        var guncel = await _t.Vt.Rezervasyonlar.AsNoTracking().FirstAsync(x => x.Id == r.Id);
        Assert.Equal(RezervasyonDurumu.IptalEdildi, guncel.Durum);
    }
}
