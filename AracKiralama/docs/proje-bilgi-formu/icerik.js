// PROJE BİLGİ FORMU — Rota Rent a Car (Grup 14) için doldurulacak metinler.
// Yazım kuralları:  "metin"            → normal paragraf (**kalın** parçalar desteklenir)
//                   { m: "metin" }      → madde işaretli paragraf
//                   { b: "metin" }      → kalın alt başlık (ardından gelen paragrafın başına eklenir)
//                   { r: "metin" }      → başı kalın paragraf
//                   { k: "metin" }      → küçük, italik not
module.exports = {
  grupNo: "14",
  baslik: "Rota Rent a Car: Web Tabanlı Araç Kiralama Otomasyonu",
  lider: { no: "m255035001", ad: "Mehmet Arda Çekiç" },
  katilimcilar: [
    { no: "m255035059", ad: "Azra Ardıç" },
    { no: "m255035077", ad: "Esra Ağlar" },
    { no: "m265035083", ad: "Ömer Faruk Çelik" },
  ],

  // ───────────────────────────────── ULUSAL KAZANIM ve PROJENİN ÖNEMİ
  ulusalKazanim: [
    "Araç kiralama, turizm ve iş seyahatleriyle birlikte her yıl büyüyen bir sektör. Garenta, Avis, Enterprise, Sixt ve Europcar gibi büyük firmaların gelişmiş siteleri ve kendilerine ait yazılımları var. Ama Konya gibi şehirlerde çalışan küçük kiralama firmalarının çoğu rezervasyonu hâlâ telefonla ya da WhatsApp üzerinden alıyor. Hangi aracın hangi gün dolu olduğunu, kimin ne kadar ödediğini ve araçtaki hasarları Excel'de ya da bir deftere yazarak takip ediyor. Ön çalışmamızda bu çalışma şeklinin şu sorunlara yol açtığını gördük:",
    { m: "Aynı araç aynı günler için iki ayrı müşteriye verilebiliyor." },
    { m: "Kira bedeli, uzun kiralama indirimi, geç iade ve eksik yakıt ücreti elle hesaplandığı için hata yapılıyor, bu da müşteriyle tartışmaya sebep oluyor." },
    { m: "Sürücünün yaşı ya da ehliyet süresi gibi şartlar bazen hiç kontrol edilmeden araç teslim ediliyor." },
    { m: "Sitedeki küçük bir yazıyı ya da bir fiyatı değiştirmek için bile yazılımcı çağırmak gerekiyor." },
    { m: "Yönetici o gün hangi aracın teslim edileceğini, hangisinin geri geleceğini tek bir ekranda göremiyor." },
    "Ekim 2026'da altı kiralama sitesini 15 farklı açıdan karşılaştırdık ve sonuçları Proje Öneri ve Emsal Analizi Raporu'nda topladık. Büyük firmaların siteleri müşteri için gerçekten iyi, fakat bu sistemler küçük bir firmanın karşılayabileceği fiyatta ya da yapıda değil. Biz de Rota Rent a Car projesinde müşterinin kullandığı web sitesini ve şube çalışanlarının kullandığı yönetim panelini tek bir uygulamada, aynı veritabanı üzerinde birleştirdik. Projenin ülkemize sağlayacağını düşündüğümüz faydalar şunlar:",
    { m: "Tamamen yerli ve Türkçe bir ürün ortaya çıkıyor. Kodun içindeki sınıf, tablo ve sayfa adlarına kadar her şey Türkçe olduğu için Türkçe bilen bir geliştirici programı kolayca okuyup geliştirebilir. Meslek yüksekokullarında örnek proje olarak da kullanılabilir." },
    { m: "Küçük firmalar az masrafla dijitale geçebilir. Lisans ücreti olan hiçbir yazılım kullanmadık (.NET, PostgreSQL ve SQLite ücretsiz). Uygulama ücretsiz bulut servislerinde ya da firmanın kendi bilgisayarında çalışabiliyor. On İkinci Kalkınma Planı'nda (2024-2028) KOBİ'lerin dijitalleşmesine önem verilmesi de projemizle aynı yönde." },
    { m: "Firmanın yazılımcıya ve yabancı programlara bağımlılığı azalır. Hazır kiralama programları genelde aylık ücretli ve firmanın verileri başka şirketlerin sunucularında duruyor. Bizim sistemde firma sahibi site yazılarını, fiyatları, arama filtrelerini ve araç özelliklerini yönetim panelinden kendisi değiştirebiliyor." },
    { m: "Kişisel veriler daha iyi korunur. Sitede reklam ya da takip kodu yok, kullandığımız kütüphaneler dışarıdan değil uygulamanın içinden yükleniyor. Veritabanı bilgisi kodun içine yazılmadığı için gerektiğinde veritabanı 6698 sayılı KVKK'ya uygun şekilde Türkiye'deki bir sunucuya kod değiştirmeden taşınabilir." },
    { m: "Müşteri daha güvenilir bir hizmet alır. Ödeyeceği tutarın kira, indirim, ek hizmet ve depozito olarak nasıl oluştuğunu kiralamadan önce görüyor. Sürücü şartları rezervasyon sırasında kendiliğinden kontrol ediliyor ve aynı araç iki kişiye verilemiyor." },
    "Ayrıca dört öğrenci olarak bir yazılımı ilk analizden internette yayına almaya kadar kendimiz yapmış oluyoruz. Kısacası amacımız, büyük firmaların kullandığı sistemlere ulaşamayan küçük kiralama firmaları için ucuz, Türkçe ve gerçekten kullanılabilecek bir program hazırlamak.",
  ],

  // ───────────────────────────────── AMAÇ VE HEDEFLER
  amac: [
    { r: "**Amaç.** Bu projede bir araç kiralama firması için iki parçadan oluşan bir sistem yapıyoruz: müşterilerin araç arayıp kiraladığı bir web sitesi ve şube çalışanlarının günlük işlerini takip ettiği bir yönetim paneli. Fiyat hesabı, aracın boş olup olmadığı ve sürücü şartları gibi kuralları sistemin kendisinin uygulamasını istiyoruz. Bunun yanında yazılım bilmeyen bir yöneticinin de siteyi panelden değiştirebilmesi hedefimiz. Projeyi C# ve ASP.NET Core MVC ile geliştiriyoruz; veritabanı olarak bilgisayarda SQLite, internette PostgreSQL kullanıyoruz." },
    { r: "**Hedefler.** Hedeflerimizi ölçülebilir olacak şekilde iş paketlerine (İP) böldük. İlk dört hedefe ulaştık ve her birini haftalık olarak teslim ettik:" },
    { m: "**H1 (İP1):** 6 kiralama sitesini 15 ölçüte göre karşılaştırmak; kullanıcı rollerini (ziyaretçi, müşteri, personel, yönetici) ve kiralama kurallarını yazılı hâle getirmek. Tamamlandı." },
    { m: "**H2 (İP2):** En az 8 filtre ve 5 sıralama seçeneği olan bir araç listesi ile fiyatı kalem kalem gösteren bir araç detay sayfası yapmak. Tamamlandı." },
    { m: "**H3 (İP2):** Veritabanını Entity Framework Core ile C# sınıflarından oluşturmak ve internetteki PostgreSQL veritabanına (Neon) bağlamak; bağlantı şifresini hiçbir kod dosyasına yazmamak. Tamamlandı (23 tablo, 22 ilişki)." },
    { m: "**H4 (İP3):** Kod değiştirmeden yönetim panelinden araçlara yeni özellik ve filtre ekleyebilmek (seçim listesi, sayı, var/yok ve yazı olmak üzere 4 tür). Tamamlandı. Tablo sayısı 25'e çıktı ve denediğimiz 11 filtrenin hepsinde site ile SQL sorgusu aynı sonucu verdi." },
    { m: "**H5 (İP4):** Rezervasyonun 3 adımda bitmesi, aynı araç için çakışan tarihlere ikinci bir rezervasyonun onaylanamaması ve yaş ile ehliyet yılı şartının her rezervasyonda kontrol edilmesi." },
    { m: "**H6 (İP5):** Bir rezervasyonu onaydan teslime, iadeye ve ödemeye kadar panelden yürütebilmek. Geç iade ve eksik yakıt ücretini sistem hesaplayacak; sözleşme PDF, raporlar Excel olarak alınabilecek." },
    { m: "**H7 (İP6):** Firma adı, renkler, site yazıları, sayfalar ve kampanyaların panelden, kaydetmeden önce önizlenerek değiştirilebilmesi." },
    { m: "**H8 (İP7):** Kuralları otomatik testlerle denemek ve testlerin hepsinin geçmesi; sitenin HTTPS ile yayında olması; OWASP Top 10 listesindeki risklere karşı önlem almak; kullanılabilirlik anketinde (SUS) en az 68 puan almak." },
    { r: "**Kapsam dışı.** Gerçek kredi kartıyla ödeme almak (ödemeleri sadece kayıt olarak tutuyoruz), mobil uygulama (site zaten telefona uyumlu) ve başka dil desteği bu projede yok." },
  ],

  // ───────────────────────────────── YÖNTEM
  yontem: [
    "Projeyi haftalık parçalar hâlinde yürütüyoruz. Her hafta çalışan ve denenmiş bir bölümü teslim edip danışmanımıza gösteriyoruz, aldığımız geri bildirime göre de bir sonraki haftayı planlıyoruz. Bu yaklaşım Scrum gibi çevik yöntemlerdeki kısa döngü mantığına dayanıyor (Schwaber ve Sutherland, 2020). Aşağıda hangi işi nasıl yaptığımızı iş paketleriyle birlikte anlattık.",
    { b: "1. Ön çalışma (İP1)" },
    "İşe başlamadan önce Garenta, Yolcu360, Avis, Enterprise, Sixt ve Europcar sitelerini inceledik. Ana sayfa, arama formu, araç listesi, rezervasyon adımları ve fiyatın nasıl gösterildiği gibi 15 başlıkta bir karşılaştırma tablosu hazırladık. Bu incelemeden sonra kullanıcı rollerini, projeye neyin girip neyin girmeyeceğini ve sistemin kendiliğinden uygulayacağı kuralları belirledik. Gün hesabı, uzun kiralama indirimi, tek yön ücreti, geç iade ve yakıt ücreti, iptal kuralı ve sürücü şartları bu kurallardan bazıları.",
    { b: "2. Uygulamanın yapısı (İP1-İP2)" },
    "Uygulamayı ASP.NET Core MVC ile, yani Model-View-Controller yapısıyla yazıyoruz. Sayfalar, controller'lar, iş kurallarının bulunduğu servisler ve veritabanı erişimi ayrı katmanlarda duruyor. Fiyat hesabı gibi kuralları yalnızca servislerde yazdığımız için hem web sayfaları hem de REST API aynı kodu kullanıyor; bir kuralı değiştirdiğimizde her yerde birden değişiyor. Yönetim paneli de uygulamanın içinde ayrı bir bölüm (Area) olarak duruyor.",
    { b: "3. Veritabanı (İP2-İP3)" },
    "Veritabanını ilişkisel olarak tasarladık ve tablolar arasındaki bağlantıları bir ER diyagramıyla gösterdik. Tabloları elle oluşturmak yerine Entity Framework Core ile C# sınıflarından ürettik (code-first). Aynı yapı bilgisayarda SQLite, internette Neon üzerindeki PostgreSQL ile çalışıyor. Panelden araçlara yeni özellik eklenebilmesi için özelliğin tanımını AracOzellikleri, araçtaki değerini AracOzellikDegerleri tablosunda tutuyoruz. İkinci tabloda araç ve özellik numarası birlikte anahtar olduğu için aynı araca aynı özellik iki kez yazılamıyor; bir araç silinince ona ait değerler de siliniyor. Böylece yeni bir özellik için tabloya sütun eklemek, yani kodu değiştirmek gerekmiyor.",
    { b: "4. Kurallar ve verinin tutarlı kalması (İP4-İP5)" },
    "Rezervasyon onaylanırken bütün adımlar tek bir işlem (transaction) içinde yapılıyor. Aracın o tarihlerde boş olup olmadığı onay anında tekrar kontrol ediliyor ve aynı araca gelen çakışan diğer talepler reddediliyor. Bir rezervasyon Onay Bekliyor, Onaylandı, Kirada, Tamamlandı, İptal Edildi ya da Reddedildi durumlarından birinde oluyor ve sadece izin verilen sırayla ilerleyebiliyor. Rezervasyon anındaki fiyat da kayda yazılıyor; böylece sonradan fiyat değişse bile eski kayıtlar etkilenmiyor. İade tarihi geçtiği hâlde gelmeyen araçları arka planda çalışan bir servis saatte bir kontrol edip gecikmiş olarak işaretliyor.",
    { b: "5. Arayüz (İP1-İP6)" },
    "Arayüzü Bootstrap 5.3 ile yaptık; site telefonda da düzgün görünüyor ve açık ya da koyu tema seçilebiliyor. Tasarlarken Nielsen'in kullanılabilirlik ilkelerinden yararlandık, örneğin kullanıcıya ne olduğunu her an göstermeye ve hataları baştan önlemeye dikkat ettik (Nielsen, 1994). Seçilen filtreler adres çubuğunda durduğu için bir aramanın bağlantısı başka birine gönderilebiliyor.",
    { b: "6. Güvenlik" },
    "Güvenlik önlemlerini OWASP Top 10 listesine bakarak planladık (OWASP, 2021). Şifreler ASP.NET Core Identity ile açık hâlde değil, özet (hash) olarak saklanıyor. Yönetim paneline sadece yetkisi olan roller girebiliyor, formlarda sahte isteklere karşı CSRF koruması var ve veritabanı sorguları parametreli çalışıyor. Sitede yalnızca kendi betiklerimizin çalışmasına izin veriyoruz (CSP) ve canlı sitede HTTPS zorunlu. Yüklenen bir dosyanın gerçekten resim olup olmadığını uzantısına değil, dosyanın içine bakarak kontrol ediyoruz. Kişisel veriler konusunda 6698 sayılı KVKK'yı esas alıyoruz.",
    { b: "7. Test (her iş paketinde)" },
    "Kurallar servislerde toplandığı için onları arayüzden bağımsız olarak test edebiliyoruz. Testleri xUnit ile yazıyoruz; her test bellekte açılan boş bir SQLite veritabanında çalışıyor. Fiyat, müsaitlik, rezervasyon durumları, sürücü şartları, iade ücretleri, iptal ve kampanyalar için ayrı testlerimiz var. Her haftanın sonunda müşteri, personel ve yönetici hesaplarıyla siteyi baştan sona deniyoruz. Veritabanıyla ilgili özelliklerde sitenin gösterdiği sonucu, aynı soruyu doğrudan SQL ile sorduğumuzda çıkan sonuçla karşılaştırıyoruz. 4. haftada denediğimiz 11 filtrenin hepsinde iki sonuç aynı çıktı.",
    { b: "8. Ölçme ve değerlendirme" },
    "Projenin ölçme kısmında sistemin kolay kullanılıp kullanılmadığına ve kuralları doğru uygulayıp uygulamadığına bakacağız. Bunun için üç ölçüm planladık:",
    { m: "**Kullanılabilirlik (İP7):** Bölümümüzden en az 10 gönüllü öğrenciden üç iş yapmasını isteyeceğiz: belirli tarihlerde boş bir SUV bulmak, sonuçları filtrelerle daraltmak ve rezervasyon talebi oluşturmak. Her öğrencinin işi bitirip bitiremediğine ve ne kadar sürdüğüne bakacağız, sonunda da SUS anketini dolduracaklar (Brooke, 1996). Burada değişkenlerimiz görev türü, görevi tamamlama oranı (%), süre (saniye) ve SUS puanı. Sonuçları ortalama, standart sapma ve yüzde olarak vereceğiz. SUS'ta 68 puan ortalama kabul edildiği için hedefimiz bunun üstünde kalmak." },
    { m: "**Hesapların doğruluğu (İP4-İP6):** Fiyat, indirim, geç iade ve yakıt ücretlerinde sistemin bulduğu tutarı aynı örnekler için elle yaptığımız hesapla karşılaştıracağız. Hepsinin tutması gerekiyor." },
    { m: "**Çift rezervasyon (İP4):** Aynı araç ve tarih için aynı anda onay verilmeye çalışıldığında kaç çift rezervasyon oluştuğunu sayacağız. Beklediğimiz sayı 0." },
    { b: "9. Yayın (İP2, İP7)" },
    "Uygulamayı Docker ile paketledik. GitHub'a gönderdiğimiz her değişiklik Render.com'da kendiliğinden derlenip yayına giriyor. Canlı veritabanı Neon'da duruyor ve bağlantı bilgisi kodda değil, sunucunun ayarlarında yazılı. Site automarket.ardcek.com adresinde HTTPS ile açık; /health adresiyle sitenin çalışıp çalışmadığını kontrol ediyoruz.",
    { b: "Ön çalışma ve yapılabilirlik" },
    "Emsal incelemesini ve hangi teknolojileri kullanacağımızı Ekim 2026'da belirledik. Şu ana kadar bitirdiğimiz İP1, İP2 ve İP3, seçtiğimiz yapının bu süre içinde yapılabileceğini gösterdi.",
  ],

  // ───────────────────────────────── İŞ-ZAMAN ÇİZELGESİ
  // [İP no, adı ve hedefleri, kim(ler), zaman aralığı, başarı ölçütü ve katkı]
  isPaketleri: [
    ["1",
      ["**Emsal inceleme, gereksinimler ve ana sayfa.**", "6 kiralama sitesinin incelenmesi, kullanıcı rolleri ve kuralların belirlenmesi, ana sayfanın tasarlanması."],
      ["**M. Arda Çekiç:** emsal inceleme, proje iskeleti, yayın", "**Azra Ardıç:** giriş alanı, arama formu, tema", "**Esra Ağlar:** kategori ve araç kartları", "**Ömer F. Çelik:** menü, alt bilgi, şubeler, test"],
      ["1. hafta", "(Ekim 2026)", "**Tamamlandı**"],
      ["6 site 15 ölçütle karşılaştırıldı. Ana sayfa ardcek.com/hafta1 adresinde, telefonda ve bilgisayarda düzgün açılıyor.", "Katkısı: projenin sınırlarını ve kurallarını belirliyor."]],
    ["2",
      ["**Müşteri sayfaları ve veritabanı bağlantısı.**", "Araç listesi, araç detayı ve anlık fiyat dökümü, giriş ve üye ol formları, veritabanının Neon PostgreSQL'e bağlanması."],
      ["**M. Arda Çekiç:** veritabanı, Neon, fiyat hesabı", "**Azra Ardıç:** araç listesi ve filtreler", "**Esra Ağlar:** formlar, doğrulama, araç detayı", "**Ömer F. Çelik:** testler ve rapor"],
      ["2-3. hafta", "(Ekim-Kasım 2026)", "**Tamamlandı**"],
      ["8 filtre, 5 sıralama ve 47 araç sayfası hazır. Neon'da 23 tablo ve 22 ilişki oluştu, bağlantı bilgisi kodda yok.", "Katkısı: sistemin veri temelini kuruyor."]],
    ["3",
      ["**Panelden araç özelliği ve filtre ekleme.**", "Kod değiştirmeden yeni özellik tanımlama (4 tür), değerleri topluca girme, sitede filtre, kart ve detayda gösterme."],
      ["**M. Arda Çekiç:** yeni tablolar, ilişkiler, Neon", "**Azra Ardıç:** filtre kutuları, sorgular, kart", "**Esra Ağlar:** özellik ve değer formları", "**Ömer F. Çelik:** test, Neon karşılaştırması"],
      ["4. hafta", "(Kasım 2026)", "**Tamamlandı**"],
      ["4 özellik türü çalışıyor. 11 filtrenin hepsinde site ile SQL aynı sonucu verdi; Neon'da 25 tablo, 24 ilişki var.", "Katkısı: yeni filtreler kod yazmadan ekleniyor."]],
    ["4",
      ["**Üyelik, müşteri hesabı ve rezervasyon.**", "Gerçek giriş, kayıt ve roller; profil (ehliyet bilgileri); 3 adımlı rezervasyon; müsaitlik, sürücü şartı ve iptal kuralları; sayfaların Neon'dan okunması."],
      ["**Esra Ağlar:** rezervasyon adımları, profil", "**M. Arda Çekiç:** rezervasyon servisleri, Neon", "**Azra Ardıç:** arama ve detay bağlantısı", "**Ömer F. Çelik:** birim ve entegrasyon testleri"],
      ["5-6. hafta", "(Kasım 2026)"],
      ["Aynı anda onay denemesinde çift rezervasyon olmaması, şartı sağlamayan müşterinin engellenmesi, rezervasyon testlerinin hepsinin geçmesi.", "Katkısı: kiralama işini gerçekten çalışır hâle getiriyor."]],
    ["5",
      ["**Şube işlemleri ve raporlar.**", "Talep onayı, teslim ve iade; geç iade ve yakıt ücretinin otomatik hesabı; ödeme, bakım ve hasar kayıtları; özet ekranı; PDF sözleşme ve Excel rapor."],
      ["**Esra Ağlar:** onay, teslim/iade, ödeme, hasar", "**M. Arda Çekiç:** ücretler, gecikme kontrolü", "**Ömer F. Çelik:** raporlar, PDF sözleşme, Excel", "**Azra Ardıç:** özet ekranı, doluluk takvimi"],
      ["7-8. hafta", "(Aralık 2026)"],
      ["Bir rezervasyonun talepten \"Tamamlandı\"ya kadar panelden yürütülmesi; ücretlerin elle yapılan hesapla birebir tutması.", "Katkısı: şubenin işlerini bilgisayara taşıyor."]],
    ["6",
      ["**Kod yazmadan yönetim, kampanyalar ve yorumlar.**", "Site düzenleyici (önizlemeli), içerik sayfaları (Hakkımızda, İletişim, Koşullar, KVKK), kampanyalar ve müşteri bilgileri, müşteri yorumları."],
      ["**Ömer F. Çelik:** ayarlar ve içerik sayfaları", "**Azra Ardıç:** site düzenleyici, önizleme", "**M. Arda Çekiç:** kampanya kuralları", "**Esra Ağlar:** müşteri bilgileri, yorumlar"],
      ["9-10. hafta", "(Aralık 2026)"],
      ["Yazıların, sayfaların, kampanyaların ve filtrelerin kod yazmadan değiştirilmesi; kampanya indiriminin fiyat dökümünde ayrı satırda görünmesi.", "Katkısı: firmanın yazılımcıya ihtiyacını azaltıyor."]],
    ["7",
      ["**API, güvenlik, test ve yayın.**", "REST API ve Swagger, OWASP Top 10 kontrolleri, otomatik testler, kullanılabilirlik anketi (SUS), Docker ile Render'da yayın."],
      ["**Ömer F. Çelik:** REST API, Swagger, test planı", "**M. Arda Çekiç:** Docker, Render, güvenlik", "**Azra Ardıç:** erişilebilirlik, SUS anketi", "**Esra Ağlar:** kabul testi senaryoları"],
      ["11-12. hafta", "(Aralık 2026 -", "Ocak 2027)"],
      ["Otomatik testlerin hepsinin geçmesi, 3 rolün kabul testlerinin bitmesi, SUS puanının 68 ve üstü olması, canlı sitede HTTPS ve /health'in çalışması.", "Katkısı: ürünü kullanıma hazır hâle getiriyor."]],
  ],
  isPaketiNotu: "Haftaları, ilk teslimi yaptığımız Ekim 2026 haftasından (1. hafta) başlayarak saydık. İP1, İP2 ve İP3 bitti; çıktıları ardcek.com/hafta1, ardcek.com/hafta3 ve ardcek.com/hafta4 adreslerinde.",

  // ───────────────────────────────── RİSK YÖNETİMİ TABLOSU  [İP no, risk, B planı]
  riskler: [
    ["Tümü", "Sürenin yetmemesi ya da birimizin hastalık, sınav gibi sebeplerle işine yetişememesi.",
      "İşleri \"mutlaka olmalı\" ve \"olursa iyi olur\" diye ayırdık, önce zorunlu olanları bitiriyoruz. Kod Türkçe ve her dosyanın başında ne işe yaradığı yazılı olduğu için birinin işini başka biri devralabiliyor. Bunun için bir kod haritası ve geliştirici rehberi de hazırladık."],
    ["2, 4, 7", "Ücretsiz bulut servislerinde (Render, Neon) kesinti olması, sitenin geç açılması, kotanın dolması ya da ücretsiz kullanım şartlarının değişmesi.",
      "Uygulama SQLite ile kurulum ve internet olmadan da çalışıyor; sunumlarda yedek olarak bilgisayardan açabiliyoruz. Veritabanı bilgisi kodda değil ayarlarda olduğu için başka bir PostgreSQL servisine kod değiştirmeden geçebiliriz. Sunucunun uykuya geçmemesi için /health adresine düzenli istek gönderiyoruz."],
    ["4, 5", "Aynı anda yapılan işlemlerde çift rezervasyon ya da tutarsız kayıt oluşması.",
      "Onay tek bir işlem içinde ve müsaitlik yeniden kontrol edilerek yapılıyor, çakışan talepler reddediliyor. Bu durumu her değişiklikten sonra testlerle deniyoruz. Bir hata olursa rezervasyon \"Onay Bekliyor\" durumunda kalıyor, veri bozulmuyor."],
    ["2, 4, 7", "Veritabanı şifresinin ya da kişisel verilerin dışarı sızması.",
      "Şifreler koda ve GitHub'a yazılmıyor; bilgisayarda user-secrets, sunucuda ortam değişkeni kullanıyoruz. Bir şüphe olursa veritabanı şifresini hemen değiştiriyoruz. Projede gerçek müşteri bilgisi yok, bütün kayıtlar örnek."],
    ["5, 6", "Ücret ve indirim hesaplarında (geç iade, yakıt, kampanya) hata yapılması.",
      "Her kural için test yazıyoruz ve sonucu elle yaptığımız hesapla karşılaştırıyoruz. Rezervasyon anındaki fiyat kayda yazıldığı için sonradan yapılan fiyat değişikliği eski kayıtları etkilemiyor."],
    ["3, 6", "Değerlendirme sırasında yeni bir özellik, filtre ya da kural istenmesi.",
      "Yeni filtre ve özellikleri panelden kod yazmadan ekleyebiliyoruz. Kod gerektiren durumlar için \"yeni filtre ya da yeni alan nasıl eklenir\" adımlarını geliştirici rehberine yazdık."],
    ["7", "Sunum yapılacak laboratuvarda internet olmaması.",
      "Bootstrap, ikonlar ve yazı tipleri uygulamanın içinde olduğu için site internetsiz de eksiksiz çalışıyor. Projeyi USB bellekte taşıyıp bilgisayarda SQLite ile açabiliyoruz."],
  ],

  // ───────────────────────────────── ARAŞTIRMA OLANAKLARI TABLOSU  [altyapı, kullanım amacı]
  olanaklar: [
    ["Konya Teknik Üniversitesi bilgisayar laboratuvarı (bilgisayarlar, projeksiyon)", "Haftalık sunumlar, canlı gösterim ve uygulamanın internetsiz denenmesi."],
    ["Ekip üyelerinin kendi dizüstü bilgisayarları", "Kod yazma, testleri çalıştırma ve raporları hazırlama."],
    ["Ücretsiz geliştirme araçları: .NET 10 SDK, Visual Studio 2026 / Visual Studio Code, Git", "Kod yazma, derleme, hata ayıklama ve sürüm takibi."],
    ["GitHub", "Ortak çalışma ve değişiklik geçmişi; her gönderimde sitenin otomatik olarak yeniden yayınlanması."],
    ["Neon (internette PostgreSQL, ücretsiz kullanım, Frankfurt)", "Canlı veritabanı: tabloların ve ilişkilerin oluşturulması, SQL sorgularıyla kontrol."],
    ["Render.com (ücretsiz kullanım, Docker) ve ekibimize ait ardcek.com alan adı", "Uygulamanın HTTPS ile yayınlanması (automarket.ardcek.com) ve haftalık teslimlerin paylaşılması (ardcek.com/haftaN)."],
    ["cron-job.org", "Ücretsiz sunucunun uykuya geçmemesi için sağlık adresine düzenli istek gönderme."],
  ],

  // ───────────────────────────────── ÇIKTILAR (mevcut 3 satırın 2. ve 3. sütunu)
  ciktilar: [
    [["1) Proje Öneri ve Emsal Analizi Raporu (6 site, 15 ölçüt).", "2) Her haftanın teknik raporu (ana sayfa; sayfalar ve veritabanı; araç özellikleri ve yeni tablolar; sonraki haftalar).", "3) Dönem sonu proje raporu ve kullanılabilirlik anketinin sonuçları.", "4) Mümkün olursa sonuçların bir öğrenci kongresinde bildiri olarak sunulması."],
      ["1-3: 0-6 ay", "4: 6-12 ay"]],
    [["1) **Çalışan ürün:** Rota Rent a Car web sitesi ve yönetim paneli (automarket.ardcek.com).", "2) **Belgeler:** veritabanı şeması ve ilişki diyagramı, API (Swagger) belgesi, kod haritası ve geliştirici rehberi.", "3) **Eser:** uygulamanın kaynak kodu (5846 sayılı Fikir ve Sanat Eserleri Kanunu'na göre bilgisayar programı olarak korunur).", "4) İleride bir firmada deneme kullanımı ve öğrenci girişimi."],
      ["1-3: 0-6 ay", "4: 6-18 ay"]],
    [["Ön lisans projesi olduğu için tez çıkması beklenmiyor.", "Dördümüz de bir yazılımı analizden yayına kadar yapmayı öğreniyoruz. Hazırladığımız geliştirici rehberi ve kod haritası sonraki dönem öğrencilerine örnek olabilir."],
      ["0-6 ay"]],
  ],

  // ───────────────────────────────── ETKİLER (mevcut 4 satırın 2. ve 3. sütunu)
  etkiler: [
    [["Müşteri ödeyeceği tutarın nasıl oluştuğunu kiralamadan önce satır satır gördüğü için firmaya güveni artıyor. Çift rezervasyon ve yanlış ücret yüzünden yaşanan sorunlar azalıyor.", "Sitede takip kodu olmadığı için ziyaretçilerin bilgileri başka şirketlere gitmiyor. Telefona uyumlu ve koyu temalı arayüz farklı kullanıcıların işini kolaylaştırıyor.", "Hedef kitle: küçük ve orta ölçekli kiralama firmaları ve müşterileri (turistler, iş seyahatine çıkanlar)."],
      ["Prototip: 0-6 ay", "Deneme kullanımı: 6-12 ay"]],
    [["Bölümde Türkçe yazılmış ve belgelenmiş bir web otomasyonu örneği oluşuyor. Panelden yeni özellik eklenebilen veri modeli derslerde örnek olarak gösterilebilir.", "Ekip olarak C#, ASP.NET Core, SQL, bulut servisleri ve Git konusunda kendimizi geliştiriyoruz.", "Bir kiralama firmasıyla deneme yapılırsa üniversite ile sanayi arasında bir iş birliği başlayabilir."],
      ["0-6 ay", "İş birliği: 6-12 ay"]],
    [["Lisans ücreti olmadan ve ücretsiz bulut servisleriyle çalışabildiği için küçük firmaların dijitale geçiş masrafını azaltıyor; yabancı ve aylık ücretli programlara yerli bir seçenek oluyor.", "Geç iade ve yakıt ücretleri otomatik hesaplandığı için firmanın kaçırdığı gelir azalıyor.", "Proje bir girişime dönüşürse kurulum ve destek işleriyle yeni iş imkânı oluşabilir."],
      ["6-12 ay", "Girişim: 12-24 ay"]],
    [["**Siber güvenlik:** Şifrelerin özet olarak saklanması, CSRF koruması, parametreli sorgular, CSP ve HTTPS gibi önlemler küçük firmaları saldırılara karşı korumaya yardım ediyor.", "**Verilerin yerinde kalması:** Firmanın müşteri ve para bilgileri kendi kontrolündeki veritabanında duruyor. Gerekirse veritabanı Türkiye'deki bir sunucuya kod değiştirmeden taşınabiliyor."],
      ["0-6 ay"]],
  ],

  // ───────────────────────────────── SANAYİ İŞBİRLİĞİ YOL HARİTASI
  yolHaritasi: [
    "Projeyi ileride gerçek firmaların kullanabileceği bir ürüne dönüştürmek için şöyle bir yol izlemeyi düşünüyoruz:",
    { r: "**1. Deneme kullanımı (6-12 ay):** Prototip bitince Konya'daki 2-3 küçük kiralama firmasıyla görüşüp ihtiyaçlarını soracağız ve bir firmaya ücretsiz olarak denemeyi önereceğiz. Aldığımız geri bildirimlere göre programı düzelteceğiz." },
    { r: "**2. Girişimcilik eğitimi (6-12 ay):** KOSGEB'in girişimcilik desteğine başvurabilmek için ekipten arkadaşların KOSGEB Uygulamalı Girişimcilik Eğitimi'ni almasını planlıyoruz." },
    { r: "**3. TÜBİTAK 1512 BİGG (12-18 ay):** Birden fazla firmanın aynı sistemi ayrı ayrı kullanabileceği, aylık abonelikli bir bulut hizmeti fikriyle TÜBİTAK 1512 Bireysel Genç Girişim programına başvurmayı hedefliyoruz. Programın ilk aşamasında iş planı hazırlanıyor, ikinci aşamada şirket kurulup alınan destekle ürün satışa hazırlanıyor." },
    { r: "**4. Yeni modüller (18-24 ay):** Deneme yaptığımız firmayla TÜBİTAK 1505 programı ya da firmanın başvurabileceği 1507 KOBİ Ar-Ge desteğiyle sanal POS ile ödeme, e-Fatura bağlantısı ve HGS ile trafik cezası takibi gibi eklemeler yapmayı düşünüyoruz." },
    { r: "**5. Şirket kurma:** Şirket kurulursa bir teknokentte çalışmayı ve Mevlana Kalkınma Ajansı'nın KOBİ desteklerini takip etmeyi düşünüyoruz." },
  ],

  // ───────────────────────────────── PAYLAŞIM VE YAYILIM TABLOSU  [etkinlik, paydaş, zaman]
  paylasim: [
    ["**Web sitesi:** Haftalık teslimlerin ve raporların ardcek.com/haftaN adreslerinde, canlı uygulamanın automarket.ardcek.com adresinde yayınlanması", "Danışman, ders arkadaşları, bölümdeki öğretim elemanları", "Her hafta, proje boyunca (12 hafta)"],
    ["**Sunum:** Ders içinde haftalık sunum ve canlı gösterim (sayfalar, yönetim paneli, Neon tabloları)", "Danışman ve sınıf", "Her hafta, 10-15 dakika"],
    ["**Eğitim:** Kod haritası, geliştirici rehberi ve kurulum belgelerinin paylaşılması; alt dönem öğrencilerine kısa bir uygulama atölyesi", "Bölümdeki diğer ve sonraki dönem öğrencileri", "Dönem sonu (Ocak 2027), 1 oturum (2 saat)"],
    ["**Görüşme:** Yerel kiralama firmalarına tanıtım ve geri bildirim görüşmeleri", "Konya'daki küçük ve orta ölçekli kiralama firmaları", "Proje bittikten sonraki 1-3. ay, 2-3 görüşme (birer saat)"],
    ["**Proje pazarı / kongre:** Üniversitenin proje sergisine ya da proje pazarına başvuru, öğrenci kongresine bildiri", "Öğrenciler, akademisyenler, sanayi temsilcileri", "6-12. ay"],
  ],

  // ───────────────────────────────── BELİRTMEK İSTEDİĞİNİZ DİĞER KONULAR
  digerKonular: [
    { r: "**Ders ve danışman.** Konya Teknik Üniversitesi, Bilgisayar Programcılığı, ders kodu 5035354. Danışmanımız: Levent Civcik." },
    { b: "Projenin şu anki durumu" },
    "İP1, İP2 ve İP3'ü bitirip teslim ettik:",
    { m: "Ana sayfa: ardcek.com/hafta1" },
    { m: "Sayfalar ve veritabanı bağlantısı: ardcek.com/hafta3" },
    { m: "Araç özellikleri ve yeni tablolar: ardcek.com/hafta4" },
    { m: "Canlı uygulama: https://automarket.ardcek.com" },
    "Neon'daki canlı veritabanında 25 tablo ve 24 ilişki var; tablolar şimdilik boş. Sitedeki araç, şube ve rezervasyon kayıtları örnek veri, gerçek kişisel bilgi kullanmıyoruz.",
    { b: "Kullandığımız teknolojiler" },
    "C# ve .NET 10, ASP.NET Core MVC, Entity Framework Core, SQLite, PostgreSQL (Neon), ASP.NET Core Identity, Bootstrap 5.3, Chart.js, QuestPDF (PDF sözleşme), ClosedXML (Excel), Swagger, xUnit, Docker, Render.com, Git ve GitHub.",
  ],

  // ───────────────────────────────── EK-1: KAYNAKLAR
  kaynaklar: [
    "Brooke, J. (1996). SUS: A \"quick and dirty\" usability scale. Usability Evaluation in Industry içinde (s. 189-194). Taylor & Francis.",
    "Microsoft. ASP.NET Core belgeleri. https://learn.microsoft.com/aspnet/core (Erişim: 12.11.2026)",
    "Microsoft. Entity Framework Core belgeleri. https://learn.microsoft.com/ef/core (Erişim: 12.11.2026)",
    "Nielsen, J. (1994). Enhancing the explanatory power of usability heuristics. CHI '94 Conference Proceedings, 152-158.",
    "OWASP Foundation (2021). OWASP Top 10:2021. https://owasp.org/Top10/",
    "Rota Rent a Car Proje Ekibi (2026). Proje Öneri ve Emsal Analizi Raporu. Konya Teknik Üniversitesi.",
    "Schwaber, K. ve Sutherland, J. (2020). The Scrum Guide. https://scrumguides.org",
    "Strateji ve Bütçe Başkanlığı (2023). On İkinci Kalkınma Planı (2024-2028).",
    "5846 sayılı Fikir ve Sanat Eserleri Kanunu.",
    "6698 sayılı Kişisel Verilerin Korunması Kanunu.",
  ],
};
