// ============================================================================
// ŞEMA GÜNCELLEYİCİ: koda yeni tablo ya da kolon eklenince veritabanını otomatik tamamlar
// ----------------------------------------------------------------------------
// Proje "migration" kullanmıyor; tablolar EnsureCreated ile oluşturuluyor. EnsureCreated
// ise veritabanında tablo VARSA hiçbir şey yapmaz. Bu yüzden örneğin SiteAyarlari'na
// yeni bir [Ayar] satırı eklendiğinde Neon'da o kolon olmaz ve site hata verir.
//
// Bu sınıf açılışta (OrnekVeriler.YukleAsync) çalışır:
//   1) Veritabanındaki tabloları ve kolonları okur.
//   2) Koddaki modelle karşılaştırır.
//   3) EKSİK tabloları oluşturur, EKSİK kolonları ekler (dolu kolonlara varsayılan değer yazılır).
// Hiçbir şeyi SİLMEZ, DEĞİŞTİRMEZ: fazla kolon ya da tür farkı olursa dokunmaz.
// Kolon silmek / yeniden adlandırmak gerekirse elle yapılır (GELISTIRICI-REHBERI.md).
// SQLite ve PostgreSQL (Neon) ile aynı şekilde çalışır.
// ============================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace AracKiralama.Veri;

public static class SemaGuncelleyici
{
    /// <summary>Eksik tablo ve kolonları ekler. Eklenenlerin listesini döndürür (boşsa şema zaten günceldi).</summary>
    public static async Task<List<string>> EksikleriEkleAsync(VeriTabani vt)
    {
        var mevcut = await MevcutKolonlarAsync(vt);
        if (mevcut.Count == 0) return [];   // veritabanı tamamen boş: EnsureCreated zaten her şeyi oluşturdu

        // Koddaki modelin "tasarım zamanı" hâli: tablo, kolon, anahtar ve indeks bilgilerinin tamamı
        var model = vt.GetService<IDesignTimeModel>().Model;
        var iliskisel = model.GetRelationalModel();
        var islemler = new List<MigrationOperation>();
        var eklenenler = new List<string>();

        // 1) EKSİK TABLOLAR: EF'in "sıfırdan oluştur" adımları içinden sadece eksik tablolarınkini al.
        //    Sıra EF'in sırasıdır (önce bağımlı olunan tablo), yabancı anahtarlar CREATE TABLE içindedir.
        var eksikTablolar = iliskisel.Tables.Where(t => !mevcut.ContainsKey(t.Name)).Select(t => t.Name).ToHashSet();
        if (eksikTablolar.Count > 0)
        {
            var hepsi = vt.GetService<IMigrationsModelDiffer>().GetDifferences(null, iliskisel);
            islemler.AddRange(hepsi.Where(i => i switch
            {
                CreateTableOperation t => eksikTablolar.Contains(t.Name),
                CreateIndexOperation x => eksikTablolar.Contains(x.Table),
                _ => false
            }));
            eklenenler.AddRange(eksikTablolar.Select(t => $"tablo {t}"));
        }

        // 2) EKSİK KOLONLAR: var olan tablolara ALTER TABLE ... ADD COLUMN
        foreach (var tablo in iliskisel.Tables.Where(t => mevcut.ContainsKey(t.Name)))
        {
            foreach (var kolon in tablo.Columns.Where(k => !mevcut[tablo.Name].Contains(k.Name)))
            {
                var tur = kolon.ProviderClrType;
                islemler.Add(new AddColumnOperation
                {
                    Table = tablo.Name,
                    Schema = tablo.Schema,
                    Name = kolon.Name,
                    ClrType = tur,
                    ColumnType = kolon.StoreType,
                    IsNullable = kolon.IsNullable,
                    // Dolu tablolarda NOT NULL kolon eklenebilmesi için varsayılan değer şart. Önce KODDAKİ varsayılan
                    // kullanılır (örn. [Ayar] ... { get; set; } = true → mevcut satırlar da true olur), yoksa 0 / false / "".
                    DefaultValue = kolon.IsNullable ? null : kolon.DefaultValue ?? KoddakiVarsayilan(kolon) ?? VarsayilanDeger(tur)
                });
                eklenenler.Add($"kolon {tablo.Name}.{kolon.Name}");
            }
        }

        if (islemler.Count == 0) return [];

        // İşlemleri bu veritabanının diline (SQLite / PostgreSQL) çevirip çalıştır
        var komutlar = vt.GetService<IMigrationsSqlGenerator>().Generate(islemler, vt.Model);
        await using var islem = await vt.Database.BeginTransactionAsync();
        foreach (var komut in komutlar) await vt.Database.ExecuteSqlRawAsync(komut.CommandText);
        await islem.CommitAsync();
        return eklenenler;
    }

    /// <summary>Veritabanındaki tablo adı → kolon adları.</summary>
    private static async Task<Dictionary<string, HashSet<string>>> MevcutKolonlarAsync(VeriTabani vt)
    {
        var sorgu = vt.Database.IsNpgsql()
            ? "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = current_schema()"
            : "SELECT m.name, p.name FROM sqlite_master m JOIN pragma_table_info(m.name) p WHERE m.type = 'table'";

        var sonuc = new Dictionary<string, HashSet<string>>();
        var baglanti = vt.Database.GetDbConnection();
        var kapat = baglanti.State != System.Data.ConnectionState.Open;
        if (kapat) await baglanti.OpenAsync();
        try
        {
            await using var komut = baglanti.CreateCommand();
            komut.CommandText = sorgu;
            await using var okuyucu = await komut.ExecuteReaderAsync();
            while (await okuyucu.ReadAsync())
            {
                var tablo = okuyucu.GetString(0);
                if (!sonuc.TryGetValue(tablo, out var kolonlar)) sonuc[tablo] = kolonlar = [];
                kolonlar.Add(okuyucu.GetString(1));
            }
        }
        finally { if (kapat) await baglanti.CloseAsync(); }
        return sonuc;
    }

    /// <summary>
    /// Kolonun C# sınıfındaki başlangıç değeri: new SiteAyarlari().OzelImlec gibi. Sınıf boş oluşturulamıyorsa
    /// ya da değer okunamıyorsa null. Enum gibi dönüştürülerek saklananlar veritabanı biçimine çevrilir.
    /// </summary>
    private static object? KoddakiVarsayilan(IColumn kolon)
    {
        try
        {
            var ozellik = kolon.PropertyMappings.First().Property;
            var sinif = ozellik.DeclaringType.ClrType;
            if (ozellik.PropertyInfo == null || sinif.GetConstructor(Type.EmptyTypes) == null) return null;
            var deger = ozellik.PropertyInfo.GetValue(Activator.CreateInstance(sinif));
            if (deger == null) return null;
            var cevirici = ozellik.GetTypeMapping().Converter;
            return cevirici != null ? cevirici.ConvertToProvider(deger) : deger;
        }
        catch { return null; }
    }

    private static object VarsayilanDeger(Type tur) =>
        tur == typeof(string) ? "" : tur == typeof(byte[]) ? Array.Empty<byte>() : Activator.CreateInstance(tur)!;
}
