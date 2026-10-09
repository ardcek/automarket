// ============================================================================
// KÜÇÜK YARDIMCILAR: roller, iş kuralı hatası, sayfalama
// ============================================================================
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Yardimcilar;

/// <summary>Rol adları. Kullanım: [Authorize(Roles = Roller.Admin)]</summary>
public static class Roller
{
    public const string Admin = "Admin";
    public const string Personel = "Personel";
    public const string Musteri = "Musteri";

    /// <summary>Yönetim paneline girebilenler.</summary>
    public const string Yonetim = Admin + "," + Personel;
}

/// <summary>
/// Kurala aykırı bir işlem (ör. "Araç bu tarihlerde dolu").
/// Mesaj doğrudan kullanıcıya gösterilir, bu yüzden her zaman anlaşılır Türkçe yazın.
/// </summary>
public class IsKuraliHatasi : Exception
{
    public IsKuraliHatasi(string mesaj) : base(mesaj) { }
}

/// <summary>Uzun listeleri sayfalara böler. Görünümü: Views/Shared/_Sayfalama.cshtml</summary>
public class SayfaListesi<T>
{
    public List<T> Kayitlar { get; set; } = new();
    public int Sayfa { get; set; }
    public int ToplamKayit { get; set; }
    public int SayfaBoyutu { get; set; }
    public int ToplamSayfa => (int)Math.Ceiling(ToplamKayit / (double)SayfaBoyutu);

    public static async Task<SayfaListesi<T>> OlusturAsync(IQueryable<T> sorgu, int sayfa, int sayfaBoyutu)
    {
        var toplam = await sorgu.CountAsync();
        var sonSayfa = Math.Max(1, (int)Math.Ceiling(toplam / (double)sayfaBoyutu));
        sayfa = Math.Clamp(sayfa, 1, sonSayfa);

        return new SayfaListesi<T>
        {
            Kayitlar = await sorgu.Skip((sayfa - 1) * sayfaBoyutu).Take(sayfaBoyutu).ToListAsync(),
            Sayfa = sayfa,
            ToplamKayit = toplam,
            SayfaBoyutu = sayfaBoyutu,
        };
    }
}
