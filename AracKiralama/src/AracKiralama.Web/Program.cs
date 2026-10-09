// ============================================================================
// PROGRAM.CS — uygulamanın başlangıç noktası
// Yukarıdan aşağı okunur:
//   1) Servisleri kaydet (veritabanı, giriş sistemi, iş kuralları)
//   2) Uygulamayı oluştur, veritabanını hazırla
//   3) İstek hattını (middleware) kur ve çalıştır
// ============================================================================
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1) VERİTABANI
// ConnectionStrings:PostgreSql doluysa PostgreSQL (canlı site, Neon) kullanılır,
// boşsa bilgisayarda kurulum gerektirmeyen SQLite dosyası (arackiralama.db).
// ---------------------------------------------------------------------------
var veriKlasoru = Environment.GetEnvironmentVariable("DATA_DIR") ?? builder.Environment.ContentRootPath;
Directory.CreateDirectory(veriKlasoru);
var postgreBaglantisi = builder.Configuration.GetConnectionString("PostgreSql");

// PostgreSQL'in tarih kuralını SQLite ile aynı yapar (yerel saatli tarihler sorunsuz kaydedilsin).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<VeriTabani>(secenek =>
{
    if (!string.IsNullOrWhiteSpace(postgreBaglantisi)) secenek.UseNpgsql(postgreBaglantisi);
    else secenek.UseSqlite($"Data Source={Path.Combine(veriKlasoru, "arackiralama.db")}");
});

// ---------------------------------------------------------------------------
// 2) GİRİŞ SİSTEMİ (ASP.NET Identity): kullanıcılar, şifreler, roller
// ---------------------------------------------------------------------------
builder.Services.AddIdentity<Kullanici, IdentityRole>(secenek =>
    {
        secenek.User.RequireUniqueEmail = true;
        secenek.Password.RequiredLength = 6;
        secenek.Password.RequireNonAlphanumeric = false;
        secenek.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<VeriTabani>()
    .AddErrorDescriber<TurkceIdentityMesajlari>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(secenek =>
{
    secenek.LoginPath = "/Hesap/Giris";
    secenek.AccessDeniedPath = "/Hesap/ErisimEngellendi";
    secenek.ReturnUrlParameter = "donusAdresi";   // girişten sonra kullanıcının gitmek istediği sayfaya dönülür
    secenek.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Oturum çerezlerinin şifreleme anahtarları diske yazılır (uygulama yeniden başlayınca oturum düşmesin).
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(veriKlasoru, "anahtarlar")));

// ---------------------------------------------------------------------------
// 3) UYGULAMA SERVİSLERİ (iş kuralları — Servisler klasörü)
// Singleton = uygulama boyunca tek örnek · Scoped = her istekte yeni örnek
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IcerikServisi>();
builder.Services.AddScoped(s => s.GetRequiredService<IcerikServisi>().Ayarlar);   // SiteAyarlari'nı isteyen her yere güncel ayarlar
builder.Services.AddScoped<FiyatServisi>();
builder.Services.AddScoped<MusaitlikServisi>();
builder.Services.AddScoped<RezervasyonServisi>();
builder.Services.AddScoped<RaporServisi>();
builder.Services.AddScoped<SozlesmeServisi>();
builder.Services.AddScoped<GorselServisi>();
builder.Services.AddHostedService<GecikmeKontrolu>();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;   // fontta olmayan karakter PDF'i bozmasın

// ---------------------------------------------------------------------------
// 4) MVC, API ve diğer ayarlar
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews(secenek =>
{
    TurkceFormMesajlari.Uygula(secenek.ModelBindingMessageProvider);        // "abc sayı değil" gibi mesajlar
    secenek.ModelMetadataDetailsProviders.Add(new TurkceDogrulamaMesajlari());  // "X zorunludur" gibi mesajlar
});
builder.Services.Configure<WebEncoderOptions>(s => s.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All)); // "ş" → "&#x15F;" olmasın
builder.Services.AddOpenApi();        // API dokümantasyonu (/swagger)
builder.Services.AddHealthChecks();   // /health → sunucu ayakta mı?
builder.Services.Configure<ForwardedHeadersOptions>(s =>
{
    // Render gibi sunucularda HTTPS önde çözülür; gerçek bilgi X-Forwarded-* başlıklarıyla gelir.
    s.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    s.KnownIPNetworks.Clear();
    s.KnownProxies.Clear();
});

var app = builder.Build();

// Formdaki sayılar "1500.50" gelir → okuma kültürden bağımsız. Ekrandaki Türkçe biçim: Yardimcilar/Bicim.cs
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("tr-TR");

// ---------------------------------------------------------------------------
// 5) VERİTABANINI HAZIRLA: tablolar yoksa oluştur, boşsa örnek verileri yükle
// ---------------------------------------------------------------------------
using (var kapsam = app.Services.CreateScope())
{
    var sure = System.Diagnostics.Stopwatch.StartNew();
    await OrnekVeriler.YukleAsync(kapsam.ServiceProvider);
    app.Logger.LogInformation("Veritabanı hazır ({Tur}, {Ms} ms).", string.IsNullOrWhiteSpace(postgreBaglantisi) ? "SQLite" : "PostgreSQL", sure.ElapsedMilliseconds);
}

// ---------------------------------------------------------------------------
// 6) İSTEK HATTI: her istek bu sırayla bu adımlardan geçer
// ---------------------------------------------------------------------------
app.UseForwardedHeaders();
app.UseGuvenlikBasliklari();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/AnaSayfa/Hata");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/AnaSayfa/Durum", "?kod={0}");   // 404 vb. için özel sayfa
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapOpenApi();
app.UseSwaggerUI(s => s.SwaggerEndpoint("/openapi/v1.json", "Araç Kiralama API"));

// Adres kalıpları: /Yonetim/Araclar/Duzenle/5  ve  /Araclar/Detay/5
app.MapControllerRoute("yonetim", "{area:exists}/{controller=Panel}/{action=Index}/{id?}");
app.MapControllerRoute("varsayilan", "{controller=AnaSayfa}/{action=Index}/{id?}").WithStaticAssets();

app.Run();

public partial class Program;   // testlerin uygulamaya erişebilmesi için
