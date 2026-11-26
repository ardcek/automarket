// ============================================================================
// ARAÇ KATALOĞU: her modelin teknik bilgileri (motor hacmi, beygir, bagaj, kasa tipi...).
// Bu bilgiler sitede GÖRÜNMEZ; Yönetim → Filtreler ve Özellikler'den hazır bir özellik
// (örn. "Motor hacmi") eklendiği anda bütün araçlara buradan otomatik yazılır ve filtre
// araç listesinde hemen çalışır. Panelden yeni araç eklenince de aynı şekilde doldurulur.
//
// Yeni model eklemek = Modeller sözlüğüne bir satır ("Marka Model" → bilgiler).
// Katalogda olmayan araçlarda değer boş kalır; Değerler ekranından elle girilir.
// Var / Yok özellikleri (klima, park sensörü...) aracın yılına ve sınıfına göre tahmin edilir.
// ============================================================================
using System.Globalization;
using AracKiralama.Modeller;
using AracKiralama.Veri;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Yardimcilar;

/// <summary>Bir modelin teknik bilgisi. Elektrikli araçta MotorHacmi ve Tuketim boş, Menzil dolu.</summary>
public record ModelBilgisi(int? MotorHacmi, int Beygir, int Bagaj, int Kapi, string Kasa, string Cekis, decimal? Tuketim, int? Menzil = null);

