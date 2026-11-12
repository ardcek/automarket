// ============================================================================
// YAPAY ZEKÂ TESTLERİ: modelin yazdığı JSON'un doğrulanması, uygulanması ve geri alınması
// (Servisler/YapayZeka/PanelIslemleri.cs) ve kod modunun güvenlik kuralları (KodModu.cs).
// Gerçek bir yapay zekâ modeli gerekmez: modelin yazacağı JSON elle verilir.
// ============================================================================
using System.Text.Json.Nodes;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace AracKiralama.Testler;

public class YapayZekaTestleri : IDisposable
{
    private readonly TestVeriTabani _t = new();
    private readonly PanelIslemleri _panel;

    public YapayZekaTestleri()
    {
        _t.Vt.SiteAyarlari.Add(new SiteAyarlari { FiltreIndirimli = false });
        _t.Vt.SaveChanges();
        _panel = new PanelIslemleri(_t.Vt, new IcerikServisi(null!, null!, new TaslakServisi()));
    }

    public void Dispose() => _t.Dispose();

    private static JsonArray Json(string metin) => JsonNode.Parse(metin)!.AsArray();

    /// <summary>Kullanıcının örneği: "Meslek gruplarına göre indirim yap, filtrede de görünsün".</summary>
    private const string MeslekIndirimi = """
        [{"tur":"musteri_bilgisi_ekle","ad":"Meslek","bilgi_turu":"secim","secenekler":["Öğrenci","Öğretmen","Doktor"],"kayitta_sor":true,"zorunlu":false},
         {"tur":"kampanya_ekle","ad":"Öğretmen indirimi","indirim_yuzdesi":15,"rozet":"Öğretmenlere %15","musteri_bilgisi":"Meslek","musteri_degeri":"öğretmen"},
         {"tur":"kampanya_ekle","ad":"Öğrenci indirimi","indirim_yuzdesi":10,"musteri_bilgisi":"meslek","musteri_degeri":"Öğrenci"},
         {"tur":"ayar_degistir","ayar":"FiltreIndirimli","deger":"true"}]
        """;

    [Fact]
    public async Task Meslek_indirimi_dogrulanir_uygulanir_ve_geri_alinir()
    {
        var kartlar = await _panel.DogrulaAsync(Json(MeslekIndirimi));
        Assert.All(kartlar, k => Assert.Empty(k.Hatalar));   // aynı istekte eklenen "Meslek" kampanyada kullanılabiliyor

        var kayit = await _panel.UygulaAsync(Json(MeslekIndirimi), "Meslek gruplarına göre indirim yap", "anladım", "admin@rota.com", "test", 1);
        var meslek = await _t.Vt.MusteriAlanlari.SingleAsync();
        Assert.Equal("Öğrenci|Öğretmen|Doktor", meslek.Secenekler);
        var kampanyalar = await _t.Vt.Kampanyalar.OrderBy(k => k.Id).ToListAsync();
        Assert.Equal(2, kampanyalar.Count);
        Assert.All(kampanyalar, k => Assert.Equal(meslek.Id, k.MusteriAlanId));
        Assert.Equal("Öğretmen", kampanyalar[0].MusteriDegeri);   // "öğretmen" yazıldı, listedeki yazılış kullanıldı
        Assert.Equal(0.15m, kampanyalar[0].IndirimOrani);
        Assert.True((await _t.Vt.SiteAyarlari.SingleAsync()).FiltreIndirimli);
        Assert.Equal(1, await _t.Vt.YapayZekaKayitlari.CountAsync());

        await _panel.GeriAlAsync(kayit);
        Assert.Equal(0, await _t.Vt.Kampanyalar.CountAsync());
        Assert.Equal(0, await _t.Vt.MusteriAlanlari.CountAsync());
        Assert.False((await _t.Vt.SiteAyarlari.AsNoTracking().SingleAsync()).FiltreIndirimli);
        Assert.True(kayit.GeriAlindi);
    }

