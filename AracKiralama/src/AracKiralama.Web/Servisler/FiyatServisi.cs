// ============================================================================
// FİYAT SERVİSİ: kiralama ücretini hesaplar.
// Veritabanına gitmez, sadece hesap yapar → birim testi çok kolaydır
// (tests/AracKiralama.Tests/FiyatTestleri.cs).
// Oranlar (indirim, tek yön, gecikme...) admin panelinden değiştirilir:
// Yönetim → Ayarlar → Fiyatlar  (Modeller/SiteAyarlari.cs)
// ============================================================================
using AracKiralama.Modeller;

namespace AracKiralama.Servisler;

/// <summary>Fiyat dökümündeki bir ekstra satırı.</summary>
public class EkstraSatiri
{
    public int EkstraId { get; set; }
    public string Ad { get; set; } = "";
    public decimal GunlukFiyat { get; set; }
    public decimal Tutar { get; set; }
}

/// <summary>Kalem kalem fiyat teklifi. Hem ekranda gösterilir hem rezervasyona kaydedilir.</summary>
public class FiyatTeklifi
{
    public int GunSayisi { get; set; }
    public decimal GunlukFiyat { get; set; }
    public decimal KiraTutari { get; set; }
    public decimal IndirimOrani { get; set; }
    public decimal Indirim { get; set; }
    public List<EkstraSatiri> Ekstralar { get; set; } = new();
    public decimal EkstraTutari { get; set; }
    public decimal TekYonUcreti { get; set; }
    public decimal Toplam { get; set; }
    public decimal Depozito { get; set; }
}

public class FiyatServisi
{
    private readonly SiteAyarlari _ayarlar;

    public FiyatServisi(SiteAyarlari ayarlar)
    {
        _ayarlar = ayarlar;
    }

    /// <summary>Kaç gün? Başlayan her 24 saat 1 gündür, en az 1 gün.</summary>
    public int GunSayisi(DateTime baslangic, DateTime bitis)
    {
        if (bitis <= baslangic) return 1;
        var gun = (int)Math.Ceiling((bitis - baslangic).TotalHours / 24);
        return Math.Max(1, gun);
    }

    public FiyatTeklifi Hesapla(Arac arac, DateTime baslangic, DateTime bitis, List<Ekstra> ekstralar, bool farkliSubeyeIade)
    {
        var teklif = new FiyatTeklifi
        {
            GunSayisi = GunSayisi(baslangic, bitis),
            GunlukFiyat = arac.GunlukFiyat,
            Depozito = arac.Depozito,
        };
        teklif.KiraTutari = teklif.GunSayisi * arac.GunlukFiyat;

        // Uzun kiralama indirimi: önce aylık, değilse haftalık kontrol edilir.
        if (teklif.GunSayisi >= _ayarlar.AylikIndirimGunu) teklif.IndirimOrani = _ayarlar.AylikIndirimOrani;
        else if (teklif.GunSayisi >= _ayarlar.HaftalikIndirimGunu) teklif.IndirimOrani = _ayarlar.HaftalikIndirimOrani;
        teklif.Indirim = Math.Round(teklif.KiraTutari * teklif.IndirimOrani, 2);

        // Ekstralar gün sayısıyla çarpılır; indirim ekstralara uygulanmaz.
        foreach (var ekstra in ekstralar)
        {
            var satir = new EkstraSatiri
            {
                EkstraId = ekstra.Id,
                Ad = ekstra.Ad,
                GunlukFiyat = ekstra.GunlukFiyat,
                Tutar = ekstra.GunlukFiyat * teklif.GunSayisi,
            };
            teklif.Ekstralar.Add(satir);
            teklif.EkstraTutari += satir.Tutar;
        }

        if (farkliSubeyeIade) teklif.TekYonUcreti = _ayarlar.TekYonUcreti;

        teklif.Toplam = teklif.KiraTutari - teklif.Indirim + teklif.EkstraTutari + teklif.TekYonUcreti;
        return teklif;
    }

    /// <summary>Geç iade: tolerans süresinden sonra başlayan her gün için günlük fiyat × çarpan.</summary>
    public decimal GecikmeUcreti(decimal gunlukFiyat, DateTime planlananIade, DateTime gercekIade)
    {
        var gecikme = gercekIade - planlananIade;
        if (gecikme.TotalMinutes <= _ayarlar.GecikmeToleransi) return 0;

        var gecikenGun = (int)Math.Ceiling(gecikme.TotalHours / 24);
        return gecikenGun * gunlukFiyat * _ayarlar.GecikmeCarpani;
    }

    /// <summary>Araç teslim edildiğinden az yakıtla dönerse eksik her çeyrek depo ücretlendirilir.</summary>
    public decimal YakitUcreti(YakitSeviyesi teslimde, YakitSeviyesi iadede)
    {
        var eksikCeyrek = (int)teslimde - (int)iadede;
        return eksikCeyrek > 0 ? eksikCeyrek * _ayarlar.YakitCeyrekUcreti : 0;
    }

    /// <summary>Onaylanmış rezervasyon, alışa belirlenen süreden az kala iptal edilirse 1 günlük ücret kesilir.</summary>
    public decimal IptalUcreti(Rezervasyon rezervasyon, DateTime simdi)
    {
        if (rezervasyon.Durum != RezervasyonDurumu.Onaylandi) return 0;
        var kalanSaat = (rezervasyon.BaslangicTarihi - simdi).TotalHours;
        return kalanSaat < _ayarlar.UcretsizIptalSaati ? rezervasyon.GunlukFiyat : 0;
    }
}
