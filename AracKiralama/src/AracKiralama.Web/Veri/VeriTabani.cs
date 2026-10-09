// ============================================================================
// VERİTABANI BAĞLANTISI (Entity Framework Core)
// Her "DbSet" bir tablodur. Örn: veriTabani.Araclar.Where(a => a.Yil > 2020)
// Tablolar uygulama ilk açıldığında otomatik oluşturulur (Program.cs → EnsureCreated).
// Bilgisayarda SQLite (tek dosya), canlı sitede PostgreSQL (Neon) kullanılır;
// kod ikisinde de aynıdır.
// ============================================================================
using AracKiralama.Modeller;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Veri;

public class VeriTabani : IdentityDbContext<Kullanici>   // Identity: kullanıcı, rol, giriş tabloları
{
    public VeriTabani(DbContextOptions<VeriTabani> secenekler) : base(secenekler) { }

    // ---- İşletme tabloları ----
    public DbSet<Arac> Araclar => Set<Arac>();
    public DbSet<Rezervasyon> Rezervasyonlar => Set<Rezervasyon>();
    public DbSet<RezervasyonEkstrasi> RezervasyonEkstralari => Set<RezervasyonEkstrasi>();
    public DbSet<Odeme> Odemeler => Set<Odeme>();
    public DbSet<Hasar> Hasarlar => Set<Hasar>();
    public DbSet<Bakim> Bakimlar => Set<Bakim>();

    // ---- Tanımlar (admin panelinden yönetilir) ----
    public DbSet<Marka> Markalar => Set<Marka>();
    public DbSet<Kategori> Kategoriler => Set<Kategori>();
    public DbSet<YakitTuru> YakitTurleri => Set<YakitTuru>();
    public DbSet<VitesTuru> VitesTurleri => Set<VitesTuru>();
    public DbSet<Sube> Subeler => Set<Sube>();
    public DbSet<Ekstra> Ekstralar => Set<Ekstra>();

    // ---- Site içeriği ----
    public DbSet<SiteAyarlari> SiteAyarlari => Set<SiteAyarlari>();
    public DbSet<Sayfa> Sayfalar => Set<Sayfa>();
    public DbSet<Metin> Metinler => Set<Metin>();
    public DbSet<Gorsel> Gorseller => Set<Gorsel>();

    protected override void ConfigureConventions(ModelConfigurationBuilder ayar)
    {
        // SQLite'ta "decimal" türü yok; hesap yapabilmesi için sayıları double olarak saklıyoruz.
        if (Database.IsSqlite())
            ayar.Properties<decimal>().HaveConversion<double>();

        // Enum'lar veritabanında okunabilir metin olarak saklanır ("OnayBekliyor", "Kirada"...).
        ayar.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        // Aynı plakadan / rezervasyon kodundan iki tane olamaz.
        model.Entity<Arac>().HasIndex(a => a.Plaka).IsUnique();
        model.Entity<Rezervasyon>().HasIndex(r => r.Kod).IsUnique();
        model.Entity<Sayfa>().HasIndex(s => s.Adres).IsUnique();

        // Rezervasyon ↔ Ekstra çoka-çok tablosunun anahtarı iki kolondan oluşur.
        model.Entity<RezervasyonEkstrasi>().HasKey(x => new { x.RezervasyonId, x.EkstraId });

        // Rezervasyonun iki ayrı şubesi ve iki ayrı kullanıcısı var; EF'e hangisinin hangisi olduğunu söylüyoruz.
        model.Entity<Rezervasyon>().HasOne(r => r.AlisSube).WithMany().HasForeignKey(r => r.AlisSubeId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Rezervasyon>().HasOne(r => r.IadeSube).WithMany().HasForeignKey(r => r.IadeSubeId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Rezervasyon>().HasOne(r => r.Musteri).WithMany(k => k.Rezervasyonlar).HasForeignKey(r => r.MusteriId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Rezervasyon>().HasOne(r => r.IslemYapan).WithMany().HasForeignKey(r => r.IslemYapanId).OnDelete(DeleteBehavior.SetNull);
        model.Entity<Odeme>().HasOne(o => o.AlanKullanici).WithMany().HasForeignKey(o => o.AlanKullaniciId).OnDelete(DeleteBehavior.SetNull);

        // Kiralaması olan araç, kullanılan marka / kategori / şube yanlışlıkla silinemesin.
        model.Entity<Rezervasyon>().HasOne(r => r.Arac).WithMany(a => a.Rezervasyonlar).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Arac>().HasOne(a => a.Marka).WithMany().OnDelete(DeleteBehavior.Restrict);
        model.Entity<Arac>().HasOne(a => a.Kategori).WithMany().OnDelete(DeleteBehavior.Restrict);
        model.Entity<Arac>().HasOne(a => a.YakitTuru).WithMany().OnDelete(DeleteBehavior.Restrict);
        model.Entity<Arac>().HasOne(a => a.VitesTuru).WithMany().OnDelete(DeleteBehavior.Restrict);
        model.Entity<Arac>().HasOne(a => a.Sube).WithMany().OnDelete(DeleteBehavior.Restrict);
        model.Entity<RezervasyonEkstrasi>().HasOne(x => x.Ekstra).WithMany().OnDelete(DeleteBehavior.Restrict);
    }
}
