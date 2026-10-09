# Kod Haritası: hangi dosya ne zaman çalışır?

Kodu satır satır ezberlemenize gerek yok. Bir şeyin nerede yapıldığını bulmak için **"kullanıcı neye bastı → hangi dosyalar sırayla çalıştı"** sorusunu sormak yeterli. Bu sayfa en önemli 6 akışı gösterir.

Bütün yollar `src/AracKiralama.Web/` klasörüne göredir.

---

## Önce: her klasör tek cümleyle

| Klasör | Görevi | Bir benzetme |
|---|---|---|
| `Program.cs` | Uygulama açılırken bir kez çalışır: veritabanını seçer, servisleri tanıtır, adresleri tanımlar. | Dükkânın açılış hazırlığı |
| `Modeller/` | Veritabanı tabloları. Her sınıf bir tablo, her özellik bir sütun. | Dosya dolabındaki formlar |
| `Veri/` | `VeriTabani.cs` veritabanıyla konuşur; `OrnekVeriler.cs` ilk açılışta örnek veri yükler. | Arşiv memuru |
| `Servisler/` | **İş kuralları.** Fiyat nasıl hesaplanır, araç boş mu, rezervasyon onaylanabilir mi... | Şube müdürü |
| `Controllers/` | Gelen isteği karşılar, ilgili servisi çağırır, sonucu bir sayfayla gösterir. | Danışma görevlisi |
| `Areas/Yonetim/` | Yönetim panelinin controller'ları ve sayfaları (sadece Admin / Personel). | Personel odası |
| `Views/` | Sayfaların HTML şablonları (Razor, `.cshtml`). | Vitrin |
| `FormModelleri/` | Formlardan gelen verinin şekli ve kuralları (zorunlu alan, aralık...). | Başvuru formları |
| `Yardimcilar/` | Her yerde kullanılan küçük araçlar: para / tarih biçimi, T.C. ve plaka doğrulama, varsayılan site yazıları, güvenlik. | Alet çantası |
| `wwwroot/` | Tarayıcıya olduğu gibi gönderilen dosyalar: CSS, JavaScript, araç fotoğrafları. | Tabela ve broşürler |

**Altın kural:** Controller **düşünmez**, sadece yönlendirir. **Karar veren her şey `Servisler/` içindedir.** Bir kuralı arıyorsanız önce oraya bakın.

---

## Akış 1: Müşteri "Araç Bul"a basıyor

```
Tarayıcı: GET /Araclar?Baslangic=...&Bitis=...&KategoriId=2
   │
   ▼
Controllers/AraclarController.cs → Index(AracFiltresi filtre)
   │   ASP.NET adres çubuğundaki değerleri AracFiltresi'ne kendisi doldurur
   │   (FormModelleri/SiteModelleri.cs)
   │
   ├─ her dolu filtre için bir "if" → sorguya Where(...) eklenir
   ├─ tarih seçildiyse → Servisler/MusaitlikServisi.cs → SadeceMusaitler(...)
   │                     (o tarihlerde onaylı / kiradaki rezervasyonu veya bakımı olanları çıkarır)
   ├─ sıralama → switch (fiyat-artan, yeni, km, ad...)
   └─ sayfalama → Yardimcilar/Genel.cs → SayfaListesi
   │
   ▼
Views/Araclar/Index.cshtml
   ├─ filtre paneli: Icerik.Ayarlar.FiltreMarka gibi ayarlara göre kutuları gösterir / gizler
   └─ her araç için → Views/Shared/_AracKarti.cshtml
```

**Buradan ne öğreniriz?** Yeni filtre = `AracFiltresi`'ne bir özellik + controller'a bir `if` + view'a bir kutu ([rehber §1](GELISTIRICI-REHBERI.md#1-araç-listesine-yeni-filtre)).

---

## Akış 2: Araç detayında fiyat anında değişiyor

```
Kullanıcı tarih / şube / ekstra değiştirir
   │
   ▼
wwwroot/js/fiyat-hesapla.js → fetch("/api/fiyat", { aracId, baslangic, bitis, ... })
   │
   ▼
Controllers/Api/ApiController.cs → Fiyat(FiyatIstegi)
   │
   ▼
Servisler/RezervasyonServisi.cs → TeklifAsync(...)          (aracı ve ekstraları veritabanından alır)
   │
   ▼
Servisler/FiyatServisi.cs → Hesapla(...)
   │   gün sayısı → kira tutarı → indirim (Ayarlar'daki oranlar) → ekstralar → tek yön ücreti → toplam
   ▼
JSON cevap → fiyat-hesapla.js dökümü sayfaya yazar
```

**Buradan ne öğreniriz?** Fiyatla ilgili her şey **tek bir yerde**: `FiyatServisi.Hesapla`. Rezervasyon kaydı, PDF sözleşme ve API aynı metodu kullanır; bir yerde değiştirmek her yeri değiştirir.

