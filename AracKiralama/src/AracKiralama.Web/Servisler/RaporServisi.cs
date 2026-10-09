// ============================================================================
// RAPOR SERVİSİ: yönetim panelindeki gösterge paneli ve rapor sayfasının verileri.
// Gelir tanımı: TAMAMLANAN kiralamaların toplam tutarı (iade tarihine göre).
// Yeni bir rapor sütunu eklemek için: AracRaporSatiri'na alan ekleyin, RaporAsync'te
// hesaplayın, Areas/Yonetim/Views/Raporlar/Index.cshtml'de gösterin, ExcelOlustur'a ekleyin.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

public class GrafikNoktasi
{
    public string Etiket { get; set; } = "";
    public decimal Deger { get; set; }
}

/// <summary>Gösterge panelindeki tüm rakamlar ve listeler.</summary>
public class PanelVerileri
{
    public int ToplamArac { get; set; }
    public int KiradakiArac { get; set; }
    public int BakimdakiArac { get; set; }
    public double DolulukOrani { get; set; }
    public int BekleyenTalep { get; set; }
    public decimal BuAyGelir { get; set; }
    public decimal GecenAyAyniDonemGelir { get; set; }
    public List<Rezervasyon> BugunTeslimler { get; set; } = new();
    public List<Rezervasyon> BugunIadeler { get; set; } = new();
    public List<Rezervasyon> Gecikenler { get; set; } = new();
    public List<Rezervasyon> SonTalepler { get; set; } = new();
    public List<GrafikNoktasi> AylikGelir { get; set; } = new();
    public List<GrafikNoktasi> KategoriDagilimi { get; set; } = new();
    public List<GrafikNoktasi> EnCokKiralananlar { get; set; } = new();
}

public class AracRaporSatiri
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = "";
    public string Arac { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string Sube { get; set; } = "";
    public int KiralamaSayisi { get; set; }
    public int KiraliGun { get; set; }
    public double KullanimOrani { get; set; }
    public decimal Gelir { get; set; }
}

public class SubeRaporSatiri
{
    public string Sube { get; set; } = "";
    public int KiralamaSayisi { get; set; }
    public decimal Gelir { get; set; }
}

public class RaporSonucu
{
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public decimal ToplamGelir { get; set; }
    public int KiralamaSayisi { get; set; }
    public decimal OrtalamaKiralama { get; set; }
    public double OrtalamaKullanim { get; set; }
    public List<AracRaporSatiri> Araclar { get; set; } = new();
    public List<SubeRaporSatiri> Subeler { get; set; } = new();
}

public class RaporServisi
{
    private readonly VeriTabani _vt;

    public RaporServisi(VeriTabani vt)
    {
        _vt = vt;
    }

    public async Task<PanelVerileri> PanelVerileriAsync()
    {
        var simdi = DateTime.Now;
        var bugun = DateTime.Today;
        var yarin = bugun.AddDays(1);
        var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);
        var v = new PanelVerileri();

        // ---- Araç sayıları ----
        var araclar = await _vt.Araclar.Where(a => a.Durum != AracDurumu.Pasif).Select(a => a.Durum).ToListAsync();
        v.ToplamArac = araclar.Count;
        v.KiradakiArac = araclar.Count(d => d == AracDurumu.Kirada);
        v.BakimdakiArac = araclar.Count(d => d == AracDurumu.Bakimda);
        v.DolulukOrani = v.ToplamArac == 0 ? 0 : (double)v.KiradakiArac / v.ToplamArac;

        // ---- Bugünün işleri ----
        var detayli = _vt.Rezervasyonlar.Include(r => r.Musteri).Include(r => r.Arac).ThenInclude(a => a!.Marka)
            .Include(r => r.AlisSube).Include(r => r.IadeSube);
        v.BugunTeslimler = await detayli.Where(r => r.Durum == RezervasyonDurumu.Onaylandi && r.BaslangicTarihi >= bugun && r.BaslangicTarihi < yarin).OrderBy(r => r.BaslangicTarihi).ToListAsync();
        v.BugunIadeler = await detayli.Where(r => r.Durum == RezervasyonDurumu.Kirada && r.BitisTarihi >= simdi && r.BitisTarihi < yarin).OrderBy(r => r.BitisTarihi).ToListAsync();
        v.Gecikenler = await detayli.Where(r => r.Durum == RezervasyonDurumu.Kirada && r.BitisTarihi < simdi).OrderBy(r => r.BitisTarihi).ToListAsync();
        v.SonTalepler = await detayli.Where(r => r.Durum == RezervasyonDurumu.OnayBekliyor).OrderBy(r => r.OlusturmaTarihi).Take(6).ToListAsync();
        v.BekleyenTalep = await _vt.Rezervasyonlar.CountAsync(r => r.Durum == RezervasyonDurumu.OnayBekliyor);

