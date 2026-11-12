// ============================================================================
// YAPAY ZEKÂ ASİSTANI sayfa modeli ve tarayıcıdan gelen istekler
// (Areas/Yonetim/Controllers/YapayZekaController.cs, wwwroot/js/yapay-zeka.js)
// ============================================================================
using System.Text.Json.Nodes;
using AracKiralama.Modeller;
using AracKiralama.Servisler;

namespace AracKiralama.FormModelleri;

/// <summary>/Yonetim/YapayZeka sayfasının modeli.</summary>
public class YapayZekaSayfasi
{
    public SunucuDurumu Durum { get; set; }
    public string Mesaj { get; set; } = "";
    public bool Kurulu { get; set; }
    public string? ModelAdi { get; set; }
    public List<(string Ad, double BoyutGb)> Modeller { get; set; } = new();
    public double RamGb { get; set; }
    public bool KodModuAcik { get; set; }          // kaynak koddan çalışıyor ve admin
    public string? KodModuNedenKapali { get; set; }
    public List<YapayZekaKaydi> Gecmis { get; set; } = new();
}

/// <summary>Tarayıcının gönderdiği JSON (her uç nokta ihtiyacı olanı kullanır).</summary>
public class YapayZekaIstegi
{
    public string? Istek { get; set; }             // kullanıcının yazdığı cümle
    public string? Anladigim { get; set; }
    public JsonArray? Islemler { get; set; }       // (düzenlenmiş) işlem listesi
    public double Sure { get; set; }               // modelin düşünme süresi
    public string? Model { get; set; }             // "Başlat"ta seçilen model dosyası
    public List<string>? Dosyalar { get; set; }                    // kod modu: değiştirilecek dosyalar
    public List<KodDegisikligi>? Degisiklikler { get; set; }       // kod modu: dosya değişiklikleri
    public string? OncekiHata { get; set; }                        // kod modu: düzeltilmesi istenen derleme hataları
}