public static class AracKatalogu
{
    public static readonly Dictionary<string, ModelBilgisi> Modeller = new(StringComparer.OrdinalIgnoreCase)
    {
        //                         cc    hp   bagaj kapı  kasa              çekiş           lt/100km
        ["Audi A3"]           = new(1498, 150, 380, 5, "Hatchback",      "Önden çekiş", 5.6m),
        ["Audi A4"]           = new(1968, 163, 460, 4, "Sedan",          "Önden çekiş", 4.9m),
        ["Audi A5"]           = new(1984, 204, 450, 2, "Coupe",          "Önden çekiş", 6.4m),
        ["Audi A6"]           = new(1968, 204, 520, 4, "Sedan",          "Önden çekiş", 5.0m),
        ["Audi A7"]           = new(2995, 340, 535, 5, "Coupe",          "4x4",         8.1m),
        ["Audi Q3"]           = new(1498, 150, 530, 5, "SUV",            "Önden çekiş", 6.5m),
        ["Audi Q5"]           = new(1968, 204, 520, 5, "SUV",            "4x4",         5.9m),
        ["Audi Q7"]           = new(2967, 286, 865, 5, "SUV",            "4x4",         7.6m),
        ["Fiat 500L"]         = new(1368,  95, 400, 5, "Minivan",        "Önden çekiş", 6.4m),
        ["Fiat Doblo"]        = new(1598, 120, 790, 5, "Minivan",        "Önden çekiş", 5.0m),
        ["Fiat Egea"]         = new(1598, 130, 520, 4, "Sedan",          "Önden çekiş", 4.2m),
        ["Fiat Fiorino"]      = new(1248,  95, 2500, 4, "Panelvan",      "Önden çekiş", 4.5m),
        ["Fiat Linea"]        = new(1368,  77, 500, 4, "Sedan",          "Önden çekiş", 8.5m),
        ["Fiat Palio"]        = new(1368,  77, 280, 5, "Hatchback",      "Önden çekiş", 6.5m),
        ["Ford Fiesta"]       = new( 999, 100, 292, 5, "Hatchback",      "Önden çekiş", 5.3m),
        ["Ford Focus"]        = new(1499, 120, 511, 4, "Sedan",          "Önden çekiş", 4.5m),
        ["Ford Mondeo"]       = new(1999, 187, 383, 4, "Sedan",          "Önden çekiş", 4.6m),
        ["Ford Tourneo Courier"] = new(1499, 100, 708, 5, "Minivan",     "Önden çekiş", 4.5m),
        ["Ford Transit"]      = new(1995, 130, 1000, 4, "Minibüs",       "Önden çekiş", 7.5m),
        ["Hyundai Accent"]    = new(1399,  97, 390, 4, "Sedan",          "Önden çekiş", 8.5m),
        ["Hyundai Accent Blue"] = new(1582, 136, 465, 4, "Sedan",        "Önden çekiş", 4.9m),
        ["Hyundai Bayon"]     = new( 998, 100, 411, 5, "SUV",            "Önden çekiş", 5.4m),
        ["Hyundai Tucson"]    = new(1598, 230, 616, 5, "SUV",            "Önden çekiş", 5.9m),
        ["Hyundai i20"]       = new( 998, 100, 352, 5, "Hatchback",      "Önden çekiş", 5.3m),
        ["Hyundai i20N"]      = new(1598, 204, 301, 5, "Hatchback",      "Önden çekiş", 7.0m),
        ["Hyundai i30"]       = new(1482, 160, 395, 5, "Hatchback",      "Önden çekiş", 6.0m),
        ["Peugeot 206"]       = new(1360,  75, 245, 5, "Hatchback",      "Önden çekiş", 6.8m),
        ["Peugeot 208"]       = new(null, 136, 311, 5, "Hatchback",      "Önden çekiş", null, Menzil: 362),
        ["Peugeot 3008"]      = new(1499, 130, 520, 5, "SUV",            "Önden çekiş", 5.0m),
        ["Peugeot 301"]       = new(1499, 100, 506, 4, "Sedan",          "Önden çekiş", 4.2m),
        ["Peugeot 307"]       = new(1587, 110, 341, 5, "Hatchback",      "Önden çekiş", 7.5m),
        ["Peugeot 308"]       = new(1199, 130, 412, 5, "Hatchback",      "Önden çekiş", 5.6m),
        ["Peugeot 5008"]      = new(1499, 130, 780, 5, "SUV",            "Önden çekiş", 5.2m),
        ["Renault Broadway"]  = new(1390,  75, 450, 4, "Sedan",          "Önden çekiş", 9.0m),
        ["Renault Clio"]      = new( 999,  90, 391, 5, "Hatchback",      "Önden çekiş", 5.3m),
        ["Renault Express"]   = new(1461,  95, 775, 5, "Minivan",        "Önden çekiş", 4.9m),
        ["Renault Megane"]    = new(1461, 115, 503, 4, "Sedan",          "Önden çekiş", 4.4m),
        ["Renault Symbol"]    = new(1461,  90, 510, 4, "Sedan",          "Önden çekiş", 4.0m),
        ["Renault Talisman"]  = new(1598, 160, 608, 4, "Sedan",          "Önden çekiş", 4.6m),
        ["Volkswagen Arteon"] = new(1984, 190, 563, 5, "Sedan",          "Önden çekiş", 6.6m),
        ["Volkswagen Caddy"]  = new(1968, 122, 750, 5, "Minivan",        "Önden çekiş", 5.4m),
        ["Volkswagen Golf"]   = new(1498, 150, 381, 5, "Hatchback",      "Önden çekiş", 5.6m),
        ["Volkswagen Jetta"]  = new(1395, 150, 510, 4, "Sedan",          "Önden çekiş", 5.9m),
        ["Volkswagen Passat"] = new(1968, 150, 586, 4, "Sedan",          "Önden çekiş", 4.6m),
        ["Volkswagen Polo"]   = new( 999,  80, 351, 5, "Hatchback",      "Önden çekiş", 5.4m),
        ["Volkswagen Tiguan"] = new(1968, 150, 615, 5, "SUV",            "Önden çekiş", 5.7m),
        ["Volkswagen Transporter"] = new(1968, 150, 1200, 4, "Minibüs",  "Önden çekiş", 7.4m),
    };

    /// <summary>Katalogdan doldurulabilen hazır özellikler (kod → kısa açıklama). Diğerleri işletmenin kararıdır (evcil hayvan, lastik...).</summary>
    public static bool Doldurabilir(string? sablonKod) => sablonKod is not null && sablonKod is not ("evcil-hayvan" or "lastik" or "multimedya");

    /// <summary>Özelliğin hazır şablon kodu; eskiden eklenmiş (kodu boş) özellik şablonla aynı addaysa onu kullanır.</summary>
    public static string? SablonKodu(AracOzelligi o)
        => o.SablonKod ?? OzellikSablonlari.Liste.FirstOrDefault(s => string.Equals(s.Ad, o.Ad.Trim(), StringComparison.CurrentCultureIgnoreCase))?.Kod;

    public static ModelBilgisi? Bul(Arac a) => Modeller.GetValueOrDefault($"{a.Marka?.Ad} {a.Model}".Trim());

