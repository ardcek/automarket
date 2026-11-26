// ============================================================================
// GARAJ: ana sayfadaki "sahneye tek tek gelen araçlar" bölümünün araç seçimi
// ----------------------------------------------------------------------------
// Sıra: en çok kiralanan önce. Kiralama kaydı yetmezse (yeni site) öne çıkanlar,
// sonra en gösterişli (günlük fiyatı en yüksek) araçlar tamamlar.
//   - Fotoğrafı olmayan araç girmez (sahnede büyük fotoğraf şart).
//   - Pasif araç girmez.
//   - Aynı marka + model iki kez çıkmaz (filoda üç Clio varsa sahneye bir tanesi gelir).
// Veritabanı yok, test edilebilir: tests/AracKiralama.Tests/GarajTestleri.cs
// Kullanan: Controllers/AnaSayfaController.cs → Views/AnaSayfa/Index.cshtml → "GARAJ"
// Aç / kapa ve araç sayısı: Yönetim → Ayarlar → Ana Sayfa.
// ============================================================================
using AracKiralama.Modeller;

namespace AracKiralama.Yardimcilar;

/// <summary>Garajdaki bir araç ve kaç kez kiralandığı ("37 kez kiralandı" yazısı için).</summary>
public record GarajAraci(Arac Arac, int KiralamaSayisi);

public static class GarajSecimi
{
    /// <summary>Panelde daha büyük sayı girilse bile en fazla bu kadar araç (sayfa çok uzamasın).</summary>
    public const int EnFazla = 8;

    /// <param name="kiralamaSayilari">araç numarası → iptal / ret dışındaki rezervasyon sayısı</param>
    public static List<GarajAraci> Sec(IEnumerable<Arac> araclar, IReadOnlyDictionary<int, int> kiralamaSayilari, int adet)
    {
        adet = Math.Clamp(adet, 0, EnFazla);
        return araclar
            .Where(a => a.Durum != AracDurumu.Pasif && !string.IsNullOrWhiteSpace(a.GorselAdresi))
            .Select(a => new GarajAraci(a, kiralamaSayilari.GetValueOrDefault(a.Id)))
            .OrderByDescending(g => g.KiralamaSayisi)
            .ThenByDescending(g => g.Arac.OneCikan)
            .ThenByDescending(g => g.Arac.GunlukFiyat)
            .ThenBy(g => g.Arac.Id)
            .DistinctBy(g => $"{g.Arac.MarkaId}|{g.Arac.Model.Trim()}", StringComparer.OrdinalIgnoreCase)   // ilk (en üstteki) kalır
            .Take(adet)
            .ToList();
    }
}
