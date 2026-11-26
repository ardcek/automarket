// ============================================================================
// E-POSTA TESTLERİ: "Şifremi unuttum" e-postası Brevo'ya doğru gidiyor mu? (Servisler/EpostaServisi.cs)
// Gerçek Brevo'ya bağlanılmaz: istekler sahte bir HTTP işleyicisinde yakalanır.
// ============================================================================
using System.Net;
using System.Text.Json;
using AracKiralama.Servisler;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AracKiralama.Testler;

public class EpostaTestleri
{
    /// <summary>Gelen isteği saklayan, istenen durum koduyla cevap veren sahte HTTP işleyicisi.</summary>
    private class SahteBrevo(HttpStatusCode kod) : HttpMessageHandler
    {
        public HttpRequestMessage? Istek;
        public string Govde = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            Istek = istek;
            Govde = await istek.Content!.ReadAsStringAsync(iptal);
            return new HttpResponseMessage(kod) { Content = new StringContent(kod == HttpStatusCode.Created ? "{\"messageId\":\"x\"}" : "{\"code\":\"unauthorized\",\"message\":\"Key not found\"}") };
        }
    }

    private static (EpostaServisi Servis, SahteBrevo Brevo) Olustur(EpostaAyarlari ayar, HttpStatusCode kod = HttpStatusCode.Created)
    {
        var brevo = new SahteBrevo(kod);
        return (new EpostaServisi(Options.Create(ayar), new HttpClient(brevo), NullLogger<EpostaServisi>.Instance), brevo);
    }

    [Fact]
    public async Task Ayar_yoksa_gonderilmez_ve_hata_vermez()
    {
        var (servis, brevo) = Olustur(new EpostaAyarlari());

        Assert.False(servis.Ayarli);
        Assert.False(await servis.GonderAsync("a@b.com", "Konu", "<p>x</p>"));
        Assert.Null(brevo.Istek);
    }

    [Fact]
    public void Gonderen_adres_olmadan_ayarli_sayilmaz()
    {
        var (servis, _) = Olustur(new EpostaAyarlari { BrevoApiAnahtari = "anahtar" });
        Assert.False(servis.Ayarli);
        Assert.Contains("Gonderen", servis.Yontem);
    }

    [Fact]
    public async Task Sifirlama_epostasi_brevoya_dogru_bicimde_gider()
    {
        var (servis, brevo) = Olustur(new EpostaAyarlari { BrevoApiAnahtari = " anahtar-123 ", Gonderen = "destek@rota.com", GonderenAdi = "Rota" });
        var link = "https://site/Hesap/SifreSifirla?eposta=ayse%40ornek.com&kod=ABC";

        Assert.True(await servis.SifirlamaLinkiGonderAsync("ayse@ornek.com", "Ayşe <script>", link, "Rota"));

        Assert.Equal("https://api.brevo.com/v3/smtp/email", brevo.Istek!.RequestUri!.ToString());
        Assert.Equal("anahtar-123", brevo.Istek.Headers.GetValues("api-key").Single());   // boşluklar kırpılır
        using var json = JsonDocument.Parse(brevo.Govde);
        var kok = json.RootElement;
        Assert.Equal("destek@rota.com", kok.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("ayse@ornek.com", kok.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Contains(link, kok.GetProperty("textContent").GetString());              // düz metinde link aynen
        var html = kok.GetProperty("htmlContent").GetString()!;
        Assert.Contains("eposta=ayse%40ornek.com&amp;kod=ABC", html);                      // HTML'de güvenli biçimde
        Assert.DoesNotContain("<script>", html);                                           // ad HTML olarak çalışmaz
    }

    [Fact]
    public async Task Brevo_hata_verirse_false_doner_ve_sebep_saklanir()
    {
        var (servis, _) = Olustur(new EpostaAyarlari { BrevoApiAnahtari = "yanlis", Gonderen = "destek@rota.com" }, HttpStatusCode.Unauthorized);

        Assert.False(await servis.GonderAsync("a@b.com", "Konu", "<p>x</p>"));
        Assert.False(EpostaServisi.SonDeneme!.Value.Basarili);
        Assert.Contains("401", EpostaServisi.SonDeneme.Value.Mesaj);
    }
}
