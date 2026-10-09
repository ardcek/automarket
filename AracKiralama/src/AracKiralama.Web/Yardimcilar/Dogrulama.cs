// ============================================================================
// DOĞRULAMA: T.C. Kimlik No ve Türk plakası
// Form modellerinde etiket olarak kullanılır: [TcKimlik], [PlakaKontrol]
// ============================================================================
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AracKiralama.Yardimcilar;

/// <summary>
/// T.C. Kimlik No algoritması:
///  - 11 hane, ilk hane 0 olamaz
///  - 10. hane = ((1+3+5+7+9. haneler) × 7 − (2+4+6+8. haneler)) mod 10
///  - 11. hane = ilk 10 hanenin toplamı mod 10
/// </summary>
public static class TcKimlikNo
{
    public static bool GecerliMi(string? no)
    {
        if (no == null || no.Length != 11 || !no.All(char.IsAsciiDigit) || no[0] == '0') return false;

        var h = no.Select(c => c - '0').ToArray();   // haneler
        var tekler = h[0] + h[2] + h[4] + h[6] + h[8];
        var ciftler = h[1] + h[3] + h[5] + h[7];
        var onuncu = ((tekler * 7 - ciftler) % 10 + 10) % 10;
        var onbirinci = h.Take(10).Sum() % 10;
        return h[9] == onuncu && h[10] == onbirinci;
    }

    /// <summary>Örnek veriler için algoritmaya uygun (gerçek kişiye ait olmayan) numara üretir.</summary>
    public static string Uret(Random rastgele)
    {
        var h = new int[11];
        h[0] = rastgele.Next(1, 10);
        for (var i = 1; i < 9; i++) h[i] = rastgele.Next(0, 10);
        h[9] = (((h[0] + h[2] + h[4] + h[6] + h[8]) * 7 - (h[1] + h[3] + h[5] + h[7])) % 10 + 10) % 10;
        h[10] = h.Take(10).Sum() % 10;
        return string.Concat(h);
    }
}

/// <summary>Türk plakası: il kodu (01-81) + 1-3 harf + 2-4 rakam. Örn: "34 ABC 123"</summary>
public static class Plaka
{
    private static readonly Regex Kalip = new(@"^(0[1-9]|[1-7][0-9]|8[01]) ?([A-Z]{1,3}) ?(\d{2,4})$");

    public static bool GecerliMi(string? plaka) => plaka != null && Kalip.IsMatch(Duzenle(plaka));

    /// <summary>"34abc123" → "34 ABC 123"</summary>
    public static string Duzenle(string plaka)
    {
        var sade = new string(plaka.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        var eslesme = Kalip.Match(sade);
        return eslesme.Success
            ? $"{eslesme.Groups[1].Value} {eslesme.Groups[2].Value} {eslesme.Groups[3].Value}"
            : plaka.Trim().ToUpperInvariant();
    }
}

public class TcKimlikAttribute : ValidationAttribute
{
    public TcKimlikAttribute() : base("Geçerli bir T.C. Kimlik Numarası giriniz.") { }
    public override bool IsValid(object? deger) => string.IsNullOrEmpty(deger as string) || TcKimlikNo.GecerliMi((string)deger!);
}

public class PlakaKontrolAttribute : ValidationAttribute
{
    public PlakaKontrolAttribute() : base("Geçerli bir plaka giriniz (örn: 34 ABC 123).") { }
    public override bool IsValid(object? deger) => Plaka.GecerliMi(deger as string);
}
