# Geliştirici Rehberi: "Hoca şunu eklemenizi isterse"

Bu dosya, sunumda veya ödev sırasında gelebilecek **ekleme isteklerinin** hazır tarifleridir. Her tarif "şu dosyayı aç, şu satırı ekle" şeklindedir ve projedeki gerçek kodla birebir uyumludur.

> **Önce panele bakın.** Firma bilgisi, renk, yazılar, sayfalar, fiyat oranları, kategori / marka / yakıt / vites listeleri, filtrelerin açık / kapalı olması **yönetim panelinden** değişir; kod gerekmez. Bu rehber, panelde **olmayan yeni bir özellik** istendiğinde içindir.

Bütün yollar `AracKiralama/src/AracKiralama.Web/` klasörüne göredir. Kodun genel akışı için önce **[KOD-HARITASI.md](KOD-HARITASI.md)**'ye göz atın.

## İçindekiler

| # | İstek | Kaç dosya? |
|---|---|---|
| 1 | [Araç listesine yeni filtre ("renge göre filtrele")](#1-araç-listesine-yeni-filtre) | 3 |
| 2 | [Yeni sıralama ("koltuk sayısına göre sırala")](#2-yeni-sıralama) | 2–3 |
| 3 | [Araca yeni alan ("bagaj hacmi")](#3-araca-yeni-alan) | 5 |
| 4 | [Panele yeni ayar + kuralda kullanmak ("hafta sonu zammı")](#4-panele-yeni-ayar-ve-fiyatta-kullanmak) | 2 |
| 5 | [Yeni iş kuralı ("bir müşteri en fazla 2 aktif rezervasyon")](#5-yeni-iş-kuralı) | 1–2 |
| 6 | [Panelden değişebilen yeni bir site yazısı](#6-panelden-değişebilen-yeni-site-yazısı) | 2 |
| 7 | [Yönetim paneline yeni sayfa ("Kampanyalar")](#7-yönetim-paneline-yeni-sayfa) | 5 |
| 8 | [Rapora / Excel'e yeni sütun](#8-rapora-ve-excele-yeni-sütun) | 2 |
| 9 | [Yeni içerik sayfası veya menü linki](#9-yeni-içerik-sayfası-veya-menü-linki) | 0 (panelden) |
| 10 | [Yeni API adresi](#10-yeni-api-adresi) | 1 |
| 11 | [Müşteri grubuna göre indirim ("meslek gruplarına indirim")](#11-müşteri-grubuna-göre-indirim) | 0 (panelden) |
| 12 | [Yapay Zekâ Asistanı'na yaptırmak (internetsiz)](#12-yapay-zekâ-asistanına-yaptırmak) | 0 |
| 13 | [Asistana yeni bir yetenek eklemek](#13-asistana-yeni-bir-yetenek-eklemek) | 1 |
| 14 | [Sitenin görünümü ve hareketli efektler ("bu bölüm de kayarak gelsin")](#14-sitenin-görünümü-ve-hareketli-efektler) | 0–1 |

**Her değişiklikten sonra:** siteyi kapatıp (`Ctrl + C`) tekrar `dotnet run --project src/AracKiralama.Web` ile açın. **Bir tabloya yeni alan / tablo eklediyseniz bir şey yapmanız gerekmez:** açılışta `Veri/SemaGuncelleyici.cs` eksik kolon ve tabloları kendiliğinden ekler (verilere dokunmaz). Bu hem bilgisayardaki SQLite'ta hem canlı sitedeki Neon'da çalışır; loga "Veritabanı şeması güncellendi, eklenenler: ..." yazar. Sadece bir kolonu **silmek** ya da **adını değiştirmek** elle yapılır (bilgisayarda en kolayı `arackiralama.db` dosyasını silmek).

**Canlı site (Render) her şeyi Neon'da tutar** (`appsettings.Production.json` → `"SayfalarNeredenOkusun": "Neon"`): üyeler, rezervasyonlar ve panelden yapılan her değişiklik kalıcıdır. Bilgisayarınızdaki site (BASLAT.bat, `dotnet run`) internetsiz çalışsın diye SQLite kullanmaya devam eder.

---

## 1. Araç listesine yeni filtre

**Örnek istek:** "Araçları renge göre de filtreleyebilelim."

Bir filtre her zaman **3 parçadır**: filtre sınıfında bir alan, controller'da bir `if`, sayfada bir kutu.

**Adım 1:** `FormModelleri/SiteModelleri.cs` → `AracFiltresi` sınıfında "Filtreler" bölümüne:

```csharp
public string? Renk { get; set; }
```

**Adım 2:** `Controllers/AraclarController.cs` → `Index` metodunda diğer `if` satırlarının yanına:

```csharp
if (!string.IsNullOrWhiteSpace(filtre.Renk)) sorgu = sorgu.Where(a => a.Renk == filtre.Renk);
```

Açılır listede renkleri göstermek için aynı metodun sonundaki `new AracListesiSayfasi { ... }` içine:

```csharp
Renkler = await _vt.Araclar.Select(a => a.Renk).Distinct().OrderBy(r => r).ToListAsync(),
```

ve `FormModelleri/SiteModelleri.cs` → `AracListesiSayfasi` sınıfına:

```csharp
public List<string> Renkler { get; set; } = new();
```

**Adım 3:** `Views/Araclar/Index.cshtml` → filtre panelinde, örneğin "Model yılı" bloğunun altına:

```html
<div class="filtre-grubu">
    <div class="kucuk-baslik mb-2">Renk</div>
    <select name="Renk" class="form-select form-select-sm">
        <option value="">Fark etmez</option>
        @foreach (var renk in Model.Renkler) { <option value="@renk" selected="@(f.Renk == renk)">@renk</option> }
    </select>
</div>
```

`name="Renk"` ile filtre sınıfındaki özelliğin adı **aynı** olmalı; ASP.NET ikisini kendisi eşleştirir.

**Bitti.** Adres çubuğu (`/Araclar?Renk=Beyaz`), sayfalama ve "Temizle" linki kendiliğinden çalışır (`AracFiltresi.Degerler()` bütün özellikleri otomatik dolaşır). Kutu değişince liste kendiliğinden yenilenir (formdaki `data-otomatik-gonder`).

**İsteğe bağlı (panelden aç / kapat):** `Modeller/SiteAyarlari.cs` → "Araç Listesi" bölümüne:

```csharp
[Ayar("Araç Listesi", "Filtre: Renk")] public bool FiltreRenk { get; set; } = true;
```

ve view'daki bloğu `@if (a.FiltreRenk) { ... }` içine alın. Tabloya kolon eklendiği için `.db` dosyasını silin.

> **Sayısal aralık filtresi** (örn. "en çok 50.000 km") için aynı kalıp: `public int? EnFazlaKm { get; set; }` → `if (filtre.EnFazlaKm != null) sorgu = sorgu.Where(a => a.Kilometre <= filtre.EnFazlaKm);` → `<input type="number" name="EnFazlaKm" value="@f.EnFazlaKm" class="form-control form-control-sm" />`

---

## 2. Yeni sıralama

**Örnek istek:** "Koltuk sayısına göre sıralayabilelim."

**Adım 1:** `Controllers/AraclarController.cs` → `Index` → `switch` bloğuna bir satır:

```csharp
"koltuk" => sorgu.OrderByDescending(a => a.KoltukSayisi).ThenBy(a => a.GunlukFiyat),
```

**Adım 2:** `Views/Araclar/Index.cshtml` → "Sıralama" kutusundaki listeye bir eleman:

```csharp
("koltuk", "Koltuk sayısı (çoktan aza)")
```

**Adım 3 (isteğe bağlı):** Bu sıralama panelde "varsayılan sıralama" olarak da seçilebilsin isterseniz `Modeller/SiteAyarlari.cs` → `VarsayilanSiralama` satırındaki `Secenekler` metninin sonuna `|koltuk:Koltuk sayısı` ekleyin.

---

## 3. Araca yeni alan

**Örnek istek:** "Araçların bagaj hacmi de tutulsun ve detay sayfasında görünsün."

**Adım 1, tablo:** `Modeller/Arac.cs`:

```csharp
public int BagajHacmi { get; set; }               // litre
```

**Adım 2, form modeli:** `FormModelleri/YonetimFormlari.cs` → `AracFormu`:

```csharp
[Display(Name = "Bagaj hacmi (L)"), Range(0, 5000)] public int BagajHacmi { get; set; }
```

**Adım 3, kopyalama:** `Areas/Yonetim/Controllers/AracControllerlari.cs` içinde iki yer var:
- `Duzenle(int id)` metodunda araçtan forma kopyalanan satırlara: `BagajHacmi = a.BagajHacmi,`
- `KaydetmeyiDeneAsync` metodunda formdan araca kopyalanan satırlara: `arac.BagajHacmi = form.BagajHacmi;`

**Adım 4, admin formu:** `Areas/Yonetim/Views/Araclar/Form.cshtml` → örneğin "Renk" kutusunun yanına:

```html
<div class="col-md-3"><label asp-for="BagajHacmi" class="form-label"></label><input asp-for="BagajHacmi" class="form-control" /></div>
```

**Adım 5, müşteri sayfası:** `Views/Araclar/Detay.cshtml` → özellik listesindeki diziye (Vites, Yakıt, Koltuk, Renk'in yanına) bir eleman:

```csharp
("bi-suitcase", "Bagaj", a.BagajHacmi + " L")
```

**Son:** `.db` dosyasını silip siteyi açın. Örnek araçlarda değer 0 olur; isterseniz `Veri/OrnekVeriler.cs`'te araç oluşturulan yere `BagajHacmi = 400,` ekleyin.

> İkon adları: https://icons.getbootstrap.com (projede `wwwroot/lib/bootstrap-icons` içinde hepsi var).

---

## 4. Panele yeni ayar ve fiyatta kullanmak

**Örnek istek:** "Hafta sonu günleri için yüzde zam uygulansın, oranı panelden değişsin."

**Adım 1, ayar:** `Modeller/SiteAyarlari.cs` → "Fiyatlar" bölümüne **tek satır**:

```csharp
[Ayar("Fiyatlar", "Hafta sonu zammı", Aciklama = "Cumartesi ve Pazar günleri için. 0.15 = %15, 0 = kapalı")]
public decimal HaftaSonuZammi { get; set; } = 0;
```

Yönetim → Ayarlar → Fiyatlar sekmesinde kutu **kendiliğinden** çıkar (form, `[Ayar]` etiketli özelliklerden otomatik oluşturulur). Sayı, açma / kapama, renk veya açılır liste olacağına türüne göre karar verilir.

**Adım 2, kuralda kullanmak:** `Servisler/FiyatServisi.cs` → `Hesapla` metodunda `teklif.KiraTutari = ...` satırının **hemen altına**:

```csharp
// Hafta sonu zammı: Cumartesi ve Pazar'a denk gelen her gün için günlük fiyat × oran eklenir.
var haftaSonuGunu = 0;
for (var i = 0; i < teklif.GunSayisi; i++)
{
    var gun = baslangic.AddDays(i).DayOfWeek;
    if (gun == DayOfWeek.Saturday || gun == DayOfWeek.Sunday) haftaSonuGunu++;
}
teklif.KiraTutari += haftaSonuGunu * arac.GunlukFiyat * _ayarlar.HaftaSonuZammi;
```

Sitedeki canlı fiyat hesaplayıcı, rezervasyon, PDF sözleşme ve API hepsi bu servisi kullandığı için **her yerde** geçerli olur.

**Adım 3, test (önerilir):** `tests/AracKiralama.Tests/FiyatTestleri.cs`:

```csharp
[Fact]
public void Hafta_sonu_gunlerine_zam_uygulanir()
{
    var ayarlar = new SiteAyarlari { HaftaSonuZammi = 0.10m };
    var cuma = new DateTime(2026, 11, 6, 10, 0, 0);                      // Cuma → Pazartesi: Cmt + Paz = 2 gün
    var t = new FiyatServisi(ayarlar).Hesapla(Araba, cuma, cuma.AddDays(3), new(), false);
    Assert.Equal(3000 + 2 * 1000 * 0.10m, t.KiraTutari);
}
```

Tabloya kolon eklendiği için `.db` dosyasını silin. Ayarı içerik sayfalarında (Kiralama Koşulları gibi) göstermek isterseniz `Servisler/IcerikServisi.cs` dosyasındaki `.Replace("{haftalik-indirim}", ...)` satırlarının yanına bir tane daha ekleyin:

```csharp
.Replace("{hafta-sonu-zammi}", Bicim.Yuzde(a.HaftaSonuZammi))
```

---

## 5. Yeni iş kuralı

**Örnek istek:** "Bir müşterinin aynı anda en fazla 2 aktif rezervasyonu olabilsin."

Bütün rezervasyon kuralları `Servisler/RezervasyonServisi.cs` dosyasındadır. Kural çiğnenirse `IsKuraliHatasi` fırlatılır; controller mesajı kullanıcıya kendisi gösterir.

`OlusturAsync` metodunda sürücü kontrolünün (`SurucuHatalari`) altına:

```csharp
// Aynı anda en fazla 2 aktif rezervasyon
var aktifSayisi = await _vt.Rezervasyonlar.CountAsync(r => r.MusteriId == musteriId
    && (r.Durum == RezervasyonDurumu.OnayBekliyor || r.Durum == RezervasyonDurumu.Onaylandi || r.Durum == RezervasyonDurumu.Kirada));
if (aktifSayisi >= 2) throw new IsKuraliHatasi("Aynı anda en fazla 2 aktif rezervasyonunuz olabilir.");
```

**Sınır panelden değişsin isterseniz** [4. tarifteki](#4-panele-yeni-ayar-ve-fiyatta-kullanmak) gibi bir ayar ekleyin (`[Ayar("Rezervasyon", "Müşteri başına en fazla aktif rezervasyon")] public int EnFazlaAktifRezervasyon { get; set; } = 2;`) ve `2` yerine `_ayarlar.EnFazlaAktifRezervasyon` yazın.

**Test:** `RezervasyonTestleri.cs`'teki `TalepOlustur(...)` kısayoluyla üç talep oluşturup üçüncüsünde `Assert.ThrowsAsync<IsKuraliHatasi>` bekleyin.

---

## 6. Panelden değişebilen yeni site yazısı

**Örnek istek:** "Araç listesinin üstüne kampanya duyurusu yazalım, panelden değişsin."

**Adım 1:** `Yardimcilar/VarsayilanMetinler.cs` → `Liste` sözlüğüne:

```csharp
["araclar.duyuru"] = "Bu ay tüm SUV'larda %10 indirim!",
```

Anahtarın ilk kısmı (`araclar`) panelde hangi grupta görüneceğini belirler.

**Adım 2:** `Views/Araclar/Index.cshtml` → istediğiniz yere:

```html
<div class="alert alert-info">@Icerik.Metin("araclar.duyuru")</div>
```

Yönetim → **Site Metinleri** sayfasında kendiliğinden görünür. Bu yazılar ayrı bir tabloda (Metinler) tutulduğu için **veritabanını silmeye gerek yoktur**.

---

## 7. Yönetim paneline yeni sayfa

**Örnek istek:** "Kampanyaları yönetebileceğimiz bir sayfa olsun (ad, indirim oranı, aktif mi)."

Yönetimdeki tanım sayfaları (Şubeler, Markalar, Kategoriler, Ek Hizmetler) **aynı kalıptadır**. En kolay yol `Subeler`'i kopyalamaktır.

**Adım 1, tablo:** `Modeller/Tanimlar.cs` dosyasının sonuna:

```csharp
public class Kampanya
{
    public int Id { get; set; }
    [Display(Name = "Kampanya adı"), Required, MaxLength(80)] public string Ad { get; set; } = "";
    [Display(Name = "İndirim oranı"), Range(0, 1)] public decimal IndirimOrani { get; set; }
    [Display(Name = "Aktif")] public bool Aktif { get; set; } = true;
}
```

**Adım 2, veritabanına tanıtmak:** `Veri/VeriTabani.cs` → diğer `DbSet` satırlarının yanına:

```csharp
public DbSet<Kampanya> Kampanyalar => Set<Kampanya>();
```

**Adım 3, controller:** `Areas/Yonetim/Controllers/TanimControllerlari.cs` içindeki `SubelerController` sınıfını kopyalayıp dosyanın sonuna yapıştırın, sonra:
- `SubelerController` → `KampanyalarController`
- `Sube` → `Kampanya`, `sube` → `kampanya`, `_vt.Subeler` → `_vt.Kampanyalar`
- `Index`'teki sıralamayı `OrderBy(k => k.Ad)` yapın, `Sil`'deki "kullanımda mı" kontrolünü kaldırın.
- Bildirim mesajlarındaki "Şube güncellendi / Şube silindi" yazılarını "Kampanya ..." yapın.

> Bu tarif projenin bir kopyasında birebir uygulanıp derlendi ve çalıştırıldı; diğer tarifler de öyle.

**Adım 4, sayfalar:** `Areas/Yonetim/Views/Subeler/` klasörünü kopyalayıp adını `Kampanyalar` yapın. `Index.cshtml` ve `Form.cshtml` içinde `Sube` → `Kampanya`, `/Yonetim/Subeler` → `/Yonetim/Kampanyalar` yapın ve sütunları / kutuları yeni alanlara (Ad, IndirimOrani, Aktif) göre düzenleyin. Onay kutusu için:

```html
<div class="form-check form-switch"><input class="form-check-input" asp-for="Aktif" /><label class="form-check-label" asp-for="Aktif"></label></div>
```

**Adım 5, menü:** `Views/Shared/_YonetimLayout.cshtml` → "Tanımlar" bölümüne:

```csharp
@Menu("Kampanyalar", "bi-megaphone", "Kampanyalar", controller)
```

`.db` dosyasını silip açın → `/Yonetim/Kampanyalar`. Sayfa otomatik olarak sadece Admin ve Personel'e açıktır (bütün yönetim controller'ları `YonetimController`'dan türer).

---

## 8. Rapora ve Excel'e yeni sütun

**Örnek istek:** "Araç raporunda araç başına ortalama kiralama süresi de görünsün."

**Adım 1:** `Servisler/RaporServisi.cs`
- `AracRaporSatiri` sınıfına: `public double OrtalamaGun { get; set; }`
- `new AracRaporSatiri { ... }` bloğuna: `OrtalamaGun = aracKiralamalari.Count == 0 ? 0 : (double)kiraliGun / aracKiralamalari.Count,`
- `ExcelOlustur` metodunda `basliklar` dizisine `"Ort. Gün"` ekleyin ve döngüye `sayfa1.Cell(satir, 9).Value = a.OrtalamaGun;` satırını ekleyin.

**Adım 2:** `Areas/Yonetim/Views/Raporlar/Index.cshtml` → tablo başlığına `<th class="text-end">Ort. gün</th>`, satıra `<td class="text-end">@a.OrtalamaGun.ToString("0.0")</td>`.

Tabloya kolon eklenmediği için (hesaplanan değer) veritabanını silmeye gerek yoktur.

---

## 9. Yeni içerik sayfası veya menü linki

**Kod gerekmez.** Yönetim → **Sayfalar** → **Yeni sayfa**: başlık, adres (örn. `sss` → `/Sayfa/sss`), içerik; **Üst menüde göster** ve/veya **Alt bilgide göster**'i işaretleyin. Menüye ve alt bilgiye kendiliğinden eklenir.

İçerikte boş satır = paragraf, `- ` = madde, `## ` = ara başlık; `{firma}`, `{telefon}`, `{haftalik-indirim}` gibi yer tutucular ayarlardaki güncel değerle dolar.

- **Açılır menü ("Kurumsal ▾"):** sayfaların **Menü grubu** kutusuna aynı adı yazın (örn. Hakkımızda ve İletişim → `Kurumsal`). Grup, içindeki ilk sayfanın sırasında durur. Boş bırakılan sayfa tek link olur.
- **Menüde kısa ad:** başlık uzunsa **Menüdeki kısa adı** kutusunu doldurun (başlık "Sıkça Sorulan Sorular", menüde "SSS").
- **Soru-cevap sayfası (SSS):** **Soru-cevap düzeni**'ni açın. Her `## Soru?` satırı açılır kapanır bir soru, altındaki satırlar cevabı olur. Yeni soru eklemek = yeni bir `## ` satırı yazmak.
- Yapay Zekâ Asistanı da aynısını yapabilir: "SSS'ye 'Kasko dahil mi?' sorusunu ekle", "Kampanya koşulları diye bir sayfa aç, Kurumsal menüsüne koy".

---

## 10. Yeni API adresi

**Örnek istek:** "Şubeleri JSON olarak veren bir API olsun."

`Controllers/Api/ApiController.cs` → sınıfın içine:

```csharp
/// <summary>Bütün şubeler.</summary>
[HttpGet("subeler")]
public async Task<List<Sube>> Subeler() => await _vt.Subeler.OrderBy(s => s.Sehir).AsNoTracking().ToListAsync();
```

`/api/subeler` adresinde çalışır ve `/swagger` sayfasında kendiliğinden listelenir.

---

## 11. Müşteri grubuna göre indirim

> "Meslek gruplarına göre indirim yapın, filtrede de indirimli araçlar görünsün."

Kod gerekmez:

1. **Yönetim → Müşteri Bilgileri → Yeni bilgi:** Ad `Meslek`, Tür `Seçim listesi`, seçenekler (her satıra bir tane) `Öğrenci`, `Öğretmen`, `Sağlık çalışanı`…
2. **Yönetim → Kampanyalar → Yeni kampanya:** Ad `Öğretmen indirimi`, İndirim `15`, Müşteri bilgisi `Meslek: Öğretmen`. Her meslek için tekrarlayın.
3. Araç listesini açın: solda **İndirimler** (Mesleğiniz + Sadece indirimli araçlar), kartlarda rozet ve indirimli fiyat.

Hoca "kodda nerede?" derse:

| Ne | Dosya |
|---|---|
| Tablolar | `Modeller/MusteriAlani.cs`, `Modeller/Kampanya.cs` |
| Kural (hangi indirim kime) | `Servisler/KampanyaServisi.cs` → `KampanyaBaglami.Durum()` |
| Fiyata uygulanması | `Servisler/FiyatServisi.cs` → `Hesapla(..., kampanyalar)` |
| Filtre | `Controllers/AraclarController.cs` → `SadeceIndirimli`, `Views/Araclar/Index.cshtml` → "İNDİRİMLER" |
| Kart | `Views/Shared/_AracKarti.cshtml` |
| Testler | `tests/AracKiralama.Tests/KampanyaTestleri.cs` |

Başka koşullar da aynı ekrandan: kategori ("SUV'larda %10"), marka, araç özelliği, en az gün ("7 gün ve üzeri"), tarih aralığı ("yaz kampanyası").

---

## 12. Yapay Zekâ Asistanı'na yaptırmak

**Yönetim → Yapay Zekâ Asistanı** (`/Yonetim/YapayZeka`). İnternet gerekmez; kurulum: [yapay-zeka/BENI-OKU.md](../yapay-zeka/BENI-OKU.md).

1. İsteği yazın → **Gönder**.
2. Yapay zekâ "Anladığım: …" yazar ve yapacaklarını **kart kart** gösterir. Kartlardaki değerleri değiştirebilir, istemediğinizin işaretini kaldırabilirsiniz. Yazı / renk değişikliğini **Önizle** ile görün.
3. **Seçilenleri uygula.** Yanlışsa sayfanın altındaki **Geçmiş → Geri al**.

**Gerçek modelle denenmiş istekler** (2026-11-11; orta model 11/11, küçük model 10/11 doğru; tekrar denemek için `node AracKiralama/tests/uctan-uca/yapay-zeka.mjs`):

| İstek | Asistanın yaptığı |
|---|---|
| Meslek gruplarına göre indirim yap, filtrede de indirimli araçlar görünsün | Meslek bilgisi + meslek başına kampanya (filtre zaten açıksa ayara dokunmaz) |
| Araçlara klima bilgisi ekle, bütün araçlarda olsun, filtrede seçilebilsin | Var/Yok özelliği + 47 araca değer |
| SUV kategorisindeki araçların fiyatını %10 artır | Toplu fiyat değişikliği (örnek araçla önce/sonra) |
| SUV'larda 7 gün ve üzeri kiralamalara %12 indirim yap | Kategori + en az gün koşullu kampanya |
| Ana sayfadaki büyük başlık "Yolculuk burada başlar" olsun | Üç parçalı başlığı böler: Yolculuk [burada] başlar |
| Butonların rengi turuncu olsun | Ana renk ayarı (önizlenebilir) |
| Siteye Sıkça Sorulan Sorular sayfası ekle, üst menüde görünsün | Yeni sayfa, içeriğini de yazar |
| Kar zinciri ek hizmetinin günlük fiyatı 150 lira olsun | Var olan ek hizmeti günceller |
| Çocuk koltuğu ek hizmeti ekle, günlüğü 100 lira | Yeni ek hizmet |
| Ekonomi kategorisini öğrencilere %20 indirimli yap | Müşteri bilgisi + kategori koşullu kampanya |
| Öğretmen indirimini %20 yap | Var olan kampanyayı günceller |
| Kaç tane kampanya var? | Sadece cevap, değişiklik yok |

**Panelin yapamadığı istekler** ("araç kartında kilometre de görünsün" gibi) için **Kod modu** sekmesi: yapay zekâ dosyaları seçer, değişikliği yazar, geçici kopyada derleyip dener; "Uygula" dosyaları yedekleyip değiştirir. Küçük değişikliklerde iyidir; sonucu her zaman okuyun. Denemek için: `node AracKiralama/tests/uctan-uca/kod-modu.mjs` (uygulama `BASLAT.bat` ile açıkken).

---

## 13. Asistana yeni bir yetenek eklemek

Asistanın yapabildiği her şey `Servisler/YapayZeka/PanelIslemleri.cs` dosyasındadır. Örneğin "şube sil" eklemek:

1. Dosyada en benzer işlemi (`SubeEkle`) kopyalayın, adını `SubeSil` yapın.
2. Doldurun: `Tur` (modelin yazacağı ad, örn. `"sube_sil"`), `Baslik`, `Aciklama` (modele ne zaman kullanacağını anlatan cümle), `Parametreler` (örn. `ad`), `DogrulaAsync` (şube var mı, aracı var mı?), `UygulaAsync` (sil ve geri almak için eski bilgileri döndür), `GeriAlAsync` (geri ekle).
3. Dosyanın sonundaki `Hepsi` listesine `new SubeSil(),` ekleyin.

Modele anlatım, JSON şeması, ekrandaki kart ve geçmiş kendiliğinden çalışır. Test örneği: `tests/AracKiralama.Tests/YapayZekaTestleri.cs`.

---

## 14. Sitenin görünümü ve hareketli efektler

Müşteri sitesinin tasarımı `wwwroot/css/vitrin.css`, hareketleri `wwwroot/js/efektler.js` dosyasındadır (yönetim paneli bunları yüklemez; panelin görünümü `wwwroot/css/stil.css`). Renkler yine **Yönetim → Ayarlar → Görünüm**'den gelir: ana rengi değiştirince ışıklar, geçişler ve butonlar da o renge döner.

Efektler için JavaScript yazılmaz, HTML etiketine bir **data-** özelliği eklenir:

| İstek | Ne yazılır? | Örnek |
|---|---|---|
| "Bu bölüm kaydırınca kayarak gelsin" | `data-goster` (yandan: `data-goster="sol"` / `"sag"`, büyüyerek: `"olcek"`) | `<div class="col-md-4" data-goster>…</div>` |
| "Bu sayı sayarak artsın" | `data-sayac="hedef sayı"` (+ isteğe bağlı `data-sonek="+"`) | `<strong data-sayac="47" data-sonek="+">47+</strong>` |
| "Kart fareyle eğilsin, ışık izlesin" | `data-egim` (sadece ışık: `data-egim="isik"`) | `_AracKarti.cshtml` → `<article class="arac-karti" data-egim>` |
| "Buton fareye doğru çekilsin" | `data-miknatis` | `<a class="btn btn-isik" data-miknatis>` |
| "Koyu bölümde bir parça fareyle kaysın (derinlik)" | `data-sahne` olan bölümün içinde `data-derinlik="24"` (px; eksi = ters yön, büyük = öne yakın) ya da `data-dondur="8"` (derece) | Ana sayfanın üstündeki süzülen kartlar, araç fotoğrafı |
| "Kaydırdıkça bu parça farklı hızda kaysın (paralaks)" | `vitrin.css`'in sonundaki **KAYDIRMAYA BAĞLI PARALAKS** bölümüne bir satır + bir `@keyframes` | `.animasyon .sehir-arka { animation: sehir-kay linear both; animation-timeline: view(); }` |
| "Koyu, hareketli arka planlı bir bant" | bölüme `data-sahne` + içine `<partial name="_SahneZemini" />` | Ana sayfanın sonundaki "son çağrı bandı" |
| "Sayfa koyu bantla başlıyor, menü saydam olsun" | view'ın başına `ViewData["SaydamMenu"] = true;` ve bölüme `ust-altinda` sınıfı | `Views/Araclar/Index.cshtml` |
| "Bir efekti kapat" | ilgili `data-…` özelliğini silin; hepsini kapatmak için `tema.js`'te `animasyon` sınıfını ekleyen satırı silin | — |

Ana sayfadaki **akan yazı bandı**, **marka şeridi** ve **son çağrı bandı** panelden kapatılabilir: Yönetim → Site Metinleri → "Ana sayfa" → yazılarını boş bırakın.

**Performans kuralları** (site yavaşlarsa önce buraya bakın):
- Sürekli dönen bir animasyon sadece `transform` ve `opacity` değiştirmeli; `filter`, `background-position`, `box-shadow`, `backdrop-filter` (buzlu cam) ve `mix-blend-mode` her karede işlemciyi yorar.
- Kaydırmaya bağlı hareketler JavaScript ile değil CSS (`animation-timeline`) ile yapılır; tarayıcı bunları kaydırmayla birlikte ayrı iş parçacığında oynatır.
- **Yeni araç fotoğrafı:** en fazla ~1600 px genişlikte olsun. Kartlar `wwwroot/img/cars/k/` klasöründeki 720 px'lik `.webp` kopyayı kullanır (aynı ad, örn. `Audi_A3.jpg` → `k/Audi_A3.webp`; herhangi bir resim programıyla ya da squoosh.app ile üretilir). Kopyası yoksa büyük fotoğraf kullanılır, hiçbir şey bozulmaz. Kod: `Yardimcilar/Gorseller.cs`.
- Fotoğraflar ve kütüphaneler tarayıcıda önbelleğe alınır (`Yardimcilar/Altyapi.cs`); bir fotoğrafı aynı adla değiştirdiyseniz tarayıcıda `Ctrl + F5`.

Hoca "kodda nerede?" derse:

| Ne | Dosya |
|---|---|
| Kaydırınca belirme, sayaç, kart eğilmesi | `wwwroot/js/efektler.js` (her bölüm numaralı ve açıklamalı) |
| Işık bulutları, yol ızgarası, far izleri | `Views/Shared/_SahneZemini.cshtml` + `vitrin.css` → "SAHNE ZEMİNİ" |
| Karttan detaya uçan fotoğraf (sayfa geçişi) | `_AracKarti.cshtml` ve `Araclar/Detay.cshtml`'deki `view-transition-name: arac-@id` + `vitrin.css` → "Sayfalar arası geçiş" |
| Tema butonunda daire gibi açılan yeni tema | `wwwroot/js/genel.js` → "Açık / koyu tema" |
| "Hareketi azalt" açık cihazda her şeyin durması | `wwwroot/js/tema.js` (`animasyon` sınıfı) |
| Kategorilerin yatay akması, katmanların farklı hızda kayması | `vitrin.css` → "KAYDIRMAYA BAĞLI PARALAKS" |
| Kartlardaki küçük fotoğraf, tarayıcı önbelleği | `Yardimcilar/Gorseller.cs`, `Yardimcilar/Altyapi.cs` |

---

## Sık yapılan hatalar

| Belirti | Sebep | Çözüm |
|---|---|---|
| `SQLite Error 1: 'no such column: ...'` | Kolonun adı değişti ya da tür farkı var (eksik kolonları SemaGuncelleyici kendisi ekler) | Bilgisayarda `arackiralama.db` dosyasını silin |
| Panelde açma/kapama düğmesi yerine "Aktif" yazan kutu çıkıyor | View'da `asp-for="@degisken"` yazıldı | `asp-for` özelliğin adını ister: `asp-for="Aktif"` |
| Seçenek listesinde yazı ":" işaretinden sonra kesiliyor | `[Ayar(Secenekler = ...)]` görünen adında ":" var | Görünen adda ":" yerine parantez kullanın |
| Filtre seçiliyor ama liste değişmiyor | View'daki `name="..."` ile `AracFiltresi`'ndeki ad farklı | İkisini birebir aynı yazın (büyük / küçük harf dahil) |
| Formdaki yeni alan kaydedilmiyor | Controller'daki kopyalama satırı unutuldu | [3. tarif](#3-araca-yeni-alan), adım 3 |
| Yeni ayar panelde görünmüyor | `[Ayar(...)]` etiketi yok veya özellik `public` değil | Etiketi ve `{ get; set; }`'i kontrol edin |
| `The process cannot access the file` | Site açıkken derlediniz | Siteyi `Ctrl + C` ile kapatıp tekrar deneyin |
| Butona basınca JavaScript çalışmıyor | View'a `<script>` veya `onclick=` yazıldı (güvenlik politikası engeller) | Kodu `wwwroot/js/genel.js`'e yazın; `data-onay`, `data-otomatik-gonder` gibi hazır öznitelikleri kullanın |
