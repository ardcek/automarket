// ============================================================================
// SİTEDEKİ BÜTÜN YAZILARIN VARSAYILAN HALİ
// View'larda şöyle kullanılır:  @Icerik.Metin("anasayfa.baslik")
// Admin, Yönetim → Site Metinleri sayfasından her birini değiştirebilir;
// değiştirilen hali veritabanına (Metinler tablosu) yazılır ve buradakinin yerine geçer.
//
// ► Siteye yeni bir yazı eklemek için:
//   1) Aşağıya bir satır ekleyin:   ["araclar.yeni-yazi"] = "Varsayılan metin",
//   2) View'da kullanın:            @Icerik.Metin("araclar.yeni-yazi")
//   Admin panelinde kendiliğinden görünür.
// Anahtarın ilk kısmı (noktadan önce) admin panelindeki grubu belirler.
// ============================================================================
namespace AracKiralama.Yardimcilar;

public static class VarsayilanMetinler
{
    /// <summary>Admin panelinde grup başlıkları.</summary>
    public static readonly Dictionary<string, string> Gruplar = new()
    {
        ["menu"] = "Üst menü",
        ["anasayfa"] = "Ana sayfa",
        ["arama"] = "Arama kutusu",
        ["araclar"] = "Araç listesi ve detay",
        ["rezervasyon"] = "Rezervasyon",
        ["hesap"] = "Giriş / kayıt",
        ["profil"] = "Hesabım",
        ["altbilgi"] = "Alt bilgi (footer)",
    };

