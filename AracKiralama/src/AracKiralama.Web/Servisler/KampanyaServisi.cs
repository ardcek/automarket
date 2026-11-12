// ============================================================================
// KAMPANYA SERVİSİ: hangi araca, hangi müşteriye, hangi indirimin uygulanacağına karar verir.
// Kural TEK YERDE durur; araç kartı, "sadece indirimli araçlar" filtresi, canlı fiyat
// ve rezervasyon hep buradaki KampanyaBaglami.Durum(...) metodunu kullanır.
//
// Bir kampanya bir araç için üç sonuçtan birini verir:
//   Uyar   → bütün koşullar sağlandı, indirim uygulanır (kartta üstü çizili eski fiyat)
//   Olasi  → araç uyuyor ama müşteri bilgisi seçilmemiş ya da gün sayısı yetmiyor
//            (kartta sadece rozet görünür: "Öğretmenlere %15")
//   Uymaz  → araç, tarih ya da müşterinin seçtiği bilgi tutmuyor
// Birden fazla kampanya uyarsa oranı en yüksek olan seçilir (müşteriye en avantajlı).
//
// Müşteri bilgileri nereden gelir?
//   • Giriş yapmış MÜŞTERİ → profilindeki cevaplar (Yönetim → Müşteri Bilgileri)
//   • Ziyaretçi / personel  → araç listesindeki "Mesleğiniz" gibi seçimler (adres: ?Mu[1]=Öğretmen)
// ============================================================================
using System.Globalization;
using System.Security.Claims;
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

/// <summary>Bir aracın kampanya durumu (kart ve filtre için).</summary>
public class KampanyaDurumu
{
    /// <summary>Bu müşteri ve süre için uygulanan en avantajlı kampanya (yoksa null).</summary>
    public Kampanya? Uygulanan { get; init; }

    /// <summary>Uygulanabilecek ama henüz koşulu tamamlanmamış en avantajlı kampanya (sadece rozet olarak gösterilir).</summary>
    public Kampanya? Olasi { get; init; }

    /// <summary>"Sadece indirimli araçlar" filtresinde bu araç çıksın mı?</summary>
    public bool IndirimliMi => Uygulanan != null || Olasi != null;
}

/// <summary>
/// Bir sayfa açılışı için hazırlanmış kampanya bilgisi: aktif kampanyalar + müşterinin cevapları.
/// Veritabanına gitmez, sadece karar verir → birim testi kolaydır (tests/.../KampanyaTestleri.cs).
/// </summary>
public class KampanyaBaglami
{
    private enum Sonuc { Uymaz, Olasi, Uyar }

    public KampanyaBaglami(List<Kampanya> kampanyalar, Dictionary<int, string> musteriDegerleri, DateTime tarih, bool profildenMi = false)
    {
        Kampanyalar = kampanyalar;
        MusteriDegerleri = musteriDegerleri;
        Tarih = tarih;
        ProfildenMi = profildenMi;
    }

    /// <summary>Yayındaki kampanyalar.</summary>
    public List<Kampanya> Kampanyalar { get; }

    /// <summary>Müşterinin cevapları: alan numarası → cevap (örn. 1 → "Öğretmen").</summary>
    public Dictionary<int, string> MusteriDegerleri { get; }

    /// <summary>Tarih koşulu için kullanılan gün (seçilen alış tarihi ya da bugün).</summary>
    public DateTime Tarih { get; }

    /// <summary>Cevaplar giriş yapmış müşterinin profilinden mi geldi? (Öyleyse filtredeki seçim kutusu kilitlenir.)</summary>
    public bool ProfildenMi { get; }

    /// <summary>Hiç kampanya olmayan durum (Render'da ya da kampanya eklenmemişken).</summary>
    public static KampanyaBaglami Bos { get; } = new(new(), new(), DateTime.Today);

    /// <summary>Aracın kampanya durumu. gunSayisi bilinmiyorsa (tarih seçilmediyse) null verilir.</summary>
    public KampanyaDurumu Durum(Arac arac, int? gunSayisi = null, DateTime? alisTarihi = null)
    {
        Kampanya? uygulanan = null, olasi = null;
        foreach (var kampanya in Kampanyalar)
        {
            var sonuc = Degerlendir(kampanya, arac, gunSayisi, alisTarihi ?? Tarih);
            if (sonuc == Sonuc.Uyar && (uygulanan == null || kampanya.IndirimOrani > uygulanan.IndirimOrani)) uygulanan = kampanya;
            if (sonuc == Sonuc.Olasi && (olasi == null || kampanya.IndirimOrani > olasi.IndirimOrani)) olasi = kampanya;
        }
        // Zaten bir indirim uygulanıyorsa daha düşük "olası" kampanyayı göstermeye gerek yok.
        if (uygulanan != null && olasi != null && olasi.IndirimOrani <= uygulanan.IndirimOrani) olasi = null;
        return new KampanyaDurumu { Uygulanan = uygulanan, Olasi = olasi };
    }