    /// <summary>
    /// Hazır özelliğin bu araçtaki değeri, özellik değeri biçiminde ("1598", "true", "Sedan").
    /// Bilinmiyorsa ya da araca uymuyorsa (benzinli araçta menzil gibi) null. Arac.Marka, Kategori ve YakitTuru yüklü olmalı.
    /// </summary>
    public static string? Deger(string sablonKod, Arac a)
    {
        var m = Bul(a);
        var sinif = a.Kategori?.Ad ?? "";
        bool luks = sinif == "Lüks", suv = sinif == "SUV", orta = sinif == "Orta Sınıf";
        var yil = a.Yil;
        static string? Var(bool v) => v ? "true" : null;   // Var / Yok özelliğinde "yok" = değer yazılmaz
        static string? Sayi(decimal? v) => v?.ToString(CultureInfo.InvariantCulture);

        return sablonKod switch
        {
            "motor-hacmi" => Sayi(m?.MotorHacmi),
            "beygir"      => Sayi(m?.Beygir),
            "bagaj"       => Sayi(m?.Bagaj),
            "kapi"        => Sayi(m?.Kapi),
            "tuketim"     => Sayi(m?.Tuketim),
            "menzil"      => Sayi(m?.Menzil),
            "kasa"        => m?.Kasa,
            "cekis"       => m?.Cekis,

            "klima"           => "true",
            "bluetooth"       => Var(yil >= 2013),
            "usb"             => Var(yil >= 2012),
            "carplay"         => Var(yil >= 2019 && m?.Kasa != "Panelvan"),
            "navigasyon"      => Var(luks || suv || (orta && yil >= 2022)),
            "geri-kamera"     => Var(luks || suv || (orta && yil >= 2020) || yil >= 2023 || m?.Kasa == "Minibüs"),
            "park-sensoru"    => Var(luks || yil >= 2017),
            "hiz-sabitleyici" => Var(luks || (yil >= 2015 && sinif != "Ekonomi") || yil >= 2020),
            "cam-tavan"       => Var((luks && a.Model != "A4") || a.Model == "Tucson"),
            "isitmali-koltuk" => Var(luks || (suv && yil >= 2021)),
            "cocuk-koltugu-baglanti" => Var(yil >= 2012 && m?.Kasa != "Panelvan"),
            "koltuk-dosemesi" => luks ? "Deri" : (suv || orta) && yil >= 2022 ? "Yarı deri" : "Kumaş",
            _ => null,
        };
    }

    /// <summary>
    /// Bu hazır özelliğin değerlerini katalogdan araçlara yazar. sadeceBoslar = true ise elle girilmiş değerlere dokunmaz.
    /// Döner: kaç araç dolduruldu, kaç araç için bilgi yok. (Değişiklikler kaydedilir.)
    /// </summary>
    public static async Task<(int Doldurulan, int Bilinmeyen)> DoldurAsync(VeriTabani vt, AracOzelligi ozellik, bool sadeceBoslar = true, int? sadeceAracId = null)
    {
        var kod = SablonKodu(ozellik);
        if (!Doldurabilir(kod)) return (0, 0);
        var araclar = await vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori).Include(a => a.YakitTuru)
            .Where(a => a.Durum != AracDurumu.Pasif && (sadeceAracId == null || a.Id == sadeceAracId)).ToListAsync();
        var mevcut = await vt.AracOzellikDegerleri.Where(d => d.OzellikId == ozellik.Id).ToDictionaryAsync(d => d.AracId);
        int dolan = 0, bilinmeyen = 0;
        foreach (var arac in araclar)
        {
            mevcut.TryGetValue(arac.Id, out var kayit);
            if (sadeceBoslar && kayit != null) continue;
            var deger = AracOzellikDegeri.Temizle(ozellik, Deger(kod!, arac));
            if (ozellik.Tur == OzellikTuru.Secim && deger != null && !ozellik.SecenekListesi.Contains(deger)) deger = null;   // seçenek silinmişse yazma
            if (deger == null)
            {
                // Var / Yok'ta "yok" da bir bilgidir; bilinmeyen sadece katalogda olmayan model
                if (Bul(arac) == null) bilinmeyen++;
                continue;
            }
            if (kayit == null) vt.AracOzellikDegerleri.Add(kayit = new AracOzellikDegeri { AracId = arac.Id, OzellikId = ozellik.Id });
            kayit.Deger = deger;
            kayit.SayiDeger = ozellik.Tur == OzellikTuru.Sayi ? decimal.Parse(deger, CultureInfo.InvariantCulture) : null;
            dolan++;
        }
        await vt.SaveChangesAsync();
        return (dolan, bilinmeyen);
    }
}