        // ---- Son 12 ayın geliri ----
        var yilOnce = ayBasi.AddMonths(-11);
        var tamamlananlar = await _vt.Rezervasyonlar
            .Where(r => r.Durum == RezervasyonDurumu.Tamamlandi && r.IadeTarihi >= yilOnce)
            .Select(r => new { r.IadeTarihi, r.ToplamTutar }).ToListAsync();
        for (var i = 0; i < 12; i++)
        {
            var ay = yilOnce.AddMonths(i);
            var gelir = tamamlananlar.Where(r => r.IadeTarihi >= ay && r.IadeTarihi < ay.AddMonths(1)).Sum(r => r.ToplamTutar);
            v.AylikGelir.Add(new GrafikNoktasi { Etiket = Bicim.Ay(ay), Deger = gelir });
        }
        v.BuAyGelir = v.AylikGelir[^1].Deger;

        // Adil karşılaştırma: bu ayın ilk N günü ↔ geçen ayın ilk N günü
        var gecenAyBasi = ayBasi.AddMonths(-1);
        var gecenAyAyniGun = gecenAyBasi + (simdi - ayBasi);
        v.GecenAyAyniDonemGelir = tamamlananlar.Where(r => r.IadeTarihi >= gecenAyBasi && r.IadeTarihi < gecenAyAyniGun).Sum(r => r.ToplamTutar);

        // ---- Kategori dağılımı ve en çok kiralanan 5 araç (iptal / ret hariç) ----
        var sayilanlar = await _vt.Rezervasyonlar
            .Where(r => r.Durum == RezervasyonDurumu.Onaylandi || r.Durum == RezervasyonDurumu.Kirada || r.Durum == RezervasyonDurumu.Tamamlandi)
            .Select(r => new { Kategori = r.Arac!.Kategori!.Ad, Arac = r.Arac.Marka!.Ad + " " + r.Arac.Model })
            .ToListAsync();
        v.KategoriDagilimi = sayilanlar.GroupBy(x => x.Kategori).OrderByDescending(g => g.Count())
            .Select(g => new GrafikNoktasi { Etiket = g.Key, Deger = g.Count() }).ToList();
        v.EnCokKiralananlar = sayilanlar.GroupBy(x => x.Arac).OrderByDescending(g => g.Count()).Take(5)
            .Select(g => new GrafikNoktasi { Etiket = g.Key, Deger = g.Count() }).ToList();

