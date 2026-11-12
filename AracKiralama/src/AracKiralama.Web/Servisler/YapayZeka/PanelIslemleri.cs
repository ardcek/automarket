// ============================================================================
// PANEL İŞLEMLERİ: Yapay Zekâ Asistanı'nın yapabileceği değişikliklerin listesi.
// Yapay zekâ kod yazmaz; kullanıcının isteğini bu listedeki işlemlere çevirir (JSON).
// Her işlem önce DOĞRULANIR (ekranda kart olarak gösterilir), kullanıcı onaylarsa
// UYGULANIR ve eski değerler saklanır; böylece GERİ ALINABİLİR.
//
// ► Asistana yeni bir yetenek eklemek (örn. "şube sil"):
//   1) Aşağıya PanelIslemi'nden türeyen yeni bir sınıf yazın (en benzer olanı kopyalayın):
//      Tur, Baslik, Aciklama (modele anlatılan), Parametreler (JSON şeması), Dogrula, Uygula, GeriAl
//   2) PanelIslemleri.Hepsi listesine ekleyin.
//   Yapay zekâya anlatım, JSON şeması ve ekrandaki kart kendiliğinden oluşur.
// ============================================================================
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

/// <summary>Bir işlemin ekrandaki kartı: ne yapılacağı, hatalar, düzenlenebilir değerler.</summary>
public class IslemOnizleme
{
    public string Tur { get; set; } = "";
    public string Baslik { get; set; } = "";        // "Kampanya ekle"
    public string Ikon { get; set; } = "bi-gear";
    public string Ozet { get; set; } = "";          // "\"Öğretmen indirimi\": %15 · Meslek: Öğretmen"
    public List<string> Hatalar { get; set; } = new();
    public JsonObject Parametreler { get; set; } = new();
    public string? Link { get; set; }               // uygulandıktan sonra bakılacak sayfa
    public bool Uygulanabilir { get; set; } = true; // cevap_ver / yapilamaz uygulanmaz
    public bool OnizlemeVar { get; set; }           // yazı / ayar değişikliği canlı önizlemede görülebilir

    /// <summary>Kart ekranda işaretli mi gelsin? Hatalı ya da "değişiklik yok" diyen kartlar işaretsiz gelir.</summary>
    public bool Secili => Uygulanabilir && Hatalar.Count == 0 && !Ozet.Contains("değişiklik yok");
}

/// <summary>
/// Doğrulama sırasında sitenin durumu + bu istekte eklenecek olanlar.
/// Örn. aynı istekte önce "Meslek" eklenip sonra kampanyada kullanılabilsin diye "planlananlar" tutulur.
/// </summary>
public class PlanBaglami
{
    public static readonly StringComparer Karsilastirici = StringComparer.Create(Bicim.Tr, ignoreCase: true);

    public required VeriTabani Vt { get; init; }
    public List<Kategori> Kategoriler { get; init; } = new();
    public List<Marka> Markalar { get; init; } = new();
    public List<AracOzelligi> Ozellikler { get; init; } = new();
    public List<MusteriAlani> MusteriAlanlari { get; init; } = new();
    public List<Kampanya> Kampanyalar { get; init; } = new();
    public List<Sayfa> Sayfalar { get; init; } = new();
    public List<Arac> Araclar { get; init; } = new();

    // Bu istekte eklenecekler (ad → türü ve seçenekleri)
    public Dictionary<string, (OzellikTuru Tur, List<string> Secenekler)> YeniMusteriAlanlari { get; } = new(Karsilastirici);
    public Dictionary<string, (OzellikTuru Tur, List<string> Secenekler)> YeniOzellikler { get; } = new(Karsilastirici);
    public HashSet<string> YeniKategoriler { get; } = new(Karsilastirici);
    public HashSet<string> YeniMarkalar { get; } = new(Karsilastirici);
    public HashSet<string> YeniKampanyalar { get; } = new(Karsilastirici);
    public HashSet<string> YeniSayfalar { get; } = new(Karsilastirici);
    public Dictionary<string, string> YeniYazilar { get; } = new(Karsilastirici);   // yeni yazı → hangi anahtara yazıldı

    public static async Task<PlanBaglami> OlusturAsync(VeriTabani vt) => new()
    {
        Vt = vt,
        Kategoriler = await vt.Kategoriler.AsNoTracking().ToListAsync(),
        Markalar = await vt.Markalar.AsNoTracking().ToListAsync(),
        Ozellikler = await vt.AracOzellikleri.AsNoTracking().ToListAsync(),
        MusteriAlanlari = await vt.MusteriAlanlari.AsNoTracking().ToListAsync(),
        Kampanyalar = await vt.Kampanyalar.AsNoTracking().ToListAsync(),
        Sayfalar = await vt.Sayfalar.AsNoTracking().ToListAsync(),
        Araclar = await vt.Araclar.Include(a => a.Kategori).Include(a => a.Marka).Where(a => a.Durum != AracDurumu.Pasif).AsNoTracking().ToListAsync(),
    };

    /// <summary>Müşteri bilgisinin türü ve seçenekleri (var olan ya da bu istekte eklenecek).</summary>
    public (OzellikTuru Tur, IEnumerable<string> Secenekler)? MusteriAlani(string ad)
    {
        var alan = PanelIslemi.AdIle(MusteriAlanlari, a => a.Ad, ad);
        if (alan != null) return (alan.Tur, alan.SecenekListesi.Concat(YeniMusteriAlanlari.GetValueOrDefault(ad).Secenekler ?? new()));
        return YeniMusteriAlanlari.TryGetValue(ad, out var yeni) ? (yeni.Tur, yeni.Secenekler) : null;
    }

    /// <summary>Araç özelliğinin türü ve seçenekleri (var olan ya da bu istekte eklenecek).</summary>
    public (OzellikTuru Tur, IEnumerable<string> Secenekler)? Ozellik(string ad)
    {
        var oz = PanelIslemi.AdIle(Ozellikler, o => o.Ad, ad);
        if (oz != null) return (oz.Tur, oz.SecenekListesi);
        return YeniOzellikler.TryGetValue(ad, out var yeni) ? (yeni.Tur, yeni.Secenekler) : null;
    }

    public bool KategoriVar(string ad) => PanelIslemi.AdIle(Kategoriler, k => k.Ad, ad) != null || YeniKategoriler.Contains(ad);
    public bool MarkaVar(string ad) => PanelIslemi.AdIle(Markalar, m => m.Ad, ad) != null || YeniMarkalar.Contains(ad);

    /// <summary>Kategori / marka / model koşuluna uyan araçlar.</summary>
    public List<Arac> Uyanlar(string? kategori, string? marka, string? modelIceren) => Araclar
        .Where(a => kategori == null || Karsilastirici.Equals(a.Kategori?.Ad, kategori))
        .Where(a => marka == null || Karsilastirici.Equals(a.Marka?.Ad, marka))
        .Where(a => modelIceren == null || a.TamAd.Contains(modelIceren, StringComparison.OrdinalIgnoreCase))
        .ToList();
}

/// <summary>Bütün işlemlerin atası. Yeni işlem = bu sınıftan türeyen yeni bir sınıf.</summary>
public abstract class PanelIslemi
{
    public abstract string Tur { get; }            // modelin yazdığı ad: "kampanya_ekle"
    public abstract string Baslik { get; }         // kartın başlığı: "Kampanya ekle"
    public virtual string Ikon => "bi-gear";
    public abstract string Aciklama { get; }       // modele anlatılan: ne işe yarar, ne zaman kullanılır
    public virtual bool Uygulanabilir => true;

    /// <summary>Parametrelerin JSON şeması (özellik adı → şema) ve zorunlu olanlar.</summary>
    public abstract (JsonObject Ozellikler, string[] Zorunlular) Parametreler();

