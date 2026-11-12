// ============================================================================
// KOD MODU (deneysel, sadece Admin): panelden yapılamayan istekler için yapay zekâ
// projenin KOD DOSYALARINI değiştirir. Güvenlik adımları:
//   1) Dosya seçimi : model, proje haritasından (her dosyanın başındaki açıklama) en fazla 6 dosya seçer.
//   2) Öneri        : model "eski metin → yeni metin" parçaları yazar; "eski" dosyada tam bir kez geçmeli.
//   3) Dene         : değişiklikler GEÇİCİ BİR KOPYADA uygulanıp derlenir (dotnet build). Gerçek dosyalar değişmez.
//   4) Uygula       : değişecek dosyalar önce AracKiralama/.yapay-zeka-yedek/<tarih>/ klasörüne yedeklenir,
//                     sonra yazılır. Geçmişe kaydedilir; panelden ya da GERI-AL.bat ile geri alınır.
//   5) Yeniden başlat: uygulama 42 koduyla kapanır, BASLAT.bat yeniden derleyip açar.
// Sınırlar: sadece src/AracKiralama.Web içine yazılır; bin, obj, wwwroot/lib, Properties,
// .csproj ve appsettings dosyalarına dokunulmaz.
// ============================================================================
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;

namespace AracKiralama.Servisler;

/// <summary>Tek bir dosya değişikliği. YeniDosya = true ise Eski boştur ve Yeni dosyanın tamamıdır.</summary>
public class KodDegisikligi
{
    public string Dosya { get; set; } = "";     // "Views/Shared/_AracKarti.cshtml"
    public string Eski { get; set; } = "";
    public string Yeni { get; set; } = "";
    public bool YeniDosya { get; set; }
    public string? Hata { get; set; }           // "eski metin dosyada bulunamadı" gibi
}

public class KodModu
{
    /// <summary>Uygulamanın BASLAT.bat'a "beni yeniden derleyip aç" demek için kullandığı çıkış kodu.</summary>
    public const int YenidenBaslatKodu = 42;

    private static readonly string[] Uzantilar = [".cs", ".cshtml", ".js", ".css", ".json"];
    private static readonly string[] Yasaklar = ["bin/", "obj/", "wwwroot/lib/", "Properties/", "anahtarlar/"];
    private const int DosyaButcesi = 18_000;     // modele gönderilecek dosya içeriği üst sınırı (karakter); bağlam 16 bin token'a sığsın

    private readonly VeriTabani _vt;
    private readonly YapayZekaServisi _yapayZeka;
    private readonly IHostApplicationLifetime _omur;
    private readonly ILogger<KodModu> _log;

    public KodModu(VeriTabani vt, YapayZekaServisi yapayZeka, IWebHostEnvironment ortam, IHostApplicationLifetime omur, ILogger<KodModu> log)
    {
        _vt = vt;
        _yapayZeka = yapayZeka;
        _omur = omur;
        _log = log;
        ProjeKlasoru = ortam.ContentRootPath;
        YedekKlasoru = Path.GetFullPath(Path.Combine(ProjeKlasoru, "..", "..", ".yapay-zeka-yedek"));
    }

    /// <summary>src/AracKiralama.Web (kodun olduğu yer).</summary>
    public string ProjeKlasoru { get; }
    public string YedekKlasoru { get; }

    /// <summary>Uygulama BASLAT.bat ile mi açıldı? (Öyleyse "yeniden başlat" düğmesi çalışır.)</summary>
    public static bool BaslaticidanMi => Environment.GetEnvironmentVariable("ROTA_BASLATICI") == "1";

    /// <summary>Kod modu bu kullanıcı ve bu bilgisayar için açık mı? Kapalıysa sebebi.</summary>
    public (bool Acik, string? Neden) AcikMi(ClaimsPrincipal kullanici)
    {
        if (!kullanici.IsInRole(Roller.Admin)) return (false, "Kod modu sadece Admin hesabıyla kullanılabilir.");
        if (!File.Exists(Path.Combine(ProjeKlasoru, "AracKiralama.Web.csproj"))) return (false, "Uygulama kaynak koddan çalışmıyor (canlı sitede kod modu kapalıdır).");
        return (true, null);
    }

    // ===================== 1) PROJE HARİTASI VE DOSYA SEÇİMİ =====================

