// ============================================================================
// LOGO AMBLEMİ: sitenin kendi çizilmiş logosu (SVG)
// Yönetim → Ayarlar → Görünüm → "Logo" ile seçilir:
//   amblem-r     → "R" harfi; bacağı kesikli bir yol (Rota'nın baş harfi + rota)
//   amblem-rota  → başlangıç noktasından hedef iğnesine giden kesikli rota (firma adından bağımsız)
//   bi-...       → hazır Bootstrap ikonu (eski görünüm)
// Çizimler beyazdır; arkadaki renkli kare ana renktir (stil.css → .logo-isaret).
// Ana renk panelden değişince logo da kendiliğinden değişir. Kullanım: <partial name="_LogoIsareti" />
// ============================================================================
namespace AracKiralama.Yardimcilar;

public static class LogoAmblemi
{
    public static bool AmblemMi(string? logo) => logo is "amblem-r" or "amblem-rota";

    /// <summary>
    /// Amblemin SVG içi (viewBox 0 0 40 40). Çizgiler <paramref name="cizgiRengi"/>, iğnenin ortasındaki delik <paramref name="delikRengi"/>.
    /// "amblem-yol" sınıflı parça, logonun üstüne gelince kayan noktalı yoldur (vitrin.css).
    /// </summary>
    public static string Cizim(string logo, string cizgiRengi = "currentColor", string delikRengi = "var(--ana)") => logo switch
    {
        "amblem-rota" =>
            $"<path class='amblem-yol' d='M9.5 31.5c0-7.5 12.5-4 16-10.5' stroke='{cizgiRengi}' stroke-width='3.4' stroke-linecap='round' stroke-dasharray='0 5'/>" +
            $"<circle cx='9.5' cy='31.5' r='3.4' fill='{cizgiRengi}'/>" +
            $"<path d='M27 4a7.5 7.5 0 0 0-7.5 7.5c0 5.4 7.5 11.5 7.5 11.5s7.5-6.1 7.5-11.5A7.5 7.5 0 0 0 27 4z' fill='{cizgiRengi}'/>" +
            $"<circle cx='27' cy='11.5' r='2.8' style='fill:{delikRengi}'/>",
        _ =>
            $"<path d='M11 33V7.5h9.8a7.6 7.6 0 0 1 0 15.2H11' stroke='{cizgiRengi}' stroke-width='4.2' stroke-linecap='round' stroke-linejoin='round'/>" +
            $"<path class='amblem-yol' d='M20.5 22.7 31 33.6' stroke='{cizgiRengi}' stroke-width='4.2' stroke-linecap='round' stroke-dasharray='0 4.9'/>",
    };

    /// <summary>Tarayıcı sekmesindeki küçük simge (favicon): ana renkli yuvarlak kare + beyaz amblem.</summary>
    public static string FaviconAdresi(string logo, string anaRenk, string firmaAdi)
    {
        var ic = AmblemMi(logo)
            ? $"<g transform='translate(4 4) scale(.8)' fill='none'>{Cizim(logo, "white", anaRenk)}</g>"
            : $"<text x='20' y='27' font-size='20' text-anchor='middle' fill='white' font-family='Arial' font-weight='bold'>{(firmaAdi.Length > 0 ? firmaAdi[..1] : "R")}</text>";
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 40 40'><rect width='40' height='40' rx='11' fill='{anaRenk}'/>{ic}</svg>";
        return "data:image/svg+xml," + Uri.EscapeDataString(svg);
    }
}
