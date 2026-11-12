// ============================================================================
// KAMPANYA VE MÜŞTERİ BİLGİSİ FORMLARI (Yönetim → Kampanyalar / Müşteri Bilgileri)
// Form sınıfı ekranda kolay doldurulacak biçimdedir (örn. indirim "15" yazılır),
// Aktar() metodu veritabanındaki biçime çevirir (0.15).
// ============================================================================
using System.ComponentModel.DataAnnotations;
using AracKiralama.Modeller;

namespace AracKiralama.FormModelleri;

/// <summary>Müşteri bilgisi ekleme / düzenleme formu (örn. "Meslek").</summary>
public class MusteriAlaniFormu
{
    public int? Id { get; set; }
    [Display(Name = "Bilgi adı"), Required(ErrorMessage = "Bilgi adı zorunludur."), MaxLength(60)] public string Ad { get; set; } = "";
    [Display(Name = "Türü")] public OzellikTuru Tur { get; set; } = OzellikTuru.Secim;
    [Display(Name = "Seçenekler (her satıra bir tane)")] public string? SeceneklerMetni { get; set; }
    [Display(Name = "Kayıt formunda sor")] public bool KayittaGoster { get; set; } = true;
    [Display(Name = "Zorunlu")] public bool Zorunlu { get; set; }
    [Display(Name = "Sıra (küçük olan önce gelir)")] public int Sira { get; set; }
    [Display(Name = "Yayında")] public bool Aktif { get; set; } = true;

    /// <summary>Seçenek kutusundaki satırlar (boşlar atılır, aynı olanlar bir kez yazılır).</summary>
    public List<string> SecenekSatirlari() => (SeceneklerMetni ?? "")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(s => s.Replace("|", "/")).Distinct().ToList();

    public void Aktar(MusteriAlani alan)
    {
        alan.Ad = Ad.Trim();
        alan.Tur = Tur;
        alan.Secenekler = Tur == OzellikTuru.Secim ? string.Join("|", SecenekSatirlari()) : null;
        alan.KayittaGoster = KayittaGoster;
        alan.Zorunlu = Zorunlu;
        alan.Sira = Sira;
        alan.Aktif = Aktif;
    }

    public static MusteriAlaniFormu Olustur(MusteriAlani a) => new()
    {
        Id = a.Id, Ad = a.Ad, Tur = a.Tur, SeceneklerMetni = string.Join("\n", a.SecenekListesi),
        KayittaGoster = a.KayittaGoster, Zorunlu = a.Zorunlu, Sira = a.Sira, Aktif = a.Aktif,
    };
}

/// <summary>Kampanya ekleme / düzenleme formu.</summary>
public class KampanyaFormu
{
    public int? Id { get; set; }
    [Display(Name = "Kampanya adı"), Required(ErrorMessage = "Kampanya adı zorunludur."), MaxLength(80)] public string Ad { get; set; } = "";
    [Display(Name = "Rozet yazısı (kartta görünür)"), MaxLength(60)] public string? RozetYazisi { get; set; }

    [Display(Name = "İndirim (%)"), Range(1, 90, ErrorMessage = "İndirim %1 ile %90 arasında olmalıdır.")]
    public decimal IndirimYuzdesi { get; set; } = 10;

    // Koşullar. Açılır listelerde "numara|değer" biçiminde gelir: "3|Öğretmen" (3 = alanın / özelliğin numarası)
    [Display(Name = "Müşteri bilgisi")] public string? MusteriKosulu { get; set; }
    [Display(Name = "Kategori")] public int? KategoriId { get; set; }
    [Display(Name = "Marka")] public int? MarkaId { get; set; }
    [Display(Name = "Araç özelliği")] public string? AracKosulu { get; set; }
    [Display(Name = "En az kaç gün"), Range(1, 365, ErrorMessage = "1 ile 365 arasında bir gün girin.")] public int? EnAzGun { get; set; }
    [Display(Name = "Başlangıç tarihi"), DataType(DataType.Date)] public DateTime? BaslangicTarihi { get; set; }
    [Display(Name = "Bitiş tarihi"), DataType(DataType.Date)] public DateTime? BitisTarihi { get; set; }

    [Display(Name = "Yayında")] public bool Aktif { get; set; } = true;
    [Display(Name = "Sıra")] public int Sira { get; set; }

    /// <summary>"3|Öğretmen" → (3, "Öğretmen"). Boş ya da bozuksa (null, null).</summary>
    public static (int? Id, string? Deger) Ayir(string? kosul)
    {
        if (string.IsNullOrWhiteSpace(kosul)) return (null, null);
        var parcalar = kosul.Split('|', 2);
        return parcalar.Length == 2 && int.TryParse(parcalar[0], out var id) && parcalar[1].Length > 0 ? (id, parcalar[1]) : (null, null);
    }

    public void Aktar(Kampanya k)
    {
        k.Ad = Ad.Trim();
        k.RozetYazisi = string.IsNullOrWhiteSpace(RozetYazisi) ? null : RozetYazisi.Trim();
        k.IndirimOrani = Math.Round(IndirimYuzdesi / 100m, 4);   // 15 → 0.15
        (k.MusteriAlanId, k.MusteriDegeri) = Ayir(MusteriKosulu);
        k.KategoriId = KategoriId;
        k.MarkaId = MarkaId;
        (k.AracOzellikId, k.AracOzellikDegeri) = Ayir(AracKosulu);
        k.EnAzGun = EnAzGun;
        k.BaslangicTarihi = BaslangicTarihi?.Date;
        k.BitisTarihi = BitisTarihi?.Date;
        k.Aktif = Aktif;
        k.Sira = Sira;
    }

    public static KampanyaFormu Olustur(Kampanya k) => new()
    {
        Id = k.Id, Ad = k.Ad, RozetYazisi = k.RozetYazisi, IndirimYuzdesi = k.IndirimOrani * 100,
        MusteriKosulu = k.MusteriAlanId == null ? null : $"{k.MusteriAlanId}|{k.MusteriDegeri}",
        KategoriId = k.KategoriId, MarkaId = k.MarkaId,
        AracKosulu = k.AracOzellikId == null ? null : $"{k.AracOzellikId}|{k.AracOzellikDegeri}",
        EnAzGun = k.EnAzGun, BaslangicTarihi = k.BaslangicTarihi, BitisTarihi = k.BitisTarihi, Aktif = k.Aktif, Sira = k.Sira,
    };
}