---

## Akış 3: Müşteri rezervasyon talebini gönderiyor

```
Tarayıcı: POST /Rezervasyon/Yeni   (3 adımlı form: wwwroot/js/rezervasyon-adimlari.js)
   │
   ▼
Controllers/RezervasyonController.cs → Yeni(RezervasyonFormu form)
   │   form kuralları geçerli mi? (FormModelleri/SiteModelleri.cs → RezervasyonFormu)
   ▼
Servisler/RezervasyonServisi.cs → OlusturAsync(...)
   ├─ rezervasyon alımı açık mı?                   (Ayarlar → Rezervasyon)
   ├─ TarihHatalari: geçmiş tarih, en uzun süre
   ├─ SurucuHatalari: kara liste, yaş, ehliyet yılı (kategorinin şartları)
   ├─ MusaitlikServisi.MusaitMiAsync: araç boş mu?
   ├─ aynı müşterinin aynı tarihte bekleyen talebi var mı?
   ├─ FiyatServisi ile teklif → rezervasyona kopyalanır (TeklifiUygula)
   └─ kayıt → durum "Onay Bekliyor" (otomatik onay açıksa "Onaylandı")
   │
   ├─ kural çiğnendiyse → IsKuraliHatasi("Türkçe mesaj") → controller formun üstünde gösterir
   ▼
RedirectToAction("Tamamlandi") → Views/Rezervasyon/Tamamlandi.cshtml (rezervasyon kodu)
```

