# Yapay Zekâ Asistanı — internetsiz kurulum ve kullanım

Yönetim panelindeki **Yapay Zekâ Asistanı** (`/Yonetim/YapayZeka`), siteyi Türkçe yazarak değiştirmenizi sağlar:

> "Meslek gruplarına göre indirim yap, filtrede de indirimli araçlar görünsün"

Model **bu bilgisayarda** çalışır; internet gerekmez. Proje USB ile laboratuvara götürülebilir.

## İki mod

| Mod | Ne yapar | Ne zaman |
|---|---|---|
| **Panel modu** | Yapay zekâ kod yazmaz. İsteği panelin yapabildiği işlemlere çevirir: kampanya ekle, müşteri bilgisi ekle, yazı değiştir, ayar değiştir, filtre/özellik ekle, fiyat değiştir, sayfa ekle… Her işlem önce kart olarak gösterilir, siz **Uygula** dersiniz. | Çoğu istek. Hızlı ve güvenli; her değişiklik geri alınabilir. |
| **Kod modu** (deneysel) | Yapay zekâ kod dosyalarını değiştirir. Önce dosyaları seçer, değişikliği yazar, **geçici bir kopyada derleyip dener**. Siz onaylarsanız dosyalar yedeklenip değiştirilir. | Panelin yapamadığı istekler (yeni bir sayfa düzeni, yeni bir hesap kuralı…). |

> Küçük yerel modeller 1-3 dosyalık değişikliklerde iyidir, büyük özelliklerde sık hata yapar. Kod modunda "Uygula"dan önce kırmızı/yeşil farkı mutlaka okuyun.

---

## 1) Evde, internetle (bir kere)

**Gerekenler:** Windows 10/11, en az 8 GB RAM (16 GB önerilir), ~12 GB boş alan.

1. `AracKiralama\yapay-zeka\HAZIRLA.bat` dosyasına **çift tıklayın**.
2. Şunlar iner (indirme yarıda kalırsa tekrar çalıştırın, kaldığı yerden devam eder):

