// ============================================================================
// HAZIR ÖZELLİK ŞABLONLARI ve İKON LİSTESİ (Yönetim → Filtreler ve Özellikler)
// - Sablonlar: "Yeni özellik / filtre" ekranında kart kart listelenir; tıklayınca formun adı, türü,
//   ikonu, birimi ve seçenekleri dolu gelir. Yeni şablon eklemek = aşağıya bir satır.
// - Ikonlar  : ikon seçici penceresinin listesi. wwwroot/lib/bootstrap-icons'taki bütün ikon adları
//   (bir kez okunur, sonra bellekte tutulur). Seçici: wwwroot/js/genel.js → data-ikon-onizle kutuları.
// ============================================================================
using System.Text.RegularExpressions;
using AracKiralama.Modeller;

namespace AracKiralama.Yardimcilar;

/// <summary>Bir hazır özellik. Secenekler "|" ile ayrılır (sadece Seçim listesi için).</summary>
public record OzellikSablonu(string Kod, string Ad, OzellikTuru Tur, string Ikon, string Aciklama,
    string? Birim = null, string? Secenekler = null, bool KarttaGoster = false);

public static partial class OzellikSablonlari
{
    public static readonly OzellikSablonu[] Liste =
    [
        // ---- Konfor ve teknoloji (Var / Yok) ----
        new("klima", "Klima", OzellikTuru.EvetHayir, "bi-snow", "Araçta klima var mı?", KarttaGoster: true),
        new("navigasyon", "Navigasyon", OzellikTuru.EvetHayir, "bi-geo-alt", "Dahili navigasyon sistemi"),
        new("carplay", "Apple CarPlay / Android Auto", OzellikTuru.EvetHayir, "bi-phone", "Telefonu ekrana bağlama"),
        new("bluetooth", "Bluetooth", OzellikTuru.EvetHayir, "bi-bluetooth", "Kablosuz ses ve arama"),
        new("geri-kamera", "Geri görüş kamerası", OzellikTuru.EvetHayir, "bi-camera-video", "Geri manevrada görüntü"),
        new("park-sensoru", "Park sensörü", OzellikTuru.EvetHayir, "bi-broadcast", "Engel yaklaşınca sesli uyarı"),
        new("hiz-sabitleyici", "Hız sabitleyici", OzellikTuru.EvetHayir, "bi-speedometer2", "Uzun yolda sabit hız"),
        new("cam-tavan", "Cam tavan", OzellikTuru.EvetHayir, "bi-brightness-high", "Açılır ya da sabit cam tavan"),
        new("isitmali-koltuk", "Isıtmalı koltuk", OzellikTuru.EvetHayir, "bi-thermometer-sun", "Kışın ısıtmalı ön koltuklar"),
        new("usb", "USB şarj", OzellikTuru.EvetHayir, "bi-usb-plug", "Telefon şarjı için USB girişi"),
        new("evcil-hayvan", "Evcil hayvan kabul edilir", OzellikTuru.EvetHayir, "bi-heart", "Evcil hayvanla yolculuğa uygun"),
        new("cocuk-koltugu-baglanti", "ISOFIX çocuk koltuğu bağlantısı", OzellikTuru.EvetHayir, "bi-shield-check", "Çocuk koltuğu güvenli takılır"),

        // ---- Ölçüler (Sayı → en az / en çok filtresi) ----
        new("bagaj", "Bagaj hacmi", OzellikTuru.Sayi, "bi-suitcase", "Bagaja ne kadar eşya sığar", Birim: "lt", KarttaGoster: true),
        new("kapi", "Kapı sayısı", OzellikTuru.Sayi, "bi-door-closed", "2, 3, 4 ya da 5 kapı", Birim: "kapı"),
        new("motor-hacmi", "Motor hacmi", OzellikTuru.Sayi, "bi-gear-wide-connected", "Motorun silindir hacmi", Birim: "cc"),
        new("beygir", "Motor gücü", OzellikTuru.Sayi, "bi-lightning-charge", "Beygir gücü", Birim: "hp"),
        new("tuketim", "Ortalama yakıt tüketimi", OzellikTuru.Sayi, "bi-fuel-pump", "100 km'de harcanan yakıt", Birim: "lt/100km"),
        new("menzil", "Elektrikli menzil", OzellikTuru.Sayi, "bi-battery-charging", "Tam şarjla gidilen yol", Birim: "km"),

        // ---- Seçim listeleri ----
        new("kasa", "Kasa tipi", OzellikTuru.Secim, "bi-car-front", "Aracın gövde şekli", Secenekler: "Sedan|Hatchback|SUV|Station wagon|Minivan|Pick-up|Panelvan", KarttaGoster: true),
        new("cekis", "Çekiş", OzellikTuru.Secim, "bi-diagram-3", "Hangi tekerlekler güç alır", Secenekler: "Önden çekiş|Arkadan itiş|4x4"),
        new("lastik", "Lastik tipi", OzellikTuru.Secim, "bi-circle", "Mevsime uygun lastik", Secenekler: "Yaz|Kış|4 mevsim"),
        new("koltuk-dosemesi", "Koltuk döşemesi", OzellikTuru.Secim, "bi-person-workspace", "Kumaş, deri ya da yarı deri", Secenekler: "Kumaş|Yarı deri|Deri"),

        // ---- Serbest yazı ----
        new("multimedya", "Multimedya sistemi", OzellikTuru.Yazi, "bi-music-note-beamed", "Ekran ve ses sistemi (marka / boyut)"),
    ];

    public static OzellikSablonu? Bul(string? kod) => Liste.FirstOrDefault(s => s.Kod == kod);

    // ---- İkon listesi: bootstrap-icons.min.css içindeki ".bi-xxx::before" adları ----
    private static List<string>? _ikonlar;

    public static List<string> Ikonlar(string wwwroot)
    {
        if (_ikonlar != null) return _ikonlar;
        var css = File.ReadAllText(Path.Combine(wwwroot, "lib", "bootstrap-icons", "bootstrap-icons.min.css"));
        _ikonlar = IkonDeseni().Matches(css).Select(m => "bi-" + m.Groups[1].Value).Distinct().Order().ToList();
        return _ikonlar;
    }

    [GeneratedRegex(@"\.bi-([a-z0-9-]+)::before")]
    private static partial Regex IkonDeseni();
}
