// ============================================================================
// BİÇİM: para, tarih ve metinlerin ekranda nasıl görüneceği tek yerden yönetilir.
// View'larda kullanım: @Bicim.Para(arac.GunlukFiyat)  →  "1.250 ₺"
//                      @Bicim.Tarih(r.BaslangicTarihi) →  "09 Eki 2026"
//                      @durum.Ad()                     →  "Onay Bekliyor"
// ============================================================================
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace AracKiralama.Yardimcilar;

public static class Bicim
{
    public static readonly CultureInfo Tr = new("tr-TR");

    public static string Para(decimal tutar) => tutar.ToString("N0", Tr) + " ₺";
    public static string Sayi(int sayi) => sayi.ToString("N0", Tr);
    public static string Yuzde(decimal oran) => "%" + (oran * 100).ToString("0", Tr);
    public static string Tarih(DateTime t) => t.ToString("dd MMM yyyy", Tr);
    public static string TarihSaat(DateTime t) => t.ToString("dd MMM yyyy HH:mm", Tr);
    public static string TarihSaat(DateTime? t) => t == null ? "-" : TarihSaat(t.Value);
    public static string Ay(DateTime t) => t.ToString("MMM yy", Tr);
    public static string GunAdi(DateTime t) => t.ToString("ddd", Tr);

    /// <summary>HTML tarih kutusunun (datetime-local) istediği biçim: 2026-10-09T10:00</summary>
    public static string Kutu(DateTime t) => t.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    /// <summary>Enum'un [Display(Name="...")] ile yazılmış Türkçe adı. Kullanım: durum.Ad()</summary>
    public static string Ad(this Enum deger)
    {
        var alan = deger.GetType().GetField(deger.ToString());
        return alan?.GetCustomAttribute<DisplayAttribute>()?.Name ?? deger.ToString();
    }

    /// <summary>
    /// Admin panelinde yazılan düz metni HTML'e çevirir:
    /// boş satır = yeni paragraf, "- " = madde işareti, "## " = ara başlık.
    /// Yazılan her şey önce güvenli hale getirilir (HTML etiketi çalıştırılamaz).
    /// </summary>
    public static IHtmlContent Paragraflar(string? metin)
    {
        var html = new StringBuilder();
        var maddeler = false;
        foreach (var hamSatir in (metin ?? "").Replace("\r", "").Split('\n'))
        {
            var satir = Guvenli(hamSatir.Trim());
            var maddeMi = satir.StartsWith("- ");
            if (maddeler && !maddeMi) { html.Append("</ul>"); maddeler = false; }

            if (satir.Length == 0) continue;
            if (maddeMi)
            {
                if (!maddeler) { html.Append("<ul>"); maddeler = true; }
                html.Append("<li>").Append(satir[2..]).Append("</li>");
            }
            else if (satir.StartsWith("## ")) html.Append("<h2 class=\"h5 mt-4\">").Append(satir[3..]).Append("</h2>");
            else html.Append("<p>").Append(satir).Append("</p>");
        }
        if (maddeler) html.Append("</ul>");
        return new HtmlString(html.ToString());
    }

    /// <summary>
    /// Metni HTML içine güvenle yazar: &lt; &gt; &amp; " karakterleri zararsız hale gelir,
    /// böylece admin metne &lt;script&gt; yazsa bile çalışmaz. Türkçe harflere dokunmaz.
    /// </summary>
    public static string Guvenli(string metin)
        => metin.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>"#2747d6" → "39, 71, 214" (CSS'te saydam renk üretmek için).</summary>
    public static string RgbDegeri(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return "39, 71, 214";
        return $"{Convert.ToInt32(hex[..2], 16)}, {Convert.ToInt32(hex[2..4], 16)}, {Convert.ToInt32(hex[4..], 16)}";
    }
}
