// ============================================================================
// MÜSAİTLİK SERVİSİ: "Bu araç bu tarihlerde boş mu?" sorusunun tek cevap yeri.
//
// Bir araç şu durumlarda DOLUDUR:
//   1) Durumu "Pasif" ise,
//   2) Aynı tarihlerde "Onaylandı" veya "Kirada" bir rezervasyonu varsa,
//   3) Aynı tarihlerde tamamlanmamış bir bakım kaydı varsa.
// "Onay Bekliyor" talepler aracı kilitlemez: personel birini onaylayınca
// çakışan diğer talepler otomatik reddedilir (RezervasyonServisi.OnaylaAsync).
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Veri;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

public class MusaitlikServisi
{
    private readonly VeriTabani _veriTabani;

    public MusaitlikServisi(VeriTabani veriTabani)
    {
        _veriTabani = veriTabani;
    }

    /// <summary>Aracı takvimde gerçekten kilitleyen durumlar.</summary>
    public static readonly RezervasyonDurumu[] KilitleyenDurumlar = [RezervasyonDurumu.Onaylandi, RezervasyonDurumu.Kirada];

    /// <summary>
    /// İki tarih aralığı çakışıyor mu? Biri tam bittiği anda diğeri başlıyorsa çakışma YOKTUR.
    ///   A: |-----|          A: |-----|
    ///   B:    |-----|  ✗    B:       |-----|  ✓
    /// </summary>
    public static bool Cakisiyor(DateTime aBaslangic, DateTime aBitis, DateTime bBaslangic, DateTime bBitis)
        => aBaslangic < bBitis && bBaslangic < aBitis;

    public async Task<bool> MusaitMiAsync(int aracId, DateTime baslangic, DateTime bitis, int? haricRezervasyonId = null)
    {
        var arac = await _veriTabani.Araclar.FindAsync(aracId);
        if (arac == null || arac.Durum == AracDurumu.Pasif) return false;

        var rezervasyonVar = await _veriTabani.Rezervasyonlar.AnyAsync(r =>
            r.AracId == aracId
            && r.Id != haricRezervasyonId
            && KilitleyenDurumlar.Contains(r.Durum)
            && r.BaslangicTarihi < bitis && baslangic < r.BitisTarihi);   // = Cakisiyor(...)
        if (rezervasyonVar) return false;

        var bakimVar = await _veriTabani.Bakimlar.AnyAsync(b =>
            b.AracId == aracId && !b.Tamamlandi
            && b.BaslangicTarihi < bitis && baslangic < b.BitisTarihi);
        return !bakimVar;
    }

    /// <summary>Araç sorgusuna "sadece bu tarihlerde boş olanlar" şartını ekler (tek SQL sorgusu olur).</summary>
    public IQueryable<Arac> SadeceMusaitler(IQueryable<Arac> araclar, DateTime baslangic, DateTime bitis)
    {
        return araclar.Where(a =>
            a.Durum != AracDurumu.Pasif
            && !a.Rezervasyonlar.Any(r => KilitleyenDurumlar.Contains(r.Durum) && r.BaslangicTarihi < bitis && baslangic < r.BitisTarihi)
            && !a.Bakimlar.Any(b => !b.Tamamlandi && b.BaslangicTarihi < bitis && baslangic < b.BitisTarihi));
    }
}
