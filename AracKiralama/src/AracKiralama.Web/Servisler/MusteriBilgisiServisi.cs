// ============================================================================
// MÜŞTERİ BİLGİSİ SERVİSİ: panelden eklenen müşteri bilgilerini (Meslek vb.)
// okur, kontrol eder ve kaydeder. Kayıt formu, profil ve yönetim paneli kullanır.
// Formlarda kutuların adı "Alan[3]" biçimindedir (3 = MusteriAlanlari.Id).
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Veri;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

public class MusteriBilgisiServisi
{
    private readonly VeriTabani _vt;

    public MusteriBilgisiServisi(VeriTabani vt)
    {
        _vt = vt;
    }

    /// <summary>
    /// Yayındaki müşteri bilgileri. sadeceKayit = true ise sadece kayıt formunda sorulacaklar.
    /// SQL: SELECT * FROM "MusteriAlanlari" WHERE "Aktif" [AND "KayittaGoster"] ORDER BY "Sira", "Ad"
    /// </summary>
    public Task<List<MusteriAlani>> AlanlarAsync(bool sadeceKayit = false)
        => _vt.MusteriAlanlari.Where(a => a.Aktif && (!sadeceKayit || a.KayittaGoster))
            .OrderBy(a => a.Sira).ThenBy(a => a.Ad).AsNoTracking().ToListAsync();

    /// <summary>
    /// Bir müşterinin cevapları: alan numarası → cevap.
    /// SQL: SELECT "AlanId", "Deger" FROM "MusteriAlanDegerleri" WHERE "KullaniciId" = @id
    /// </summary>
    public Task<Dictionary<int, string>> DegerlerAsync(string kullaniciId)
        => _vt.MusteriAlanDegerleri.Where(d => d.KullaniciId == kullaniciId).ToDictionaryAsync(d => d.AlanId, d => d.Deger);

    /// <summary>Zorunlu olup boş bırakılan ya da geçersiz cevap verilen alanların hata mesajları.</summary>
    public static List<string> Hatalar(List<MusteriAlani> alanlar, Dictionary<int, string?>? gelen)
    {
        var hatalar = new List<string>();
        foreach (var alan in alanlar)
        {
            var ham = gelen?.GetValueOrDefault(alan.Id);
            var temiz = alan.Temizle(ham);
            if (!string.IsNullOrWhiteSpace(ham) && temiz == null && alan.Tur != OzellikTuru.EvetHayir) hatalar.Add($"{alan.Ad}: geçerli bir değer seçin.");
            else if (alan.Zorunlu && temiz == null && alan.Tur != OzellikTuru.EvetHayir) hatalar.Add($"{alan.Ad} zorunludur.");
        }
        return hatalar;
    }

    /// <summary>
    /// Formdan gelen cevapları kaydeder. Boş bırakılanın kaydı silinir.
    /// Sadece "alanlar" listesindekilere dokunulur (kayıt formunda sorulmayan bir cevap silinmez).
    /// </summary>
    public async Task KaydetAsync(string kullaniciId, List<MusteriAlani> alanlar, Dictionary<int, string?>? gelen)
    {
        var mevcut = await _vt.MusteriAlanDegerleri.Where(d => d.KullaniciId == kullaniciId).ToDictionaryAsync(d => d.AlanId);
        foreach (var alan in alanlar)
        {
            var temiz = alan.Temizle(gelen?.GetValueOrDefault(alan.Id));
            mevcut.TryGetValue(alan.Id, out var kayit);

            if (temiz == null)
            {
                if (kayit != null) _vt.MusteriAlanDegerleri.Remove(kayit);    // DELETE
                continue;
            }
            if (kayit == null)
            {
                kayit = new MusteriAlanDegeri { KullaniciId = kullaniciId, AlanId = alan.Id };
                _vt.MusteriAlanDegerleri.Add(kayit);                           // INSERT
            }
            kayit.Deger = temiz;                                               // UPDATE
        }
        await _vt.SaveChangesAsync();
    }
}
