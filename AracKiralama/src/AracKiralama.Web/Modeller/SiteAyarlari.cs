// ============================================================================
// SİTE AYARLARI (veritabanında tek satır)
// Yönetim → Ayarlar sayfasındaki form, bu sınıftaki [Ayar(...)] etiketli
// özelliklerden OTOMATİK oluşturulur.
//
// ► Yeni bir ayar eklemek için tek yapmanız gereken buraya bir satır eklemek:
//     [Ayar("Fiyatlar", "Hafta sonu zammı (%)", Aciklama = "0.15 = %15")]
//     public decimal HaftaSonuZammi { get; set; } = 0;
//   Form alanı kendiliğinden çıkar. Kodda kullanmak için: ayarlar.HaftaSonuZammi
//   Tabloya yeni kolon eklendiği için veritabanını yeniden oluşturun: bilgisayarda arackiralama.db
//   dosyasını silin, canlıda README → 13.5. ("Demo verilerini sıfırla" tablo yapısını değiştirmez.)
// ============================================================================
namespace AracKiralama.Modeller;

/// <summary>Bir özelliğin Ayarlar sayfasında hangi sekmede, hangi adla görüneceğini belirtir.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class AyarAttribute : Attribute
{
    public AyarAttribute(string grup, string ad)
    {
        Grup = grup;
        Ad = ad;
    }

    public string Grup { get; }
    public string Ad { get; }
    public string? Aciklama { get; set; }        // alanın altındaki küçük açıklama
    public string? Secenekler { get; set; }      // "deger:Görünen ad|deger2:Ad 2" → açılır liste
    public bool CokSatir { get; set; }           // true → geniş metin kutusu
    public bool Renk { get; set; }               // true → renk seçici
}

public class SiteAyarlari
{
    public int Id { get; set; } = 1;

    // ---------------- GENEL ----------------
    [Ayar("Genel", "Firma adı")] public string FirmaAdi { get; set; } = "Rota";
    [Ayar("Genel", "Slogan")] public string Slogan { get; set; } = "Rent a Car";
    [Ayar("Genel", "Telefon")] public string Telefon { get; set; } = "0850 555 76 82";
    [Ayar("Genel", "E-posta")] public string Eposta { get; set; } = "destek@rota-rentacar.com";
    [Ayar("Genel", "Adres", CokSatir = true)] public string Adres { get; set; } = "Maslak Mah. Büyükdere Cad. No:255, Sarıyer / İstanbul";
    [Ayar("Genel", "Çalışma saatleri")] public string CalismaSaatleri { get; set; } = "Her gün 08:00 – 22:00";
    [Ayar("Genel", "Instagram adresi")] public string? InstagramAdresi { get; set; } = "https://instagram.com";
    [Ayar("Genel", "X (Twitter) adresi")] public string? XAdresi { get; set; } = "https://x.com";
    [Ayar("Genel", "LinkedIn adresi")] public string? LinkedinAdresi { get; set; } = "https://linkedin.com";

    // ---------------- GÖRÜNÜM ----------------
    [Ayar("Görünüm", "Ana renk", Renk = true, Aciklama = "Butonlar, linkler, vurgular")]
    public string AnaRenk { get; set; } = "#2747d6";

    [Ayar("Görünüm", "Koyu alan rengi", Renk = true, Aciklama = "Ana sayfa üst bölümü ve alt bilgi arka planı")]
    public string KoyuRenk { get; set; } = "#0b1230";

    [Ayar("Görünüm", "Köşe yuvarlaklığı (px)", Aciklama = "0 = keskin köşe, 20 = çok yuvarlak")]
    public int KoseYuvarlakligi { get; set; } = 14;

    [Ayar("Görünüm", "Varsayılan tema", Secenekler = "sistem:Ziyaretçinin cihazına göre|acik:Açık|koyu:Koyu")]
    public string VarsayilanTema { get; set; } = "sistem";

    [Ayar("Görünüm", "Logo ikonu", Aciklama = "Bootstrap ikon adı, örn: bi-signpost-split-fill, bi-car-front-fill")]
    public string LogoIkonu { get; set; } = "bi-signpost-split-fill";