    public static readonly Dictionary<string, string> Liste = new()
    {
        // ---- Üst menü ----
        ["menu.anasayfa"] = "Ana Sayfa",
        ["menu.araclar"] = "Araçlar",
        ["menu.subeler"] = "Şubeler",
        ["menu.giris"] = "Giriş Yap",
        ["menu.uyeol"] = "Üye Ol",
        ["menu.hesabim"] = "Hesabım",
        ["menu.rezervasyonlarim"] = "Rezervasyonlarım",
        ["menu.profil"] = "Profil ve Ehliyet",
        ["menu.yonetim"] = "Yönetim Paneli",
        ["menu.cikis"] = "Çıkış Yap",

        // ---- Ana sayfa ----
        ["anasayfa.rozet"] = "{sube} şubede {arac} araç hazır",
        ["anasayfa.baslik"] = "Yolculuğun nereye olursa olsun,",
        ["anasayfa.baslik-vurgu"] = "doğru araç",
        ["anasayfa.baslik-son"] = "seni bekliyor.",
        ["anasayfa.aciklama"] = "Tarihini seç, aracını karşılaştır, dakikalar içinde rezervasyonunu yap. Gizli ücret yok, fiyatın tamamını en baştan görürsün.",
        ["anasayfa.istatistik-arac"] = "Araç",
        ["anasayfa.istatistik-kiralama"] = "Tamamlanan kiralama",
        ["anasayfa.istatistik-ucuncu-deger"] = "7/24",
        ["anasayfa.istatistik-ucuncu"] = "Yol yardımı",
        ["anasayfa.kart1-baslik"] = "Tam kasko",
        ["anasayfa.kart1-metin"] = "Tek tıkla ekle",
        ["anasayfa.kart2-baslik"] = "Anında fiyat",
        ["anasayfa.kart2-metin"] = "Sürpriz ücret yok",
        ["anasayfa.kategoriler-ust"] = "Kategoriler",
        ["anasayfa.kategoriler-baslik"] = "İhtiyacına uygun sınıfı seç",
        ["anasayfa.tum-araclar"] = "Tüm araçlar",
        ["anasayfa.onecikan-ust"] = "Öne çıkanlar",
        ["anasayfa.onecikan-baslik"] = "Her bütçeye uygun seçenekler",
        ["anasayfa.adim1-baslik"] = "1. Tarih ve şube seç",
        ["anasayfa.adim1-metin"] = "Alış ve iade noktanı belirle; farklı şubeye iade de mümkün.",
        ["anasayfa.adim2-baslik"] = "2. Aracını ve ekstralarını belirle",
        ["anasayfa.adim2-metin"] = "Sadece o tarihlerde müsait araçlar listelenir, fiyat anında hesaplanır.",
        ["anasayfa.adim3-baslik"] = "3. Onayı al, anahtarını teslim al",
        ["anasayfa.adim3-metin"] = "Talebin onaylandığında sözleşmen hazır; şubeden aracını al ve yola çık.",
        ["anasayfa.subeler-ust"] = "Şubelerimiz",
        ["anasayfa.subeler-baslik"] = "Havalimanında ve şehir merkezinde",
        ["anasayfa.subeler-buton"] = "Araçları gör",

        // ---- Arama kutusu ----
        ["arama.alis-sube"] = "Alış şubesi",
        ["arama.iade-sube"] = "İade şubesi",
        ["arama.tum-subeler"] = "Tüm şubeler",
        ["arama.ayni-sube"] = "Alış şubesiyle aynı",
        ["arama.alis"] = "Alış",
        ["arama.iade"] = "İade",
        ["arama.buton"] = "Araç Bul",

        // ---- Araç listesi ve detay ----
        ["araclar.baslik"] = "Kiralık Araçlar",
        ["araclar.tarih-sec"] = "Müsaitlik görmek için alış ve iade tarihini seçin.",
        ["araclar.filtreler"] = "Filtreler",
        ["araclar.temizle"] = "Temizle",
        ["araclar.bulundu"] = "araç bulundu",
        ["araclar.bos-baslik"] = "Kriterlere uygun araç bulunamadı",
        ["araclar.bos-metin"] = "Farklı tarihler deneyebilir veya filtreleri gevşetebilirsiniz.",
        ["araclar.incele"] = "İncele",
        ["araclar.gunluk"] = "/ gün",
        ["araclar.sartlar"] = "Kiralama şartları",
        ["araclar.ekstralar"] = "Ek hizmetler",
        ["araclar.rezervasyon-yap"] = "Rezervasyon Yap",
        ["araclar.odeme-notu"] = "Şimdi ödeme alınmaz, onaydan sonra şubede ödenir.",
        ["araclar.benzer"] = "Benzer araçlar",

        // ---- Rezervasyon ----
        ["rezervasyon.baslik"] = "Rezervasyonu tamamla",
        ["rezervasyon.adim1"] = "Tarih & Şube",
        ["rezervasyon.adim2"] = "Ek Hizmetler",
        ["rezervasyon.adim3"] = "Özet & Onay",
        ["rezervasyon.gonder"] = "Talebi Gönder",
        ["rezervasyon.tamam-baslik"] = "Rezervasyon talebiniz alındı!",
        ["rezervasyon.tamam-metin"] = "Durumu \"Rezervasyonlarım\" sayfasından takip edebilirsiniz.",
        ["rezervasyon.kapali"] = "Şu anda yeni rezervasyon alınmamaktadır. Lütfen daha sonra tekrar deneyin.",

        // ---- Giriş / kayıt ----
        ["hesap.giris-baslik"] = "Giriş yap",
        ["hesap.giris-yan-baslik"] = "Tekrar hoş geldiniz.",
        ["hesap.giris-yan-metin"] = "Rezervasyonlarınızı takip edin, sözleşmenizi indirin, bir sonraki yolculuğunuzu planlayın.",
        ["hesap.kayit-baslik"] = "Hesap oluştur",
        ["hesap.kayit-yan-baslik"] = "Dakikalar içinde yola çıkın.",
        ["hesap.kayit-yan-metin"] = "Üyelik ücretsizdir. Ehliyet bilgilerinizi bir kez girin, sonraki rezervasyonlarınız tek tık.",
        ["hesap.kayit-kapali"] = "Şu anda yeni üye kaydı alınmamaktadır.",

        // ---- Hesabım ----
        ["profil.merhaba"] = "Merhaba",
        ["profil.aciklama"] = "Rezervasyonlarınızı buradan takip edebilir, iptal edebilir veya sözleşmenizi indirebilirsiniz.",
        ["profil.eksik-bilgi"] = "Rezervasyon yapabilmek için doğum tarihi ve ehliyet bilgilerinizi tamamlayın.",

        // ---- Alt bilgi ----
        ["altbilgi.aciklama"] = "Türkiye'nin dört bir yanındaki şubelerimizde, şeffaf fiyat ve sürprizsiz kiralama deneyimi.",
        ["altbilgi.kiralama"] = "Kiralama",
        ["altbilgi.kurumsal"] = "Kurumsal",
        ["altbilgi.iletisim"] = "Bize ulaşın",
        ["altbilgi.telif"] = "Proje ödevi amaçlı geliştirilmiştir.",
    };
}
