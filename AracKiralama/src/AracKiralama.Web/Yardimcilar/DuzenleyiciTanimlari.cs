// ============================================================================
// SİTE DÜZENLEYİCİ HARİTASI
// Yönetim → Site Düzenleyici'deki sekmeler ve her sekmede hangi yazıların /
// ayarların düzenleneceği burada yazılıdır.
//
// ► Düzenleyiciye yeni bir yazı eklemek için:
//   1) Yazıyı Yardimcilar/VarsayilanMetinler.cs'e ekleyin (örn. ["araclar.yeni"] = "Merhaba")
//   2) Aşağıdaki "Aciklamalar" listesine bir satır ekleyin: ["araclar.yeni"] = ("Bölüm adı", "Kutunun adı")
//   3) Yazının görüneceği sekmenin "Metinler" listesine anahtarı ekleyin.
// ► Yeni bir ayar: Modeller/SiteAyarlari.cs'e [Ayar("Grup", "Ad")] satırı yeter;
//   o grup hangi sekmenin "AyarGruplari" listesindeyse orada kendiliğinden görünür.
// ============================================================================
namespace AracKiralama.Yardimcilar;

/// <summary>Düzenleyicideki bir sekme (sitenin bir sayfası ya da bölümü).</summary>
public record DuzenleyiciSekmesi(
    string Kod,                              // adresteki ad: /Yonetim/Duzenleyici?sayfa=anasayfa
    string Ad,                               // sekmede görünen ad
    string Ikon,                             // Bootstrap ikonu
    string Aciklama,                         // sekmenin altındaki kısa açıklama
    (string Ad, string Adres)[] Onizlemeler, // sağdaki önizlemede açılabilecek sayfalar ({arac} → ilk aracın numarası)
    string[] Metinler,                       // bu sekmede düzenlenen yazıların anahtarları
    string[] AyarGruplari,                   // bu sekmede gösterilecek ayar grupları ([Ayar("Grup", ...)])
    bool KurumsalSayfalar = false);          // Hakkımızda, KVKK gibi sayfaların aç/kapa düğmeleri gösterilsin mi?

public static class DuzenleyiciTanimlari
{
    // ---------------------------------------------------------------- SEKMELER
    public static readonly DuzenleyiciSekmesi[] Sekmeler =
    [
        new("anasayfa", "Ana sayfa", "bi-house", "Büyük başlık, rakamlar, arama kutusu, kategoriler, öne çıkanlar, adımlar, şubeler.",
            [("Ana sayfa", "/")],
            [.. VarsayilanMetinler.Liste.Keys.Where(k => k.StartsWith("anasayfa.")), "arama.alis-sube", "arama.iade-sube", "arama.tum-subeler", "arama.ayni-sube", "arama.alis", "arama.iade", "arama.buton"],
            ["Ana Sayfa"]),

        new("araclar", "Araç listesi", "bi-grid-3x3-gap", "Liste başlığı, filtre yazıları, araç kartları. Hangi filtrelerin görüneceği ve sıralama.",
            [("Araç listesi", "/Araclar")],
            ["araclar.baslik", "araclar.tarih-sec", "araclar.filtreler", "araclar.temizle", "araclar.bulundu", "araclar.incele", "araclar.gunluk", "araclar.bos-baslik", "araclar.bos-metin",
             "araclar.indirimler", "araclar.sadece-indirimli", "araclar.indirim-sec"],
            ["Araç Listesi"]),

        new("detay", "Araç detayı ve fiyat", "bi-card-text", "Fiyat kutusu, kiralama şartları, ek hizmetler ve fiyat kuralları (indirimler, tek yön ücreti).",
            [("Araç detayı", "/Araclar/Detay/{arac}")],
            ["araclar.sartlar", "araclar.ekstralar", "araclar.rezervasyon-yap", "araclar.odeme-notu", "araclar.benzer", "araclar.gunluk"],
            ["Fiyatlar"]),

        new("hesap", "Giriş ve kayıt", "bi-person-lock", "Giriş yap ve Üye ol sayfalarının yazıları, uyarı mesajları.",
            [("Giriş", "/Hesap/Giris"), ("Üye ol", "/Hesap/Kayit")],
            [.. VarsayilanMetinler.Liste.Keys.Where(k => k.StartsWith("hesap."))],
            []),

        new("menu", "Menü ve alt bilgi", "bi-layout-text-window", "Üst menü yazıları, sayfanın en altındaki bilgiler, firma adı, telefon, adres.",
            [("Ana sayfa", "/"), ("Araç listesi", "/Araclar")],
            [.. VarsayilanMetinler.Liste.Keys.Where(k => k.StartsWith("menu.") || k.StartsWith("altbilgi."))],
            ["Genel"]),

        new("gorunum", "Renk ve görünüm", "bi-palette", "Ana renk, koyu alan rengi, köşe yuvarlaklığı, tema, logo ikonu.",
            [("Ana sayfa", "/"), ("Araç listesi", "/Araclar")],
            [],
            ["Görünüm"]),

        new("sunum", "Sunum ayarları", "bi-easel", "Hocanın gördüğü site: üye girişi / kaydı açık mı, demo hesaplar, kurumsal sayfalar.",
            [("Ana sayfa", "/"), ("Giriş", "/Hesap/Giris")],
            [],
            ["Sunum", "Rezervasyon"],
            KurumsalSayfalar: true),

        new("diger", "Diğer yazılar", "bi-three-dots", "Rezervasyon ve profil sayfalarının yazıları (bu sayfalar giriş yapınca görünür).",
            [("Ana sayfa", "/")],
            [.. VarsayilanMetinler.Liste.Keys.Where(k => k.StartsWith("rezervasyon.") || k.StartsWith("profil."))],
            []),
    ];