    /// <summary>Kontrol eder; sorun varsa hatalar listesine yazar. Kartta görünecek özeti döndürür.</summary>
    public abstract Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar);

    /// <summary>Değişikliği yapar (SaveChanges dahil). Geri almak için gereken bilgiyi döndürür.</summary>
    public abstract Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt);

    /// <summary>UygulaAsync'in döndürdüğü bilgiyle değişikliği tersine çevirir.</summary>
    public abstract Task GeriAlAsync(JsonObject geri, VeriTabani vt);

    /// <summary>Uygulandıktan sonra kullanıcının bakacağı sayfa.</summary>
    public virtual string? Link(JsonObject p) => null;

    public virtual bool OnizlemeVar => false;

    // ---------------- Yardımcılar (alt sınıflar kullanır) ----------------

    /// <summary>Listede adı (Türkçe büyük-küçük harf farkı gözetmeden) eşleşen kayıt.</summary>
    public static T? AdIle<T>(IEnumerable<T> liste, Func<T, string> ad, string? aranan) where T : class
        => string.IsNullOrWhiteSpace(aranan) ? null : liste.FirstOrDefault(x => PlanBaglami.Karsilastirici.Equals(ad(x).Trim(), aranan.Trim()));

    /// <summary>Parametredeki yazı (boşsa null). Sayı ya da evet/hayır gelse de yazıya çevrilir.</summary>
    protected static string? Yazi(JsonObject p, string ad)
    {
        var deger = p[ad];
        if (deger == null) return null;
        var yazi = deger is JsonValue v && v.TryGetValue<string>(out var s) ? s : deger.ToJsonString().Trim('"');
        return string.IsNullOrWhiteSpace(yazi) || yazi == "null" ? null : yazi.Trim();
    }

    /// <summary>Parametredeki sayı ("15", "15,5" ya da 15 gelebilir).</summary>
    protected static decimal? Sayi(JsonObject p, string ad)
    {
        var yazi = Yazi(p, ad)?.Replace("%", "").Replace(',', '.');
        return decimal.TryParse(yazi, NumberStyles.Number, CultureInfo.InvariantCulture, out var sayi) ? sayi : null;
    }

    protected static bool Evet(JsonObject p, string ad, bool varsayilan)
        => Yazi(p, ad)?.ToLowerInvariant() switch { "true" or "evet" or "1" => true, "false" or "hayır" or "hayir" or "0" => false, _ => varsayilan };

    /// <summary>Parametredeki liste (JSON dizisi ya da satır / virgül ile ayrılmış yazı).</summary>
    protected static List<string> Liste(JsonObject p, string ad)
    {
        IEnumerable<string> parcalar = p[ad] is JsonArray dizi
            ? dizi.Select(x => x?.ToString() ?? "")
            : (Yazi(p, ad) ?? "").Split(['\n', ',', '|'], StringSplitOptions.RemoveEmptyEntries);
        return parcalar.Select(s => s.Trim().Replace("|", "/")).Where(s => s.Length > 0).Distinct(PlanBaglami.Karsilastirici).ToList();
    }

    protected static JsonObject Sema(string tur, string? aciklama = null, JsonArray? secenekler = null)
    {
        var sema = new JsonObject { ["type"] = tur };
        if (aciklama != null) sema["description"] = aciklama;
        if (secenekler != null) sema["enum"] = secenekler;
        return sema;
    }
    /// <summary>
    /// Evet/hayır parametresi ve varsayılanı. Model bu parametreyi yazmasa da kartta varsayılanıyla görünür,
    /// kullanıcı tek tıkla değiştirebilir (örn. "üst menüde göster").
    /// </summary>
    protected static JsonObject EvetHayir(bool varsayilan) => new() { ["type"] = "boolean", ["default"] = varsayilan };

    protected static JsonObject YaziListesi() => new() { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["maxItems"] = 20 };

    protected static OzellikTuru TurCevir(string? tur) => tur?.ToLowerInvariant() switch
    {
        "evet_hayir" => OzellikTuru.EvetHayir,
        "sayi" => OzellikTuru.Sayi,
        "yazi" => OzellikTuru.Yazi,
        _ => OzellikTuru.Secim,
    };
    protected static readonly JsonArray TurSecenekleri = ["secim", "evet_hayir", "sayi", "yazi"];

    /// <summary>"Sağlık Çalışanları" → "saglik-calisanlari" (sayfa adresi için).</summary>
    public static string AdresYap(string metin)
    {
        var kucuk = metin.ToLower(Bicim.Tr).Replace('ç', 'c').Replace('ğ', 'g').Replace('ı', 'i').Replace('ö', 'o').Replace('ş', 's').Replace('ü', 'u');
        return Regex.Replace(kucuk, "[^a-z0-9]+", "-").Trim('-');
    }

    protected static string Tirnak(string? metin, int uzunluk = 60)
        => metin == null ? "(varsayılan)" : "\"" + (metin.Length > uzunluk ? metin[..uzunluk] + "…" : metin) + "\"";

    protected static JsonObject Geri(params (string Ad, JsonNode? Deger)[] alanlar)
    {
        var json = new JsonObject();
        foreach (var (ad, deger) in alanlar) json[ad] = deger;
        return json;
    }
}

// ============================================================================
//                              İŞLEMLER
// ============================================================================

/// <summary>Sitedeki bir yazıyı değiştirir (Metinler tablosu).</summary>
public class MetinDegistir : PanelIslemi
{
    public override string Tur => "metin_degistir";
    public override string Baslik => "Yazıyı değiştir";
    public override string Ikon => "bi-fonts";
    public override bool OnizlemeVar => true;
    public override string Aciklama => "Sitedeki bir başlığı, butonu ya da açıklamayı değiştirir. anahtar: YAZILAR listesindeki anahtar. yeni_metin: yeni yazı. "
        + "Parçalı bir yazıyı değiştirirken her parçaya cümlenin FARKLI bir kısmını yaz (aynı cümleyi her parçaya yazma).";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["anahtar"] = Sema("string", null, new JsonArray(VarsayilanMetinler.Liste.Keys.Select(k => (JsonNode)k).ToArray())),
        ["yeni_metin"] = Sema("string"),
    }, ["anahtar", "yeni_metin"]);

    public override async Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var anahtar = Yazi(p, "anahtar") ?? "";
        var yeni = Yazi(p, "yeni_metin");
        if (!VarsayilanMetinler.Liste.ContainsKey(anahtar)) { hatalar.Add($"\"{anahtar}\" diye bir yazı yok."); return anahtar; }
        if (yeni == null) hatalar.Add("Yeni yazı boş olamaz.");
        // Parçalı yazılarda (büyük başlık) küçük modeller bazen aynı cümleyi her parçaya yazar → sitede cümle tekrarlanır
        else if (yeni.Length > 3 && b.YeniYazilar.TryGetValue(yeni, out var digerAnahtar) && digerAnahtar != anahtar)
            hatalar.Add($"Aynı yazı \"{digerAnahtar}\" için de yazılmış; sitede tekrar eder. Parçalı yazıda cümleyi parçalara bölün (örn. \"Yolculuk\" / \"burada\" / \"başlar.\").");
        else b.YeniYazilar[yeni] = anahtar;
        var eski = (await b.Vt.Metinler.FindAsync(anahtar))?.Deger ?? VarsayilanMetinler.Liste[anahtar];
        var (bolum, ad) = DuzenleyiciTanimlari.Aciklama(anahtar);
        return $"{bolum} › {ad}: {Tirnak(eski)} → {Tirnak(yeni)}";
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var anahtar = Yazi(p, "anahtar")!;
        var kayit = await vt.Metinler.FindAsync(anahtar);
        var geri = Geri(("anahtar", anahtar), ("eski", kayit?.Deger));   // eski null = varsayılan yazıydı
        if (kayit == null) vt.Metinler.Add(new Metin { Anahtar = anahtar, Deger = Yazi(p, "yeni_metin")! });   // INSERT
        else kayit.Deger = Yazi(p, "yeni_metin")!;                                                            // UPDATE
        await vt.SaveChangesAsync();
        return geri;
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var kayit = await vt.Metinler.FindAsync(Yazi(geri, "anahtar"));
        var eski = Yazi(geri, "eski");
        if (eski == null) { if (kayit != null) vt.Metinler.Remove(kayit); }
        else if (kayit == null) vt.Metinler.Add(new Metin { Anahtar = Yazi(geri, "anahtar")!, Deger = eski });
        else kayit.Deger = eski;
        await vt.SaveChangesAsync();
    }

    public override string? Link(JsonObject p) => (Yazi(p, "anahtar") ?? "").Split('.')[0] switch
    {
        "araclar" => "/Araclar",
        "hesap" => "/Hesap/Giris",
        _ => "/",
    };
}

/// <summary>
/// Ana sayfadaki büyük başlığı tek cümleyle değiştirir. Başlık sayfada üç parçadır:
/// anasayfa.baslik + (renkli) anasayfa.baslik-vurgu + anasayfa.baslik-son. Cümleyi parçalara bölmek
/// küçük modellere zor geldiği için bölme işini kod yapar.
/// </summary>
public class AnaBaslikDegistir : PanelIslemi
{
    private static readonly string[] Anahtarlar = ["anasayfa.baslik", "anasayfa.baslik-vurgu", "anasayfa.baslik-son"];

    public override string Tur => "ana_baslik_degistir";
    public override string Baslik => "Ana sayfa başlığını değiştir";
    public override string Ikon => "bi-type-h1";
    public override bool OnizlemeVar => true;
    public override string Aciklama => "Ana sayfadaki büyük başlığın TAMAMINI değiştirir. metin: yeni başlık cümlesi. renkli_kisim: cümlenin içinden renkli gösterilecek kelime(ler) "
        + "(boşsa ortadaki kelime renkli olur). Büyük başlık için metin_degistir kullanma.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject { ["metin"] = Sema("string"), ["renkli_kisim"] = Sema("string") }, ["metin"]);

    /// <summary>"Yolculuk burada başlar" → ("Yolculuk", "burada", "başlar"). Renkli kısım verilirse onun etrafından bölünür.</summary>
    public static Dictionary<string, string> Parcalar(JsonObject p)
    {
        var metin = Yazi(p, "metin") ?? "";
        var renkli = Yazi(p, "renkli_kisim");
        string bas, vurgu, son;
        var konum = renkli == null ? -1 : metin.IndexOf(renkli, StringComparison.CurrentCultureIgnoreCase);
        if (konum >= 0)
            (bas, vurgu, son) = (metin[..konum].Trim(), metin.Substring(konum, renkli!.Length), metin[(konum + renkli.Length)..].Trim());
        else
        {
            var kelimeler = metin.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var orta = kelimeler.Length / 2;   // 3 kelime → ortadaki, 2 kelime → ikincisi, 1 kelime → kendisi
            (bas, vurgu, son) = kelimeler.Length == 0 ? ("", "", "")
                : (string.Join(' ', kelimeler[..orta]), kelimeler[orta], string.Join(' ', kelimeler[(orta + 1)..]));
        }
        return new() { [Anahtarlar[0]] = bas, [Anahtarlar[1]] = vurgu, [Anahtarlar[2]] = son };
    }

    public override async Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        if (Yazi(p, "metin") == null) { hatalar.Add("Başlık boş olamaz."); return ""; }
        if (Yazi(p, "renkli_kisim") is string renkli && !Yazi(p, "metin")!.Contains(renkli, StringComparison.CurrentCultureIgnoreCase))
            hatalar.Add($"Renkli kısım \"{renkli}\" başlığın içinde geçmiyor.");
        var eskiler = new List<string>();
        foreach (var anahtar in Anahtarlar) eskiler.Add((await b.Vt.Metinler.FindAsync(anahtar))?.Deger ?? VarsayilanMetinler.Liste[anahtar]);
        var yeni = Parcalar(p);
        return $"\"{eskiler[0]} [{eskiler[1]}] {eskiler[2]}\" → \"{yeni[Anahtarlar[0]]} [{yeni[Anahtarlar[1]]}] {yeni[Anahtarlar[2]]}\" (köşeli parantez = renkli kısım)";
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var geri = new JsonObject();
        foreach (var (anahtar, yeni) in Parcalar(p))
        {
            var kayit = await vt.Metinler.FindAsync(anahtar);
            geri[anahtar] = kayit?.Deger;   // null = varsayılan yazıydı
            if (kayit == null) vt.Metinler.Add(new Metin { Anahtar = anahtar, Deger = yeni });
            else kayit.Deger = yeni;
        }
        await vt.SaveChangesAsync();
        return geri;
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        foreach (var anahtar in Anahtarlar)
        {
            var kayit = await vt.Metinler.FindAsync(anahtar);
            var eski = geri[anahtar]?.GetValue<string>();
            if (eski == null) { if (kayit != null) vt.Metinler.Remove(kayit); }
            else if (kayit == null) vt.Metinler.Add(new Metin { Anahtar = anahtar, Deger = eski });
            else kayit.Deger = eski;
        }
        await vt.SaveChangesAsync();
    }

    public override string? Link(JsonObject p) => "/";
}

