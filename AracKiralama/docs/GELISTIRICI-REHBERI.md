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

**Her değişiklikten sonra:** siteyi kapatıp (`Ctrl + C`) tekrar `dotnet run --project src/AracKiralama.Web` ile açın. **Bir tabloya yeni alan / tablo eklediyseniz** `src/AracKiralama.Web/arackiralama.db` dosyasını silin (veritabanı yeni yapıyla baştan oluşur; canlı site için README → 13.5).

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

## Sık yapılan hatalar

| Belirti | Sebep | Çözüm |
|---|---|---|
| `SQLite Error 1: 'no such column: ...'` | Tabloya alan eklendi, veritabanı eski | `arackiralama.db` dosyasını silin |
| Filtre seçiliyor ama liste değişmiyor | View'daki `name="..."` ile `AracFiltresi`'ndeki ad farklı | İkisini birebir aynı yazın (büyük / küçük harf dahil) |
| Formdaki yeni alan kaydedilmiyor | Controller'daki kopyalama satırı unutuldu | [3. tarif](#3-araca-yeni-alan), adım 3 |
| Yeni ayar panelde görünmüyor | `[Ayar(...)]` etiketi yok veya özellik `public` değil | Etiketi ve `{ get; set; }`'i kontrol edin |
| `The process cannot access the file` | Site açıkken derlediniz | Siteyi `Ctrl + C` ile kapatıp tekrar deneyin |
| Butona basınca JavaScript çalışmıyor | View'a `<script>` veya `onclick=` yazıldı (güvenlik politikası engeller) | Kodu `wwwroot/js/genel.js`'e yazın; `data-onay`, `data-otomatik-gonder` gibi hazır öznitelikleri kullanın |