    // ---------------- ANA SAYFA ----------------
    [Ayar("Ana Sayfa", "Üst bölüm görseli", Aciklama = "Görsel adresi (Yönetim → Görseller'den yükleyip adresini kopyalayın)")]
    public string AnaSayfaGorseli { get; set; } = "/img/cars/Audi_A7.jpg";
    [Ayar("Ana Sayfa", "İstatistikleri göster")] public bool IstatistikleriGoster { get; set; } = true;
    [Ayar("Ana Sayfa", "Kategoriler bölümünü göster")] public bool KategorileriGoster { get; set; } = true;
    [Ayar("Ana Sayfa", "Öne çıkan araçları göster")] public bool OneCikanlariGoster { get; set; } = true;
    [Ayar("Ana Sayfa", "Öne çıkan araç sayısı")] public int OneCikanAracSayisi { get; set; } = 8;
    [Ayar("Ana Sayfa", "\"Nasıl çalışır\" bölümünü göster")] public bool NasilCalisirGoster { get; set; } = true;
    [Ayar("Ana Sayfa", "Şubeler bölümünü göster")] public bool SubeleriGoster { get; set; } = true;

    // ---------------- FİYATLAR ----------------
    [Ayar("Fiyatlar", "Haftalık indirim kaç günden başlar")] public int HaftalikIndirimGunu { get; set; } = 7;
    [Ayar("Fiyatlar", "Haftalık indirim oranı", Aciklama = "0.10 = %10")] public decimal HaftalikIndirimOrani { get; set; } = 0.10m;
    [Ayar("Fiyatlar", "Aylık indirim kaç günden başlar")] public int AylikIndirimGunu { get; set; } = 30;
    [Ayar("Fiyatlar", "Aylık indirim oranı", Aciklama = "0.20 = %20")] public decimal AylikIndirimOrani { get; set; } = 0.20m;
    [Ayar("Fiyatlar", "Farklı şubeye iade ücreti (₺)")] public decimal TekYonUcreti { get; set; } = 750;
    [Ayar("Fiyatlar", "Geç iade çarpanı", Aciklama = "Geç kalınan her gün: günlük fiyat × bu sayı")] public decimal GecikmeCarpani { get; set; } = 1.5m;
    [Ayar("Fiyatlar", "Geç iade toleransı (dakika)")] public int GecikmeToleransi { get; set; } = 59;
    [Ayar("Fiyatlar", "Eksik her ¼ depo yakıt ücreti (₺)")] public decimal YakitCeyrekUcreti { get; set; } = 450;
    [Ayar("Fiyatlar", "Ücretsiz iptal süresi (saat)", Aciklama = "Alışa bundan az kala iptal edilirse 1 günlük ücret kesilir")] public int UcretsizIptalSaati { get; set; } = 24;

    // ---------------- REZERVASYON ----------------
    [Ayar("Rezervasyon", "Yeni rezervasyon alınsın")] public bool RezervasyonaAcik { get; set; } = true;
    [Ayar("Rezervasyon", "Talepler otomatik onaylansın", Aciklama = "Açıksa müşteri talebi personel onayı beklemeden 'Onaylandı' olur")] public bool OtomatikOnay { get; set; }
    [Ayar("Rezervasyon", "En uzun kiralama (gün)")] public int EnUzunKiralama { get; set; } = 90;
    [Ayar("Rezervasyon", "Yeni üye kaydı açık")] public bool UyeKaydiAcik { get; set; } = true;

    // ---------------- ARAÇ LİSTESİ (filtreler ve sıralamalar) ----------------
    [Ayar("Araç Listesi", "Sayfa başına araç")] public int SayfaBasinaArac { get; set; } = 12;
    [Ayar("Araç Listesi", "Varsayılan sıralama", Secenekler = "fiyat-artan:Fiyat (artan)|fiyat-azalan:Fiyat (azalan)|yeni:En yeni model|km:En düşük km|ad:Marka-model (A→Z)")]
    public string VarsayilanSiralama { get; set; } = "fiyat-artan";
    [Ayar("Araç Listesi", "Filtre: Arama kutusu")] public bool FiltreArama { get; set; } = true;
    [Ayar("Araç Listesi", "Filtre: Kategori")] public bool FiltreKategori { get; set; } = true;
    [Ayar("Araç Listesi", "Filtre: Marka")] public bool FiltreMarka { get; set; } = true;
    [Ayar("Araç Listesi", "Filtre: Vites")] public bool FiltreVites { get; set; } = true;
    [Ayar("Araç Listesi", "Filtre: Yakıt")] public bool FiltreYakit { get; set; } = true;
    [Ayar("Araç Listesi", "Filtre: Koltuk sayısı")] public bool FiltreKoltuk { get; set; } = true;
    [Ayar("Araç Listesi", "Filtre: Fiyat aralığı")] public bool FiltreFiyat { get; set; } = true;
    [Ayar("Araç Listesi", "Filtre: Model yılı")] public bool FiltreYil { get; set; } = true;
    [Ayar("Araç Listesi", "Sıralama seçeneklerini göster")] public bool SiralamaGoster { get; set; } = true;
}