/// <summary>Bir site ayarını değiştirir (SiteAyarlari tablosu, [Ayar] etiketli özellikler).</summary>
public class AyarDegistir : PanelIslemi
{
    public override string Tur => "ayar_degistir";
    public override string Baslik => "Ayarı değiştir";
    public override string Ikon => "bi-sliders";
    public override bool OnizlemeVar => true;
    public override string Aciklama => "Bir site ayarını değiştirir (renk, filtreleri aç/kapa, fiyat oranları...). ayar: AYARLAR listesindeki ad. deger: yeni değer; aç/kapa için true/false, oranlar için 0.15 gibi, renk için #rrggbb.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ayar"] = Sema("string", null, new JsonArray(Ayarlar().Select(a => (JsonNode)a.Name).ToArray())),
        ["deger"] = Sema("string"),
    }, ["ayar", "deger"]);

    public static IEnumerable<System.Reflection.PropertyInfo> Ayarlar()
        => typeof(SiteAyarlari).GetProperties().Where(o => AyarCevirici.AyarOzelligi(o.Name) != null);

    public override async Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ad = Yazi(p, "ayar") ?? "";
        var ozellik = AyarCevirici.AyarOzelligi(ad);
        if (ozellik == null) { hatalar.Add($"\"{ad}\" diye bir ayar yok."); return ad; }
        var ayarlar = await b.Vt.SiteAyarlari.AsNoTracking().FirstAsync();
        var deneme = AyarCevirici.Kopyala(ayarlar);
        if (!AyarCevirici.Uygula(deneme, ad, Yazi(p, "deger"))) hatalar.Add($"\"{Yazi(p, "deger")}\" bu ayar için geçerli bir değer değil.");
        var bilgi = (AyarAttribute)ozellik.GetCustomAttributes(typeof(AyarAttribute), false)[0];
        var (eski, yeni) = (Goster(ozellik.GetValue(ayarlar)), Goster(ozellik.GetValue(deneme)));
        return eski == yeni ? $"{bilgi.Grup} › {bilgi.Ad}: zaten {eski} (değişiklik yok)" : $"{bilgi.Grup} › {bilgi.Ad}: {eski} → {yeni}";
    }

    private static string Goster(object? deger) => deger switch { true => "Açık", false => "Kapalı", null => "(boş)", _ => Tirnak(AyarCevirici.Yazi(deger)) };

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var ad = Yazi(p, "ayar")!;
        var ayarlar = await vt.SiteAyarlari.FirstAsync();
        var geri = Geri(("ayar", ad), ("eski", AyarCevirici.Yazi(AyarCevirici.AyarOzelligi(ad)!.GetValue(ayarlar))));
        AyarCevirici.Uygula(ayarlar, ad, Yazi(p, "deger"));   // UPDATE "SiteAyarlari" SET ...
        await vt.SaveChangesAsync();
        return geri;
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var ayarlar = await vt.SiteAyarlari.FirstAsync();
        AyarCevirici.Uygula(ayarlar, Yazi(geri, "ayar")!, Yazi(geri, "eski") ?? "");
        await vt.SaveChangesAsync();
    }

    public override string? Link(JsonObject p)
    {
        var grup = (AyarCevirici.AyarOzelligi(Yazi(p, "ayar") ?? "")?.GetCustomAttributes(typeof(AyarAttribute), false).FirstOrDefault() as AyarAttribute)?.Grup;
        return grup switch { "Araç Listesi" => "/Araclar", "Sunum" => "/Hesap/Giris", _ => "/" };
    }
}

/// <summary>Müşterilere sorulacak yeni bir bilgi ekler (örn. Meslek). Zaten varsa eksik seçenekleri ekler.</summary>
public class MusteriBilgisiEkle : PanelIslemi
{
    public override string Tur => "musteri_bilgisi_ekle";
    public override string Baslik => "Müşteri bilgisi ekle";
    public override string Ikon => "bi-person-vcard";
    public override string Aciklama => "Müşterilere sorulacak yeni bir bilgi ekler (kayıt formunda ve profilde sorulur), örn. Meslek, Öğrenci mi. Kampanyalarda koşul olarak kullanılabilir. bilgi_turu: secim (seçenekli liste), evet_hayir, sayi, yazi.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ad"] = Sema("string"),
        ["bilgi_turu"] = Sema("string", null, TurSecenekleri.DeepClone().AsArray()),
        ["secenekler"] = YaziListesi(),
        ["kayitta_sor"] = EvetHayir(true),
        ["zorunlu"] = EvetHayir(false),
    }, ["ad", "bilgi_turu", "secenekler"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ad = Yazi(p, "ad");
        var tur = TurCevir(Yazi(p, "bilgi_turu"));
        var secenekler = Liste(p, "secenekler");
        if (ad == null) { hatalar.Add("Bilginin adı boş olamaz."); return Task.FromResult(""); }
        if (tur == OzellikTuru.Secim && secenekler.Count == 0) hatalar.Add("Seçim listesi için en az bir seçenek gerekli.");
        // Araca ait koşullar müşteri bilgisi değildir (küçük modeller "SUV'lara indirim" için "Kategori" sorusu eklemeye kalkabiliyor)
        if (AracaAitAdlar.Any(a => PlanBaglami.Karsilastirici.Equals(a, ad)))
            hatalar.Add($"\"{ad}\" araca ait bir bilgi, müşteriye sorulmaz. Kampanyada kategori / marka / en_az_gun alanlarını kullanın; bu kartı çıkarın.");

        var mevcut = AdIle(b.MusteriAlanlari, a => a.Ad, ad);
        if (mevcut != null)
        {
            // Zaten var: yeniden eklenmez, sadece listede olmayan seçenekler eklenir
            var eksikler = secenekler.Where(s => !mevcut.SecenekListesi.Contains(s, PlanBaglami.Karsilastirici)).ToList();
            b.YeniMusteriAlanlari[ad] = (mevcut.Tur, eksikler);
            return Task.FromResult(eksikler.Count == 0 ? $"\"{mevcut.Ad}\" zaten var; değişiklik yok." : $"\"{mevcut.Ad}\" zaten var; eklenecek seçenekler: {string.Join(", ", eksikler)}");
        }
        b.YeniMusteriAlanlari[ad] = (tur, tur == OzellikTuru.Secim ? secenekler : new());
        return Task.FromResult($"\"{ad}\" ({tur.Ad()}){(tur == OzellikTuru.Secim ? ": " + string.Join(", ", secenekler) : "")}{(Evet(p, "kayitta_sor", true) ? " · kayıt formunda sorulur" : "")}");
    }

    private static readonly string[] AracaAitAdlar = ["Kategori", "Araç kategorisi", "Araç sınıfı", "Marka", "Araç markası", "Gün", "Gün sayısı", "Kiralama süresi", "Süre", "Tarih", "Araç"];

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var ad = Yazi(p, "ad")!;
        var mevcut = AdIle(await vt.MusteriAlanlari.ToListAsync(), a => a.Ad, ad);
        if (mevcut != null)
        {
            // Zaten var: sadece eksik seçenekleri ekle
            var geri = Geri(("guncellenenId", mevcut.Id), ("eskiSecenekler", mevcut.Secenekler));
            var yeniler = mevcut.SecenekListesi.Concat(Liste(p, "secenekler")).Distinct(PlanBaglami.Karsilastirici);
            mevcut.Secenekler = mevcut.Tur == OzellikTuru.Secim ? string.Join("|", yeniler) : mevcut.Secenekler;
            await vt.SaveChangesAsync();
            return geri;
        }
        var tur = TurCevir(Yazi(p, "bilgi_turu"));
        var alan = new MusteriAlani
        {
            Ad = ad, Tur = tur, Secenekler = tur == OzellikTuru.Secim ? string.Join("|", Liste(p, "secenekler")) : null,
            KayittaGoster = Evet(p, "kayitta_sor", true), Zorunlu = Evet(p, "zorunlu", false),
        };
        vt.MusteriAlanlari.Add(alan);   // INSERT INTO "MusteriAlanlari"
        await vt.SaveChangesAsync();
        return Geri(("eklenenId", alan.Id));
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        if (geri["eklenenId"] is JsonNode eklenen)
        {
            var id = eklenen.GetValue<int>();
            if (await vt.Kampanyalar.AnyAsync(k => k.MusteriAlanId == id))
                throw new IsKuraliHatasi("Bu müşteri bilgisi başka bir kampanyada kullanılıyor; önce o kampanyayı silin ya da o kaydı geri alın.");
            await vt.MusteriAlanDegerleri.Where(d => d.AlanId == id).ExecuteDeleteAsync();
            await vt.MusteriAlanlari.Where(a => a.Id == id).ExecuteDeleteAsync();
        }
        else if (geri["guncellenenId"] is JsonNode guncellenen)
        {
            var alan = await vt.MusteriAlanlari.FindAsync(guncellenen.GetValue<int>());
            if (alan != null) { alan.Secenekler = Yazi(geri, "eskiSecenekler"); await vt.SaveChangesAsync(); }
        }
    }

    public override string? Link(JsonObject p) => "/Yonetim/MusteriAlanlari";
}

