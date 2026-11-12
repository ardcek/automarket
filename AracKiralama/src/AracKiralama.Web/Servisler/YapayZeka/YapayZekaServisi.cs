// ============================================================================
// YAPAY ZEKÂ SERVİSİ: bilgisayardaki modele soru sorar, cevabı alır.
// Motorla "OpenAI uyumlu" adresten konuşur: POST {adres}/v1/chat/completions
// (llama-server, Ollama ve LM Studio bu adresi destekler.)
//
// En önemli ayrıntı: cevabın biçimi bir JSON şemasıyla ZORLANIR (response_format).
// Motor, şemaya uymayan bir kelime üretemez; böylece küçük modeller bile her zaman
// okunabilir bir cevap verir. Şemalar: PanelIslemleri.Sema() ve KodModu.
// ============================================================================
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AracKiralama.Yardimcilar;

namespace AracKiralama.Servisler;

/// <summary>Modelin cevabı ve ölçümler.</summary>
public class YapayZekaCevabi
{
    public string Icerik { get; set; } = "";     // modelin yazdığı (şema verildiyse JSON)
    public double Sure { get; set; }             // saniye
    public int UretilenToken { get; set; }       // kaç parça (token) yazdı
    public double TokenHizi { get; set; }        // saniyede kaç token
}

public class YapayZekaServisi
{
    private readonly HttpClient _http;
    private readonly YapayZekaSunucusu _sunucu;

    public YapayZekaServisi(HttpClient http, YapayZekaSunucusu sunucu)
    {
        _http = http;
        _sunucu = sunucu;
    }

    /// <summary>
    /// Modele sorar.
    ///   sistem    : modelin rolü ve kurallar (her soruda başta gönderilir)
    ///   mesajlar  : sohbet (rol "user" = kullanıcı, "assistant" = modelin önceki cevabı)
    ///   sema      : cevabın uyması gereken JSON şeması (null ise serbest yazı)
    ///   enCokToken: cevabın en fazla uzunluğu
    /// </summary>
    public async Task<YapayZekaCevabi> SorAsync(string sistem, IEnumerable<(string Rol, string Icerik)> mesajlar, JsonNode? sema,
        int enCokToken, CancellationToken iptal)
    {
        // Motor kapalıysa önce aç (ilk soruda model belleğe yüklenir)
        if (_sunucu.Durum != SunucuDurumu.Hazir) await _sunucu.BaslatAsync();
        if (_sunucu.Durum != SunucuDurumu.Hazir) throw new IsKuraliHatasi(_sunucu.Mesaj);

        var liste = new JsonArray { new JsonObject { ["role"] = "system", ["content"] = sistem } };
        foreach (var (rol, metin) in mesajlar) liste.Add(new JsonObject { ["role"] = rol, ["content"] = metin });

        var istek = new JsonObject
        {
            ["model"] = _sunucu.ModelAdi ?? "yerel",
            ["messages"] = liste,
            ["temperature"] = 0.2,          // düşük = daha tutarlı, daha az "yaratıcı" cevap
            ["max_tokens"] = enCokToken,
            // Düşünen (thinking) modellerde düşünme bölümünü kapat: cevap doğrudan JSON olsun, daha hızlı olsun
            ["chat_template_kwargs"] = new JsonObject { ["enable_thinking"] = false },
        };
        if (sema != null)
            istek["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject { ["name"] = "cevap", ["strict"] = true, ["schema"] = sema.DeepClone() },
            };

        var sayac = Stopwatch.StartNew();
        using var cevap = await _http.PostAsync(_sunucu.Adres + "/v1/chat/completions",
            new StringContent(istek.ToJsonString(), Encoding.UTF8, "application/json"), iptal);
        var govde = await cevap.Content.ReadAsStringAsync(iptal);
        if (!cevap.IsSuccessStatusCode)
            throw new IsKuraliHatasi($"Yapay zekâ hata verdi ({(int)cevap.StatusCode}): {Kisalt(govde, 300)}");

        var json = JsonNode.Parse(govde)!;
        var icerik = json["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";
        var sonuc = new YapayZekaCevabi
        {
            Icerik = Temizle(icerik),
            Sure = Math.Round(sayac.Elapsed.TotalSeconds, 1),
            UretilenToken = json["usage"]?["completion_tokens"]?.GetValue<int>() ?? 0,
        };
        sonuc.TokenHizi = json["timings"]?["predicted_per_second"] is JsonValue hiz ? Math.Round(hiz.GetValue<double>(), 1)
            : sonuc.Sure > 0 ? Math.Round(sonuc.UretilenToken / sonuc.Sure, 1) : 0;
        return sonuc;
    }

    /// <summary>Cevabı JSON olarak okur; bozuksa anlaşılır bir hata verir.</summary>
    public static JsonObject JsonOku(string icerik)
    {
        try
        {
            return JsonNode.Parse(icerik) as JsonObject ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new IsKuraliHatasi("Yapay zekânın cevabı okunamadı (yarım kalmış olabilir). Lütfen isteği tekrar gönderin. Cevap: " + Kisalt(icerik, 200));
        }
    }

    /// <summary>Bazı modellerin eklediği düşünme bölümünü ve ``` işaretlerini atar.</summary>
    private static string Temizle(string metin)
    {
        metin = Regex.Replace(metin, "<think>.*?</think>", "", RegexOptions.Singleline).Trim();
        var kod = Regex.Match(metin, "^```(?:json)?\\s*(.*?)\\s*```$", RegexOptions.Singleline);
        return kod.Success ? kod.Groups[1].Value : metin;
    }

    private static string Kisalt(string metin, int uzunluk) => metin.Length <= uzunluk ? metin : metin[..uzunluk] + "…";
}