    [Fact]
    public async Task Olmayan_adlar_hata_olarak_gosterilir_ve_uygulanmaz()
    {
        var islemler = Json("""
            [{"tur":"kampanya_ekle","ad":"Yaşlı indirimi","indirim_yuzdesi":20,"musteri_bilgisi":"Yaş grubu","musteri_degeri":"65+"},
             {"tur":"kampanya_ekle","ad":"Uçan indirimi","indirim_yuzdesi":5,"kategori":"Uçan araba"},
             {"tur":"ayar_degistir","ayar":"SayfaBasinaArac","deger":"çok"},
             {"tur":"metin_degistir","anahtar":"olmayan.anahtar","yeni_metin":"x"}]
            """);
        var kartlar = await _panel.DogrulaAsync(islemler);
        Assert.All(kartlar, k => Assert.NotEmpty(k.Hatalar));
        await Assert.ThrowsAsync<AracKiralama.Yardimcilar.IsKuraliHatasi>(() => _panel.UygulaAsync(islemler, "x", null, "admin", null, 0));
        Assert.Equal(0, await _t.Vt.Kampanyalar.CountAsync());
    }

    [Fact]
    public async Task Ayni_adli_kampanya_ve_gecersiz_oran_reddedilir()
    {
        var kartlar = await _panel.DogrulaAsync(Json("""
            [{"tur":"kampanya_ekle","ad":"Yaz","indirim_yuzdesi":10},{"tur":"kampanya_ekle","ad":"yaz","indirim_yuzdesi":150}]
            """));
        Assert.Empty(kartlar[0].Hatalar);
        Assert.Contains(kartlar[1].Hatalar, h => h.Contains("zaten var"));
        Assert.Contains(kartlar[1].Hatalar, h => h.Contains("%1 ile %90"));
    }

    [Fact]
    public async Task Fiyat_degisikligi_sadece_kategoriye_uygulanir_ve_geri_alinir()
    {
        var islemler = Json("""[{"tur":"arac_fiyati_degistir","yuzde":10,"kategori":"ekonomik"}]""");
        var kayit = await _panel.UygulaAsync(islemler, "Ekonomik araçlara %10 zam", null, "admin", null, 0);
        Assert.Equal(1100, (await _t.Vt.Araclar.FindAsync(_t.Ekonomik.Id))!.GunlukFiyat);
        Assert.Equal(3000, (await _t.Vt.Araclar.FindAsync(_t.Luks.Id))!.GunlukFiyat);   // Lüks değişmedi

        await _panel.GeriAlAsync(kayit);
        Assert.Equal(1000, (await _t.Vt.Araclar.FindAsync(_t.Ekonomik.Id))!.GunlukFiyat);
    }

    [Fact]
    public async Task Yazi_degisikligi_geri_alininca_varsayilan_yaziya_doner()
    {
        var kayit = await _panel.UygulaAsync(Json("""[{"tur":"metin_degistir","anahtar":"anasayfa.baslik","yeni_metin":"Yolculuk burada başlar"}]"""), "başlık", null, "admin", null, 0);
        Assert.Equal("Yolculuk burada başlar", (await _t.Vt.Metinler.FindAsync("anasayfa.baslik"))!.Deger);
        await _panel.GeriAlAsync(kayit);
        Assert.Null(await _t.Vt.Metinler.AsNoTracking().FirstOrDefaultAsync(m => m.Anahtar == "anasayfa.baslik"));
    }

    [Fact]
    public async Task Ozellik_eklenip_araclara_deger_verilir()
    {
        var kayit = await _panel.UygulaAsync(Json("""
            [{"tur":"arac_ozelligi_ekle","ad":"Klima","ozellik_turu":"evet_hayir","filtrede_goster":true},
             {"tur":"arac_ozellik_degeri_ver","ozellik":"Klima","deger":"true"}]
            """), "klima", null, "admin", null, 0);
        Assert.Equal(2, await _t.Vt.AracOzellikDegerleri.CountAsync());   // test veritabanındaki 2 aracın ikisine de
        await _panel.GeriAlAsync(kayit);
        Assert.Equal(0, await _t.Vt.AracOzellikleri.CountAsync());
        Assert.Equal(0, await _t.Vt.AracOzellikDegerleri.CountAsync());
    }

