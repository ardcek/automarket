// ============================================================================
// ROL TESTLERİ: Süper Admin / Admin ayrımı (Yardimcilar/Genel.cs → Roller)
// - Site düzenleme sayfaları sadece Süper Admin'e açık; operasyon ve kayıt sayfaları Admin'e açık
// - Süper Admin aynı zamanda Admin sayılır
// - Panelden Süper Admin yetkisi verilemez
// ============================================================================
using System.Reflection;
using System.Security.Claims;
using AracKiralama.Areas.Yonetim.Controllers;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Authorization;

namespace AracKiralama.Testler;

public class RolTestleri
{
    private static string[] Roller_(Type controller) =>
        controller.GetCustomAttributes<AuthorizeAttribute>(inherit: false).Select(a => a.Roles ?? "").ToArray();

    [Theory]
    [InlineData(typeof(YapayZekaController))]
    [InlineData(typeof(DuzenleyiciController))]
    [InlineData(typeof(OzelliklerController))]
    [InlineData(typeof(KampanyalarController))]
    [InlineData(typeof(MusteriAlanlariController))]
    [InlineData(typeof(AyarlarController))]
    [InlineData(typeof(SayfalarController))]
    [InlineData(typeof(MetinlerController))]
    [InlineData(typeof(GorsellerController))]
    public void Site_duzenleme_sayfalari_sadece_super_admine_acik(Type controller)
        => Assert.Contains(Roller.SuperAdmin, Roller_(controller));

    [Theory]
    [InlineData(typeof(PanelController))]
    [InlineData(typeof(TakvimController))]
    [InlineData(typeof(RezervasyonlarController))]
    [InlineData(typeof(BakimController))]
    [InlineData(typeof(OdemelerController))]
    [InlineData(typeof(MusterilerController))]
    [InlineData(typeof(RaporlarController))]
    [InlineData(typeof(AraclarController))]
    [InlineData(typeof(KategorilerController))]
    [InlineData(typeof(SubelerController))]
    public void Hocanin_paneli_admin_ve_personele_acik(Type controller)
        => Assert.DoesNotContain(Roller.SuperAdmin, Roller_(controller));

    [Fact]
    public async Task Super_admin_ayni_zamanda_admin_sayilir()
    {
        var kimlik = new ClaimsIdentity([new Claim(ClaimTypes.Role, Roller.SuperAdmin)], "deneme");
        var kullanici = await new SuperAdminAyniZamandaAdmin().TransformAsync(new ClaimsPrincipal(kimlik));
        kullanici = await new SuperAdminAyniZamandaAdmin().TransformAsync(kullanici);   // iki kez çalışsa da tek "Admin" eklenir

        Assert.True(kullanici.IsInRole(Roller.Admin));
        Assert.Single(kullanici.FindAll(ClaimTypes.Role), c => c.Value == Roller.Admin);
    }

    [Fact]
    public async Task Admin_super_admin_sayilmaz()
    {
        var kimlik = new ClaimsIdentity([new Claim(ClaimTypes.Role, Roller.Admin)], "deneme");
        var kullanici = await new SuperAdminAyniZamandaAdmin().TransformAsync(new ClaimsPrincipal(kimlik));
        Assert.False(kullanici.IsInRole(Roller.SuperAdmin));
    }

    [Fact]
    public void Panelden_super_admin_verilemez()
    {
        Assert.DoesNotContain(Roller.SuperAdmin, Roller.PaneldenVerilebilir);
        Assert.Contains(Roller.SuperAdmin, Roller.Yonetim.Split(','));   // ama panele girebilir
    }
}