    /// <summary>Düzenlenebilir dosyalar ve her birinin başındaki açıklamanın ilk satırı.</summary>
    public List<(string Yol, string Aciklama)> ProjeHaritasi()
    {
        var liste = new List<(string, string)>();
        foreach (var tam in Directory.EnumerateFiles(ProjeKlasoru, "*", SearchOption.AllDirectories))
        {
            var yol = Path.GetRelativePath(ProjeKlasoru, tam).Replace('\\', '/');
            if (!Uzantilar.Contains(Path.GetExtension(yol)) || !Izinli(yol)) continue;
            if (yol.StartsWith("wwwroot/") && !yol.StartsWith("wwwroot/js/") && !yol.StartsWith("wwwroot/css/")) continue;
            liste.Add((yol, Aciklama(tam)));
        }
        return liste.OrderBy(x => x.Item1).ToList();
    }

    /// <summary>Dosyanın başındaki açıklama yorumundan ilk anlamlı satır ("// ARAÇ KARTI: ..." gibi).</summary>
    private static string Aciklama(string tamYol)
    {
        foreach (var satir in File.ReadLines(tamYol).Take(12))
        {
            var temiz = satir.Trim().TrimStart('/', '@', '*', ' ').TrimEnd('*', '@', ' ');
            if (temiz.Length > 3 && !temiz.StartsWith("====") && !temiz.StartsWith("using ") && !temiz.StartsWith("<"))
                return temiz.Length > 110 ? temiz[..110] : temiz;
        }
        return "";
    }

