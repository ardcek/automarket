# Sunum Notları (≈ 12 dakika)

> **Sunumdan önce:** https://automarket.ardcek.com adresini bir kez açın. Admin ile girip **Ayarlar → Demo verilerini sıfırla**'ya basın; "bugün teslim / gecikmiş" örnekleri tazelenir. İnternet yoksa yerelde `dotnet run` ile çalıştırın; hiçbir dosya internetten yüklenmez.

## Açılış (1 dk)
"Araç kiralama firmasının hem müşteri sitesini hem de şube personelinin kullandığı otomasyonu C# ile geliştirdik. İki odağımız var: **gerçek iş kuralları** (aynı araç aynı tarihte iki kişiye verilemez, fiyat, geç iade ve eksik yakıt otomatik hesaplanır) ve **koda dokunmadan yönetilebilen site** (firma bilgisinden renge, fiyat oranlarından filtrelere kadar her şey yönetim panelinden değişiyor). Kodun tamamı Türkçe ve her dosyanın başında ne yaptığı yazıyor."

## Canlı demo akışı

**1. Müşteri (3 dk)**: `musteri@rota.com`
1. Ana sayfa → şube ve tarih seç → **Araç Bul**. Sadece o tarihlerde boş araçlar gelir.
2. Filtreler: SUV + Otomatik + fiyat aralığı, sıralama "en düşük km". Adres çubuğunun değiştiğini göster (link paylaşılabilir).
3. Araç detayı → tarihi 8 güne çıkar: **%10 indirim** satırı anında belirir. Tam kasko ekle, iade şubesini değiştir → tek yön ücreti.
4. **Rezervasyon Yap** → 3 adımlı sihirbaz → talebi gönder → rezervasyon kodu.
5. (İsteğe bağlı) `genc@rota.com` ile lüks araç dene → "En az 27 yaşında olmalısınız" uyarısı.

**2. Personel (3 dk)**: `admin@rota.com`
1. Gösterge paneli: rakamlar, aylık gelir grafiği, gecikme uyarısı, bugünün işleri.
2. **Doluluk takvimi**: araç × gün; renkler durumları gösterir.
3. Rezervasyonlar → az önceki talep → **Onayla**. Aynı araca çakışan başka talep varsa otomatik reddedilir.
4. Kırmızı **gecikme uyarısı** → gecikmiş bir kiralamayı aç → **İade al**: tahmini gecikme ücreti hazır gelir; yakıtı 1/2 seç → gecikme ve yakıt ücretleri otomatik eklenir.
5. Ödeme al → kalan bakiye güncellenir. **Sözleşme PDF**'i aç. Raporlar → **Excel'e aktar**.

**3. "Her şey panelden değişiyor" (3 dk)**: en etkileyici bölüm
1. **Ayarlar → Görünüm → Ana renk**'i kırmızı yap → Kaydet → "Siteyi aç": bütün site kırmızı.
2. **Ayarlar → Genel → Firma adı**'nı değiştir → menü, sayfa başlıkları, PDF sözleşme değişir.
3. **Ayarlar → Fiyatlar → Haftalık indirim oranı**'nı `0.15` yap → araç detayında %15 görünür; **Kiralama Koşulları** sayfası da kendiliğinden "%15" yazar.
4. **Kategoriler → Yeni kategori** "Elektrikli" → araç listesindeki filtrede **hemen** çıkar.
5. **Ayarlar → Araç Listesi** → "Koltuk sayısı" filtresini kapat → listeden kaybolur.
6. **Site Metinleri** → menüdeki "Araçlar"ı "Filomuz" yap. **Sayfalar** → KVKK metnini düzenle.
7. Demodan sonra **Ayarları varsayılana döndür**.

**4. Teknik (2 dk)**
1. `/swagger` → `POST /api/fiyat` dene: "Sitedeki canlı fiyat hesaplayıcı da bu API'yi kullanıyor."
2. `dotnet test` → 66 test yeşil.
3. Kod: `Servisler/RezervasyonServisi.cs` içindeki durum geçiş tablosu (`Gecisler`) ve `OnaylaAsync` transaction'ı. `Modeller/SiteAyarlari.cs`: "yeni ayar = tek satır, form kendiliğinden oluşuyor."

