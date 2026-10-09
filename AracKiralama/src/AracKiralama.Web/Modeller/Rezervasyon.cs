// ============================================================================
// REZERVASYON (+ seçilen ekstralar, ödemeler, hasarlar)
// Fiyatlar rezervasyon anında KOPYALANIR: aracın fiyatı sonradan değişse de
// eski rezervasyonların tutarı bozulmaz.
// Akış: Onay Bekliyor → Onaylandı → Kirada → Tamamlandı  (Servisler/RezervasyonServisi.cs)
// ============================================================================
using System.ComponentModel.DataAnnotations.Schema;

namespace AracKiralama.Modeller;

public class Rezervasyon
{
    public int Id { get; set; }
    public string Kod { get; set; } = "";            // "RZ-2026-0042"

    public string MusteriId { get; set; } = "";
    public Kullanici? Musteri { get; set; }
    public int AracId { get; set; }
    public Arac? Arac { get; set; }

    public int AlisSubeId { get; set; }
    public Sube? AlisSube { get; set; }
    public int IadeSubeId { get; set; }
    public Sube? IadeSube { get; set; }

    public DateTime BaslangicTarihi { get; set; }    // planlanan alış
    public DateTime BitisTarihi { get; set; }        // planlanan iade
    public DateTime? TeslimTarihi { get; set; }      // araç gerçekten verildiğinde
    public DateTime? IadeTarihi { get; set; }        // araç gerçekten geri geldiğinde

    public RezervasyonDurumu Durum { get; set; } = RezervasyonDurumu.OnayBekliyor;
    public bool Gecikti { get; set; }                // iade tarihi geçti ama araç dönmedi

    // ---- Ücret dökümü (₺) ----
    public int GunSayisi { get; set; }
    public decimal GunlukFiyat { get; set; }
    public decimal KiraTutari { get; set; }          // gün × günlük fiyat
    public decimal Indirim { get; set; }
    public decimal EkstraTutari { get; set; }
    public decimal TekYonUcreti { get; set; }
    public decimal GecikmeUcreti { get; set; }
    public decimal YakitUcreti { get; set; }
    public decimal HasarUcreti { get; set; }
    public decimal IptalUcreti { get; set; }
    public decimal Depozito { get; set; }
    public decimal ToplamTutar { get; set; }

    // ---- Teslim / iade bilgileri ----
    public int? TeslimKm { get; set; }
    public int? IadeKm { get; set; }
    public YakitSeviyesi? TeslimYakit { get; set; }
    public YakitSeviyesi? IadeYakit { get; set; }

    public string? Not { get; set; }
    public string? DurumAciklamasi { get; set; }     // ret / iptal sebebi
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public string? IslemYapanId { get; set; }        // son işlemi yapan personel
    public Kullanici? IslemYapan { get; set; }

    public List<RezervasyonEkstrasi> Ekstralar { get; set; } = new();
    public List<Odeme> Odemeler { get; set; } = new();
    public List<Hasar> Hasarlar { get; set; } = new();

    /// <summary>Tahsil edilen kira + ceza (depozito hareketleri hariç).</summary>
    [NotMapped]
    public decimal OdenenTutar => Odemeler
        .Where(o => o.Tur == OdemeTuru.KiraBedeli || o.Tur == OdemeTuru.Ceza)
        .Sum(o => o.Tutar);

    [NotMapped] public decimal KalanTutar => ToplamTutar - OdenenTutar;

    [NotMapped]
    public bool IptalEdilebilir => Durum == RezervasyonDurumu.OnayBekliyor || Durum == RezervasyonDurumu.Onaylandi;
}

/// <summary>Rezervasyonda seçilen ek hizmet (fiyatı o anki haliyle saklanır).</summary>
public class RezervasyonEkstrasi
{
    public int RezervasyonId { get; set; }
    public Rezervasyon? Rezervasyon { get; set; }
    public int EkstraId { get; set; }
    public Ekstra? Ekstra { get; set; }
    public decimal GunlukFiyat { get; set; }
    public decimal Tutar { get; set; }
}

public class Odeme
{
    public int Id { get; set; }
    public int RezervasyonId { get; set; }
    public Rezervasyon? Rezervasyon { get; set; }
    public decimal Tutar { get; set; }
    public OdemeYontemi Yontem { get; set; }
    public OdemeTuru Tur { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;
    public string? Not { get; set; }
    public string? AlanKullaniciId { get; set; }
    public Kullanici? AlanKullanici { get; set; }
}

/// <summary>İadede tespit edilen hasar.</summary>
public class Hasar
{
    public int Id { get; set; }
    public int RezervasyonId { get; set; }
    public Rezervasyon? Rezervasyon { get; set; }
    public string Aciklama { get; set; } = "";
    public decimal Tutar { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;
}

/// <summary>Bakım kaydı. Tamamlanmamış bakım tarihlerinde araç kiralanamaz.</summary>
public class Bakim
{
    public int Id { get; set; }
    public int AracId { get; set; }
    public Arac? Arac { get; set; }
    public BakimTuru Tur { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public int Kilometre { get; set; }
    public decimal Maliyet { get; set; }
    public string? Aciklama { get; set; }
    public bool Tamamlandi { get; set; }
}
