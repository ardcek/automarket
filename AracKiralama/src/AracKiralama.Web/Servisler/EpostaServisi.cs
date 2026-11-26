// ============================================================================
// E-POSTA GÖNDERİMİ ("Şifremi unuttum" linki, panelden test e-postası)
// İki yol var, hangisinin ayarı girilmişse o kullanılır:
//
//   1) BREVO (önerilen, canlı site için ŞART): e-postayı internet sitesi gibi HTTPS ile gönderir.
//      Render'ın ücretsiz planı e-posta portlarını (25/465/587) kapattığı için canlıda SMTP ÇALIŞMAZ.
//      Render → Environment:
//        Eposta__BrevoApiAnahtari = xkeysib-...           (brevo.com → SMTP & API → API Keys)
//        Eposta__Gonderen         = sizin@adresiniz.com   (brevo.com → Senders'ta doğrulanmış adres)
//        Eposta__GonderenAdi      = Rota Rent a Car       (isteğe bağlı)
//
//   2) SMTP (bilgisayarda ya da ücretli sunucuda): Gmail "uygulama şifresi" vb.
//        Eposta__Sunucu = smtp.gmail.com, Eposta__Port = 587, Eposta__Kullanici, Eposta__Sifre, Eposta__Gonderen
//
// Hiçbiri girilmemişse e-posta GÖNDERİLMEZ: link loga yazılır, bilgisayarda (Development) ekranda da gösterilir.
// Durum ve "Test e-postası gönder": Yönetim → Personel ve Roller → E-posta gönderimi.
// ============================================================================
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace AracKiralama.Servisler;

public class EpostaAyarlari
{
    public string? BrevoApiAnahtari { get; set; }
    /// <summary>Brevo'nun adresi; değiştirmeyin (sadece testte sahte bir sunucuya yönlendirmek için).</summary>
    public string BrevoAdresi { get; set; } = "https://api.brevo.com/v3/smtp/email";
    public string? Sunucu { get; set; }
    public int Port { get; set; } = 587;
    public string? Kullanici { get; set; }
    public string? Sifre { get; set; }
    public string? Gonderen { get; set; }
    public string GonderenAdi { get; set; } = "Rota Rent a Car";
}

public class EpostaServisi(IOptions<EpostaAyarlari> ayarlar, HttpClient http, ILogger<EpostaServisi> log)
{
    private readonly EpostaAyarlari _a = ayarlar.Value;

    /// <summary>Son gönderim denemesinin sonucu (panelde gösterilir; uygulama yeniden başlayınca sıfırlanır).</summary>
    public static (DateTime Zaman, bool Basarili, string Mesaj)? SonDeneme { get; private set; }

    private bool BrevoVar => !string.IsNullOrWhiteSpace(_a.BrevoApiAnahtari);
    private bool SmtpVar => !string.IsNullOrWhiteSpace(_a.Sunucu);

    /// <summary>E-posta ayarları girilmiş mi? (Girilmemişse e-posta yerine loga yazılır.)</summary>
    public bool Ayarli => (BrevoVar || SmtpVar) && !string.IsNullOrWhiteSpace(_a.Gonderen);

    /// <summary>Panelde gösterilecek kısa açıklama: hangi yolla, hangi adresten gönderiliyor.</summary>
    public string Yontem => !Ayarli
        ? (BrevoVar || SmtpVar ? "Gönderen adres (Eposta__Gonderen) eksik" : "Ayarlı değil")
        : BrevoVar ? $"Brevo (HTTPS) · gönderen {_a.Gonderen}" : $"SMTP {_a.Sunucu}:{_a.Port} · gönderen {_a.Gonderen}";

    /// <summary>E-posta gönderir. Gönderildiyse true; ayar yoksa ya da hata olursa false (hata loga yazılır).</summary>
    public async Task<bool> GonderAsync(string alici, string konu, string html, string? duzMetin = null)
    {
        if (!Ayarli)
        {
            log.LogWarning("E-posta ayarlı değil. Gönderilmedi → {Alici} · {Konu}", alici, konu);
            Kaydet(false, "E-posta ayarlı değil");
            return false;
        }
        try
        {
            if (BrevoVar) await BrevoIleGonderAsync(alici, konu, html, duzMetin);
            else await SmtpIleGonderAsync(alici, konu, html);
            Kaydet(true, $"{alici} adresine gönderildi");
            return true;
        }
        catch (Exception hata)
        {
            log.LogError("E-posta gönderilemedi ({Alici}): {Mesaj}", alici, hata.Message);
            Kaydet(false, hata.Message);
            return false;
        }
    }