    /// <summary>Koduyla sekmeyi bulur; bulamazsa ilk sekmeyi (Ana sayfa) döndürür.</summary>
    public static DuzenleyiciSekmesi Bul(string? kod) => Sekmeler.FirstOrDefault(s => s.Kod == kod) ?? Sekmeler[0];

    // ---------------------------------------------------------------- YAZILARIN AÇIKLAMALARI
    // Anahtar → (hangi bölümde gösterilecek, kutunun sade Türkçe adı)
    public static readonly Dictionary<string, (string Bolum, string Ad)> Aciklamalar = new()
    {
        // Ana sayfa · üst bölüm
        ["anasayfa.rozet"] = ("Üst bölüm", "Küçük rozet ({sube} ve {arac} sayıları kendiliğinden dolar)"),
        ["anasayfa.baslik"] = ("Üst bölüm", "Büyük başlık — 1. parça"),
        ["anasayfa.baslik-vurgu"] = ("Üst bölüm", "Büyük başlık — renkli parça"),
        ["anasayfa.baslik-son"] = ("Üst bölüm", "Büyük başlık — son parça"),
        ["anasayfa.aciklama"] = ("Üst bölüm", "Başlığın altındaki açıklama"),
        ["anasayfa.istatistik-arac"] = ("Üst bölüm", "1. rakamın yazısı (araç sayısı)"),
        ["anasayfa.istatistik-kiralama"] = ("Üst bölüm", "2. rakamın yazısı (kiralama sayısı)"),
        ["anasayfa.istatistik-ucuncu-deger"] = ("Üst bölüm", "3. rakam"),
        ["anasayfa.istatistik-ucuncu"] = ("Üst bölüm", "3. rakamın yazısı"),
        ["anasayfa.kart1-baslik"] = ("Üst bölüm", "Fotoğraf üstündeki 1. kart: başlık"),
        ["anasayfa.kart1-metin"] = ("Üst bölüm", "Fotoğraf üstündeki 1. kart: alt yazı"),
        ["anasayfa.kart2-baslik"] = ("Üst bölüm", "Fotoğraf üstündeki 2. kart: başlık"),
        ["anasayfa.kart2-metin"] = ("Üst bölüm", "Fotoğraf üstündeki 2. kart: alt yazı"),
        // Arama kutusu
        ["arama.alis-sube"] = ("Arama kutusu", "Alış şubesi etiketi"),
        ["arama.iade-sube"] = ("Arama kutusu", "İade şubesi etiketi"),
        ["arama.tum-subeler"] = ("Arama kutusu", "Şube seçilmemişken yazan"),
        ["arama.ayni-sube"] = ("Arama kutusu", "İade şubesi seçilmemişken yazan"),
        ["arama.alis"] = ("Arama kutusu", "Alış tarihi etiketi"),
        ["arama.iade"] = ("Arama kutusu", "İade tarihi etiketi"),
        ["arama.buton"] = ("Arama kutusu", "Arama butonu"),
        // Ana sayfa · bölümler
        ["anasayfa.kategoriler-ust"] = ("Kategoriler", "Küçük üst başlık"),
        ["anasayfa.kategoriler-baslik"] = ("Kategoriler", "Büyük başlık"),
        ["anasayfa.tum-araclar"] = ("Kategoriler", "\"Tüm araçlar\" butonu"),
        ["anasayfa.onecikan-ust"] = ("Öne çıkanlar", "Küçük üst başlık"),
        ["anasayfa.onecikan-baslik"] = ("Öne çıkanlar", "Büyük başlık"),
        ["anasayfa.adim1-baslik"] = ("Nasıl çalışır (3 adım)", "1. adım başlığı"),
        ["anasayfa.adim1-metin"] = ("Nasıl çalışır (3 adım)", "1. adım yazısı"),
        ["anasayfa.adim2-baslik"] = ("Nasıl çalışır (3 adım)", "2. adım başlığı"),
        ["anasayfa.adim2-metin"] = ("Nasıl çalışır (3 adım)", "2. adım yazısı"),
        ["anasayfa.adim3-baslik"] = ("Nasıl çalışır (3 adım)", "3. adım başlığı"),
        ["anasayfa.adim3-metin"] = ("Nasıl çalışır (3 adım)", "3. adım yazısı"),
        ["anasayfa.subeler-ust"] = ("Şubeler", "Küçük üst başlık"),
        ["anasayfa.subeler-baslik"] = ("Şubeler", "Büyük başlık"),
        ["anasayfa.subeler-buton"] = ("Şubeler", "Şube kartındaki buton"),
        // Araç listesi
        ["araclar.baslik"] = ("Liste sayfası", "Sayfa başlığı"),
        ["araclar.tarih-sec"] = ("Liste sayfası", "Başlığın altındaki bilgi"),
        ["araclar.filtreler"] = ("Filtre paneli", "Filtre paneli başlığı"),
        ["araclar.temizle"] = ("Filtre paneli", "\"Temizle\" linki"),
        ["araclar.bulundu"] = ("Liste sayfası", "Sonuç sayısının yanındaki yazı"),
        ["araclar.bos-baslik"] = ("Sonuç yoksa", "Başlık"),
        ["araclar.bos-metin"] = ("Sonuç yoksa", "Açıklama"),
        ["araclar.incele"] = ("Araç kartı", "Kartın butonu"),
        ["araclar.gunluk"] = ("Araç kartı", "Fiyatın yanındaki yazı (/ gün)"),
        ["araclar.indirimler"] = ("Filtre paneli", "İndirimler bölümünün başlığı (kampanya varsa görünür)"),
        ["araclar.sadece-indirimli"] = ("Filtre paneli", "\"Sadece indirimli araçlar\" kutusu"),
        ["araclar.indirim-sec"] = ("Filtre paneli", "\"Mesleğiniz\" gibi seçimin altındaki not"),
        // Araç detayı
        ["araclar.sartlar"] = ("Araç detayı", "Kiralama şartları kutusunun başlığı"),
        ["araclar.ekstralar"] = ("Fiyat kutusu", "Ek hizmetler başlığı"),
        ["araclar.rezervasyon-yap"] = ("Fiyat kutusu", "Rezervasyon butonu"),
        ["araclar.odeme-notu"] = ("Fiyat kutusu", "Butonun altındaki not"),
        ["araclar.benzer"] = ("Araç detayı", "Benzer araçlar başlığı"),
        // Giriş ve kayıt
        ["hesap.giris-baslik"] = ("Giriş sayfası", "Başlık ve buton yazısı"),
        ["hesap.giris-yan-baslik"] = ("Giriş sayfası", "Fotoğraf üstündeki başlık"),
        ["hesap.giris-yan-metin"] = ("Giriş sayfası", "Fotoğraf üstündeki yazı"),
        ["hesap.giris-kapali"] = ("Giriş sayfası", "Giriş kapalıyken çıkan uyarı"),
        ["hesap.kayit-baslik"] = ("Üye ol sayfası", "Başlık"),
        ["hesap.kayit-yan-baslik"] = ("Üye ol sayfası", "Fotoğraf üstündeki başlık"),
        ["hesap.kayit-yan-metin"] = ("Üye ol sayfası", "Fotoğraf üstündeki yazı"),
        ["hesap.kayit-kapali"] = ("Üye ol sayfası", "Kayıt kapalıyken çıkan uyarı"),
        // Menü
        ["menu.anasayfa"] = ("Üst menü", "\"Ana Sayfa\" linki"),
        ["menu.araclar"] = ("Üst menü", "\"Araçlar\" linki"),
        ["menu.subeler"] = ("Üst menü", "\"Şubeler\" linki"),
        ["menu.giris"] = ("Üst menü", "Giriş butonu"),
        ["menu.uyeol"] = ("Üst menü", "Üye ol butonu"),
        ["menu.hesabim"] = ("Giriş yapınca", "\"Hesabım\" menüsü"),
        ["menu.rezervasyonlarim"] = ("Giriş yapınca", "\"Rezervasyonlarım\" linki"),
        ["menu.profil"] = ("Giriş yapınca", "Profil linki"),
        ["menu.yonetim"] = ("Giriş yapınca", "Yönetim paneli linki (personel)"),
        ["menu.cikis"] = ("Giriş yapınca", "Çıkış linki"),
        // Alt bilgi
        ["altbilgi.aciklama"] = ("Alt bilgi", "Logonun altındaki tanıtım yazısı"),
        ["altbilgi.kiralama"] = ("Alt bilgi", "1. sütun başlığı"),
        ["altbilgi.kurumsal"] = ("Alt bilgi", "\"Kurumsal\" sütun başlığı"),
        ["altbilgi.iletisim"] = ("Alt bilgi", "İletişim sütunu başlığı"),
        ["altbilgi.telif"] = ("Alt bilgi", "En alttaki satır"),
        // Rezervasyon ve profil
        ["rezervasyon.baslik"] = ("Rezervasyon", "Sayfa başlığı"),
        ["rezervasyon.adim1"] = ("Rezervasyon", "1. adım adı"),
        ["rezervasyon.adim2"] = ("Rezervasyon", "2. adım adı"),
        ["rezervasyon.adim3"] = ("Rezervasyon", "3. adım adı"),
        ["rezervasyon.gonder"] = ("Rezervasyon", "Gönder butonu"),
        ["rezervasyon.tamam-baslik"] = ("Rezervasyon", "Talep alınınca başlık"),
        ["rezervasyon.tamam-metin"] = ("Rezervasyon", "Talep alınınca yazı"),
        ["rezervasyon.kapali"] = ("Rezervasyon", "Rezervasyon kapalıyken uyarı"),
        ["profil.merhaba"] = ("Profil", "Selamlama"),
        ["profil.aciklama"] = ("Profil", "Açıklama"),
        ["profil.eksik-bilgi"] = ("Profil", "Bilgi eksikse uyarı"),
    };

    /// <summary>Bir yazının bölümü ve adı (listede yoksa anahtarın kendisi).</summary>
    public static (string Bolum, string Ad) Aciklama(string anahtar) =>
        Aciklamalar.TryGetValue(anahtar, out var aciklama) ? aciklama : ("Diğer", anahtar);
}
