// ============================================================================
// E-POSTA GÖNDERİMİ (şimdilik sadece "Şifremi unuttum" linki için)
// .NET'in kendi SMTP istemcisiyle gönderir; ek paket yok.
// Ayarlar appsettings.json → "Eposta" bölümünde; canlı sitede Render → Environment:
//   Eposta__Sunucu   = smtp-relay.brevo.com  (ya da smtp.gmail.com)
//   Eposta__Port     = 587
//   Eposta__Kullanici, Eposta__Sifre  = SMTP kullanıcı adı / şifresi (Gmail'de "uygulama şifresi")
//   Eposta__Gonderen = destek@alanadiniz.com   (gönderen adres)
// Sunucu boşsa e-posta GÖNDERİLMEZ: link loga yazılır, bilgisayarda (Development) ekranda da gösterilir.
// ============================================================================
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace AracKiralama.Servisler;

public class EpostaAyarlari
{
    public string? Sunucu { get; set; }
    public int Port { get; set; } = 587;
    public string? Kullanici { get; set; }
    public string? Sifre { get; set; }
    public string? Gonderen { get; set; }
    public string GonderenAdi { get; set; } = "Rota Rent a Car";
}

public class EpostaServisi(IOptions<EpostaAyarlari> ayarlar, ILogger<EpostaServisi> log)
{
    private readonly EpostaAyarlari _a = ayarlar.Value;

    /// <summary>SMTP ayarları girilmiş mi? (Girilmemişse e-posta yerine loga yazılır.)</summary>
    public bool Ayarli => !string.IsNullOrWhiteSpace(_a.Sunucu) && !string.IsNullOrWhiteSpace(_a.Gonderen);

    /// <summary>E-posta gönderir. Gönderildiyse true; ayar yoksa ya da hata olursa false (hata loga yazılır).</summary>
    public async Task<bool> GonderAsync(string alici, string konu, string html)
    {
        if (!Ayarli)
        {
            log.LogWarning("E-posta ayarlı değil (Eposta:Sunucu boş). Gönderilmedi → {Alici} · {Konu}", alici, konu);
            return false;
        }
        try
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
            await istemci.SendMailAsync(mesaj);
            return true;
        }
        catch (Exception hata)
        {
            log.LogError("E-posta gönderilemedi ({Alici}): {Mesaj}", alici, hata.Message);
            return false;
        }
    }
}