    /// <summary>"Şifremi unuttum" e-postası: düğmeli HTML + düz metin (bazı e-posta programları HTML göstermez).</summary>
    public Task<bool> SifirlamaLinkiGonderAsync(string alici, string ad, string link, string firmaAdi)
    {
        var guvenliAd = WebUtility.HtmlEncode(ad);
        var guvenliLink = WebUtility.HtmlEncode(link);
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:0 auto;padding:24px;color:#0f172a">
              <h2 style="margin:0 0 16px">{WebUtility.HtmlEncode(firmaAdi)} · Şifre sıfırlama</h2>
              <p>Merhaba {guvenliAd},</p>
              <p>Şifrenizi sıfırlamak için aşağıdaki düğmeye tıklayın. Bağlantı <b>2 saat</b> geçerlidir ve bir kez kullanılabilir.</p>
              <p style="margin:28px 0"><a href="{guvenliLink}" style="background:#2563eb;color:#fff;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:bold;display:inline-block">Yeni şifre belirle</a></p>
              <p style="font-size:13px;color:#475569">Düğme çalışmazsa bu adresi tarayıcınıza yapıştırın:<br><a href="{guvenliLink}" style="color:#2563eb;word-break:break-all">{guvenliLink}</a></p>
              <p style="font-size:13px;color:#475569">Bu isteği siz yapmadıysanız bu e-postayı görmezden gelebilirsiniz; şifreniz değişmez.</p>
            </div>
            """;
        var metin = $"Merhaba {ad},\n\nŞifrenizi sıfırlamak için bu bağlantıyı açın (2 saat geçerli, tek kullanımlık):\n{link}\n\nBu isteği siz yapmadıysanız bu e-postayı görmezden gelebilirsiniz.\n\n{firmaAdi}";
        return GonderAsync(alici, $"{firmaAdi} · Şifre sıfırlama", html, metin);
    }

    // Brevo'nun e-posta API'si: https://developers.brevo.com/reference/sendtransacemail
    private async Task BrevoIleGonderAsync(string alici, string konu, string html, string? duzMetin)
    {
        using var istek = new HttpRequestMessage(HttpMethod.Post, _a.BrevoAdresi);
        istek.Headers.Add("api-key", _a.BrevoApiAnahtari!.Trim());
        istek.Headers.Add("accept", "application/json");
        istek.Content = JsonContent.Create(new
        {
            sender = new { name = _a.GonderenAdi, email = _a.Gonderen },
            to = new[] { new { email = alici } },
            subject = konu,
            htmlContent = html,
            textContent = duzMetin,
        });
        using var yanit = await http.SendAsync(istek);
        if (!yanit.IsSuccessStatusCode)
        {
            // Brevo hatayı {"code":"...","message":"..."} olarak döner; anahtar yanlış, gönderen doğrulanmamış vb.
            var govde = await yanit.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Brevo {(int)yanit.StatusCode}: {(govde.Length > 300 ? govde[..300] : govde)}");
        }
    }

    private async Task SmtpIleGonderAsync(string alici, string konu, string html)
    {
        using var istemci = new SmtpClient(_a.Sunucu, _a.Port)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrEmpty(_a.Kullanici) ? null : new NetworkCredential(_a.Kullanici, _a.Sifre),
        };
        using var mesaj = new MailMessage(new MailAddress(_a.Gonderen!, _a.GonderenAdi), new MailAddress(alici))
        {
            Subject = konu, Body = html, IsBodyHtml = true,
        };
        // Port kapalıysa (Render ücretsiz plan) sayfa dakikalarca beklemesin: 20 saniyede vazgeç
        using var sure = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await istemci.SendMailAsync(mesaj, sure.Token);
    }

    private static void Kaydet(bool basarili, string mesaj) => SonDeneme = (DateTime.Now, basarili, mesaj);
}
