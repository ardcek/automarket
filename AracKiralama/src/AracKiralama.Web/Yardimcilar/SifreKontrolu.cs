// ============================================================================
// GEÇİCİ ŞİFRE KONTROLÜ
// Admin birine geçici şifre verince (Yönetim → Müşteriler → kişi → "Geçici şifre ver" ya da yeni kullanıcı açınca)
// Kullanici.SifreDegistirmeli = true olur. O kişi giriş yaptığında:
//   1) KimlikFabrikasi giriş çerezine "sifre-degistirmeli" bilgisini ekler,
//   2) SifreKontrolu her istekte bunu görür ve kişiyi /Hesap/YeniSifre sayfasına gönderir.
// Yeni şifre belirlenince bilgi silinir, site normal açılır. (Program.cs'te ikisi de kayıtlı.)
// ============================================================================
using System.Security.Claims;
using AracKiralama.Modeller;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AracKiralama.Yardimcilar;

/// <summary>Giriş çerezini oluştururken "şifresini değiştirmeli" bilgisini ekler.</summary>
public class KimlikFabrikasi(UserManager<Kullanici> kullanicilar, RoleManager<IdentityRole> roller, IOptions<IdentityOptions> secenekler)
    : UserClaimsPrincipalFactory<Kullanici, IdentityRole>(kullanicilar, roller, secenekler)
{
    public const string SifreDegistirmeli = "sifre-degistirmeli";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Kullanici kullanici)
    {
        var kimlik = await base.GenerateClaimsAsync(kullanici);
        if (kullanici.SifreDegistirmeli) kimlik.AddClaim(new Claim(SifreDegistirmeli, "1"));
        return kimlik;
    }
}

/// <summary>Geçici şifreyle giren kişiyi, yeni şifresini belirleyene kadar /Hesap/YeniSifre'de tutar.</summary>
public static class SifreKontrolu
{
    // Bu adreslere her zaman izin var: şifre sayfası, çıkış, dosyalar (css/js/resim)
    private static readonly string[] Serbest = ["/Hesap/YeniSifre", "/Hesap/Cikis", "/css", "/js", "/lib", "/img", "/gorsel", "/health", "/favicon"];

    public static IApplicationBuilder UseSifreKontrolu(this IApplicationBuilder uygulama) => uygulama.Use(async (istek, sonraki) =>
    {
        var yol = istek.Request.Path.Value ?? "";
        if (istek.User.HasClaim(KimlikFabrikasi.SifreDegistirmeli, "1") && !Serbest.Any(s => yol.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
        {
            istek.Response.Redirect("/Hesap/YeniSifre");
            return;
        }
        await sonraki();
    });
}