/// <summary>Yeni bir kampanya (indirim kuralı) ekler.</summary>
public class KampanyaEkle : PanelIslemi
{
    public override string Tur => "kampanya_ekle";
    public override string Baslik => "Kampanya ekle";
    public override string Ikon => "bi-percent";
    public override string Aciklama => "İndirim kampanyası ekler. indirim_yuzdesi: 15 = %15. Koşullar isteğe bağlıdır (boş = herkese / bütün araçlara): "
        + "musteri_bilgisi + musteri_degeri (örn. Meslek + Öğretmen), kategori, marka, arac_ozelligi + arac_ozellik_degeri, en_az_gun, baslangic/bitis (yyyy-aa-gg). "
        + "rozet: araç kartında görünen kısa yazı (örn. \"Öğretmenlere %15\"). Her meslek / grup için ayrı kampanya eklenir.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ad"] = Sema("string"),
        ["indirim_yuzdesi"] = Sema("number"),
        ["rozet"] = Sema("string"),
        ["musteri_bilgisi"] = Sema("string"),
        ["musteri_degeri"] = Sema("string"),
        ["kategori"] = Sema("string"),
        ["marka"] = Sema("string"),
        ["arac_ozelligi"] = Sema("string"),
        ["arac_ozellik_degeri"] = Sema("string"),
        ["en_az_gun"] = Sema("integer"),
        ["baslangic"] = Sema("string"),
        ["bitis"] = Sema("string"),
    }, ["ad", "indirim_yuzdesi"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ad = Yazi(p, "ad");
        var yuzde = Sayi(p, "indirim_yuzdesi");
        if (ad == null) hatalar.Add("Kampanya adı boş olamaz.");
        else if (AdIle(b.Kampanyalar, k => k.Ad, ad) != null || b.YeniKampanyalar.Contains(ad)) hatalar.Add($"\"{ad}\" adında bir kampanya zaten var.");
        else b.YeniKampanyalar.Add(ad);
        if (yuzde is null or < 1 or > 90) hatalar.Add("İndirim %1 ile %90 arasında olmalı.");

        var kosullar = new List<string>();
        var bilgi = Yazi(p, "musteri_bilgisi");
        var deger = Yazi(p, "musteri_degeri");
        if (bilgi != null || deger != null)
        {
            var alan = bilgi == null ? null : b.MusteriAlani(bilgi);
            if (alan == null) hatalar.Add($"\"{bilgi}\" diye bir müşteri bilgisi yok (önce musteri_bilgisi_ekle ile eklenmeli).");
            else if (deger == null) hatalar.Add($"\"{bilgi}\" için hangi değer? (örn. Öğretmen)");
            else if (alan.Value.Tur == OzellikTuru.EvetHayir && deger.ToLower(Bicim.Tr) is not ("evet" or "true" or "var"))
                hatalar.Add($"\"{bilgi}\" bir Evet/Hayır bilgisi; kampanya koşulu sadece \"Evet\" olabilir.");
            else if (alan.Value.Tur == OzellikTuru.Secim && !alan.Value.Secenekler.Contains(deger, PlanBaglami.Karsilastirici))
                hatalar.Add($"\"{deger}\", \"{bilgi}\" seçenekleri arasında yok ({string.Join(", ", alan.Value.Secenekler)}).");
            kosullar.Add($"{bilgi}: {deger}");
        }
        if (Yazi(p, "kategori") is string kategori) { if (!b.KategoriVar(kategori)) hatalar.Add($"\"{kategori}\" diye bir kategori yok."); kosullar.Add($"Kategori: {kategori}"); }
        if (Yazi(p, "marka") is string marka) { if (!b.MarkaVar(marka)) hatalar.Add($"\"{marka}\" diye bir marka yok."); kosullar.Add($"Marka: {marka}"); }
        if (Yazi(p, "arac_ozelligi") is string ozellik)
        {
            var oz = b.Ozellik(ozellik);
            var ozDeger = Yazi(p, "arac_ozellik_degeri");
            if (oz == null) hatalar.Add($"\"{ozellik}\" diye bir araç özelliği yok.");
            else if (ozDeger == null) hatalar.Add($"\"{ozellik}\" için hangi değer?");
            kosullar.Add($"{ozellik}: {ozDeger}");
        }
        if (Sayi(p, "en_az_gun") is decimal gun) { if (gun is < 1 or > 365) hatalar.Add("En az gün 1-365 arası olmalı."); kosullar.Add($"{gun:0}+ gün"); }
        foreach (var alanAdi in new[] { "baslangic", "bitis" })
            if (Yazi(p, alanAdi) is string t && !DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) hatalar.Add($"\"{t}\" geçerli bir tarih değil (yyyy-aa-gg).");
        if (Yazi(p, "baslangic") != null || Yazi(p, "bitis") != null) kosullar.Add($"{Yazi(p, "baslangic") ?? "…"} – {Yazi(p, "bitis") ?? "…"}");

        return Task.FromResult($"\"{ad}\": %{yuzde:0.#} · {(kosullar.Count == 0 ? "herkese, bütün araçlarda" : string.Join(" · ", kosullar))}");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var kampanya = new Kampanya
        {
            Ad = Yazi(p, "ad")!,
            IndirimOrani = Math.Round(Sayi(p, "indirim_yuzdesi")!.Value / 100m, 4),
            RozetYazisi = Yazi(p, "rozet"),
            MusteriAlanId = AdIle(await vt.MusteriAlanlari.ToListAsync(), a => a.Ad, Yazi(p, "musteri_bilgisi"))?.Id,
            KategoriId = AdIle(await vt.Kategoriler.ToListAsync(), k => k.Ad, Yazi(p, "kategori"))?.Id,
            MarkaId = AdIle(await vt.Markalar.ToListAsync(), m => m.Ad, Yazi(p, "marka"))?.Id,
            AracOzellikId = AdIle(await vt.AracOzellikleri.ToListAsync(), o => o.Ad, Yazi(p, "arac_ozelligi"))?.Id,
            EnAzGun = Sayi(p, "en_az_gun") is decimal g ? (int)g : null,
            BaslangicTarihi = DateTime.TryParse(Yazi(p, "baslangic"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var bas) ? bas.Date : null,
            BitisTarihi = DateTime.TryParse(Yazi(p, "bitis"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var bit) ? bit.Date : null,
        };
        // Seçenekteki yazılışı kullan ("öğretmen" yazıldıysa listedeki "Öğretmen")
        if (kampanya.MusteriAlanId != null)
        {
            var alan = await vt.MusteriAlanlari.FindAsync(kampanya.MusteriAlanId);
            var deger = Yazi(p, "musteri_degeri")!;
            kampanya.MusteriDegeri = alan!.Tur == OzellikTuru.EvetHayir ? "true" : alan.SecenekListesi.FirstOrDefault(s => PlanBaglami.Karsilastirici.Equals(s, deger)) ?? deger;
        }
        if (kampanya.AracOzellikId != null)
        {
            var oz = await vt.AracOzellikleri.FindAsync(kampanya.AracOzellikId);
            var deger = Yazi(p, "arac_ozellik_degeri") ?? "";
            kampanya.AracOzellikDegeri = oz!.Tur == OzellikTuru.EvetHayir ? "true" : oz.SecenekListesi.FirstOrDefault(s => PlanBaglami.Karsilastirici.Equals(s, deger)) ?? deger;
        }
        vt.Kampanyalar.Add(kampanya);   // INSERT INTO "Kampanyalar"
        await vt.SaveChangesAsync();
        return Geri(("eklenenId", kampanya.Id));
    }

    public override Task GeriAlAsync(JsonObject geri, VeriTabani vt)
        => vt.Kampanyalar.Where(k => k.Id == geri["eklenenId"]!.GetValue<int>()).ExecuteDeleteAsync();

    public override string? Link(JsonObject p) => "/Araclar?SadeceIndirimli=true";
}

/// <summary>Var olan bir kampanyanın oranını, rozetini ya da yayın durumunu değiştirir.</summary>
public class KampanyaGuncelle : PanelIslemi
{
    public override string Tur => "kampanya_guncelle";
    public override string Baslik => "Kampanyayı güncelle";
    public override string Ikon => "bi-pencil-square";
    public override string Aciklama => "Var olan bir kampanyanın indirim oranını, rozet yazısını ya da yayında olup olmadığını değiştirir (örn. \"öğretmen indirimini %20 yap\"). ad: KAMPANYALAR listesindeki ad. Sadece değişecek alanları yaz.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ad"] = Sema("string"), ["indirim_yuzdesi"] = Sema("number"), ["rozet"] = Sema("string"), ["yayinda"] = Sema("boolean"),
    }, ["ad"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var kampanya = AdIle(b.Kampanyalar, k => k.Ad, Yazi(p, "ad"));
        if (kampanya == null) { hatalar.Add($"\"{Yazi(p, "ad")}\" diye bir kampanya yok."); return Task.FromResult(""); }
        var degisenler = new List<string>();
        if (Sayi(p, "indirim_yuzdesi") is decimal yuzde)
        {
            if (yuzde is < 1 or > 90) hatalar.Add("İndirim %1 ile %90 arasında olmalı.");
            degisenler.Add($"indirim {kampanya.OranYazisi} → %{yuzde:0.#}");
        }
        if (Yazi(p, "rozet") is string rozet) degisenler.Add($"rozet → \"{rozet}\"");
        if (p["yayinda"] != null) degisenler.Add(Evet(p, "yayinda", true) ? "yayına alınacak" : "yayından kaldırılacak");
        if (degisenler.Count == 0) hatalar.Add("Değişecek bir şey yazılmamış (indirim, rozet ya da yayında).");
        return Task.FromResult($"\"{kampanya.Ad}\": {string.Join(", ", degisenler)}");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var kampanya = AdIle(await vt.Kampanyalar.ToListAsync(), k => k.Ad, Yazi(p, "ad"))!;
        var geri = Geri(("id", kampanya.Id), ("oran", kampanya.IndirimOrani), ("rozet", kampanya.RozetYazisi), ("aktif", kampanya.Aktif));
        if (Sayi(p, "indirim_yuzdesi") is decimal yuzde) kampanya.IndirimOrani = Math.Round(yuzde / 100m, 4);
        if (Yazi(p, "rozet") is string rozet) kampanya.RozetYazisi = rozet;
        if (p["yayinda"] != null) kampanya.Aktif = Evet(p, "yayinda", true);
        await vt.SaveChangesAsync();   // UPDATE "Kampanyalar" SET ...
        return geri;
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var kampanya = await vt.Kampanyalar.FindAsync(geri["id"]!.GetValue<int>());
        if (kampanya == null) return;
        kampanya.IndirimOrani = geri["oran"]!.GetValue<decimal>();
        kampanya.RozetYazisi = Yazi(geri, "rozet");
        kampanya.Aktif = geri["aktif"]!.GetValue<bool>();
        await vt.SaveChangesAsync();
    }

    public override string? Link(JsonObject p) => "/Yonetim/Kampanyalar";
}

