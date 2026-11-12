// ============================================================================
// AYAR ÇEVİRİCİ
// Formdan gelen ayarlar hep yazı (string) olarak gelir: "true", "0.15", "#2747d6"...
// Bu sınıf yazıyı SiteAyarlari'ndaki doğru türe (bool, int, decimal, string) çevirip
// ilgili özelliğe yazar. Site Düzenleyici hem önizlemede hem kaydederken bunu kullanır.
// ============================================================================
using System.Globalization;
using System.Reflection;
using AracKiralama.Modeller;

namespace AracKiralama.Yardimcilar;

public static class AyarCevirici
{
    /// <summary>SiteAyarlari'nda [Ayar] etiketi olan (panelden değiştirilebilen) özellik mi?</summary>
    public static PropertyInfo? AyarOzelligi(string ad)
    {
        var ozellik = typeof(SiteAyarlari).GetProperty(ad);
        return ozellik?.GetCustomAttribute<AyarAttribute>() == null ? null : ozellik;
    }

    /// <summary>Bir ayarın değerini forma yazılacak hale getirir (ondalık sayılar noktalı yazılır).</summary>
    public static string Yazi(object? deger) => deger switch
    {
        decimal sayi => sayi.ToString(CultureInfo.InvariantCulture),
        bool evet => evet ? "true" : "false",
        null => "",
        _ => deger.ToString() ?? "",
    };

    /// <summary>
    /// Yazı halindeki değeri ayara yazar. Değer bu türe çevrilemiyorsa (örn. sayı kutusuna harf)
    /// hiçbir şey yapmaz ve false döner; böylece bozuk bir değer siteyi bozamaz.
    /// </summary>
    public static bool Uygula(SiteAyarlari ayarlar, string ad, string? deger)
    {
        var ozellik = AyarOzelligi(ad);
        if (ozellik == null) return false;

        var tur = ozellik.PropertyType;
        object? yeni;
        if (tur == typeof(bool)) yeni = deger is "true" or "True" or "on";
        else if (tur == typeof(int))
        {
            if (!int.TryParse(deger, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sayi)) return false;
            yeni = sayi;
        }
        else if (tur == typeof(decimal))
        {
            // Türkçe klavyede virgülle yazılırsa da kabul edilir: "0,15" → 0.15
            if (!decimal.TryParse(deger?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var ondalik)) return false;
            yeni = ondalik;
        }
        else yeni = deger?.Trim() ?? "";

        ozellik.SetValue(ayarlar, yeni);
        return true;
    }

    /// <summary>Ayarların birebir kopyasını çıkarır (önizlemede asıl ayarlara dokunmamak için).</summary>
    public static SiteAyarlari Kopyala(SiteAyarlari kaynak)
    {
        var kopya = new SiteAyarlari();
        foreach (var ozellik in typeof(SiteAyarlari).GetProperties().Where(o => o.CanWrite))
            ozellik.SetValue(kopya, ozellik.GetValue(kaynak));
        return kopya;
    }
}