    /// <summary>Tek bir kampanyanın koşullarını sırayla kontrol eder.</summary>
    private Sonuc Degerlendir(Kampanya k, Arac arac, int? gunSayisi, DateTime tarih)
    {
        // 1) Tarih aralığı: alış tarihi kampanya tarihlerinin dışındaysa hiç uymaz
        if (k.BaslangicTarihi != null && tarih.Date < k.BaslangicTarihi.Value.Date) return Sonuc.Uymaz;
        if (k.BitisTarihi != null && tarih.Date > k.BitisTarihi.Value.Date) return Sonuc.Uymaz;

        // 2) Araç koşulları: kategori, marka, panelden eklenen özellik
        if (k.KategoriId != null && arac.KategoriId != k.KategoriId) return Sonuc.Uymaz;
        if (k.MarkaId != null && arac.MarkaId != k.MarkaId) return Sonuc.Uymaz;
        if (k.AracOzellikId != null && !arac.OzellikDegerleri.Any(d => d.OzellikId == k.AracOzellikId && Ayni(d.Deger, k.AracOzellikDegeri)))
            return Sonuc.Uymaz;

        var sonuc = Sonuc.Uyar;

        // 3) Müşteri koşulu: cevap seçilmemişse "olası", farklı cevap seçilmişse uymaz
        if (k.MusteriAlanId != null)
        {
            if (!MusteriDegerleri.TryGetValue(k.MusteriAlanId.Value, out var cevap)) sonuc = Sonuc.Olasi;
            else if (!Ayni(cevap, k.MusteriDegeri)) return Sonuc.Uymaz;
        }

        // 4) En az gün: süre bilinmiyorsa ya da yetmiyorsa "olası" (müşteri süreyi uzatabilir)
        if (k.EnAzGun != null && (gunSayisi == null || gunSayisi < k.EnAzGun)) sonuc = Sonuc.Olasi;

        return sonuc;
    }

    /// <summary>Büyük-küçük harf ve baştaki/sondaki boşluk farkını önemsemeden karşılaştırır (Türkçe kurallarıyla).</summary>
    private static bool Ayni(string? a, string? b)
        => string.Compare(a?.Trim(), b?.Trim(), Bicim.Tr, CompareOptions.IgnoreCase) == 0;
}

public class KampanyaServisi
{
    private readonly VeriTabani _vt;

    public KampanyaServisi(VeriTabani vt)
    {
        _vt = vt;
    }

    /// <summary>
    /// Bu istek için kampanya bilgisini hazırlar.
    ///   kullanici : giriş yapan kişi (müşteriyse profilindeki cevaplar kullanılır)
    ///   secimler  : ziyaretçinin ekranda seçtikleri (?Mu[1]=Öğretmen)
    ///   tarih     : seçilen alış tarihi (seçilmediyse bugün)
    /// </summary>
    public async Task<KampanyaBaglami> BaglamAsync(ClaimsPrincipal? kullanici, Dictionary<int, string?>? secimler = null, DateTime? tarih = null)
    {
        // SQL: SELECT * FROM "Kampanyalar" WHERE "Aktif" ORDER BY "Sira"
        var kampanyalar = await _vt.Kampanyalar.Where(k => k.Aktif).OrderBy(k => k.Sira).ThenBy(k => k.Id).AsNoTracking().ToListAsync();
        if (kampanyalar.Count == 0) return new KampanyaBaglami(kampanyalar, new(), tarih ?? DateTime.Today);

        var musteriId = kullanici?.IsInRole(Roller.Musteri) == true ? kullanici.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        if (musteriId != null)
        {
            // Giriş yapmış müşteri: sadece profilindeki cevaplar geçerli (rezervasyonda da aynısı kullanılır)
            // SQL: SELECT "AlanId", "Deger" FROM "MusteriAlanDegerleri" WHERE "KullaniciId" = @id
            var profil = await _vt.MusteriAlanDegerleri.Where(d => d.KullaniciId == musteriId).ToDictionaryAsync(d => d.AlanId, d => d.Deger);
            return new KampanyaBaglami(kampanyalar, profil, tarih ?? DateTime.Today, profildenMi: true);
        }

        // Ziyaretçi ya da personel: ekranda seçilenler
        var degerler = (secimler ?? new())
            .Where(s => !string.IsNullOrWhiteSpace(s.Value))
            .ToDictionary(s => s.Key, s => s.Value!.Trim());
        return new KampanyaBaglami(kampanyalar, degerler, tarih ?? DateTime.Today);
    }

    /// <summary>Rezervasyon için: sadece müşterinin profilindeki cevaplar kullanılır (ekrandaki seçim değil).</summary>
    public async Task<KampanyaBaglami> MusteriIcinAsync(string musteriId, DateTime alisTarihi)
    {
        var kampanyalar = await _vt.Kampanyalar.Where(k => k.Aktif).OrderBy(k => k.Sira).ThenBy(k => k.Id).AsNoTracking().ToListAsync();
        var profil = kampanyalar.Count == 0 ? new()
            : await _vt.MusteriAlanDegerleri.Where(d => d.KullaniciId == musteriId).ToDictionaryAsync(d => d.AlanId, d => d.Deger);
        return new KampanyaBaglami(kampanyalar, profil, alisTarihi, profildenMi: true);
    }

    /// <summary>
    /// Araç listesinde "Mesleğiniz" gibi sorulacak müşteri bilgileri: yayındaki bir kampanyada koşul olarak geçenler.
    /// SQL: SELECT * FROM "MusteriAlanlari" WHERE "Aktif" AND "Id" IN (kampanyalardaki alanlar) ORDER BY "Sira"
    /// </summary>
    public async Task<List<MusteriAlani>> FiltreAlanlariAsync(KampanyaBaglami baglam)
    {
        var alanIdleri = baglam.Kampanyalar.Where(k => k.MusteriAlanId != null).Select(k => k.MusteriAlanId!.Value).Distinct().ToList();
        if (alanIdleri.Count == 0) return new();
        return await _vt.MusteriAlanlari.Where(a => a.Aktif && alanIdleri.Contains(a.Id)).OrderBy(a => a.Sira).ThenBy(a => a.Ad).AsNoTracking().ToListAsync();
    }
}