## Hocanın sorabileceği sorular

| Soru | Cevap |
|---|---|
| Aynı araç iki kişiye kiralanabilir mi? | Hayır. `MusaitlikServisi` çakışma kontrolü yapar; onay işlemi **transaction** içinde tekrar kontrol eder ve çakışan bekleyen talepleri reddeder. Testi: `Onaylaninca_cakisan_bekleyen_talepler_otomatik_reddedilir`. |
| Fiyat değişirse eski faturalar ne olur? | Fiyatlar rezervasyon anında kopyalanır; araç fiyatı veya indirim oranı sonradan değişse de eski kayıtlar etkilenmez. |
| Admin panelinden her şey nasıl değişiyor? | Ayarlar veritabanında tek satırlık bir tabloda (`SiteAyarlari`). Ayarlar formu, sınıftaki `[Ayar]` etiketli özelliklerden **reflection** ile otomatik oluşturuluyor; yeni ayar eklemek tek satır. Ayarlar bellekte önbelleğe alınıyor (`IcerikServisi`), kaydedilince yenileniyor. |
| Şunu ekleyebilir misiniz? (yeni filtre, alan...) | Evet, `docs/GELISTIRICI-REHBERI.md`'de hazır tarifler var. Örn. yeni filtre 3 satır: `AracFiltresi`'ne özellik, controller'a `if`, view'a kutu. |
| Hangi veritabanı? | Bilgisayarda kurulumsuz **SQLite**, canlıda kalıcı **PostgreSQL** (Neon). `Program.cs` bağlantı ayarına bakıp kendisi seçiyor; EF Core sayesinde kod aynı. Testlerde ikisinin de tablo oluşturabildiği kontrol ediliyor. |
| Şifreler nasıl saklanıyor? | ASP.NET Core Identity: PBKDF2 ile hash'lenir, düz metin saklanmaz. Canlı veritabanı şifresi koda değil sunucunun ortam değişkenine yazılı. |
| Yetkilendirme nasıl? | Rol bazlı: `[Authorize(Roles = "Admin,Personel")]` (`YonetimController`). Müşteri `/Yonetim`'e girerse "Erişim engellendi". Personel, "Personel ve Roller" sayfasını göremez. |
| Neden servis katmanı? | Kurallar tek yerde, controller'dan bağımsız → test edilebilir. Aynı servis hem MVC sayfaları hem API tarafından kullanılıyor. |
| Gecikmeler nasıl tespit ediliyor? | `Servisler/GecikmeKontrolu.cs` (BackgroundService) saatte bir çalışır, iade tarihi geçmiş kiralamaları işaretler. |
| Nerede yayında, nasıl? | Docker imajı olarak Render.com'da, veritabanı Neon'da; GitHub'a push edince otomatik yayınlanır. Hostinger paketi C# çalıştıramadığı için ardcek.com/automarket alt alan adına yönlendirir. |
| Güvenlik? | Her sayfada Content-Security-Policy (sadece kendi script'lerimiz çalışır, inline script ve dış CDN yok), anti-forgery token, panelde yazılan metinlerin HTML'e güvenli çevrilmesi, yüklenen dosyanın gerçekten resim olduğunun ilk baytlarından kontrolü. `Yardimcilar/Altyapi.cs`, `Servisler/GorselServisi.cs`. |
| T.C. kimlik doğrulaması? | `Yardimcilar/Dogrulama.cs`: resmi algoritma (10. ve 11. hane kontrolü). |

## Ekip iş bölümü (öneri)
- **İş kuralları ve veri**: Modeller, Servisler, testler, API
- **Müşteri arayüzü**: ana sayfa, araç listesi / filtreler / detay, rezervasyon sihirbazı, hesabım
- **Yönetim paneli**: gösterge paneli, takvim, rezervasyon işlemleri, raporlar, **site içeriği yönetimi** (Ayarlar, Metinler, Sayfalar, Görseller)

Her kişi kendi bölümünü sunarsa hoca "kim ne yaptı" sorusunun cevabını doğrudan görür. Kodu anlatırken **[KOD-HARITASI.md](KOD-HARITASI.md)**'deki akışları takip edin.