**Buradan ne öğreniriz?** Yeni bir rezervasyon kuralı = `OlusturAsync` içine bir `if` + `throw new IsKuraliHatasi(...)` ([rehber §5](GELISTIRICI-REHBERI.md#5-yeni-iş-kuralı)).

---

## Akış 4: Personel "Onayla"ya basıyor

```
Tarayıcı: POST /Yonetim/Rezervasyonlar/Onayla/42
   │
   ▼
Areas/Yonetim/Controllers/RezervasyonControllerlari.cs → Onayla(id)
   │   bütün tek tıklık işlemler (Onayla, Reddet, IptalEt, OdemeEkle) aynı kalıp:
   │   CalistirAsync → servisi çağır → başarılıysa yeşil, hatalıysa kırmızı bildirim → detaya dön
   ▼
Servisler/RezervasyonServisi.cs → OnaylaAsync(id, personelId)
   ├─ transaction başlar (kontrol ve kayıt arasında başkası araya giremesin)
   ├─ GecisiKontrolEt: "Onay Bekliyor" → "Onaylandı" geçişine izin var mı? (Gecisler tablosu)
   ├─ araç hâlâ boş mu? (MusaitlikServisi)
   ├─ durum = Onaylandı
   ├─ aynı araca çakışan diğer bekleyen talepler → Reddedildi
   └─ transaction tamamlanır
```

Teslim et ve İade al da aynı servis dosyasındadır (`TeslimEtAsync`, `IadeAlAsync`). İadede gecikme / yakıt ücreti yine `FiyatServisi`'nden gelir.

**Durum makinesi** (hangi durumdan hangisine geçilebilir) `RezervasyonServisi.cs`'in en üstündeki `Gecisler` sözlüğündedir; tabloya bakan kişi bütün akışı görür.

---

## Akış 5: Admin Ayarlar'dan ana rengi değiştiriyor

```
Tarayıcı: GET /Yonetim/Ayarlar
   │
   ▼
Areas/Yonetim/Controllers/IcerikControllerlari.cs → AyarlarController.Index()
   │   Modeller/SiteAyarlari.cs içindeki [Ayar("Görünüm", "Ana renk", Renk = true)] etiketli
   │   her özelliği okuyup bir alan listesi yapar (reflection)
   ▼
Areas/Yonetim/Views/Ayarlar/Index.cshtml → her alanın türüne göre kutu çizer
   (bool → açma/kapama, Renk → renk seçici, Secenekler → açılır liste, sayı → sayı kutusu)

Kullanıcı "Kaydet"e basar: POST /Yonetim/Ayarlar
   │
   ▼
AyarlarController.Index(SiteAyarlari form)
   ├─ formdaki değerler veritabanındaki tek satıra kopyalanır
   └─ Servisler/IcerikServisi.cs → Yenile()   (bellekteki ayarlar tazelenir)
   │
   ▼
Sonraki her sayfada: Views/Shared/_Head.cshtml
   :root { --ana: @Icerik.Ayarlar.AnaRenk; ... }   → wwwroot/css/stil.css bu değişkeni her yerde kullanır
```

**Buradan ne öğreniriz?** Yeni ayar = `SiteAyarlari.cs`'e **tek satır**; form kendiliğinden oluşur ([rehber §4](GELISTIRICI-REHBERI.md#4-panele-yeni-ayar-ve-fiyatta-kullanmak)). Her view'da `Icerik` hazırdır (`Views/_ViewImports.cshtml` → `@inject IcerikServisi Icerik`).

Site yazıları da aynı servisle gelir: `@Icerik.Metin("menu.araclar")` → önce veritabanındaki (panelde değiştirilmiş) hali, yoksa `Yardimcilar/VarsayilanMetinler.cs`'teki varsayılan.

---

## Akış 6: Uygulama açılıyor

```
dotnet run
   │
   ▼
Program.cs
   1) Veritabanı seçimi: ConnectionStrings:PostgreSql dolu mu?
        evet → PostgreSQL (canlı site, Neon)      hayır → SQLite dosyası (arackiralama.db)
   2) Giriş sistemi (Identity): şifre kuralları, Türkçe hata mesajları, roller
   3) Servisler: IcerikServisi (tek kopya, bellekte), FiyatServisi, MusaitlikServisi,
      RezervasyonServisi, RaporServisi, SozlesmeServisi, GorselServisi (her istekte yeni)
      GecikmeKontrolu (arka planda saatte bir gecikmeleri işaretler)
   4) Veri/OrnekVeriler.cs → YukleAsync: tablolar yoksa oluşturur (EnsureCreated),
      boşsa örnek verileri yükler
   5) Güvenlik başlıkları (Yardimcilar/Altyapi.cs), adresler:
        /Yonetim/{controller}/{action}/{id}   → Areas/Yonetim
        /{controller}/{action}/{id}           → Controllers  (varsayılan: AnaSayfa/Index)
```

---

## Bir adresin hangi dosyaya gittiğini bulmak

Adres kalıbı: `/{Controller}/{Aksiyon}/{id}`. Yönetim panelinde başına `/Yonetim` gelir.

| Adres | Controller dosyası | Metot | Sayfa |
|---|---|---|---|
| `/` | `Controllers/AnaSayfaController.cs` | `Index` | `Views/AnaSayfa/Index.cshtml` |
| `/Araclar/Detay/5` | `Controllers/AraclarController.cs` | `Detay(5)` | `Views/Araclar/Detay.cshtml` |
| `/Sayfa/kvkk` | `Controllers/AnaSayfaController.cs` | `Sayfa("kvkk")` | `Views/AnaSayfa/Sayfa.cshtml` |
| `/Profil` | `Controllers/ProfilController.cs` | `Index` | `Views/Profil/Index.cshtml` |
| `/Yonetim/Araclar/Duzenle/5` | `Areas/Yonetim/Controllers/AracControllerlari.cs` | `AraclarController.Duzenle(5)` | `Areas/Yonetim/Views/Araclar/Form.cshtml` |
| `/Yonetim/Kategoriler` | `Areas/Yonetim/Controllers/TanimControllerlari.cs` | `KategorilerController.Index` | `Areas/Yonetim/Views/Kategoriler/Index.cshtml` |
| `/api/fiyat` | `Controllers/Api/ApiController.cs` | `Fiyat` | (JSON) |

> Yönetim controller'ları konularına göre birkaç dosyada toplanmıştır: `TanimControllerlari.cs` (Şube, Ekstra, Marka, Kategori, Seçenek), `IcerikControllerlari.cs` (Ayarlar, Sayfalar, Metinler, Görseller), `RezervasyonControllerlari.cs` (Rezervasyonlar, Ödemeler), `AracControllerlari.cs` (Araçlar, Bakım), `PanelControllerlari.cs` (Panel, Takvim, Raporlar), `KullaniciControllerlari.cs` (Müşteriler, Personel). Bir dosyada `class XController` diye arayın.

---

## JavaScript nerede?

Sayfalarda hiç `<script>` kodu yoktur (güvenlik politikası). Davranışlar HTML'e yazılan `data-...` öznitelikleriyle bağlanır, kodları `wwwroot/js/` altındadır:

| Dosya | Ne yapar? | Nasıl bağlanır? |
|---|---|---|
| `genel.js` | Silmeden önce onay sorusu, filtre değişince formu gönderme, tema butonu, alış / iade tarihlerini eşleme, görsel ve ikon önizleme, demo hesabı doldurma (tam liste dosyanın başında) | `data-onay="..."`, `data-otomatik-gonder`, `data-tema-degistir`, `data-gorsel-onizle`... |
| `fiyat-hesapla.js` | Araç detayında canlı fiyat | detay sayfasındaki fiyat formu |
| `rezervasyon-adimlari.js` | Rezervasyonun 3 adımı arasında geçiş | rezervasyon formu |
| `grafikler.js` | Gösterge paneli ve rapor grafikleri (Chart.js) | `<script type="application/json" id="grafik-verisi">` bloğu |
| `tema.js` | Sayfa açılmadan koyu / açık temayı uygular (beyaz yanıp sönme olmasın) | `<head>` içinde |