/// <summary>Bir kampanyayı yayından kaldırır (silmez).</summary>
public class KampanyaKapat : PanelIslemi
{
    public override string Tur => "kampanya_kapat";
    public override string Baslik => "Kampanyayı kapat";
    public override string Ikon => "bi-pause-circle";
    public override string Aciklama => "Var olan bir kampanyayı yayından kaldırır (silmez). ad: KAMPANYALAR listesindeki ad.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject { ["ad"] = Sema("string") }, ["ad"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        if (AdIle(b.Kampanyalar, k => k.Ad, Yazi(p, "ad")) == null) hatalar.Add($"\"{Yazi(p, "ad")}\" diye bir kampanya yok.");
        return Task.FromResult($"\"{Yazi(p, "ad")}\" yayından kaldırılacak.");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var kampanya = AdIle(await vt.Kampanyalar.ToListAsync(), k => k.Ad, Yazi(p, "ad"))!;
        var geri = Geri(("id", kampanya.Id), ("eskiAktif", kampanya.Aktif));
        kampanya.Aktif = false;
        await vt.SaveChangesAsync();
        return geri;
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var kampanya = await vt.Kampanyalar.FindAsync(geri["id"]!.GetValue<int>());
        if (kampanya != null) { kampanya.Aktif = geri["eskiAktif"]!.GetValue<bool>(); await vt.SaveChangesAsync(); }
    }

    public override string? Link(JsonObject p) => "/Yonetim/Kampanyalar";
}

/// <summary>Araçlara yeni bir özellik / filtre ekler (Renk, Klima, Bagaj hacmi...).</summary>
public class AracOzelligiEkle : PanelIslemi
{
    public override string Tur => "arac_ozelligi_ekle";
    public override string Baslik => "Araç özelliği / filtre ekle";
    public override string Ikon => "bi-funnel";
    public override string Aciklama => "Araçlara yeni bir bilgi ekler; araç listesinde filtre olur. ozellik_turu: secim (seçenekli), evet_hayir (var/yok, örn. Klima), sayi (örn. Bagaj hacmi; birim: lt), yazi. "
        + "Araçlara değer vermek için ardından arac_ozellik_degeri_ver kullanılır.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ad"] = Sema("string"),
        ["ozellik_turu"] = Sema("string", null, TurSecenekleri.DeepClone().AsArray()),
        ["secenekler"] = YaziListesi(),
        ["birim"] = Sema("string"),
        ["filtrede_goster"] = EvetHayir(true),
        ["kartta_goster"] = EvetHayir(false),
        ["ikon"] = Sema("string", "Bootstrap ikon adı, örn. bi-snow, bi-palette, bi-suitcase"),
    }, ["ad", "ozellik_turu"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ad = Yazi(p, "ad");
        var tur = TurCevir(Yazi(p, "ozellik_turu"));
        var secenekler = Liste(p, "secenekler");
        if (ad == null) { hatalar.Add("Özellik adı boş olamaz."); return Task.FromResult(""); }
        if (AdIle(b.Ozellikler, o => o.Ad, ad) != null || b.YeniOzellikler.ContainsKey(ad)) hatalar.Add($"\"{ad}\" adında bir özellik zaten var.");
        if (tur == OzellikTuru.Secim && secenekler.Count == 0) hatalar.Add("Seçim listesi için en az bir seçenek gerekli.");
        b.YeniOzellikler[ad] = (tur, secenekler);
        return Task.FromResult($"\"{ad}\" ({tur.Ad()}){(secenekler.Count > 0 ? ": " + string.Join(", ", secenekler) : "")}"
            + $"{(Evet(p, "filtrede_goster", true) ? " · filtrede" : "")}{(Evet(p, "kartta_goster", false) ? " · kartta" : "")} · detayda");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var tur = TurCevir(Yazi(p, "ozellik_turu"));
        var ozellik = new AracOzelligi
        {
            Ad = Yazi(p, "ad")!, Tur = tur,
            Secenekler = tur == OzellikTuru.Secim ? string.Join("|", Liste(p, "secenekler")) : null,
            Birim = tur == OzellikTuru.Sayi ? Yazi(p, "birim") : null,
            Ikon = Yazi(p, "ikon") is string ikon && ikon.StartsWith("bi-") ? ikon : "bi-tag",
            FiltredeGoster = Evet(p, "filtrede_goster", true), KarttaGoster = Evet(p, "kartta_goster", false), DetaydaGoster = true,
        };
        vt.AracOzellikleri.Add(ozellik);   // INSERT INTO "AracOzellikleri"
        await vt.SaveChangesAsync();
        return Geri(("eklenenId", ozellik.Id));
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var id = geri["eklenenId"]!.GetValue<int>();
        if (await vt.Kampanyalar.AnyAsync(k => k.AracOzellikId == id))
            throw new IsKuraliHatasi("Bu özellik bir kampanyada kullanılıyor; önce o kampanyayı silin.");
        await vt.AracOzellikDegerleri.Where(d => d.OzellikId == id).ExecuteDeleteAsync();
        await vt.AracOzellikleri.Where(o => o.Id == id).ExecuteDeleteAsync();
    }

    public override string? Link(JsonObject p) => "/Araclar";
}

/// <summary>Bir özelliğe, koşula uyan araçlarda değer verir (örn. bütün SUV'larda Klima = Var).</summary>
public class AracOzellikDegeriVer : PanelIslemi
{
    public override string Tur => "arac_ozellik_degeri_ver";
    public override string Baslik => "Araçlara özellik değeri ver";
    public override string Ikon => "bi-table";
    public override string Aciklama => "Bir araç özelliğine, koşula uyan araçlarda değer verir. kategori / marka / model_iceren boşsa BÜTÜN araçlara verilir. "
        + "deger: seçim listesinde seçeneklerden biri, evet_hayir için true/false, sayı için rakam.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ozellik"] = Sema("string"),
        ["deger"] = Sema("string"),
        ["kategori"] = Sema("string"),
        ["marka"] = Sema("string"),
        ["model_iceren"] = Sema("string", "araç adında geçen kelime, örn. Clio"),
    }, ["ozellik", "deger"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ad = Yazi(p, "ozellik") ?? "";
        var deger = Yazi(p, "deger");
        var oz = b.Ozellik(ad);
        if (oz == null) hatalar.Add($"\"{ad}\" diye bir araç özelliği yok.");
        else if (oz.Value.Tur == OzellikTuru.Secim && !oz.Value.Secenekler.Contains(deger ?? "", PlanBaglami.Karsilastirici))
            hatalar.Add($"\"{deger}\", \"{ad}\" seçenekleri arasında yok.");
        else if (AracOzellikDegeri.Temizle(new AracOzelligi { Tur = oz.Value.Tur }, deger) == null && !(oz.Value.Tur == OzellikTuru.EvetHayir && deger == "false"))
            hatalar.Add($"\"{deger}\" bu özellik için geçerli değil.");
        if (Yazi(p, "kategori") is string k && !b.KategoriVar(k)) hatalar.Add($"\"{k}\" diye bir kategori yok.");
        if (Yazi(p, "marka") is string m && !b.MarkaVar(m)) hatalar.Add($"\"{m}\" diye bir marka yok.");
        var araclar = b.Uyanlar(Yazi(p, "kategori"), Yazi(p, "marka"), Yazi(p, "model_iceren"));
        if (araclar.Count == 0 && hatalar.Count == 0) hatalar.Add("Koşula uyan araç yok.");
        var kimler = string.Join(" · ", new[] { Yazi(p, "kategori"), Yazi(p, "marka"), Yazi(p, "model_iceren") }.Where(x => x != null));
        return Task.FromResult($"{(kimler.Length == 0 ? "Bütün" : kimler)} araçlar ({araclar.Count} araç): {ad} = {(deger == "true" ? "Var" : deger == "false" ? "Yok" : deger)}");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var ozellik = AdIle(await vt.AracOzellikleri.ToListAsync(), o => o.Ad, Yazi(p, "ozellik"))!;
        var ham = Yazi(p, "deger");
        if (ozellik.Tur == OzellikTuru.Secim) ham = ozellik.SecenekListesi.FirstOrDefault(s => PlanBaglami.Karsilastirici.Equals(s, ham)) ?? ham;
        var temiz = AracOzellikDegeri.Temizle(ozellik, ham);   // "false" (Yok) → null → değer silinir

        var baglam = new PlanBaglami { Vt = vt, Araclar = await vt.Araclar.Include(a => a.Kategori).Include(a => a.Marka).Where(a => a.Durum != AracDurumu.Pasif).ToListAsync() };
        var araclar = baglam.Uyanlar(Yazi(p, "kategori"), Yazi(p, "marka"), Yazi(p, "model_iceren"));
        var mevcut = await vt.AracOzellikDegerleri.Where(d => d.OzellikId == ozellik.Id).ToDictionaryAsync(d => d.AracId);
        var eskiler = new JsonArray();
        foreach (var arac in araclar)
        {
            mevcut.TryGetValue(arac.Id, out var kayit);
            eskiler.Add(new JsonObject { ["aracId"] = arac.Id, ["eski"] = kayit?.Deger });
            if (temiz == null) { if (kayit != null) vt.AracOzellikDegerleri.Remove(kayit); continue; }
            if (kayit == null) { kayit = new AracOzellikDegeri { AracId = arac.Id, OzellikId = ozellik.Id }; vt.AracOzellikDegerleri.Add(kayit); }
            kayit.Deger = temiz;
            kayit.SayiDeger = ozellik.Tur == OzellikTuru.Sayi ? decimal.Parse(temiz, CultureInfo.InvariantCulture) : null;
        }
        await vt.SaveChangesAsync();
        return Geri(("ozellikId", ozellik.Id), ("araclar", eskiler));
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var ozellikId = geri["ozellikId"]!.GetValue<int>();
        var ozellik = await vt.AracOzellikleri.FindAsync(ozellikId);
        if (ozellik == null) return;   // özellik zaten silinmiş
        var mevcut = await vt.AracOzellikDegerleri.Where(d => d.OzellikId == ozellikId).ToDictionaryAsync(d => d.AracId);
        foreach (var satir in geri["araclar"]!.AsArray())
        {
            var aracId = satir!["aracId"]!.GetValue<int>();
            var eski = satir["eski"]?.GetValue<string>();
            mevcut.TryGetValue(aracId, out var kayit);
            if (eski == null) { if (kayit != null) vt.AracOzellikDegerleri.Remove(kayit); continue; }
            if (kayit == null) { kayit = new AracOzellikDegeri { AracId = aracId, OzellikId = ozellikId }; vt.AracOzellikDegerleri.Add(kayit); }
            kayit.Deger = eski;
            kayit.SayiDeger = ozellik.Tur == OzellikTuru.Sayi ? decimal.Parse(eski, CultureInfo.InvariantCulture) : null;
        }
        await vt.SaveChangesAsync();
    }

    public override string? Link(JsonObject p) => "/Araclar";
}

