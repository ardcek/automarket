# 🚗 Rota Rent a Car — Araç Kiralama Otomasyonu (C#)

Asıl proje **[`AracKiralama/`](AracKiralama/README.md)** klasöründedir. Detaylı kurulum ve kullanım: **[AracKiralama/README.md](AracKiralama/README.md)**

🌐 **Canlı:** https://automarket.ardcek.com (kısa yol: https://ardcek.com/automarket/) — ücretsiz sunucu uykudaysa ilk açılış 30–60 sn sürer. Yayın adımları: [AracKiralama/README.md → 13. Canlı yayın](AracKiralama/README.md#13-canlı-yayın-internette-yayınlamak)

## Hızlı başlangıç

[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) kurun, sonra:

```bash
cd AracKiralama
dotnet run --project src/AracKiralama.Web
```

Tarayıcıda **http://localhost:5046** açın. Yönetim paneli: **http://localhost:5046/Yonetim** (sitenin bütün içeriği, renkleri, fiyat kuralları ve filtreleri buradan değişir).

## 🔑 Demo hesaplar (hepsinin şifresi: `Rota123!`)

| Rol | E-posta | Şifre |
|---|---|---|
| 👑 Admin (yönetim paneli, her şey) | `admin@rota.com` | `Rota123!` |
| 🧑‍💼 Personel (yönetim paneli) | `personel@rota.com` | `Rota123!` |
| 🙂 Müşteri | `musteri@rota.com` | `Rota123!` |
| 🧒 Genç müşteri (20 yaş, lüks araç kiralayamaz) | `genc@rota.com` | `Rota123!` |

Hesaplar programın ilk açılışında otomatik oluşturulur (`AracKiralama/src/AracKiralama.Web/Veri/OrnekVeriler.cs`).

📚 Kodun tamamı Türkçedir. Kodu anlamak için: [Kod haritası](AracKiralama/docs/KOD-HARITASI.md) · Yeni özellik eklemek için: [Geliştirici rehberi](AracKiralama/docs/GELISTIRICI-REHBERI.md)

---

> Aşağıdaki içerik eski Next.js projesine aittir ve sadece referans olarak tutulmaktadır.

# Auto Marketplace

Türkiye'nin modern, dinamik ve yenilikçi araç & yedek parça ilan platformu. Yüzlerce aracı saniyeler uzaklığınızda buluşturan, yüksek performanslı ve tam yığın (full-stack) Next.js uygulamasıdır.

## 🚀 Teknolojiler
- **Framework:** Next.js (App Router)
- **Veritabanı:** Neon PostgreSQL & Prisma ORM
- **Kimlik Doğrulama:** NextAuth.js (Custom Credentials - Bcrypt Hashing)
- **CSS / UI:** Tailwind CSS (Modern Animations & Glassmorphism)
- **Versiyon Kontrolü:** Git & GitHub

---

## ✅ Şu Ana Kadar Yapılanlar

- [x] **Veritabanı Entegrasyonu:**
  - Neon PostgreSQL bağlantısı kuruldu.
  - Prisma aracılığıyla `User` (Kullanıcı) ve `Listing` (İlan/Araç) modelleri tek bir yapıda (`Category` enum: `SALE`, `RENT`, `PART` uyumlu) birleştirildi.
- [x] **Yetkilendirme ve Güvenlik:**
  - `NextAuth.js` kullanılarak kullanıcı giriş yapma (`/login`) ve kayıt olma (`/register`) sayfaları entegre edildi.
  - Şifreler `bcrypt` ile hash'lenerek güvenceye alındı.
  - Session state yönetimi için global `SessionProvider` eklendi.
- [x] **Gelişmiş Admin Ekranı:**
  - `/admin` dizini sadece `ADMIN` yetkisine sahip kullanıcıların erişimine açıldı.
  - **Dashboard:** Sistemdeki toplam kullanıcı ve ilan sayılarının takibi.
  - **Kullanıcı Yönetimi:** Sisteme kayıtlı kullanıcıların rolleriyle birlikte listelenmesi.
  - **İlan Yönetimi:** Eklenen her ilanın listelenmesi ve admin üzerinden kontrol edilebilir altyapı paneli hazırlandı.
- [x] **Arayüz Entegrasyonu:**
  - Anasayfa listelemeleri ve `/arac/[id]` detay sayfaları veritabanında yer alan `Listing` modeli ile uyumlu hale getirildi.
- [x] **Favoriler Sistemi (Veritabanı Destekli):**
  - Favori ekleme ve çıkarma yeteneği Context API ile birlikte veritabanı (`Favorite` modeli) entegrasyonuyla güvenceye alındı.
- [x] **Fotoğraf ve Dosya Yükleme (Upload):**
  - İlan oluşturma/düzenleme formuna sürükle-bırak (drag-and-drop) destekli, önizlemeli ve silme özellikli modern görsel yükleme alanı eklendi. Yüklenen dosyalar güvenli bir API aracılığıyla sunucuya kaydedilmektedir.
- [x] **Next.js Görsel Filtreleme Hata Çözümü:**
  - Veritabanındaki eski/geçersiz görsel yolu veya yerel dosya yolu dizilimlerinin Next.js `<Image>` bileşenini çökertmesini önlemek için yol doğrulama filtresi entegre edildi.

---

## 🔮 Yapılacaklar Listesi (To-Do)

Uygulamanın vizyonunu tamamlamak ve fonksiyonel bir pazara dönüştürmek için aşağıdaki adımlar hedeflenmektedir:

1. **İlan Ekleme Altyapısı (Kullanıcı)**
   - Normal `USER` yetkisindeki kullanıcıların site üzerinden araç / kiralık araç / yedek parça ilanı verebilmesi için form oluşturulması.
2. **Gelişmiş Filtreleme ve Arama Sistemi**
   - Anasayfada Marka, Model, Vites, Yakıt ve Fiyat Aralığına göre dinamik Prisma sorgularıyla arama yapılması.
3. **Mesajlaşma ve Satıcıyla İletişim**
   - İlan detay sayfasından güvenli bir şekilde satıcıyla mail/uygulama-içi sohbet yapılması.

---

## 🛠 Kurulum ve Çalıştırma

Projeyi yerel bilgisayarınızda çalıştırmak için aşağıdaki adımları izleyin:

1. Gerekli paketleri indirin:
   ```bash
   npm install
   ```

2. Prisma ve Veritabanı istemcisini oluşturun:
   ```bash
   npx prisma generate
   ```

3. Geliştirme sunucusunu başlatın:
   ```bash
   npm run dev
   ```

Projeye [http://localhost:3000](http://localhost:3000) bağlantısından erişebilirsiniz.