    [Fact]
    public async Task Kucuk_modellerin_sik_hatalari_yakalanir()
    {
        var kartlar = await _panel.DogrulaAsync(Json("""
            [{"tur":"metin_degistir","anahtar":"anasayfa.baslik","yeni_metin":"Yolculuk burada başlar"},
             {"tur":"metin_degistir","anahtar":"anasayfa.baslik-vurgu","yeni_metin":"Yolculuk burada başlar"},
             {"tur":"musteri_bilgisi_ekle","ad":"Kategori","bilgi_turu":"secim","secenekler":["SUV"]},
             {"tur":"sayfa_ekle","baslik":"SSS","icerik":"Kısa."}]
            """));
        Assert.Empty(kartlar[0].Hatalar);
        Assert.Contains(kartlar[1].Hatalar, h => h.Contains("tekrar eder"));         // aynı cümle iki parçaya
        Assert.Contains(kartlar[2].Hatalar, h => h.Contains("araca ait"));           // "Kategori" müşteri bilgisi değil
        Assert.Contains(kartlar[3].Hatalar, h => h.Contains("çok kısa"));            // boş sayfa
    }

    [Theory]
    [InlineData("Yolculuk burada başlar", null, "Yolculuk", "burada", "başlar")]
    [InlineData("Hayalindeki araç seni bekliyor", "araç", "Hayalindeki", "araç", "seni bekliyor")]
    [InlineData("Hoş geldiniz", null, "Hoş", "geldiniz", "")]
    [InlineData("Merhaba", null, "", "Merhaba", "")]
    public void Ana_baslik_uc_parcaya_bolunur(string metin, string? renkli, string bas, string vurgu, string son)
    {
        var p = new JsonObject { ["metin"] = metin, ["renkli_kisim"] = renkli };
        var parcalar = AnaBaslikDegistir.Parcalar(p);
        Assert.Equal([bas, vurgu, son], [parcalar["anasayfa.baslik"], parcalar["anasayfa.baslik-vurgu"], parcalar["anasayfa.baslik-son"]]);
    }

    [Fact]
    public async Task Kampanya_ve_ek_hizmet_guncellenir_ve_geri_alinir()
    {
        _t.Vt.Kampanyalar.Add(new Kampanya { Ad = "Öğretmen indirimi", IndirimOrani = 0.15m });
        await _t.Vt.SaveChangesAsync();
        var kayit = await _panel.UygulaAsync(Json("""
            [{"tur":"kampanya_guncelle","ad":"öğretmen indirimi","indirim_yuzdesi":20},
             {"tur":"ekstra_guncelle","ad":"tam kasko","gunluk_fiyat":350}]
            """), "güncelle", null, "admin", null, 0);
        Assert.Equal(0.20m, (await _t.Vt.Kampanyalar.SingleAsync()).IndirimOrani);
        Assert.Equal(350, (await _t.Vt.Ekstralar.SingleAsync()).GunlukFiyat);

        await _panel.GeriAlAsync(kayit);
        Assert.Equal(0.15m, (await _t.Vt.Kampanyalar.AsNoTracking().SingleAsync()).IndirimOrani);
        Assert.Equal(300, (await _t.Vt.Ekstralar.AsNoTracking().SingleAsync()).GunlukFiyat);
    }

    [Fact]
    public async Task Cevap_ve_yapilamaz_uygulanmaz()
    {
        var kartlar = await _panel.DogrulaAsync(Json("""[{"tur":"cevap_ver","metin":"2 kampanya var."},{"tur":"yapilamaz","sebep":"Kod gerekir."}]"""));
        Assert.All(kartlar, k => Assert.False(k.Uygulanabilir));
        await Assert.ThrowsAsync<AracKiralama.Yardimcilar.IsKuraliHatasi>(() => _panel.UygulaAsync(Json("""[{"tur":"cevap_ver","metin":"x"}]"""), "x", null, "a", null, 0));
    }