/// <summary>Koşula uyan araçların günlük fiyatını yüzde olarak değiştirir.</summary>
public class AracFiyatiDegistir : PanelIslemi
{
    public override string Tur => "arac_fiyati_degistir";
    public override string Baslik => "Araç fiyatlarını değiştir";
    public override string Ikon => "bi-cash-coin";
    public override string Aciklama => "Araçların günlük fiyatını yüzde olarak artırır ya da azaltır. yuzde: 10 = %10 zam, -10 = %10 ucuzlatma. kategori / marka boşsa bütün araçlar. "
        + "Müşteri grubuna indirim için bunu değil kampanya_ekle'yi kullan.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["yuzde"] = Sema("number"),
        ["kategori"] = Sema("string"),
        ["marka"] = Sema("string"),
    }, ["yuzde"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var yuzde = Sayi(p, "yuzde");
        if (yuzde is null or 0 or < -90 or > 300) hatalar.Add("Yüzde -90 ile 300 arasında ve sıfırdan farklı olmalı.");
        if (Yazi(p, "kategori") is string k && !b.KategoriVar(k)) hatalar.Add($"\"{k}\" diye bir kategori yok.");
        if (Yazi(p, "marka") is string m && !b.MarkaVar(m)) hatalar.Add($"\"{m}\" diye bir marka yok.");
        var araclar = b.Uyanlar(Yazi(p, "kategori"), Yazi(p, "marka"), null);
        if (araclar.Count == 0 && hatalar.Count == 0) hatalar.Add("Koşula uyan araç yok.");
        var ornek = araclar.FirstOrDefault();
        var kimler = string.Join(" · ", new[] { Yazi(p, "kategori"), Yazi(p, "marka") }.Where(x => x != null));
        return Task.FromResult($"{(kimler.Length == 0 ? "Bütün" : kimler)} araçlar ({araclar.Count} araç): günlük fiyat %{yuzde:+0.#;-0.#}"
            + (ornek != null && yuzde != null ? $" (örn. {ornek.TamAd}: {Bicim.Para(ornek.GunlukFiyat)} → {Bicim.Para(YeniFiyat(ornek.GunlukFiyat, yuzde.Value))})" : ""));
    }

    private static decimal YeniFiyat(decimal eski, decimal yuzde) => Math.Max(1, Math.Round(eski * (1 + yuzde / 100m)));

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var baglam = new PlanBaglami { Vt = vt, Araclar = await vt.Araclar.Include(a => a.Kategori).Include(a => a.Marka).Where(a => a.Durum != AracDurumu.Pasif).ToListAsync() };
        var eskiler = new JsonArray();
        foreach (var arac in baglam.Uyanlar(Yazi(p, "kategori"), Yazi(p, "marka"), null))
        {
            eskiler.Add(new JsonObject { ["id"] = arac.Id, ["eski"] = arac.GunlukFiyat });
            arac.GunlukFiyat = YeniFiyat(arac.GunlukFiyat, Sayi(p, "yuzde")!.Value);   // UPDATE "Araclar" SET "GunlukFiyat" = ...
        }
        await vt.SaveChangesAsync();
        return Geri(("araclar", eskiler));
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        foreach (var satir in geri["araclar"]!.AsArray())
        {
            var arac = await vt.Araclar.FindAsync(satir!["id"]!.GetValue<int>());
            if (arac != null) arac.GunlukFiyat = satir["eski"]!.GetValue<decimal>();
        }
        await vt.SaveChangesAsync();
    }

    public override string? Link(JsonObject p) => "/Araclar";
}

/// <summary>Yeni bir kategori ekler.</summary>
public class KategoriEkle : PanelIslemi
{
    public override string Tur => "kategori_ekle";
    public override string Baslik => "Kategori ekle";
    public override string Ikon => "bi-tags";
    public override string Aciklama => "Yeni bir araç kategorisi (sınıfı) ekler, örn. Elektrikli, Minibüs.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ad"] = Sema("string"),
        ["en_az_yas"] = Sema("integer"),
        ["en_az_ehliyet_yili"] = Sema("integer"),
        ["ikon"] = Sema("string", "Bootstrap ikon adı, örn. bi-ev-front, bi-truck"),
    }, ["ad"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ad = Yazi(p, "ad");
        if (ad == null) hatalar.Add("Kategori adı boş olamaz.");
        else if (b.KategoriVar(ad)) hatalar.Add($"\"{ad}\" kategorisi zaten var.");
        else b.YeniKategoriler.Add(ad);
        return Task.FromResult($"\"{ad}\" · en az {Sayi(p, "en_az_yas") ?? 21:0} yaş, {Sayi(p, "en_az_ehliyet_yili") ?? 1:0} yıl ehliyet");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var kategori = new Kategori
        {
            Ad = Yazi(p, "ad")!, MinYas = (int)(Sayi(p, "en_az_yas") ?? 21), MinEhliyetYili = (int)(Sayi(p, "en_az_ehliyet_yili") ?? 1),
            Ikon = Yazi(p, "ikon") is string ikon && ikon.StartsWith("bi-") ? ikon : "bi-car-front",
            Sira = await vt.Kategoriler.CountAsync() + 1,
        };
        vt.Kategoriler.Add(kategori);
        await vt.SaveChangesAsync();
        return Geri(("eklenenId", kategori.Id));
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var id = geri["eklenenId"]!.GetValue<int>();
        if (await vt.Araclar.AnyAsync(a => a.KategoriId == id) || await vt.Kampanyalar.AnyAsync(k => k.KategoriId == id))
            throw new IsKuraliHatasi("Bu kategoriye araç ya da kampanya bağlanmış; önce onları değiştirin.");
        await vt.Kategoriler.Where(k => k.Id == id).ExecuteDeleteAsync();
    }

    public override string? Link(JsonObject p) => "/Yonetim/Kategoriler";
}

/// <summary>Yeni bir marka ekler.</summary>
public class MarkaEkle : PanelIslemi
{
    public override string Tur => "marka_ekle";
    public override string Baslik => "Marka ekle";
    public override string Ikon => "bi-award";
    public override string Aciklama => "Yeni bir araç markası ekler.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject { ["ad"] = Sema("string") }, ["ad"]);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ad = Yazi(p, "ad");
        if (ad == null) hatalar.Add("Marka adı boş olamaz.");
        else if (b.MarkaVar(ad)) hatalar.Add($"\"{ad}\" markası zaten var.");
        else b.YeniMarkalar.Add(ad);
        return Task.FromResult($"\"{ad}\"");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var marka = new Marka { Ad = Yazi(p, "ad")! };
        vt.Markalar.Add(marka);
        await vt.SaveChangesAsync();
        return Geri(("eklenenId", marka.Id));
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var id = geri["eklenenId"]!.GetValue<int>();
        if (await vt.Araclar.AnyAsync(a => a.MarkaId == id) || await vt.Kampanyalar.AnyAsync(k => k.MarkaId == id))
            throw new IsKuraliHatasi("Bu markaya araç ya da kampanya bağlanmış; önce onları değiştirin.");
        await vt.Markalar.Where(m => m.Id == id).ExecuteDeleteAsync();
    }

    public override string? Link(JsonObject p) => "/Yonetim/Markalar";
}

/// <summary>Kiralamaya eklenebilen yeni bir ek hizmet ekler.</summary>
public class EkstraEkle : PanelIslemi
{
    public override string Tur => "ekstra_ekle";
    public override string Baslik => "Ek hizmet ekle";
    public override string Ikon => "bi-plus-square";
    public override string Aciklama => "Kiralamaya eklenebilen YENİ bir ek hizmet ekler (örn. Çocuk koltuğu, günlük 150 TL). EK HİZMETLER listesinde zaten varsa ekstra_guncelle kullan.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ad"] = Sema("string"),
        ["gunluk_fiyat"] = Sema("number"),
        ["aciklama"] = Sema("string"),
        ["ikon"] = Sema("string", "Bootstrap ikon adı, örn. bi-snow2"),
    }, ["ad", "gunluk_fiyat"]);

    public override async Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ad = Yazi(p, "ad");
        if (ad == null) hatalar.Add("Hizmet adı boş olamaz.");
        else if (AdIle(await b.Vt.Ekstralar.AsNoTracking().ToListAsync(), e => e.Ad, ad) != null) hatalar.Add($"\"{ad}\" zaten var.");
        if (Sayi(p, "gunluk_fiyat") is null or < 0 or > 100_000) hatalar.Add("Günlük fiyat 0 ile 100.000 arasında olmalı.");
        return $"\"{ad}\" · günlük {Bicim.Para(Sayi(p, "gunluk_fiyat") ?? 0)}";
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var ekstra = new Ekstra
        {
            Ad = Yazi(p, "ad")!, GunlukFiyat = Sayi(p, "gunluk_fiyat")!.Value, Aciklama = Yazi(p, "aciklama"),
            Ikon = Yazi(p, "ikon") is string ikon && ikon.StartsWith("bi-") ? ikon : "bi-plus-circle",
        };
        vt.Ekstralar.Add(ekstra);
        await vt.SaveChangesAsync();
        return Geri(("eklenenId", ekstra.Id));
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var id = geri["eklenenId"]!.GetValue<int>();
        if (await vt.RezervasyonEkstralari.AnyAsync(x => x.EkstraId == id))
            throw new IsKuraliHatasi("Bu hizmet bir rezervasyonda seçilmiş; silinemez (Ek Hizmetler'den pasif yapabilirsiniz).");
        await vt.Ekstralar.Where(e => e.Id == id).ExecuteDeleteAsync();
    }

    public override string? Link(JsonObject p) => "/Yonetim/Ekstralar";
}