        return v;
    }

    public async Task<RaporSonucu> RaporAsync(DateTime baslangic, DateTime bitis)
    {
        var bas = baslangic.Date;
        var son = bitis.Date.AddDays(1);                 // bitiş günü de dahil
        var aralikGun = Math.Max(1, (son - bas).Days);

        var araclar = await _vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori).Include(a => a.Sube)
            .Where(a => a.Durum != AracDurumu.Pasif).AsNoTracking().ToListAsync();

        // Aralıkla çakışan kiralamalar (kullanım oranı için)
        var kiralamalar = await _vt.Rezervasyonlar.Include(r => r.AlisSube)
            .Where(r => (r.Durum == RezervasyonDurumu.Tamamlandi || r.Durum == RezervasyonDurumu.Kirada)
                        && r.BaslangicTarihi < son && bas < (r.IadeTarihi ?? r.BitisTarihi))
            .AsNoTracking().ToListAsync();

        // Gelir: aralık içinde iade edilmiş kiralamalar
        var gelirler = kiralamalar.Where(r => r.Durum == RezervasyonDurumu.Tamamlandi && r.IadeTarihi >= bas && r.IadeTarihi < son).ToList();

        var sonuc = new RaporSonucu { Baslangic = bas, Bitis = son.AddDays(-1) };
        foreach (var arac in araclar)
        {
            var aracKiralamalari = kiralamalar.Where(r => r.AracId == arac.Id).ToList();
            var kiraliGun = aracKiralamalari.Sum(r => OrtakGun(r.BaslangicTarihi, r.IadeTarihi ?? r.BitisTarihi, bas, son));
            sonuc.Araclar.Add(new AracRaporSatiri
            {
                AracId = arac.Id,
                Plaka = arac.Plaka,
                Arac = arac.TamAd,
                Kategori = arac.Kategori!.Ad,
                Sube = arac.Sube!.Ad,
                KiralamaSayisi = aracKiralamalari.Count,
                KiraliGun = kiraliGun,
                KullanimOrani = Math.Min(1.0, (double)kiraliGun / aralikGun),
                Gelir = gelirler.Where(r => r.AracId == arac.Id).Sum(r => r.ToplamTutar),
            });
        }
        sonuc.Araclar = sonuc.Araclar.OrderByDescending(x => x.Gelir).ToList();

        sonuc.Subeler = gelirler.GroupBy(r => r.AlisSube!.Ad)
            .Select(g => new SubeRaporSatiri { Sube = g.Key, KiralamaSayisi = g.Count(), Gelir = g.Sum(r => r.ToplamTutar) })
            .OrderByDescending(x => x.Gelir).ToList();

        sonuc.ToplamGelir = gelirler.Sum(r => r.ToplamTutar);
        sonuc.KiralamaSayisi = gelirler.Count;
        sonuc.OrtalamaKiralama = gelirler.Count == 0 ? 0 : sonuc.ToplamGelir / gelirler.Count;
        sonuc.OrtalamaKullanim = sonuc.Araclar.Count == 0 ? 0 : sonuc.Araclar.Average(x => x.KullanimOrani);
        return sonuc;
    }

    /// <summary>İki tarih aralığının ortak gün sayısı.</summary>
    private static int OrtakGun(DateTime aBas, DateTime aSon, DateTime bBas, DateTime bSon)
    {
        var bas = aBas > bBas ? aBas : bBas;
        var son = aSon < bSon ? aSon : bSon;
        return son <= bas ? 0 : (int)Math.Ceiling((son - bas).TotalDays);
    }

    /// <summary>Raporu iki sayfalı Excel dosyasına çevirir.</summary>
    public byte[] ExcelOlustur(RaporSonucu rapor)
    {
        using var kitap = new XLWorkbook();

        var sayfa1 = kitap.Worksheets.Add("Araç Bazlı");
        sayfa1.Cell(1, 1).Value = $"Araç Kullanım Raporu ({Bicim.Tarih(rapor.Baslangic)} - {Bicim.Tarih(rapor.Bitis)})";
        sayfa1.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        string[] basliklar = ["Plaka", "Araç", "Kategori", "Şube", "Kiralama", "Kiralı Gün", "Kullanım %", "Gelir (₺)"];
        for (var i = 0; i < basliklar.Length; i++) sayfa1.Cell(3, i + 1).Value = basliklar[i];
        sayfa1.Range(3, 1, 3, basliklar.Length).Style.Font.SetBold();

        var satir = 4;
        foreach (var a in rapor.Araclar)
        {
            sayfa1.Cell(satir, 1).Value = a.Plaka;
            sayfa1.Cell(satir, 2).Value = a.Arac;
            sayfa1.Cell(satir, 3).Value = a.Kategori;
            sayfa1.Cell(satir, 4).Value = a.Sube;
            sayfa1.Cell(satir, 5).Value = a.KiralamaSayisi;
            sayfa1.Cell(satir, 6).Value = a.KiraliGun;
            sayfa1.Cell(satir, 7).Value = a.KullanimOrani;
            sayfa1.Cell(satir, 8).Value = a.Gelir;
            satir++;
        }
        sayfa1.Column(7).Style.NumberFormat.Format = "0.0%";
        sayfa1.Column(8).Style.NumberFormat.Format = "#,##0.00";
        sayfa1.Columns().AdjustToContents();

        var sayfa2 = kitap.Worksheets.Add("Şube Bazlı");
        sayfa2.Cell(1, 1).Value = "Şube";
        sayfa2.Cell(1, 2).Value = "Kiralama";
        sayfa2.Cell(1, 3).Value = "Gelir (₺)";
        sayfa2.Range(1, 1, 1, 3).Style.Font.SetBold();
        satir = 2;
        foreach (var s in rapor.Subeler)
        {
            sayfa2.Cell(satir, 1).Value = s.Sube;
            sayfa2.Cell(satir, 2).Value = s.KiralamaSayisi;
            sayfa2.Cell(satir, 3).Value = s.Gelir;
            satir++;
        }
        sayfa2.Columns().AdjustToContents();

        using var akis = new MemoryStream();
        kitap.SaveAs(akis);
        return akis.ToArray();
    }
}
