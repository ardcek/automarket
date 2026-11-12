// ============================================================================
// İÇERİK SERVİSİ: site ayarlarını, metinleri ve sayfaları bellekte tutar.
// Her sayfa açılışında veritabanına gitmemek için bir kere okunur, admin
// panelinden bir şey kaydedilince Yenile() çağrılır ve tekrar okunur.
//
// View'larda (_ViewImports'ta @inject edildi):
//   @Icerik.Ayarlar.FirmaAdi         → site ayarı
//   @Icerik.Metin("anasayfa.baslik")  → sitedeki bir yazı
//
// ÖNİZLEME: Site Düzenleyici'deki çerçeve sayfayı "?onizleme=1" ile açar. O istekte,
// giriş yapmış personelin kaydedilmemiş taslağı (TaslakServisi) kayıtlı değerlerin önüne geçer.
// Gerçek ziyaretçiler taslağı asla görmez.
// ============================================================================
using System.Security.Claims;
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

public class IcerikServisi
{
    private readonly IServiceScopeFactory _kapsamOlusturucu;
    private readonly IHttpContextAccessor _istekErisimi;   // o anki isteğe (adres, giriş yapan kişi) ulaşmak için
    private readonly TaslakServisi _taslaklar;
    private SiteAyarlari? _ayarlar;
    private Dictionary<string, string>? _metinler;
    private List<Sayfa>? _sayfalar;

    public IcerikServisi(IServiceScopeFactory kapsamOlusturucu, IHttpContextAccessor istekErisimi, TaslakServisi taslaklar)
    {
        _kapsamOlusturucu = kapsamOlusturucu;
        _istekErisimi = istekErisimi;
        _taslaklar = taslaklar;
    }

    /// <summary>Site ayarları. Önizlemede taslaktaki ayarlar uygulanmış bir kopya döner.</summary>
    public SiteAyarlari Ayarlar
    {
        get
        {
            Yukle();
            var taslak = OnizlemeTaslagi();
            return taslak == null || taslak.Ayarlar.Count == 0 ? _ayarlar! : OnizlemeAyarlari(taslak);
        }
    }

    /// <summary>Yayındaki sayfalar (menü ve alt bilgi linkleri için).</summary>
    public List<Sayfa> Sayfalar { get { Yukle(); return _sayfalar!; } }

    /// <summary>Sitedeki bir yazıyı döndürür: önce admin'in değiştirdiği hali, yoksa varsayılanı.</summary>
    public string Metin(string anahtar)
    {
        Yukle();

        // Önizlemede taslaktaki yazı önce gelir (taslakta boş bırakılmışsa varsayılan yazı gösterilir).
        var taslak = OnizlemeTaslagi();
        if (taslak != null && taslak.Metinler.TryGetValue(anahtar, out var taslakYazisi))
            return string.IsNullOrWhiteSpace(taslakYazisi) ? VarsayilanMetinler.Liste.GetValueOrDefault(anahtar, anahtar) : taslakYazisi;

        if (_metinler!.TryGetValue(anahtar, out var degistirilmis)) return degistirilmis;
        if (VarsayilanMetinler.Liste.TryGetValue(anahtar, out var varsayilan)) return varsayilan;
        return anahtar;   // tanımsız anahtar → ekranda anahtarın kendisi görünür, eksik olduğu fark edilir
    }

    /// <summary>Admin panelinden bir şey kaydedildiğinde çağrılır; bir sonraki istekte veriler yeniden okunur.</summary>
    public void Yenile()
    {
        _ayarlar = null;
        _metinler = null;
        _sayfalar = null;
    }

    /// <summary>
    /// Sayfa metinlerindeki {tek-yon} gibi yer tutucuları güncel ayarlarla doldurur.
    /// Böylece "Kiralama Koşulları" sayfası fiyat ayarı değişince kendiliğinden güncellenir.
    /// </summary>
    public string YerTutuculariDoldur(string metin)
    {
        var a = Ayarlar;
        return metin
            .Replace("{firma}", a.FirmaAdi)
            .Replace("{telefon}", a.Telefon)
            .Replace("{eposta}", a.Eposta)
            .Replace("{haftalik-gun}", a.HaftalikIndirimGunu.ToString())
            .Replace("{haftalik-indirim}", Bicim.Yuzde(a.HaftalikIndirimOrani))
            .Replace("{aylik-gun}", a.AylikIndirimGunu.ToString())
            .Replace("{aylik-indirim}", Bicim.Yuzde(a.AylikIndirimOrani))
            .Replace("{tek-yon}", Bicim.Para(a.TekYonUcreti))
            .Replace("{gecikme-carpani}", a.GecikmeCarpani.ToString("0.#", Bicim.Tr))
            .Replace("{gecikme-tolerans}", a.GecikmeToleransi.ToString())
            .Replace("{yakit-ucreti}", Bicim.Para(a.YakitCeyrekUcreti))
            .Replace("{iptal-saati}", a.UcretsizIptalSaati.ToString())
            .Replace("{en-uzun}", a.EnUzunKiralama.ToString());
    }

    /// <summary>Veriler bellekte yoksa veritabanından okur.</summary>
    private void Yukle()
    {
        if (_ayarlar != null && _metinler != null && _sayfalar != null) return;

        // Bu servis uygulama boyunca tek örnek (singleton) çalışır; veritabanı bağlantısı ise
        // istek başına açılır. Bu yüzden kısa süreli bir "kapsam" açıp bağlantıyı oradan alıyoruz.
        using var kapsam = _kapsamOlusturucu.CreateScope();
        var veriTabani = kapsam.ServiceProvider.GetRequiredService<VeriTabani>();

        _metinler = veriTabani.Metinler.AsNoTracking().ToDictionary(m => m.Anahtar, m => m.Deger);
        _sayfalar = veriTabani.Sayfalar.AsNoTracking().Where(s => s.Aktif).OrderBy(s => s.Sira).ToList();
        _ayarlar = veriTabani.SiteAyarlari.AsNoTracking().FirstOrDefault() ?? new SiteAyarlari();
    }

    /// <summary>
    /// Bu istek Site Düzenleyici'nin önizlemesi mi? Öyleyse giriş yapan personelin taslağını döndürür.
    /// Şartlar: adreste ?onizleme=1 var, kullanıcı Admin ya da Personel, ve bir taslağı var.
    /// </summary>
    private Taslak? OnizlemeTaslagi()
    {
        var istek = _istekErisimi.HttpContext;
        if (istek == null || istek.Request.Query["onizleme"] != "1") return null;
        if (!istek.User.IsInRole(Roller.Admin) && !istek.User.IsInRole(Roller.Personel)) return null;
        var kullaniciId = istek.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return kullaniciId == null ? null : _taslaklar.Getir(kullaniciId);
    }

    /// <summary>Kayıtlı ayarların kopyasına taslaktaki değerleri uygular (asıl ayarlara dokunmaz).</summary>
    private SiteAyarlari OnizlemeAyarlari(Taslak taslak)
    {
        // Aynı istekte defalarca istenebilir; bir kere hazırlayıp isteğin "çantasında" (Items) saklıyoruz.
        var istek = _istekErisimi.HttpContext!;
        if (istek.Items["onizleme-ayarlari"] is SiteAyarlari hazir) return hazir;

        var kopya = AyarCevirici.Kopyala(_ayarlar!);
        foreach (var (ad, deger) in taslak.Ayarlar) AyarCevirici.Uygula(kopya, ad, deger);
        istek.Items["onizleme-ayarlari"] = kopya;
        return kopya;
    }
}