/// <summary>Var olan bir ek hizmetin adını, fiyatını ya da yayın durumunu değiştirir.</summary>
public class EkstraGuncelle : PanelIslemi
{
    public override string Tur => "ekstra_guncelle";
    public override string Baslik => "Ek hizmeti güncelle";
    public override string Ikon => "bi-pencil-square";
    public override string Aciklama => "Var olan bir ek hizmetin günlük fiyatını, adını ya da sitede görünüp görünmeyeceğini değiştirir. ad: EK HİZMETLER listesindeki ad. Sadece değişecek alanları yaz.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ad"] = Sema("string"), ["gunluk_fiyat"] = Sema("number"), ["yeni_ad"] = Sema("string"), ["aktif"] = Sema("boolean"),
    }, ["ad"]);

    public override async Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var ekstra = AdIle(await b.Vt.Ekstralar.AsNoTracking().ToListAsync(), e => e.Ad, Yazi(p, "ad"));
        if (ekstra == null) { hatalar.Add($"\"{Yazi(p, "ad")}\" diye bir ek hizmet yok."); return ""; }
        var degisenler = new List<string>();
        if (Sayi(p, "gunluk_fiyat") is decimal fiyat)
        {
            if (fiyat is < 0 or > 100_000) hatalar.Add("Günlük fiyat 0 ile 100.000 arasında olmalı.");
            degisenler.Add($"günlük {Bicim.Para(ekstra.GunlukFiyat)} → {Bicim.Para(fiyat)}");
        }
        if (Yazi(p, "yeni_ad") is string yeniAd) degisenler.Add($"ad → \"{yeniAd}\"");
        if (p["aktif"] != null) degisenler.Add(Evet(p, "aktif", true) ? "sitede görünecek" : "sitede gizlenecek");
        if (degisenler.Count == 0) hatalar.Add("Değişecek bir şey yazılmamış (fiyat, ad ya da aktif).");
        return $"\"{ekstra.Ad}\": {string.Join(", ", degisenler)}";
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var ekstra = AdIle(await vt.Ekstralar.ToListAsync(), e => e.Ad, Yazi(p, "ad"))!;
        var geri = Geri(("id", ekstra.Id), ("ad", ekstra.Ad), ("fiyat", ekstra.GunlukFiyat), ("aktif", ekstra.Aktif));
        if (Sayi(p, "gunluk_fiyat") is decimal fiyat) ekstra.GunlukFiyat = fiyat;
        if (Yazi(p, "yeni_ad") is string yeniAd) ekstra.Ad = yeniAd;
        if (p["aktif"] != null) ekstra.Aktif = Evet(p, "aktif", true);
        await vt.SaveChangesAsync();   // UPDATE "Ekstralar" SET ...
        return geri;
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var ekstra = await vt.Ekstralar.FindAsync(geri["id"]!.GetValue<int>());
        if (ekstra == null) return;
        ekstra.Ad = Yazi(geri, "ad")!;
        ekstra.GunlukFiyat = geri["fiyat"]!.GetValue<decimal>();
        ekstra.Aktif = geri["aktif"]!.GetValue<bool>();
        await vt.SaveChangesAsync();
    }

    public override string? Link(JsonObject p) => "/Yonetim/Ekstralar";
}

/// <summary>Yeni bir şube ekler.</summary>
public class SubeEkle : PanelIslemi
{
    public override string Tur => "sube_ekle";
    public override string Baslik => "Şube ekle";
    public override string Ikon => "bi-buildings";
    public override string Aciklama => "Yeni bir şube (araç alma / iade noktası) ekler.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["ad"] = Sema("string"), ["sehir"] = Sema("string"), ["adres"] = Sema("string"), ["telefon"] = Sema("string"),
    }, ["ad", "sehir", "adres", "telefon"]);

    public override async Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        foreach (var alan in new[] { "ad", "sehir", "adres", "telefon" })
            if (Yazi(p, alan) == null) hatalar.Add($"Şubenin {alan} bilgisi boş olamaz.");
        if (AdIle(await b.Vt.Subeler.AsNoTracking().ToListAsync(), s => s.Ad, Yazi(p, "ad")) != null) hatalar.Add($"\"{Yazi(p, "ad")}\" şubesi zaten var.");
        return $"\"{Yazi(p, "ad")}\" · {Yazi(p, "sehir")} · {Yazi(p, "telefon")}";
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var sube = new Sube { Ad = Yazi(p, "ad")!, Sehir = Yazi(p, "sehir")!, Adres = Yazi(p, "adres")!, Telefon = Yazi(p, "telefon")! };
        vt.Subeler.Add(sube);
        await vt.SaveChangesAsync();
        return Geri(("eklenenId", sube.Id));
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var id = geri["eklenenId"]!.GetValue<int>();
        if (await vt.Araclar.AnyAsync(a => a.SubeId == id) || await vt.Rezervasyonlar.AnyAsync(r => r.AlisSubeId == id || r.IadeSubeId == id))
            throw new IsKuraliHatasi("Bu şubeye araç ya da rezervasyon bağlanmış; silinemez.");
        await vt.Subeler.Where(s => s.Id == id).ExecuteDeleteAsync();
    }

    public override string? Link(JsonObject p) => "/Yonetim/Subeler";
}

/// <summary>Siteye yeni bir yazı sayfası ekler (Sayfalar tablosu).</summary>
public class SayfaEkle : PanelIslemi
{
    public override string Tur => "sayfa_ekle";
    public override string Baslik => "Sayfa ekle";
    public override string Ikon => "bi-file-earmark-text";
    public override string Aciklama => "Siteye yeni bir yazı sayfası ekler (örn. Sıkça Sorulan Sorular). icerik: sayfanın metninin TAMAMI, en az birkaç paragraf; "
        + "boş satır = yeni paragraf, \"- \" ile başlayan satır = madde, \"## \" = ara başlık. adres boşsa başlıktan üretilir. "
        + "Üst menüde görünmesi için menude_goster: true yeterli; menüdeki başka yazıları değiştirme.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject
    {
        ["baslik"] = Sema("string"), ["icerik"] = Sema("string"), ["adres"] = Sema("string"), ["menude_goster"] = EvetHayir(false),
    }, ["baslik", "icerik"]);

    private static string AdresBul(JsonObject p) => AdresYap(Yazi(p, "adres") ?? Yazi(p, "baslik") ?? "");

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var adres = AdresBul(p);
        if (Yazi(p, "baslik") == null) hatalar.Add("Sayfa başlığı boş olamaz.");
        if ((Yazi(p, "icerik")?.Length ?? 0) < 80) hatalar.Add("Sayfa içeriği çok kısa; sayfanın metnini (birkaç paragraf) yazın.");
        if (adres.Length is 0 or > 60) hatalar.Add("Sayfa adresi üretilemedi.");
        else if (b.Sayfalar.Any(s => s.Adres == adres) || !b.YeniSayfalar.Add(adres)) hatalar.Add($"/Sayfa/{adres} adresinde bir sayfa zaten var.");
        return Task.FromResult($"\"{Yazi(p, "baslik")}\" → /Sayfa/{adres}{(Evet(p, "menude_goster", false) ? " · üst menüde" : "")} · {(Yazi(p, "icerik")?.Length ?? 0)} karakter");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var sayfa = new Sayfa
        {
            Baslik = Yazi(p, "baslik")!, Adres = AdresBul(p), Icerik = Yazi(p, "icerik")!,
            MenudeGoster = Evet(p, "menude_goster", false), AltBilgideGoster = true, Aktif = true, Sira = await vt.Sayfalar.CountAsync() + 1,
        };
        vt.Sayfalar.Add(sayfa);
        await vt.SaveChangesAsync();
        return Geri(("eklenenId", sayfa.Id));
    }

    public override Task GeriAlAsync(JsonObject geri, VeriTabani vt)
        => vt.Sayfalar.Where(s => s.Id == geri["eklenenId"]!.GetValue<int>()).ExecuteDeleteAsync();

    public override string? Link(JsonObject p) => "/Sayfa/" + AdresBul(p);
}

/// <summary>Bir sayfayı (Hakkımızda, KVKK...) yayına alır ya da kaldırır.</summary>
public class SayfaAcKapat : PanelIslemi
{
    public override string Tur => "sayfa_ac_kapat";
    public override string Baslik => "Sayfayı aç / kapat";
    public override string Ikon => "bi-eye";
    public override string Aciklama => "Var olan bir sayfayı yayına alır (acik: true) ya da yayından kaldırır (acik: false). sayfa: SAYFALAR listesindeki adres ya da başlık.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject { ["sayfa"] = Sema("string"), ["acik"] = Sema("boolean") }, ["sayfa", "acik"]);

    private static Sayfa? Bul(IEnumerable<Sayfa> sayfalar, string? aranan)
        => sayfalar.FirstOrDefault(s => s.Adres == aranan) ?? AdIle(sayfalar, s => s.Baslik, aranan);

    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar)
    {
        var sayfa = Bul(b.Sayfalar, Yazi(p, "sayfa"));
        if (sayfa == null && b.YeniSayfalar.Contains(AdresYap(Yazi(p, "sayfa") ?? "")))
            hatalar.Add("Bu sayfa bu istekte ekleniyor ve zaten yayında olacak; bu kart gereksiz, işaretini kaldırın.");
        else if (sayfa == null) hatalar.Add($"\"{Yazi(p, "sayfa")}\" diye bir sayfa yok.");
        return Task.FromResult($"\"{sayfa?.Baslik ?? Yazi(p, "sayfa")}\" → {(Evet(p, "acik", true) ? "yayında" : "kapalı")}");
    }

    public override async Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt)
    {
        var sayfa = Bul(await vt.Sayfalar.ToListAsync(), Yazi(p, "sayfa"))!;
        var geri = Geri(("id", sayfa.Id), ("eskiAktif", sayfa.Aktif));
        sayfa.Aktif = Evet(p, "acik", true);
        await vt.SaveChangesAsync();
        return geri;
    }

    public override async Task GeriAlAsync(JsonObject geri, VeriTabani vt)
    {
        var sayfa = await vt.Sayfalar.FindAsync(geri["id"]!.GetValue<int>());
        if (sayfa != null) { sayfa.Aktif = geri["eskiAktif"]!.GetValue<bool>(); await vt.SaveChangesAsync(); }
    }

    public override string? Link(JsonObject p) => "/Yonetim/Sayfalar";
}

