using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using AracKiralama.Web.Data;
using AracKiralama.Web.Helpers;
using AracKiralama.Web.Models.Entities;
using AracKiralama.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ---------- Ayarlar (appsettings.json) ----------
builder.Services.Configure<PricingOptions>(builder.Configuration.GetSection("Pricing"));
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("Site"));

// ---------- Klasörler ----------
// Veritabanı, yüklenen görseller ve oturum anahtarları DATA_DIR klasörüne yazılır (yoksa proje klasörü).
var paths = new AppPaths(builder.Environment.ContentRootPath);
builder.Services.AddSingleton(paths);

// Oturum (cookie) şifreleme anahtarları diske yazılır → uygulama yeniden başlayınca kullanıcılar çıkış yapmış olmaz.
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(paths.KeysDir));

// ---------- Veritabanı ----------
// Varsayılan: DATA_DIR/arackiralama.db (SQLite). İsterseniz appsettings.json'a "ConnectionStrings:Default" ekleyin.
// SQL Server'a geçmek için: UseSqlite → UseSqlServer ve connection string'i değiştirin.
var connectionString = builder.Configuration.GetConnectionString("Default") ?? $"Data Source={paths.DatabaseFile}";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

// ---------- Sunucu arkasında çalışma (Render, Nginx...) ----------
// HTTPS önde (proxy'de) sonlanır; gerçek protokol ve IP bilgisi X-Forwarded-* başlıklarıyla gelir.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// Render'ın "uygulama ayakta mı?" kontrolü için: GET /health
builder.Services.AddHealthChecks();

// ---------- Kimlik doğrulama (ASP.NET Core Identity) ----------
builder.Services
    .AddIdentity<AppUser, IdentityRole>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// ---------- Uygulama servisleri (iş kuralları) ----------
// Scoped = her HTTP isteği için bir örnek oluşturulur.
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IContractPdfService, ContractPdfService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddHostedService<OverdueReservationWorker>();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// Fontta olmayan bir karakter (ör. emoji) girilirse PDF üretimi durmasın, o karakter boş geçilsin.
QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;

// ---------- MVC + API ----------
builder.Services.AddControllersWithViews(o => TurkishModelBindingMessages.Apply(o.ModelBindingMessageProvider));

// Türkçe karakterler (ş, ğ, ı, ₺...) HTML çıktısında &#x15F; gibi kodlanmadan, olduğu gibi yazılsın.
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddOpenApi();

var app = builder.Build();

// Form verisindeki sayılar "1500.50" biçiminde gelir; okuma kültürden bağımsız olsun.
// Ekrandaki Türkçe biçimlendirme Helpers/Fmt.cs ile yapılır.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("tr-TR");

// ---------- Veritabanını oluştur + örnek verileri yükle ----------
using (var scope = app.Services.CreateScope())
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
    app.Logger.LogInformation("Veritabanı hazır ({Ms} ms).", timer.ElapsedMilliseconds);
}

app.UseForwardedHeaders();   // en başta olmalı
app.UseSecurityHeaders();    // Helpers/SecurityHeaders.cs

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

app.UseHttpsRedirection();

// Admin panelinden yüklenen araç görselleri: DATA_DIR/uploads → /uploads/...
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(paths.UploadsDir),
    RequestPath = "/uploads",
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/health");

// API dokümantasyonu: /openapi/v1.json  ve arayüz: /swagger
app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Rota Araç Kiralama API");
    o.DocumentTitle = "Rota API";
});

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Test projesinin WebApplicationFactory ile erişebilmesi için.
public partial class Program;
