// ============================================================================
// SİTE DÜZENLEYİCİ SAYFA MODELLERİ
// Views/Duzenleyici/Index.cshtml'in ihtiyaç duyduğu veriler (Yönetim alanında).
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Yardimcilar;

namespace AracKiralama.FormModelleri;

/// <summary>Düzenleyicinin solundaki tek bir kutu (bir yazı ya da bir ayar).</summary>
public class DuzenleyiciAlani
{
    public bool AyarMi { get; set; }                // false → sitedeki bir yazı (Metinler), true → bir ayar (SiteAyarlari)
    public string Anahtar { get; set; } = "";       // "anasayfa.baslik" ya da "AnaRenk"
    public string FormAdi => AyarMi ? $"ayar[{Anahtar}]" : $"metin[{Anahtar}]";   // formdaki name
    public string Bolum { get; set; } = "";         // solda hangi başlığın altında duracak
    public string Etiket { get; set; } = "";        // kutunun sade Türkçe adı
    public string? Aciklama { get; set; }           // kutunun altındaki küçük not
    public string Deger { get; set; } = "";         // şu an kayıtlı değer
    public string Varsayilan { get; set; } = "";    // ilk (fabrika) değeri
    public Type? AyarTuru { get; set; }             // ayarın türü: bool, int, decimal, string
    public AyarAttribute? AyarBilgisi { get; set; } // renk mi, seçenekli mi, çok satırlı mı?

    /// <summary>Arama kutusunun aradığı yazı (adı + bölümü, küçük harfle).</summary>
    public string AramaMetni => $"{Etiket} {Bolum} {Anahtar}".ToLower(Bicim.Tr);
}

/// <summary>Düzenleyici sayfasının bütün verisi.</summary>
public class DuzenleyiciModeli
{
    public DuzenleyiciSekmesi Sekme { get; set; } = DuzenleyiciTanimlari.Sekmeler[0];
    public List<DuzenleyiciAlani> Alanlar { get; set; } = new();
    public List<Sayfa> KurumsalSayfalar { get; set; } = new();          // sadece "Sunum ayarları" sekmesinde dolu
    public List<(string Ad, string Adres)> Onizlemeler { get; set; } = new();
}