    [Fact]
    public void Sema_her_islem_icin_bir_secenek_icerir()
    {
        var sema = PanelIslemleri.Sema();
        var secenekler = sema["properties"]!["islemler"]!["items"]!["anyOf"]!.AsArray();
        Assert.Equal(PanelIslemleri.Hepsi.Length, secenekler.Count);
        Assert.All(secenekler, s => Assert.Equal("tur", s!["required"]![0]!.ToString()));
        Assert.Equal(PanelIslemleri.Hepsi.Length, PanelIslemleri.Hepsi.Select(i => i.Tur).Distinct().Count());   // türler benzersiz
    }

    // ===================== KOD MODU GÜVENLİĞİ =====================

    private sealed class SahteOrtam(string kok) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = kok;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = kok;
        public string EnvironmentName { get; set; } = "Development";
    }

    private static (KodModu Kod, string Klasor) KodModuOlustur()
    {
        var klasor = Path.Combine(Path.GetTempPath(), "rota-test-" + Guid.NewGuid().ToString("N")[..8], "AracKiralama.Web");
        Directory.CreateDirectory(Path.Combine(klasor, "Views"));
        File.WriteAllText(Path.Combine(klasor, "Views", "Kart.cshtml"), "<div>\n  <b>Fiyat</b>\n</div>\n<b>Fiyat</b>\n<span>Tek</span>\n");
        return (new KodModu(null!, null!, new SahteOrtam(klasor), null!, NullLogger<KodModu>.Instance), klasor);
    }

    [Theory]
    [InlineData("Views/Kart.cshtml", true)]
    [InlineData("Servisler/YeniServis.cs", true)]
    [InlineData("../Dockerfile", false)]
    [InlineData("..\\..\\README.md", false)]
    [InlineData("C:/Windows/x.cs", false)]
    [InlineData("appsettings.json", false)]
    [InlineData("AracKiralama.Web.csproj", false)]
    [InlineData("bin/Debug/a.cs", false)]
    [InlineData("wwwroot/lib/bootstrap/x.js", false)]
    [InlineData("Views/resim.png", false)]
    public void Kod_modu_sadece_izinli_dosyalara_yazar(string yol, bool beklenen)
        => Assert.Equal(beklenen, KodModuOlustur().Kod.Izinli(yol));

    [Fact]
    public void Eski_metin_bulunamazsa_ya_da_iki_kez_gecerse_hata_verir()
    {
        var (kod, _) = KodModuOlustur();
        var degisiklikler = new List<KodDegisikligi>
        {
            new() { Dosya = "Views/Kart.cshtml", Eski = "<span>Tek</span>", Yeni = "<span>Bir</span>" },        // tamam
            new() { Dosya = "Views/Kart.cshtml", Eski = "<b>Fiyat</b>", Yeni = "<b>Ücret</b>" },                // iki kez geçiyor
            new() { Dosya = "Views/Kart.cshtml", Eski = "<i>yok</i>", Yeni = "x" },                              // hiç yok
            new() { Dosya = "Views/Kart.cshtml", Yeni = "x", YeniDosya = true },                                  // zaten var
            new() { Dosya = "../disari.cs", Eski = "a", Yeni = "b" },                                             // proje dışı
            new() { Dosya = "Models/Arac.cs", Yeni = "class X {}", YeniDosya = true },                            // olmayan klasör
            new() { Dosya = "Views/Yeni.cshtml", Yeni = "<p>Yeni</p>", YeniDosya = true },                        // tamam
        };
        kod.Denetle(degisiklikler);
        Assert.Null(degisiklikler[0].Hata);
        Assert.Contains("2 kez", degisiklikler[1].Hata);
        Assert.Contains("bulunamadı", degisiklikler[2].Hata);
        Assert.Contains("zaten var", degisiklikler[3].Hata);
        Assert.Contains("yazılamaz", degisiklikler[4].Hata);
        Assert.Contains("klasör yok", degisiklikler[5].Hata);
        Assert.Null(degisiklikler[6].Hata);
    }
}
