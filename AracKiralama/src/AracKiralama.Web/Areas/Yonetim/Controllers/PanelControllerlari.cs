// ============================================================================
// GÖSTERGE PANELİ, DOLULUK TAKVİMİ, RAPORLAR
//   /Yonetim            → gösterge paneli (rakamlar ve grafikler: Servisler/RaporServisi.cs)
//   /Yonetim/Takvim     → araç × gün doluluk tablosu
//   /Yonetim/Raporlar   → tarih aralığına göre rapor + Excel
// ============================================================================
using AracKiralama.FormModelleri;
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Areas.Yonetim.Controllers;

public class PanelController : YonetimController
{
    private readonly RaporServisi _rapor;
    public PanelController(RaporServisi rapor) { _rapor = rapor; }

    public async Task<IActionResult> Index() => View(await _rapor.PanelVerileriAsync());
}

public class RaporlarController : YonetimController
{
    private readonly RaporServisi _rapor;
    public RaporlarController(RaporServisi rapor) { _rapor = rapor; }

    public async Task<IActionResult> Index(DateTime? baslangic, DateTime? bitis)
    {
        var (bas, son) = Aralik(baslangic, bitis);
        return View(await _rapor.RaporAsync(bas, son));
    }

    public async Task<IActionResult> Excel(DateTime? baslangic, DateTime? bitis)
    {
        var (bas, son) = Aralik(baslangic, bitis);
        var dosya = _rapor.ExcelOlustur(await _rapor.RaporAsync(bas, son));
        return File(dosya, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Rapor_{bas:yyyyMMdd}_{son:yyyyMMdd}.xlsx");
    }

    /// <summary>Tarih seçilmediyse son 30 gün.</summary>
    private static (DateTime, DateTime) Aralik(DateTime? baslangic, DateTime? bitis)
    {
        var son = (bitis ?? DateTime.Today).Date;
        var bas = (baslangic ?? son.AddDays(-29)).Date;
        return bas > son ? (son, bas) : (bas, son);
    }
}

/// <summary>
/// Doluluk takvimi: her satır bir araç, her sütun bir gün. Rezervasyon ve bakımlar renkli bloklar.
/// Hangi bloğun hangi sütundan başlayıp kaç gün süreceği burada hesaplanır; çizim CSS Grid ile yapılır.
/// </summary>
public class TakvimController : YonetimController
{
    private readonly VeriTabani _vt;
    public TakvimController(VeriTabani vt) { _vt = vt; }

    public async Task<IActionResult> Index(DateTime? baslangic, int gun = 21, int? subeId = null, int? kategoriId = null)
    {
        gun = Math.Clamp(gun, 7, 42);
        var bas = (baslangic ?? DateTime.Today.AddDays(-3)).Date;
        var son = bas.AddDays(gun);

        var aracSorgusu = _vt.Araclar.Include(a => a.Marka).Where(a => a.Durum != AracDurumu.Pasif).AsNoTracking();
        if (subeId != null) aracSorgusu = aracSorgusu.Where(a => a.SubeId == subeId);
        if (kategoriId != null) aracSorgusu = aracSorgusu.Where(a => a.KategoriId == kategoriId);
        var araclar = await aracSorgusu.OrderBy(a => a.Marka!.Ad).ThenBy(a => a.Model).ToListAsync();
        var aracIdleri = araclar.Select(a => a.Id).ToList();

        var gosterilen = new[] { RezervasyonDurumu.OnayBekliyor, RezervasyonDurumu.Onaylandi, RezervasyonDurumu.Kirada, RezervasyonDurumu.Tamamlandi };
        var rezervasyonlar = await _vt.Rezervasyonlar.Include(r => r.Musteri)
            .Where(r => aracIdleri.Contains(r.AracId) && gosterilen.Contains(r.Durum) && r.BaslangicTarihi < son && bas < (r.IadeTarihi ?? r.BitisTarihi))
            .AsNoTracking().ToListAsync();
        var bakimlar = await _vt.Bakimlar.Where(b => aracIdleri.Contains(b.AracId) && b.BaslangicTarihi < son && bas < b.BitisTarihi).AsNoTracking().ToListAsync();

        var model = new TakvimSayfasi
        {
            Baslangic = bas, GunSayisi = gun, SubeId = subeId, KategoriId = kategoriId,
            Gunler = Enumerable.Range(0, gun).Select(i => bas.AddDays(i)).ToList(),
            Subeler = await _vt.Subeler.OrderBy(s => s.Ad).ToListAsync(),
            Kategoriler = await _vt.Kategoriler.OrderBy(k => k.Sira).ToListAsync(),
        };

        foreach (var arac in araclar)
        {
            var satir = new TakvimSatiri { Arac = arac };
            foreach (var r in rezervasyonlar.Where(x => x.AracId == arac.Id))
            {
                var renk = r.Gecikti ? "blok-gecikti" : "blok-" + r.Durum.ToString().ToLowerInvariant();
                satir.Bloklar.Add(BlokOlustur(r.BaslangicTarihi, r.IadeTarihi ?? r.BitisTarihi, bas, gun, r.Musteri!.TamAd, renk,
                    $"{r.Kod} · {r.Musteri.TamAd}\n{Bicim.TarihSaat(r.BaslangicTarihi)} → {Bicim.TarihSaat(r.BitisTarihi)}\n{r.Durum.Ad()}",
                    Url.Action("Detay", "Rezervasyonlar", new { id = r.Id })));
            }
            foreach (var b in bakimlar.Where(x => x.AracId == arac.Id))
                satir.Bloklar.Add(BlokOlustur(b.BaslangicTarihi, b.BitisTarihi, bas, gun, "Bakım", "blok-bakim",
                    $"{b.Tur.Ad()}\n{Bicim.TarihSaat(b.BaslangicTarihi)} → {Bicim.TarihSaat(b.BitisTarihi)}", Url.Action("Index", "Bakim")));
            model.Satirlar.Add(satir);
        }
        return View(model);
    }

    /// <summary>Tarih aralığını takvim sütunlarına çevirir (1. sütun = ilk gün).</summary>
    private static TakvimBloku BlokOlustur(DateTime blokBas, DateTime blokSon, DateTime takvimBas, int gunSayisi, string yazi, string renk, string ipucu, string? link)
    {
        var ilkGun = Math.Max(0, (blokBas.Date - takvimBas).Days);
        var sonGun = Math.Min(gunSayisi - 1, (blokSon.AddMinutes(-1).Date - takvimBas).Days);
        return new TakvimBloku { BaslangicSutunu = ilkGun + 1, GunSayisi = Math.Max(1, sonGun - ilkGun + 1), Yazi = yazi, Renk = renk, Ipucu = ipucu, Link = link };
    }
}
