// ============================================================================
// TASLAK SERVİSİ (Site Düzenleyici'nin kaydedilmemiş değişiklikleri)
// Düzenleyicide bir kutuya yazdıkça, yazılanlar burada "taslak" olarak durur.
// Sağdaki önizleme (?onizleme=1) sayfayı bu taslakla çizer; gerçek site ise
// "Kaydet"e basılana kadar değişmez.
//
// Taslaklar veritabanına yazılmaz, sadece bellekte tutulur ve her kullanıcının
// kendine aittir (iki kişi aynı anda düzenlese birbirini etkilemez).
// Program.cs'te Singleton olarak kayıtlı: uygulama boyunca tek bir örnek vardır.
// ============================================================================
using System.Collections.Concurrent;

namespace AracKiralama.Servisler;

/// <summary>Bir kullanıcının kaydedilmemiş değişiklikleri.</summary>
public class Taslak
{
    /// <summary>Metin anahtarı → yeni yazı (örn. "anasayfa.baslik" → "Yola çık,")</summary>
    public Dictionary<string, string> Metinler { get; } = new();

    /// <summary>Ayar adı → yeni değer (örn. "AnaRenk" → "#e8590c")</summary>
    public Dictionary<string, string> Ayarlar { get; } = new();
}

public class TaslakServisi
{
    // ConcurrentDictionary: aynı anda birden çok istek gelse de güvenle okunup yazılabilen sözlük.
    private readonly ConcurrentDictionary<string, Taslak> _taslaklar = new();

    /// <summary>Kullanıcının taslağı (yoksa null).</summary>
    public Taslak? Getir(string kullaniciId) => _taslaklar.TryGetValue(kullaniciId, out var taslak) ? taslak : null;

    /// <summary>Kullanıcının taslağını yenisiyle değiştirir.</summary>
    public void Kaydet(string kullaniciId, Taslak taslak) => _taslaklar[kullaniciId] = taslak;

    /// <summary>Kullanıcının taslağını siler ("Kaydet" ya da "Vazgeç" sonrası).</summary>
    public void Sil(string kullaniciId) => _taslaklar.TryRemove(kullaniciId, out _);
}