/// <summary>Kullanıcı soru sorduysa cevap (hiçbir şey değişmez).</summary>
public class CevapVer : PanelIslemi
{
    public override string Tur => "cevap_ver";
    public override string Baslik => "Cevap";
    public override string Ikon => "bi-chat-left-text";
    public override bool Uygulanabilir => false;
    public override string Aciklama => "Kullanıcı bir değişiklik değil soru sorduysa (örn. \"kaç kampanya var?\", \"filtreler nereden açılır?\") kısa Türkçe cevap yazar. Hiçbir şey değiştirmez.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject { ["metin"] = Sema("string") }, ["metin"]);
    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar) => Task.FromResult(Yazi(p, "metin") ?? "");
    public override Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt) => Task.FromResult(new JsonObject());
    public override Task GeriAlAsync(JsonObject geri, VeriTabani vt) => Task.CompletedTask;
}

/// <summary>İstek panel işlemleriyle yapılamıyorsa (kod değişikliği gerekiyorsa).</summary>
public class Yapilamaz : PanelIslemi
{
    public override string Tur => "yapilamaz";
    public override string Baslik => "Panelden yapılamıyor";
    public override string Ikon => "bi-code-slash";
    public override bool Uygulanabilir => false;
    public override string Aciklama => "İstek bu listedeki işlemlerle yapılamıyorsa (yeni sayfa düzeni, yeni hesaplama kuralı gibi kod değişikliği gerekiyorsa) kullanılır. sebep: kısa Türkçe açıklama.";

    public override (JsonObject, string[]) Parametreler() => (new JsonObject { ["sebep"] = Sema("string") }, ["sebep"]);
    public override Task<string> DogrulaAsync(JsonObject p, PlanBaglami b, List<string> hatalar) => Task.FromResult(Yazi(p, "sebep") ?? "");
    public override Task<JsonObject> UygulaAsync(JsonObject p, VeriTabani vt) => Task.FromResult(new JsonObject());
    public override Task GeriAlAsync(JsonObject geri, VeriTabani vt) => Task.CompletedTask;
}

// ============================================================================
//                     İŞLEM LİSTESİ, DOĞRULAMA, UYGULAMA, GERİ ALMA
// ============================================================================
public class PanelIslemleri
{
    /// <summary>Asistanın yapabildiği her şey. Yeni işlem = bu listeye bir satır.</summary>
    public static readonly PanelIslemi[] Hepsi =
    [
        new MetinDegistir(), new AnaBaslikDegistir(), new AyarDegistir(),
        new MusteriBilgisiEkle(), new KampanyaEkle(), new KampanyaGuncelle(), new KampanyaKapat(),
        new AracOzelligiEkle(), new AracOzellikDegeriVer(), new AracFiyatiDegistir(),
        new KategoriEkle(), new MarkaEkle(), new EkstraEkle(), new EkstraGuncelle(), new SubeEkle(),
        new SayfaEkle(), new SayfaAcKapat(),
        new CevapVer(), new Yapilamaz(),
    ];

    public static PanelIslemi? Bul(string? tur) => Hepsi.FirstOrDefault(i => i.Tur == tur);

    private readonly VeriTabani _vt;
    private readonly IcerikServisi _icerik;

    public PanelIslemleri(VeriTabani vt, IcerikServisi icerik)
    {
        _vt = vt;
        _icerik = icerik;
    }

    /// <summary>
    /// Modelin cevabının uyması gereken JSON şeması:
    /// { "anladigim": "...", "islemler": [ { "tur": "kampanya_ekle", ...parametreler }, ... ] }
    /// Her işlem türü için ayrı bir şema vardır (anyOf); motor şemaya uymayan bir şey yazamaz.
    /// </summary>
    public static JsonObject Sema()
    {
        var turler = new JsonArray();
        foreach (var islem in Hepsi)
        {
            var (ozellikler, zorunlular) = islem.Parametreler();
            var hepsi = new JsonObject { ["tur"] = new JsonObject { ["const"] = islem.Tur } };
            foreach (var (ad, sema) in ozellikler) hepsi[ad] = sema?.DeepClone();
            turler.Add(new JsonObject
            {
                ["type"] = "object",
                ["properties"] = hepsi,
                ["required"] = new JsonArray(zorunlular.Prepend("tur").Select(z => (JsonNode)z).ToArray()),
                ["additionalProperties"] = false,
            });
        }
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["anladigim"] = new JsonObject { ["type"] = "string" },
                ["islemler"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["anyOf"] = turler }, ["minItems"] = 1, ["maxItems"] = 15 },
            },
            ["required"] = new JsonArray("anladigim", "islemler"),
            ["additionalProperties"] = false,
        };
    }

    /// <summary>İşlemleri sırayla kontrol eder ve ekrandaki kartları hazırlar (hiçbir şey değiştirmez).</summary>
    public async Task<List<IslemOnizleme>> DogrulaAsync(JsonArray islemler)
    {
        var baglam = await PlanBaglami.OlusturAsync(_vt);
        var kartlar = new List<IslemOnizleme>();
        foreach (var dugum in islemler)
        {
            if (dugum is not JsonObject p) continue;
            var tur = p["tur"]?.ToString();
            var islem = Bul(tur);
            var kart = new IslemOnizleme { Tur = tur ?? "?", Parametreler = (JsonObject)p.DeepClone() };
            kart.Parametreler.Remove("tur");
            if (islem == null)
            {
                kart.Baslik = "Bilinmeyen işlem";
                kart.Hatalar.Add($"\"{tur}\" diye bir işlem yok.");
            }
            else
            {
                kart.Baslik = islem.Baslik;
                kart.Ikon = islem.Ikon;
                kart.Uygulanabilir = islem.Uygulanabilir;
                kart.OnizlemeVar = islem.OnizlemeVar;
                kart.Link = islem.Link(p);
                kart.Ozet = await islem.DogrulaAsync(p, baglam, kart.Hatalar);
                // Modelin yazmadığı, varsayılanı olan parametreler de kartta görünsün (düzenlenebilsin)
                foreach (var (ad, sema) in islem.Parametreler().Ozellikler)
                    if (!kart.Parametreler.ContainsKey(ad) && sema?["default"] is JsonNode varsayilan) kart.Parametreler[ad] = varsayilan.DeepClone();
            }
            kartlar.Add(kart);
        }
        return kartlar;
    }

    /// <summary>
    /// İşlemleri sırayla uygular ve geçmişe kaydeder. Hepsi tek "transaction" içindedir:
    /// biri hata verirse hiçbiri kaydedilmez. Uygulamadan önce yeniden doğrulanır.
    /// </summary>
    public async Task<YapayZekaKaydi> UygulaAsync(JsonArray islemler, string istek, string? anladigim, string kullaniciAdi, string? modelAdi, double sure)
    {
        var kartlar = await DogrulaAsync(islemler);
        var hatalilar = kartlar.Where(k => k.Uygulanabilir && k.Hatalar.Count > 0).ToList();
        if (hatalilar.Count > 0) throw new IsKuraliHatasi("Önce hataları düzeltin: " + string.Join(" ", hatalilar.SelectMany(k => k.Hatalar)));

        var uygulanacaklar = islemler.OfType<JsonObject>().Where(p => Bul(p["tur"]?.ToString())?.Uygulanabilir == true).ToList();
        if (uygulanacaklar.Count == 0) throw new IsKuraliHatasi("Uygulanacak bir değişiklik yok.");

        await using var islem = await _vt.Database.BeginTransactionAsync();
        var geriAlmalar = new JsonArray();
        foreach (var p in uygulanacaklar)
        {
            var geri = await Bul(p["tur"]!.ToString())!.UygulaAsync(p, _vt);
            geriAlmalar.Add(new JsonObject { ["tur"] = p["tur"]!.ToString(), ["geri"] = geri });
        }
        var kayit = new YapayZekaKaydi
        {
            Tur = "Panel", Istek = Kisalt(istek, 2000), Anladigim = anladigim == null ? null : Kisalt(anladigim, 2000), KullaniciAdi = kullaniciAdi,
            Ozet = string.Join("\n", kartlar.Where(k => k.Uygulanabilir).Select(k => $"{k.Baslik}: {k.Ozet}")),
            IslemlerJson = new JsonArray(uygulanacaklar.Select(p => (JsonNode)p.DeepClone()).ToArray()).ToJsonString(),
            GeriAlmaJson = geriAlmalar.ToJsonString(), ModelAdi = modelAdi, Sure = sure,
        };
        _vt.YapayZekaKayitlari.Add(kayit);
        await _vt.SaveChangesAsync();
        await islem.CommitAsync();
        _icerik.Yenile();   // yazı / ayar değiştiyse sitede hemen görünsün
        return kayit;
    }

    /// <summary>Bir kaydın değişikliklerini ters sırayla geri alır.</summary>
    public async Task GeriAlAsync(YapayZekaKaydi kayit)
    {
        if (kayit.GeriAlindi) throw new IsKuraliHatasi("Bu değişiklik zaten geri alınmış.");
        var geriAlmalar = JsonNode.Parse(kayit.GeriAlmaJson)!.AsArray();

        await using var islem = await _vt.Database.BeginTransactionAsync();
        foreach (var satir in geriAlmalar.Reverse())
            await Bul(satir!["tur"]!.ToString())!.GeriAlAsync(satir["geri"]!.AsObject(), _vt);
        kayit.GeriAlindi = true;
        kayit.GeriAlmaTarihi = DateTime.Now;
        await _vt.SaveChangesAsync();
        await islem.CommitAsync();
        _icerik.Yenile();
    }

    private static string Kisalt(string metin, int uzunluk) => metin.Length <= uzunluk ? metin : metin[..uzunluk];
}