| Klasör | İçerik | Boyut |
|---|---|---|
| `motor\vulkan`, `motor\cpu` | llama.cpp motoru (`llama-server.exe`). Kurulum istemez. | ~65 MB |
| `modeller\` | Küçük model (Qwen3.5-4B) ve orta model (Qwen3.5-9B) | ~8,4 GB |
| `araclar\dotnet` | Kurulumsuz .NET 10 SDK (laboratuvarda .NET yoksa) | ~300 MB |
| `araclar\nuget` | Projenin paketleri (internetsiz derleme için) | ~300 MB |

Seçenekler: `HAZIRLA.bat -SadeceKucuk` (orta modeli indirmez, ~3 GB daha az), `HAZIRLA.bat -DotnetYok` (laboratuvarda .NET 10 SDK kuruluysa).

## 2) USB'ye kopyalama

- USB **exFAT ya da NTFS** biçiminde olmalı. **FAT32, 4 GB'tan büyük dosyayı almaz** (orta model 5,7 GB). 32 GB'lık bir USB yeter.
- Bütün `automarket` klasörünü (ya da sadece `AracKiralama` klasörünü) kopyalayın.
- **Proje OneDrive / Masaüstü klasöründeyse** modelleri oraya indirmeyin: OneDrive ~9 GB'ı buluta yüklemeye çalışır.
  Ya doğrudan USB'ye indirin: `HAZIRLA.bat -Hedef E:\automarket\AracKiralama\yapay-zeka`
  ya da başka bir klasöre indirip (`-Hedef C:\rota-yapay-zeka`) o klasörün içindekileri USB'deki `AracKiralama\yapay-zeka\` klasörüne kopyalayın.
- USB'den doğrudan çalışır, ama laboratuvar bilgisayarının diskine kopyalamak model yüklemeyi hızlandırır.

## 2b) Tek klasör + zip (başka bilgisayara kurulum)

`PAKETLE.bat` her şeyi (proje, motor, modeller, taşınabilir .NET, paketler) tek klasörde toplayıp zip yapar.
Zip'i alan bilgisayarda hiçbir şey kurmak ve internet gerekmez.

1. Bu bilgisayarda, internetle, bir kere: `HAZIRLA.bat -Hedef C:\Users\<ad>\rota-yapay-zeka` (taşınabilir .NET de insin).
2. `PAKETLE.bat -Ad Rota-Hafta4` → `C:\Users\<ad>\Rota-Hafta4\` klasörü ve `C:\Users\<ad>\Rota-Hafta4.zip` (iki modelle ~9,5 GB).
   `-SadeceKucuk` ile sadece küçük model girer: ~3,5 GB, FAT32 USB'ye de sığar.
3. USB'ye (exFAT ya da NTFS) koyun: büyük zip, `paket\KUR.bat`, `paket\TESHIS.bat` ve paketin kökündeki `KURULUM-OKU.txt`.
   (İsteğe bağlı `automarket.zip`: sadece proje. Varsa KUR.bat projeyi ondan açar; haftalık güncellemede sadece bunu değiştirmek yeter.)
4. Diğer bilgisayarda: USB'deki `KUR.bat`'a çift tık (internetsiz). Proje `Masaüstü\automarket\AracKiralama\`,
   yapay zekâ dosyaları `C:\Users\<ad>\rota-yapay-zeka\` klasörüne kurulur; sonra her seferinde
   `Masaüstü\automarket\AracKiralama\yapay-zeka\BASLAT.bat`.
   `BASLAT.bat` yapay zekâ dosyalarını önce kendi klasöründe, yoksa `C:\Users\<ad>\rota-yapay-zeka` ve `C:\rota-yapay-zeka`'da arar.
5. "Derleme başarısız" olursa: o bilgisayarda USB'deki `TESHIS.bat` → USB'ye `teshis-<bilgisayar>.txt` yazar (Windows, disk, antivirüs, .NET bilgisi ve derleme çıktısı).

Paket OneDrive / Masaüstü dışında kurulur: içine koyulursa OneDrive ~10 GB'ı buluta yüklemeye çalışır.

## 3) Laboratuvarda (internetsiz)

1. `AracKiralama\yapay-zeka\BASLAT.bat` dosyasına **çift tıklayın**. Siyah pencere açık kalmalı.
2. ~15 saniye sonra tarayıcı açılır. Açılmazsa: <http://localhost:5046/Yonetim/YapayZeka>
3. Giriş: `admin@rota.com` / `Rota123!`
4. Üstteki durum çubuğu yeşil (**Hazır**) olana kadar bekleyin. İlk açılışta model belleğe yüklenir (10-60 sn).
5. İsteğinizi yazın → **Gönder** → kartları okuyun → gerekirse değerleri değiştirin → **Seçilenleri uygula**.
6. Beğenmediyseniz sayfanın altındaki **Geçmiş**'ten **Geri al**.

Kapatmak: siyah pencerede `Ctrl+C`, sonra `C` (ya da pencereyi kapatın). Yapay zekâ motoru da kendiliğinden kapanır, bellekte kalmaz.

### Model seçimi ve hız

Model kendiliğinden seçilir: bilgisayarın belleği 16 GB ve üzeriyse orta model, değilse küçük model. Durum çubuğundan değiştirebilirsiniz (**Başlat**).

Ölçülen süreler (2026-11-11, i7-12650H dizüstü, 32 GB RAM, RTX 4060 8 GB):

| Durum | Motorun açılması | İlk istek | Sonraki istekler | Kod modu (öneri + deneme derlemesi) |
|---|---|---|---|---|
| Ekran kartıyla, küçük model (4B) | 10-15 sn | ~28 sn | 1-8 sn | — |
| Ekran kartıyla, orta model (9B) | ~15 sn | ~15 sn | 2-5 sn (SSS sayfası gibi uzun yazılar ~20 sn) | 15-45 sn + ~4 sn |
| Sadece işlemci, küçük model (4B) | ~10 sn | ~67 sn | 7-8 sn | önerilmez |

- **İlk istek** uzundur: model, talimatı ve sitenin durumunu bir kez okur. Sonraki istekler bunu önbellekten kullanır.
- Laboratuvar bilgisayarının işlemcisi daha zayıfsa süreler 2-3 katı olabilir.
- Ekran kartı varsa motor onu kendiliğinden kullanır (Vulkan). Yoksa işlemciyle çalışır.

**Doğruluk** (aynı bilgisayarda, `tests/uctan-uca/yapay-zeka.mjs` ile 11 örnek istek):
- Orta model (9B): 11/11 doğru.
- Küçük model (4B): 10/11 doğru. Bazen fazladan bir kart ekler ya da bir değeri atlar. Hatalı kartlar kırmızı ve işaretsiz gelir.

Her durumda kartları okuyup öyle uygulayın.

---

## Kod modu nasıl kullanılır?

1. **Kod modu** sekmesi → isteği yazın → **1) Dosyaları bul**. Listeye dosya ekleyip çıkarabilirsiniz.
2. **2) Değişikliği hazırla** → kırmızı (silinen) ve yeşil (eklenen) parçaları okuyun.
3. **3) Dene** → değişiklik geçici bir kopyada derlenir. Gerçek dosyalar değişmez. Hata çıkarsa **Hataları yapay zekâya düzelt**.
4. **4) Uygula** → dosyaların eski hali `AracKiralama\.yapay-zeka-yedek\<tarih>\` klasörüne yedeklenir, sonra değiştirilir.
5. **Şimdi yeniden başlat** → `BASLAT.bat` projeyi yeniden derleyip açar, sayfa kendini yeniler.

- Bir **model dosyası** (Modeller/ ya da Veri/) değiştiyse yerel veritabanı (`arackiralama.db`) yedeklenip örnek verilerle yeniden oluşturulur (proje migration kullanmıyor).
- **Uygulama açılmazsa:** `BASLAT.bat` "G" tuşuyla son değişikliği geri almayı teklif eder. Ya da `GERI-AL.bat`'a çift tıklayın.
- Denendi (orta model, ekran kartıyla): "Araç kartında kilometre de görünsün" ve "Araç detay sayfasında plaka gösterilsin" istekleri dosya seçiminden geri almaya kadar 9 adımın hepsini geçti. Bir denemede model olmayan bir alan adı uydurdu; deneme derlemesi bunu yakaladı, gerçek dosyalara dokunulmadı.
- İstekte hangi sayfa olduğunu açık yazın: "müşterinin gördüğü araç detay sayfası" ya da "yönetim panelindeki araç detayı".
- Kod modu sadece **Admin** hesabıyla ve proje kaynak koddan çalışırken açıktır. Canlı sitede (Render) kapalıdır.

---

## Sık sorunlar

| Sorun | Çözüm |
|---|---|
| "Bu bilgisayarda yapay zekâ kurulu değil" | `HAZIRLA.bat` çalıştırılmamış ya da `motor` / `modeller` klasörleri kopyalanmamış. |
| Durum "Hata", motor açılamadı | Durum çubuğunun altındaki kırmızı satırları okuyun. Bellek yetmiyorsa küçük modeli seçip **Başlat**. Antivirüs `llama-server.exe`'yi engellemiş olabilir. |
| Çok yavaş | Küçük modeli seçin. İstekleri kısa ve tek konulu yazın. |
| ".NET bulunamadı" | `HAZIRLA.bat` `-DotnetYok` olmadan çalıştırılmalı (taşınabilir .NET `araclar\dotnet` klasörüne iner). |
| Paket hatası (NU1101, NU1301) | `araclar\nuget` klasörü eksik. Evde `HAZIRLA.bat`'ı yeniden çalıştırın. |
| Port 5046 kullanımda | Önceki pencere açık kalmış; kapatın. Gerekirse Görev Yöneticisi'nden `AracKiralama.Web` işlemini sonlandırın. |
| Yapay zekâ yanlış anladı | Kartın işaretini kaldırın ya da değerini düzeltin. İsteği daha açık yazın ("SUV kategorisindeki araçlara %10 indirim"). |

## Ollama ya da LM Studio kullanmak

Bilgisayarda zaten Ollama / LM Studio varsa `src\AracKiralama.Web\appsettings.json` → `"YapayZeka"` → `"Adres"` satırına adresini yazın
(Ollama: `http://127.0.0.1:11434`, LM Studio: `http://127.0.0.1:1234`). O zaman uygulama kendi motorunu açmaz.

## Nasıl çalışıyor? (kod)

| Dosya | Görevi |
|---|---|
| `Servisler/YapayZeka/YapayZekaSunucusu.cs` | `llama-server.exe`'yi açar / kapatır, modeli seçer |
| `Servisler/YapayZeka/YapayZekaServisi.cs` | Modele soru sorar (cevap JSON şemasına uymak zorunda) |
| `Servisler/YapayZeka/YapayZekaBaglami.cs` | Modele gönderilen talimat: kurallar + sitenin durumu + örnekler |
| `Servisler/YapayZeka/PanelIslemleri.cs` | Asistanın yapabildiği işlemler (doğrula / uygula / geri al). **Yeni yetenek = buraya yeni sınıf.** |
| `Servisler/YapayZeka/KodModu.cs` | Kod modu: dosya seçimi, öneri, deneme derlemesi, yedekli uygulama |
| `Areas/Yonetim/Controllers/YapayZekaController.cs` | Sayfa ve uç noktalar |
| `wwwroot/js/yapay-zeka.js` | Sayfanın davranışı |

> Sınavda ya da bir değerlendirmede kullanmadan önce hocanıza sorun.