    /// <summary>Bu yola yazılabilir mi? (proje içinde, yasaklı klasörlerde değil, appsettings / csproj değil)</summary>
    public bool Izinli(string yol)
    {
        yol = yol.Replace('\\', '/').TrimStart('/');
        if (yol.Contains("..") || Path.IsPathRooted(yol) || yol.Contains(':')) return false;
        if (Yasaklar.Any(y => yol.StartsWith(y, StringComparison.OrdinalIgnoreCase))) return false;
        var ad = Path.GetFileName(yol);
        if (ad.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) || ad.EndsWith(".csproj") || ad.EndsWith(".db")) return false;
        var tam = Path.GetFullPath(Path.Combine(ProjeKlasoru, yol));
        return tam.StartsWith(ProjeKlasoru, StringComparison.OrdinalIgnoreCase) && Uzantilar.Contains(Path.GetExtension(yol));
    }

    /// <summary>Modelden, istek için okunması gereken dosyaları seçmesini ister.</summary>
    public async Task<(string? Anladigim, List<string> Dosyalar, double Sure)> DosyaSecAsync(string istek, CancellationToken iptal)
    {
        var harita = ProjeHaritasi();
        var sistem = new StringBuilder("""
            Sen bir ASP.NET Core MVC (C#) projesinde değişiklik yapacak yardımcının ilk adımısın.
            Kullanıcının isteğini yapmak için hangi dosyaların okunup değiştirilmesi gerektiğini PROJE HARİTASI'ndan seç (en fazla 6, en önemlisi önce).
            Proje Türkçe adlandırılmıştır: Controllers (sayfa adresleri), Views (sayfaların HTML'i, .cshtml), Modeller (veritabanı tabloları),
            Servisler (iş kuralları), FormModelleri (formlar ve sayfa modelleri), wwwroot/css/stil.css (görünüm), wwwroot/js (tarayıcı kodu).
            ÖNEMLİ: Views/ ve Controllers/ = MÜŞTERİNİN gördüğü site. Areas/Yonetim/ = YÖNETİM PANELİ.
            Kullanıcı "panel", "yönetim" ya da "admin" demediyse müşteri sitesindeki dosyaları (Views/...) seç.
            Araç detay sayfası = Views/Araclar/Detay.cshtml, araç listesi = Views/Araclar/Index.cshtml, araç kartı = Views/Shared/_AracKarti.cshtml.
            "anladigim": isteği bir cümleyle özetle.

            PROJE HARİTASI (dosya — açıklama):
            """);
        foreach (var (yol, aciklama) in harita) sistem.AppendLine($"{yol} — {aciklama}");

        var sema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["anladigim"] = new JsonObject { ["type"] = "string" },
                ["dosyalar"] = new JsonObject
                {
                    ["type"] = "array", ["minItems"] = 1, ["maxItems"] = 6,
                    ["items"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(harita.Select(h => (JsonNode)h.Yol).ToArray()) },
                },
            },
            ["required"] = new JsonArray("anladigim", "dosyalar"),
            ["additionalProperties"] = false,
        };
        var cevap = await _yapayZeka.SorAsync(sistem.ToString(), [("user", istek)], sema, 600, iptal);
        var json = YapayZekaServisi.JsonOku(cevap.Icerik);
        var dosyalar = (json["dosyalar"] as JsonArray ?? new()).Select(d => d?.ToString() ?? "").Where(Izinli).Distinct().ToList();
        return (json["anladigim"]?.ToString(), dosyalar, cevap.Sure);
    }

    // ===================== 2) DEĞİŞİKLİK ÖNERİSİ =====================

    /// <summary>
    /// Seçilen dosyaları modele verip değişiklik ister. oncekiHata doluysa (derleme hatası gibi)
    /// önceki öneri ve hata da gönderilir; model düzeltilmiş bir öneri yazar.
    /// </summary>
    public async Task<(string? Anladigim, List<KodDegisikligi> Degisiklikler, double Sure)> OnerAsync(string istek, List<string> dosyalar,
        List<KodDegisikligi>? oncekiOneri, string? oncekiHata, CancellationToken iptal)
    {
        dosyalar = dosyalar.Where(Izinli).Distinct().ToList();
        var sistem = new StringBuilder("""
            Sen bu ASP.NET Core MVC (C#, .NET 10, Entity Framework Core) projesinde küçük ve dikkatli kod değişiklikleri yapan bir yardımcısın.
            KURALLAR:
            - Değişikliği "degisiklikler" listesine yaz. Her biri: dosya, eski, yeni.
              "eski": dosyada AYNEN geçen (boşluklar ve girintiler dahil), sadece bir kez geçen, 1-15 satırlık bir parça.
              "yeni": "eski"nin yerine gelecek metin (eski parçayı da içerebilir; örn. sonuna yeni satır eklemek için).
            - Yeni bir dosya gerekiyorsa "yeni_dosyalar" listesine tam içeriğiyle yaz (yol: proje içinde, örn. Servisler/YeniServis.cs).
            - Bütün yeni adlar Türkçe olsun (sınıf, metot, değişken, CSS sınıfı). Her değişikliğin üstüne kısa Türkçe açıklama yorumu ekle
              (C#: //, .cshtml: @* *@, CSS: /* */).
            - View'lara <script> ya da onclick yazma (güvenlik kuralı). Sayfadaki yazılar mümkünse @Icerik.Metin("...") ile gelir.
            - Veritabanı: migration yok. Bir modele özellik eklersen yerel veritabanı örnek verilerle yeniden oluşturulur; bunu "anladigim"da söyle.
            - En küçük değişikliği yap; istenmeyen hiçbir şeyi değiştirme.
            - Sınıfların özellik adlarını uydurma: aşağıdaki VERİ MODELİ'nde yazanları aynen kullan.
            - "anladigim": ne yapacağını 1-3 cümleyle Türkçe anlat.

            """);
        sistem.AppendLine(VeriModeliOzeti(dosyalar));
        sistem.AppendLine("DOSYALAR:");
        var kalan = DosyaButcesi;
        var gonderilenler = new List<string>();
        foreach (var yol in dosyalar)
        {
            var tam = Path.Combine(ProjeKlasoru, yol);
            if (!File.Exists(tam)) continue;
            var icerik = await File.ReadAllTextAsync(tam, iptal);
            if (icerik.Length > kalan) { sistem.AppendLine($"=== {yol} === (çok uzun olduğu için gönderilmedi)"); continue; }
            kalan -= icerik.Length;
            gonderilenler.Add(yol);
            sistem.AppendLine($"=== {yol} ===").AppendLine(icerik.Replace("\r\n", "\n"));
        }
        if (gonderilenler.Count == 0) throw new IsKuraliHatasi("Seçilen dosyalar okunamadı ya da çok uzun. Daha az / daha küçük dosya seçin.");

        var mesajlar = new List<(string, string)> { ("user", istek) };
        if (oncekiOneri != null && oncekiHata != null)
        {
            mesajlar.Add(("assistant", OneriJson(oncekiOneri).ToJsonString()));
            mesajlar.Add(("user", "Bu öneride şu sorunlar çıktı:\n" + oncekiHata + "\nSorunları düzelterek önerinin tamamını yeniden yaz."));
        }

        var sema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["anladigim"] = new JsonObject { ["type"] = "string" },
                ["degisiklikler"] = new JsonObject
                {
                    ["type"] = "array", ["maxItems"] = 12,
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["dosya"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(gonderilenler.Select(g => (JsonNode)g).ToArray()) },
                            ["eski"] = new JsonObject { ["type"] = "string" },
                            ["yeni"] = new JsonObject { ["type"] = "string" },
                        },
                        ["required"] = new JsonArray("dosya", "eski", "yeni"),
                        ["additionalProperties"] = false,
                    },
                },
                ["yeni_dosyalar"] = new JsonObject
                {
                    ["type"] = "array", ["maxItems"] = 3,
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject { ["dosya"] = new JsonObject { ["type"] = "string" }, ["icerik"] = new JsonObject { ["type"] = "string" } },
                        ["required"] = new JsonArray("dosya", "icerik"),
                        ["additionalProperties"] = false,
                    },
                },
            },
            ["required"] = new JsonArray("anladigim", "degisiklikler", "yeni_dosyalar"),
            ["additionalProperties"] = false,
        };

        var cevap = await _yapayZeka.SorAsync(sistem.ToString(), mesajlar, sema, 6000, iptal);
        var json = YapayZekaServisi.JsonOku(cevap.Icerik);
        var degisiklikler = new List<KodDegisikligi>();
        foreach (var d in (json["degisiklikler"] as JsonArray ?? new()).OfType<JsonObject>())
            degisiklikler.Add(new KodDegisikligi { Dosya = d["dosya"]?.ToString() ?? "", Eski = d["eski"]?.ToString() ?? "", Yeni = d["yeni"]?.ToString() ?? "" });
        foreach (var d in (json["yeni_dosyalar"] as JsonArray ?? new()).OfType<JsonObject>())
            degisiklikler.Add(new KodDegisikligi { Dosya = (d["dosya"]?.ToString() ?? "").Replace('\\', '/').TrimStart('/'), Yeni = d["icerik"]?.ToString() ?? "", YeniDosya = true });
        Denetle(degisiklikler);
        return (json["anladigim"]?.ToString(), degisiklikler, cevap.Sure);
    }

    /// <summary>
    /// Modelin özellik adı uydurmaması için kısa bir "sözlük": veritabanı sınıfları (Modeller) ve seçilen
    /// .cshtml dosyalarının "@model" satırındaki sınıflar, özellikleriyle. Örn:
    ///   Arac { Id:int, Plaka:string, Model:string, Yil:int, Kilometre:int, GunlukFiyat:decimal, Marka:Marka? ... }
    /// Uygulamanın kendi derlenmiş halinden (reflection) okunur; kod değişince kendiliğinden güncellenir.
    /// </summary>
    private string VeriModeliOzeti(List<string> dosyalar)
    {
        var derleme = typeof(Arac).Assembly;
        var turler = derleme.GetTypes().Where(t => t is { IsClass: true, IsPublic: true, IsAbstract: false } && t.Namespace == "AracKiralama.Modeller" && !t.Name.EndsWith("Attribute")).ToList();

        // Seçilen view'ların modeli (örn. "@model AracKartiModeli") de eklenir
        foreach (var yol in dosyalar.Where(d => d.EndsWith(".cshtml")))
        {
            var tam = Path.Combine(ProjeKlasoru, yol);
            if (!File.Exists(tam)) continue;
            var satir = File.ReadLines(tam).FirstOrDefault(s => s.TrimStart().StartsWith("@model "));
            var ad = satir?.Trim()[7..].Trim().Split('<', '(')[0].Split('.').Last();
            var tur = derleme.GetTypes().FirstOrDefault(t => t.Name == ad && t.IsPublic);
            if (tur != null && !turler.Contains(tur)) turler.Add(tur);
        }

        var sb = new StringBuilder("VERİ MODELİ (sınıf { özellik:tür }):\n");
        foreach (var tur in turler.OrderBy(t => t.Name))
        {
            var ozellikler = tur.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                .Select(o => $"{o.Name}:{TurAdi(o.PropertyType)}");
            var ata = tur.BaseType is { } b && b != typeof(object) ? $" : {b.Name}" : "";
            sb.AppendLine($"{tur.Name}{ata} {{ {string.Join(", ", ozellikler)} }}");
        }
        return sb.ToString();

        static string TurAdi(Type t)
        {
            if (Nullable.GetUnderlyingType(t) is { } ic) return TurAdi(ic) + "?";
            if (!t.IsGenericType) return t.Name switch { "Int32" => "int", "String" => "string", "Boolean" => "bool", "Decimal" => "decimal", "Double" => "double", _ => t.Name };
            return $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(TurAdi))}>";
        }
    }

    /// <summary>Öneriyi modelin yazdığı biçime geri çevirir (düzeltme isterken önceki cevap olarak gönderilir).</summary>
    private static JsonObject OneriJson(List<KodDegisikligi> oneri) => new()
    {
        ["degisiklikler"] = new JsonArray(oneri.Where(d => !d.YeniDosya).Select(d => (JsonNode)new JsonObject { ["dosya"] = d.Dosya, ["eski"] = d.Eski, ["yeni"] = d.Yeni }).ToArray()),
        ["yeni_dosyalar"] = new JsonArray(oneri.Where(d => d.YeniDosya).Select(d => (JsonNode)new JsonObject { ["dosya"] = d.Dosya, ["icerik"] = d.Yeni }).ToArray()),
    };

    /// <summary>
    /// Her değişikliği kontrol eder ve sorunu KodDegisikligi.Hata'ya yazar:
    /// yasaklı yol, "eski" metnin dosyada hiç ya da birden fazla geçmesi, var olan dosyayı "yeni dosya" diye ezmek.
    /// </summary>
    public void Denetle(List<KodDegisikligi> degisiklikler)
    {
        foreach (var d in degisiklikler)
        {
            d.Hata = null;
            if (!Izinli(d.Dosya)) { d.Hata = "Bu dosyaya yazılamaz (proje dışı ya da korumalı)."; continue; }
            var tam = Path.Combine(ProjeKlasoru, d.Dosya);
            if (d.YeniDosya)
            {
                if (File.Exists(tam)) d.Hata = "Bu dosya zaten var; yeni dosya olarak yazılamaz.";
                else if (string.IsNullOrWhiteSpace(d.Yeni)) d.Hata = "Yeni dosyanın içeriği boş.";
                // Yeni dosya sadece var olan bir klasöre eklenebilir (model "Models/" gibi uydurma klasörler açmasın)
                else if (!Directory.Exists(Path.GetDirectoryName(tam))) d.Hata = $"\"{Path.GetDirectoryName(d.Dosya)?.Replace('\\', '/')}\" diye bir klasör yok; projedeki klasörleri kullanın (Modeller, Servisler, Views...).";
                continue;
            }
            if (!File.Exists(tam)) { d.Hata = "Dosya bulunamadı."; continue; }
            if (d.Eski.Length == 0) { d.Hata = "\"Eski\" metin boş."; continue; }
            var adet = Say(Normal(File.ReadAllText(tam)), Normal(d.Eski));
            if (adet == 0) d.Hata = "\"Eski\" metin dosyada bulunamadı (model dosyayı yanlış hatırlamış olabilir).";
            else if (adet > 1) d.Hata = $"\"Eski\" metin dosyada {adet} kez geçiyor; hangisi olduğu belli değil.";
        }
    }

    /// <summary>Satır sonlarını tek biçime getirir (Windows \r\n / Linux \n farkı eşleşmeyi bozmasın).</summary>
    private static string Normal(string metin) => metin.Replace("\r\n", "\n");

    private static int Say(string metin, string aranan)
    {
        int adet = 0, konum = 0;
        while ((konum = metin.IndexOf(aranan, konum, StringComparison.Ordinal)) >= 0) { adet++; konum += aranan.Length; }
        return adet;
    }

    /// <summary>Değişikliği bir dosya içeriğine uygular (satır sonu biçimi korunur).</summary>
    private static string Uygula(string icerik, KodDegisikligi d)
    {
        var windows = icerik.Contains("\r\n");
        var sonuc = Normal(icerik);
        var konum = sonuc.IndexOf(Normal(d.Eski), StringComparison.Ordinal);
        sonuc = sonuc[..konum] + Normal(d.Yeni) + sonuc[(konum + Normal(d.Eski).Length)..];
        return windows ? sonuc.Replace("\n", "\r\n") : sonuc;
    }

    // ===================== 3) DENE: geçici kopyada derle =====================

    /// <summary>
    /// Değişiklikleri %TEMP%\rota-deneme kopyasında uygular ve "dotnet build" çalıştırır.
    /// Gerçek dosyalara dokunmaz. Kopya sonraki denemelerde güncellenerek yeniden kullanılır (hızlı).
    /// </summary>
    public async Task<(bool Basarili, List<string> Hatalar, double Sure)> DeneAsync(List<KodDegisikligi> degisiklikler, CancellationToken iptal)
    {
        Denetle(degisiklikler);
        var sorunlar = degisiklikler.Where(d => d.Hata != null).Select(d => $"{d.Dosya}: {d.Hata}").ToList();
        if (sorunlar.Count > 0) return (false, sorunlar, 0);

        var sayac = Stopwatch.StartNew();
        var kopya = Path.Combine(Path.GetTempPath(), "rota-deneme", "AracKiralama.Web");
        Esitle(ProjeKlasoru, kopya);

        // Değişiklikleri kopyaya uygula (wwwroot derlenmediği için orada sadece "eski" kontrolü yeterli)
        foreach (var d in degisiklikler.Where(d => !d.Dosya.StartsWith("wwwroot/")))
        {
            var hedef = Path.Combine(kopya, d.Dosya);
            Directory.CreateDirectory(Path.GetDirectoryName(hedef)!);
            await File.WriteAllTextAsync(hedef, d.YeniDosya ? d.Yeni : Uygula(await File.ReadAllTextAsync(hedef, iptal), d), iptal);
        }

        // dotnet build: -clp:ErrorsOnly → sadece hatalar yazılır
        var (kod, cikti) = await KomutAsync("dotnet", ["build", Path.Combine(kopya, "AracKiralama.Web.csproj"), "-nologo", "-v:q", "-clp:ErrorsOnly;NoSummary"], kopya, TimeSpan.FromMinutes(4), iptal);
        var hatalar = cikti.Split('\n').Select(s => s.Trim()).Where(s => s.Contains(": error ")).Distinct()
            .Select(s => s.Replace(kopya + Path.DirectorySeparatorChar, "").Replace(kopya, "")).Take(15).ToList();
        if (kod != 0 && hatalar.Count == 0) hatalar.Add("Derleme başarısız: " + (cikti.Length > 500 ? cikti[^500..] : cikti));
        return (kod == 0, hatalar, Math.Round(sayac.Elapsed.TotalSeconds, 1));
    }

    /// <summary>Kaynağı kopyayla eşitler: değişen dosyaları kopyalar, kaynakta olmayanları siler (bin, obj, wwwroot hariç).</summary>
    private static void Esitle(string kaynak, string hedef)
    {
        bool Atla(string goreli) => goreli.StartsWith("bin") || goreli.StartsWith("obj") || goreli.StartsWith("wwwroot") || goreli.StartsWith("anahtarlar")
                                    || goreli.EndsWith(".db") || goreli.EndsWith(".db-shm") || goreli.EndsWith(".db-wal") || goreli.EndsWith(".isaret");
        Directory.CreateDirectory(hedef);
        var kaynakDosyalar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tam in Directory.EnumerateFiles(kaynak, "*", SearchOption.AllDirectories))
        {
            var goreli = Path.GetRelativePath(kaynak, tam);
            if (Atla(goreli.Replace('\\', '/'))) continue;
            kaynakDosyalar.Add(goreli);
            var h = Path.Combine(hedef, goreli);
            var k = new FileInfo(tam);
            var hi = new FileInfo(h);
            if (!hi.Exists || hi.Length != k.Length || hi.LastWriteTimeUtc != k.LastWriteTimeUtc)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(h)!);
                File.Copy(tam, h, overwrite: true);
            }
        }
        foreach (var tam in Directory.EnumerateFiles(hedef, "*", SearchOption.AllDirectories))
        {
            var goreli = Path.GetRelativePath(hedef, tam);
            if (!Atla(goreli.Replace('\\', '/')) && !kaynakDosyalar.Contains(goreli)) File.Delete(tam);   // denemede eklenmiş yeni dosya
        }
    }

    private static async Task<(int Kod, string Cikti)> KomutAsync(string program, string[] parametreler, string klasor, TimeSpan sure, CancellationToken iptal)
    {
        var bilgi = new ProcessStartInfo(program) { WorkingDirectory = klasor, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var p in parametreler) bilgi.ArgumentList.Add(p);
        bilgi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";   // hata satırları her bilgisayarda aynı biçimde gelsin
        using var surec = Process.Start(bilgi)!;
        var cikti = surec.StandardOutput.ReadToEndAsync(iptal);
        var hata = surec.StandardError.ReadToEndAsync(iptal);
        using var zaman = CancellationTokenSource.CreateLinkedTokenSource(iptal);
        zaman.CancelAfter(sure);
        try { await surec.WaitForExitAsync(zaman.Token); }
        catch (OperationCanceledException) { surec.Kill(entireProcessTree: true); return (-1, "Derleme çok uzun sürdü ya da iptal edildi."); }
        return (surec.ExitCode, await cikti + await hata);
    }

    // ===================== 4) UYGULA ve GERİ AL =====================

    /// <summary>Dosyaları yedekler, değişiklikleri gerçek dosyalara yazar ve geçmişe kaydeder.</summary>
    public async Task<(YapayZekaKaydi Kayit, bool VeritabaniYenilenecek)> UygulaAsync(List<KodDegisikligi> degisiklikler, string istek, string? anladigim,
        string kullaniciAdi, string? modelAdi, double sure)
    {
        Denetle(degisiklikler);
        var sorun = degisiklikler.FirstOrDefault(d => d.Hata != null);
        if (sorun != null) throw new IsKuraliHatasi($"{sorun.Dosya}: {sorun.Hata}");
        if (degisiklikler.Count == 0) throw new IsKuraliHatasi("Uygulanacak değişiklik yok.");

        // 1) Yedek: .yapay-zeka-yedek/20261111-153000/ altına değişecek dosyaların şimdiki hali
        var klasorAdi = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var yedek = Path.Combine(YedekKlasoru, klasorAdi);
        var degisenler = degisiklikler.Where(d => !d.YeniDosya).Select(d => d.Dosya).Distinct().ToList();
        var yeniler = degisiklikler.Where(d => d.YeniDosya).Select(d => d.Dosya).Distinct().ToList();
        foreach (var yol in degisenler)
        {
            var hedef = Path.Combine(yedek, yol);
            Directory.CreateDirectory(Path.GetDirectoryName(hedef)!);
            File.Copy(Path.Combine(ProjeKlasoru, yol), hedef, overwrite: true);
        }
        var bilgi = new JsonObject
        {
            ["klasor"] = klasorAdi, ["istek"] = istek, ["tarih"] = DateTime.Now.ToString("s"),
            ["degisenler"] = new JsonArray(degisenler.Select(d => (JsonNode)d).ToArray()),
            ["yeniler"] = new JsonArray(yeniler.Select(d => (JsonNode)d).ToArray()),
        };
        Directory.CreateDirectory(yedek);
        await File.WriteAllTextAsync(Path.Combine(yedek, "degisiklik.json"), bilgi.ToJsonString(new() { WriteIndented = true }));   // GERI-AL.bat bunu okur

        // 2) Değişiklikleri yaz (aynı dosyada birden fazla değişiklik olabilir; sırayla uygulanır).
        //    Dosyanın başında BOM (görünmez UTF-8 işareti) varsa korunur, yoksa eklenmez → git farkında gereksiz değişiklik olmaz.
        foreach (var d in degisiklikler)
        {
            var tam = Path.Combine(ProjeKlasoru, d.Dosya);
            Directory.CreateDirectory(Path.GetDirectoryName(tam)!);
            var bomVar = File.Exists(tam) && (await File.ReadAllBytesAsync(tam)).Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF });
            await File.WriteAllTextAsync(tam, d.YeniDosya ? d.Yeni : Uygula(await File.ReadAllTextAsync(tam), d), new UTF8Encoding(bomVar));
        }

        // 3) Model / veritabanı dosyası değiştiyse: açılışta yerel veritabanı yeniden oluşturulsun
        var veritabani = degisiklikler.Any(d => d.Dosya.StartsWith("Modeller/") || d.Dosya.StartsWith("Veri/"));
        if (veritabani) await File.WriteAllTextAsync(Path.Combine(ProjeKlasoru, "veritabanini-yenile.isaret"), "Yapay zekâ kod modu bir model dosyasını değiştirdi.");

        var kayit = new YapayZekaKaydi
        {
            Tur = "Kod", Istek = istek.Length > 2000 ? istek[..2000] : istek, Anladigim = anladigim, KullaniciAdi = kullaniciAdi, ModelAdi = modelAdi, Sure = sure,
            Ozet = string.Join("\n", Ozetle(degisiklikler)),
            IslemlerJson = OneriJson(degisiklikler).ToJsonString(),
            GeriAlmaJson = bilgi.ToJsonString(),
        };
        _vt.YapayZekaKayitlari.Add(kayit);
        await _vt.SaveChangesAsync();
        _log.LogInformation("Kod modu: {Sayi} dosya değişti, yedek: {Yedek}", degisenler.Count + yeniler.Count, yedek);
        return (kayit, veritabani);

        // Geçmişte görünen özet: "Views/Shared/_AracKarti.cshtml (2 değişiklik)"
        static IEnumerable<string> Ozetle(List<KodDegisikligi> liste)
            => liste.GroupBy(d => d.Dosya).Select(g => g.First().YeniDosya ? $"Yeni dosya: {g.Key}" : $"{g.Key} ({g.Count()} değişiklik)");
    }

    /// <summary>Yedekteki dosyaları geri yazar, eklenen dosyaları siler.</summary>
    public async Task GeriAlAsync(YapayZekaKaydi kayit)
    {
        if (kayit.GeriAlindi) throw new IsKuraliHatasi("Bu değişiklik zaten geri alınmış.");
        var bilgi = JsonNode.Parse(kayit.GeriAlmaJson)!.AsObject();
        var yedek = Path.Combine(YedekKlasoru, bilgi["klasor"]!.ToString());
        if (!Directory.Exists(yedek)) throw new IsKuraliHatasi("Yedek klasörü bulunamadı: " + yedek);

        // GERI-AL.bat ile zaten geri alındıysa (geri-alindi.txt) dosyalara tekrar dokunma, sadece kaydı işaretle
        var isaret = Path.Combine(yedek, "geri-alindi.txt");
        if (File.Exists(isaret))
        {
            kayit.GeriAlindi = true;
            kayit.GeriAlmaTarihi = File.GetLastWriteTime(isaret);
            await _vt.SaveChangesAsync();
            return;
        }

        var veritabani = false;
        foreach (var yol in bilgi["degisenler"]!.AsArray().Select(d => d!.ToString()).Where(Izinli))
        {
            var hedef = Path.Combine(ProjeKlasoru, yol);
            File.Copy(Path.Combine(yedek, yol), hedef, overwrite: true);
            // Kopya eski tarihini taşır; derleyici "değişmemiş" sanıp yeniden derlemesin diye tarihi "şimdi" yapılır.
            File.SetLastWriteTime(hedef, DateTime.Now);
            veritabani |= yol.StartsWith("Modeller/") || yol.StartsWith("Veri/");
        }
        foreach (var yol in bilgi["yeniler"]!.AsArray().Select(d => d!.ToString()).Where(Izinli))
        {
            var tam = Path.Combine(ProjeKlasoru, yol);
            if (File.Exists(tam)) File.Delete(tam);
            veritabani |= yol.StartsWith("Modeller/") || yol.StartsWith("Veri/");
        }
        if (veritabani) await File.WriteAllTextAsync(Path.Combine(ProjeKlasoru, "veritabanini-yenile.isaret"), "Kod modu değişikliği geri alındı.");
        await File.WriteAllTextAsync(isaret, "Panelden geri alındı: " + DateTime.Now);   // GERI-AL.bat bunu tekrar geri almasın

        kayit.GeriAlindi = true;
        kayit.GeriAlmaTarihi = DateTime.Now;
        await _vt.SaveChangesAsync();
    }

    /// <summary>
    /// Uygulamayı 42 koduyla kapatır; BASLAT.bat bunu görünce projeyi yeniden derleyip açar.
    /// BASLAT.bat ile açılmadıysa false döner (kullanıcı uygulamayı kendisi kapatıp açmalı).
    /// </summary>
    public bool YenidenBaslat()
    {
        if (!BaslaticidanMi) return false;
        Environment.ExitCode = YenidenBaslatKodu;
        _ = Task.Run(async () => { await Task.Delay(500); _omur.StopApplication(); });   // önce cevap tarayıcıya gitsin
        return true;
    }
}
