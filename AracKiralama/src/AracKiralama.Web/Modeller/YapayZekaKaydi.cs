// ============================================================================
// YAPAY ZEKÂ KAYDI (YapayZekaKayitlari tablosu) — Yönetim → Yapay Zekâ Asistanı → Geçmiş
// Asistanın uyguladığı her değişiklik buraya yazılır: kim, ne istedi, ne yapıldı.
// "Geri al" butonu GeriAlmaJson'daki bilgiyle değişikliği tersine çevirir.
//   Tur = "Panel" → veritabanında yapılan değişiklik (kampanya ekle, yazı değiştir...)
//   Tur = "Kod"   → kod dosyalarında yapılan değişiklik (yedekler .yapay-zeka-yedek klasöründe)
// ============================================================================
using System.ComponentModel.DataAnnotations;

namespace AracKiralama.Modeller;

public class YapayZekaKaydi
{
    public int Id { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;

    [MaxLength(256)] public string KullaniciAdi { get; set; } = "";   // değişikliği yapan personel
    [MaxLength(20)] public string Tur { get; set; } = "Panel";         // "Panel" ya da "Kod"

    [MaxLength(2000)] public string Istek { get; set; } = "";         // kullanıcının yazdığı cümle
    [MaxLength(2000)] public string? Anladigim { get; set; }          // modelin isteği nasıl anladığı
    public string Ozet { get; set; } = "";                            // yapılanların okunur listesi

    public string IslemlerJson { get; set; } = "[]";                  // uygulanan işlemler (JSON)
    public string GeriAlmaJson { get; set; } = "[]";                  // geri almak için gereken eski değerler (JSON)

    [MaxLength(200)] public string? ModelAdi { get; set; }            // hangi yapay zekâ modeli
    public double Sure { get; set; }                                  // modelin düşünme süresi (saniye)

    public bool GeriAlindi { get; set; }
    public DateTime? GeriAlmaTarihi { get; set; }
}
