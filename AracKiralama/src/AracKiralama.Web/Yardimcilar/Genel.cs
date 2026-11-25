// ============================================================================
// KÜÇÜK YARDIMCILAR: roller, iş kuralı hatası, sayfalama, PostgreSQL bağlantı biçimi
// ============================================================================
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Yardimcilar;

/// <summary>
/// Rol adları. Kullanım: [Authorize(Roles = Roller.Admin)]
///   SuperAdmin : her şey. Yapay Zekâ Asistanı, Site Düzenleyici, Filtreler ve Özellikler, Kampanyalar,
///                Ayarlar, Yazılar, Sayfalar, Görseller SADECE onda.
///                Panelden başkasına Süper Admin, Admin ya da Personel yetkisi verebilir. İlk süper admin
///                veritabanından (AspNetUserRoles tablosu) verilir: README → 4.1.
///                SuperAdmin aynı zamanda Admin sayılır (Yardimcilar/Altyapi.cs → SuperAdminAyniZamandaAdmin).
///   Admin      : hocanın paneli: operasyon (takvim, rezervasyon, bakım, ödeme, müşteri, rapor), araçlar ve tanımlar,
///                personel. Panelden başkasına sadece Admin / Personel yetkisi verebilir; Süper Admin'lere dokunamaz.
///   Personel   : Admin'in gördüğü her şey, Personel sayfası hariç.
/// </summary>
public static class Roller
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Personel = "Personel";
    public const string Musteri = "Musteri";

    /// <summary>Yönetim paneline girebilenler.</summary>
    public const string Yonetim = SuperAdmin + "," + Admin + "," + Personel;

    /// <summary>
    /// Panelde (Personel ve Roller) kimin hangi yetkiyi verebileceği:
    ///   Süper Admin → Süper Admin, Admin, Personel   ·   Admin → sadece Admin, Personel
    /// Admin, Süper Admin hesaplarına da dokunamaz (KullaniciControllerlari.cs → PersonelController).
    /// </summary>
    public static string[] Verebilecegi(System.Security.Claims.ClaimsPrincipal veren)
        => veren.IsInRole(SuperAdmin) ? [SuperAdmin, Admin, Personel] : [Admin, Personel];

    /// <summary>Ekranda görünen ad ("SuperAdmin" → "Süper Admin").</summary>
    public static string Ad(string rol) => rol == SuperAdmin ? "Süper Admin" : rol;
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

/// <summary>
/// Neon "postgresql://kullanici:sifre@sunucu/veritabani?sslmode=require" biçiminde adres verir;
/// C#'ın PostgreSQL kütüphanesi (Npgsql) ise "Host=...;Database=...;" biçimi ister.
/// Hangisi yapıştırılırsa yapıştırılsın doğru biçime çevirir.
/// </summary>
public static class PostgreBaglantisi
{
    public static string? Duzenle(string? baglanti)
    {
        if (string.IsNullOrWhiteSpace(baglanti)) return null;
        baglanti = baglanti.Trim();
        // Neon'un "Connect" penceresinden komut olarak kopyalanırsa: psql 'postgresql://...' → sadece adres
        if (baglanti.StartsWith("psql ")) baglanti = baglanti[5..].Trim();
        baglanti = baglanti.Trim('\'', '"', ' ');
        if (!baglanti.StartsWith("postgres://") && !baglanti.StartsWith("postgresql://")) return baglanti;   // zaten "Host=..." biçiminde

        var adres = new Uri(baglanti);
        var kullanici = adres.UserInfo.Split(':', 2);
        var parcalar = new List<string>
        {
            $"Host={adres.Host}",
            $"Port={(adres.Port > 0 ? adres.Port : 5432)}",
            $"Database={adres.AbsolutePath.Trim('/')}",
            $"Username={Uri.UnescapeDataString(kullanici[0])}",
            $"Password={(kullanici.Length > 1 ? Uri.UnescapeDataString(kullanici[1]) : "")}",
            "SSL Mode=Require",
        };
        if (adres.Query.Contains("channel_binding=require")) parcalar.Add("Channel Binding=Require");
        return string.Join(";", parcalar);
    }
}
