// ============================================================================
// GÜVENLİK ANAHTARI DEPOSU TESTLERİ (Veri/AnahtarDeposu.cs)
// Canlı sitede sunucu yeniden başlayınca (Render uyuyup uyanınca) oturumlar düşmesin diye anahtarlar
// veritabanında tutulur. Test: bir "sunucu" bir yazıyı şifreler, aynı veritabanını kullanan YENİ bir
// "sunucu" (yeniden başlamış hali) onu çözebilmeli.
// ============================================================================
using AracKiralama.Veri;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AracKiralama.Testler;

public class AnahtarDeposuTestleri : IDisposable
{
    // Bellekteki SQLite, bağlantı açık kaldığı sürece yaşar: iki "sunucu" aynı veritabanını görür
    private readonly SqliteConnection _baglanti = new("DataSource=:memory:");

    public AnahtarDeposuTestleri()
    {
        _baglanti.Open();
        using var vt = new VeriTabani(new DbContextOptionsBuilder<VeriTabani>().UseSqlite(_baglanti).Options);
        vt.Database.EnsureCreated();
    }

    /// <summary>Program.cs'teki canlı site ayarının aynısıyla bir "sunucu" kurar.</summary>
    private ServiceProvider SunucuKur()
    {
        var servisler = new ServiceCollection();
        servisler.AddLogging();
        servisler.AddDbContext<VeriTabani>(s => s.UseSqlite(_baglanti));
        servisler.AddDataProtection().SetApplicationName("RotaAracKiralama");
        servisler.AddSingleton<IConfigureOptions<KeyManagementOptions>>(s => new ConfigureOptions<KeyManagementOptions>(
            secenek => secenek.XmlRepository = new VeritabaniAnahtarDeposu(s.GetRequiredService<IServiceScopeFactory>())));
        return servisler.BuildServiceProvider();
    }

    [Fact]
    public void Sunucu_yeniden_baslasa_da_eski_sifreli_yazi_cozulur()
    {
        string sifreli;
        using (var ilkSunucu = SunucuKur())
            sifreli = ilkSunucu.GetRequiredService<IDataProtectionProvider>().CreateProtector("deneme").Protect("oturum bilgisi");

        using var yeniSunucu = SunucuKur();   // "yeniden başlamış" sunucu: anahtarı diskten değil veritabanından okur
        var cozulen = yeniSunucu.GetRequiredService<IDataProtectionProvider>().CreateProtector("deneme").Unprotect(sifreli);

        Assert.Equal("oturum bilgisi", cozulen);
        using var vt = new VeriTabani(new DbContextOptionsBuilder<VeriTabani>().UseSqlite(_baglanti).Options);
        Assert.Equal(1, vt.GuvenlikAnahtarlari.Count());   // ikinci sunucu yeni anahtar üretmedi, ilkini kullandı
    }

    public void Dispose() => _baglanti.Dispose();
}
