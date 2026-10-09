// ============================================================================
// VERİTABANI TESTLERİ: modeller hem SQLite'ta (bilgisayar) hem PostgreSQL'de (canlı site, Neon)
// tabloya dönüşebiliyor mu? PostgreSQL testi sunucuya bağlanmaz, sadece CREATE TABLE
// komutlarını üretir; bir alanın türü PostgreSQL'e uymuyorsa burada hata verir.
// ============================================================================
using AracKiralama.Veri;
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
}
