// ============================================================================
// NEON BAĞLANTISI (buluttaki PostgreSQL veritabanımız)
// Bu aşamada Neon'a bağlanıp TABLOLARI OLUŞTURUYORUZ ama içine VERİ YAZMIYORUZ.
// Sayfalar şimdilik örnek verilerle (bilgisayardaki SQLite dosyası) çalışıyor.
// appsettings.json → "SayfalarNeredenOkusun": "Neon" yapılınca sayfalar da Neon'dan okur.
//
// Program.cs açılışta TablolariHazirlaAsync'i çağırır. Bağlantı adresi koda yazılmaz:
// Render'da ortam değişkeninden, bilgisayarda "user-secrets"ten gelir.
// ============================================================================
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Veri;

public static class NeonBaglantisi
{
    /// <summary>
    /// Neon'a bağlanır, tablolar yoksa oluşturur (EnsureCreated). Hiçbir tabloya satır eklemez.
    /// Neon'a ulaşılamazsa (internet yoksa) site çökmez; sadece loga uyarı yazılır.
    /// </summary>
    public static async Task<bool> TablolariHazirlaAsync(string baglantiAdresi, ILogger logger)
    {
        try
        {
            await using var neon = Ac(baglantiAdresi);
            var simdiOlustu = await neon.Database.EnsureCreatedAsync();   // true → tablolar bu açılışta oluşturuldu
            var tablolar = await TablolariListeleAsync(neon);
            logger.LogInformation("Neon bağlantısı kuruldu: {Sayi} tablo hazır, toplam {Satir} satır{Not}.",
                tablolar.Count, tablolar.Sum(t => t.SatirSayisi), simdiOlustu ? " (tablolar şimdi oluşturuldu)" : "");
            return true;
        }
        catch (Exception hata)
        {
            logger.LogWarning("Neon'a bağlanılamadı (site örnek verilerle çalışmaya devam ediyor): {Mesaj}", hata.Message);
            return false;
        }
    }

    /// <summary>Neon'daki tabloları ve her birinde kaç satır olduğunu listeler (panel ve rapor için).</summary>
    public static async Task<List<TabloBilgisi>> TablolariListeleAsync(string baglantiAdresi)
    {
        await using var neon = Ac(baglantiAdresi);
        return await TablolariListeleAsync(neon);
    }

    private static async Task<List<TabloBilgisi>> TablolariListeleAsync(VeriTabani neon)
    {
        var baglanti = neon.Database.GetDbConnection();
        await baglanti.OpenAsync();

        // 1) Tablo adları: PostgreSQL'in kendi "information_schema" kataloğundan okunur.
        var adlar = new List<string>();
        await using (var komut = baglanti.CreateCommand())
        {
            komut.CommandText = "SELECT table_name FROM information_schema.tables " +
                                "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name";
            await using var okuyucu = await komut.ExecuteReaderAsync();
            while (await okuyucu.ReadAsync()) adlar.Add(okuyucu.GetString(0));
        }

        // 2) Her tablonun satır sayısı: SELECT COUNT(*) FROM "TabloAdi"
        var sonuc = new List<TabloBilgisi>();
        foreach (var ad in adlar)
        {
            await using var komut = baglanti.CreateCommand();
            komut.CommandText = $"SELECT COUNT(*) FROM \"{ad.Replace("\"", "\"\"")}\"";
            sonuc.Add(new TabloBilgisi(ad, Convert.ToInt64(await komut.ExecuteScalarAsync())));
        }
        return sonuc;
    }

    /// <summary>Neon için geçici bir veritabanı bağlantısı açar (Npgsql = .NET'in PostgreSQL sürücüsü).</summary>
    private static VeriTabani Ac(string baglantiAdresi) =>
        new(new DbContextOptionsBuilder<VeriTabani>().UseNpgsql(baglantiAdresi).Options);
}

/// <summary>Bir tablonun adı ve içindeki satır sayısı.</summary>
public record TabloBilgisi(string Ad, long SatirSayisi);
