// ============================================================================
// VERİTABANI TESTLERİ: modeller hem SQLite'ta (bilgisayar) hem PostgreSQL'de (canlı site, Neon)
// tabloya dönüşebiliyor mu? PostgreSQL testi sunucuya bağlanmaz, sadece CREATE TABLE
// komutlarını üretir; bir alanın türü PostgreSQL'e uymuyorsa burada hata verir.
// ============================================================================
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Testler;

public class VeriTabaniTestleri
{
    [Fact]
    public void PostgreSQL_tablolari_olusturulabilir()
    {
        var secenekler = new DbContextOptionsBuilder<VeriTabani>().UseNpgsql("Host=localhost;Database=deneme").Options;
        using var vt = new VeriTabani(secenekler);

        var komutlar = vt.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE \"Araclar\"", komutlar);
        Assert.Contains("CREATE TABLE \"SiteAyarlari\"", komutlar);
        Assert.Contains("CREATE TABLE \"Gorseller\"", komutlar);
        Assert.Contains("numeric", komutlar);          // para alanları PostgreSQL'de tam sayı hassasiyetinde
    }

    [Fact]
    public void SQLite_tablolari_olusturulabilir()
    {
        using var t = new TestVeriTabani();
        Assert.True(t.Vt.Araclar.Any());
    }

    /// <summary>
    /// Koda sonradan eklenen kolon / tablo, eski bir veritabanında eksikse SemaGuncelleyici ekler,
    /// içindeki verilere dokunmaz. (Canlı sitede Neon'u koruyan adım: Veri/SemaGuncelleyici.cs)
    /// </summary>
    [Fact]
    public async Task Sema_guncelleyici_eksik_kolon_ve_tabloyu_ekler_veriyi_korur()
    {
        using var t = new TestVeriTabani();
        var aracSayisi = t.Vt.Araclar.Count();
        // "Eski veritabanı" taklidi: bir kolon ve bir tablo silinir
        await t.Vt.Database.ExecuteSqlRawAsync("ALTER TABLE Sayfalar DROP COLUMN MenuGrubu");
        await t.Vt.Database.ExecuteSqlRawAsync("DROP TABLE YapayZekaKayitlari");

        var eklenenler = await SemaGuncelleyici.EksikleriEkleAsync(t.Vt);

        Assert.Contains("kolon Sayfalar.MenuGrubu", eklenenler);
        Assert.Contains("tablo YapayZekaKayitlari", eklenenler);
        Assert.Empty(await SemaGuncelleyici.EksikleriEkleAsync(t.Vt));   // ikinci çalıştırmada eklenecek bir şey kalmaz
        Assert.Equal(aracSayisi, t.Vt.Araclar.Count());
        Assert.Equal(0, t.Vt.YapayZekaKayitlari.Count());                // yeniden oluşan tablo okunabiliyor
    }
}

public class BicimTestleri
{
    [Fact]
    public void Soru_cevap_metni_giris_ve_sorulara_ayrilir()
    {
        var (giris, sorular) = Bicim.SoruCevaplar("Merhaba.\n\n## İptal?\nÜcretsiz.\n\n## Yakıt?\nAynı seviyede.\n- madde");

        Assert.Equal("Merhaba.", giris.Trim());
        Assert.Equal(2, sorular.Count);
        Assert.Equal("İptal?", sorular[0].Soru);
        Assert.Equal("Ücretsiz.", sorular[0].Cevap.Trim());
        Assert.Contains("- madde", sorular[1].Cevap);
    }
}

public class BaglantiTestleri
{
    [Fact]
    public void Neon_adres_bicimi_Npgsql_bicimine_cevrilir()
    {
        var sonuc = PostgreBaglantisi.Duzenle("postgresql://kullanici:gizli%21@ep-ornek.eu-central-1.aws.neon.tech/arackiralama?sslmode=require&channel_binding=require");
        Assert.Equal("Host=ep-ornek.eu-central-1.aws.neon.tech;Port=5432;Database=arackiralama;Username=kullanici;Password=gizli!;SSL Mode=Require;Channel Binding=Require", sonuc);
    }

    [Fact]
    public void Npgsql_bicimi_ve_bos_deger_oldugu_gibi_kalir()
    {
        Assert.Equal("Host=a;Database=b", PostgreBaglantisi.Duzenle("Host=a;Database=b"));
        Assert.Null(PostgreBaglantisi.Duzenle(""));
    }
}
