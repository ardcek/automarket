// ============================================================================
// ÖRNEK VERİLER
// Uygulama ilk açıldığında veritabanı boşsa doldurur:
//   1) İçerik : site ayarları ve sayfalar (Hakkımızda, KVKK...)       → bir kere
//   2) Demo   : kategoriler, markalar, şubeler, 47 araç, kullanıcılar,
//               son 12 aya yayılmış kiralama geçmişi                   → "Demo verilerini sıfırla" ile yenilenir
// Rastgelelik sabit tohumla (42) yapılır → her kurulumda aynı veriler oluşur.
// Demo hesapların şifresi: Rota123!
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Servisler;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Veri;

public static class OrnekVeriler
{
    public const string DemoSifresi = "Rota123!";

    /// <summary>Program.cs açılışta çağırır: tabloları oluşturur, boşsa doldurur.</summary>
    public static async Task YukleAsync(IServiceProvider servisler)
    {
        var vt = servisler.GetRequiredService<VeriTabani>();
        await vt.Database.EnsureCreatedAsync();          // tablolar yoksa oluştur

        var roller = servisler.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var rol in new[] { Roller.Admin, Roller.Personel, Roller.Musteri })
            if (!await roller.RoleExistsAsync(rol)) await roller.CreateAsync(new IdentityRole(rol));

        if (!await vt.SiteAyarlari.AnyAsync()) await IcerikYukleAsync(vt);
        if (!await vt.Araclar.AnyAsync()) await DemoYukleAsync(vt, servisler.GetRequiredService<UserManager<Kullanici>>());
    }

    /// <summary>
    /// Yönetim → Ayarlar → "Demo verilerini sıfırla". Araç, kullanıcı, rezervasyon ve tanımları siler
    /// ve örnekleri yeniden yükler. Site ayarlarına, metinlere, sayfalara ve görsellere DOKUNMAZ.
    /// </summary>
    public static async Task DemoSifirlaAsync(VeriTabani vt, UserManager<Kullanici> kullanicilar)
    {
        // Silme sırası önemli: önce bağımlı tablolar (ödeme → rezervasyon → araç ...).
        await vt.Odemeler.ExecuteDeleteAsync();
        await vt.Hasarlar.ExecuteDeleteAsync();
        await vt.RezervasyonEkstralari.ExecuteDeleteAsync();
        await vt.Rezervasyonlar.ExecuteDeleteAsync();
        await vt.Bakimlar.ExecuteDeleteAsync();
        await vt.Araclar.ExecuteDeleteAsync();
        await vt.UserRoles.ExecuteDeleteAsync();
        await vt.Users.ExecuteDeleteAsync();
        await vt.Ekstralar.ExecuteDeleteAsync();
        await vt.Subeler.ExecuteDeleteAsync();
        await vt.Markalar.ExecuteDeleteAsync();
        await vt.Kategoriler.ExecuteDeleteAsync();
        await vt.YakitTurleri.ExecuteDeleteAsync();
        await vt.VitesTurleri.ExecuteDeleteAsync();
        vt.ChangeTracker.Clear();
        await DemoYukleAsync(vt, kullanicilar);
    }

    // ===================================================================
    // 1) İÇERİK: site ayarları + sayfalar
    // ===================================================================
    private static async Task IcerikYukleAsync(VeriTabani vt)
    {
        vt.SiteAyarlari.Add(new SiteAyarlari());   // varsayılan değerler sınıfın içinde yazılı
        vt.Sayfalar.AddRange(
            new Sayfa
            {
                Baslik = "Hakkımızda", Adres = "hakkimizda", Sira = 1, MenudeGoster = true,
                Icerik = "{firma}, Türkiye'nin önemli havalimanlarında ve şehir merkezlerinde hizmet veren bir araç kiralama markasıdır.\n\n" +
                         "Ekonomik sınıftan lüks segmente, ticari araçlardan 9 kişilik minibüslere kadar geniş bir filoyla hizmet veriyoruz.\n\n" +
                         "## Neden biz?\n- Şeffaf fiyat: tüm ücretler en baştan gösterilir\n- Esnek iade: farklı şubeye bırakabilirsiniz\n- 7/24 yol yardımı",
            },
            new Sayfa
            {
                Baslik = "İletişim", Adres = "iletisim", Sira = 2, MenudeGoster = true, IletisimKartlari = true,
                Icerik = "Rezervasyon, fatura veya kurumsal kiralama talepleriniz için bize ulaşın.",
            },
            new Sayfa
            {
                Baslik = "Kiralama Koşulları", Adres = "kiralama-kosullari", Sira = 3,
                Icerik = "Aşağıdaki oranlar sistem ayarlarından otomatik gelir.\n\n" +
                         "- Başlayan her 24 saat 1 gün olarak ücretlendirilir.\n" +
                         "- {haftalik-gun} gün ve üzeri kiralamalarda {haftalik-indirim}, {aylik-gun} gün ve üzeri kiralamalarda {aylik-indirim} indirim uygulanır.\n" +
                         "- Farklı şubeye iadede {tek-yon} tek yön ücreti alınır.\n" +
                         "- Planlanan iade saatinden {gecikme-tolerans} dakika sonra başlayan her gün için günlük ücretin {gecikme-carpani} katı ücretlendirilir.\n" +
                         "- Eksik her çeyrek depo yakıt için {yakit-ucreti} alınır.\n" +
                         "- Alışa {iptal-saati} saatten fazla varken iptal ücretsizdir; daha geç iptalde 1 günlük ücret kesilir.\n" +
                         "- En uzun kiralama süresi {en-uzun} gündür.",
            },
            new Sayfa
            {
                Baslik = "KVKK Aydınlatma Metni", Adres = "kvkk", Sira = 4,
                Icerik = "6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında, veri sorumlusu olarak {firma} kişisel verilerinizi aşağıdaki çerçevede işler.\n\n" +
                         "## İşlenen veriler\nAd, soyad, T.C. kimlik numarası, iletişim bilgileri, doğum tarihi, ehliyet bilgileri ve kiralama geçmişi.\n\n" +
                         "## Haklarınız\nVerilerinize erişme, düzeltme ve silinmesini talep etme hakkına sahipsiniz. Talepleriniz için: {eposta}\n\n" +
                         "Bu metin proje ödevi kapsamında örnek olarak hazırlanmıştır.",
            });
        await vt.SaveChangesAsync();
    }

    // ===================================================================
    // 2) DEMO VERİLERİ
    // ===================================================================
    private static async Task DemoYukleAsync(VeriTabani vt, UserManager<Kullanici> kullanicilar)
    {
        var rastgele = new Random(42);
        var fiyat = new FiyatServisi(await vt.SiteAyarlari.FirstAsync());

        // ---- Tanımlar ----
        var kategoriler = new Dictionary<string, Kategori>
        {
            ["Ekonomi"] = new() { Ad = "Ekonomi", Ikon = "bi-piggy-bank", GorselAdresi = "/img/cars/Renault_Clio.jpg", MinYas = 21, MinEhliyetYili = 1, VarsayilanDepozito = 3000, Sira = 1 },
            ["Orta Sınıf"] = new() { Ad = "Orta Sınıf", Ikon = "bi-car-front", GorselAdresi = "/img/cars/Volkswagen_Golf.jpg", MinYas = 21, MinEhliyetYili = 2, VarsayilanDepozito = 5000, Sira = 2 },
            ["SUV"] = new() { Ad = "SUV", Ikon = "bi-truck-front", GorselAdresi = "/img/cars/Volkswagen_Tiguan.jpg", MinYas = 23, MinEhliyetYili = 3, VarsayilanDepozito = 7500, Sira = 3 },
            ["Lüks"] = new() { Ad = "Lüks", Ikon = "bi-gem", GorselAdresi = "/img/cars/Audi_A6.jpg", MinYas = 27, MinEhliyetYili = 5, VarsayilanDepozito = 12000, Sira = 4 },
            ["Ticari"] = new() { Ad = "Ticari", Ikon = "bi-truck", GorselAdresi = "/img/cars/Volkswagen_Transporter.jpg", MinYas = 23, MinEhliyetYili = 2, VarsayilanDepozito = 4000, Sira = 5 },
        };
        var yakitlar = new[] { "Benzin", "Dizel", "Hibrit", "Elektrik", "LPG" }
            .Select((ad, i) => new YakitTuru { Ad = ad, Sira = i }).ToDictionary(y => y.Ad);
        var vitesler = new[] { "Manuel", "Otomatik" }
            .Select((ad, i) => new VitesTuru { Ad = ad, Sira = i }).ToDictionary(v => v.Ad);
        var markalar = new[] { "Audi", "Fiat", "Ford", "Hyundai", "Peugeot", "Renault", "Volkswagen" }
            .Select((ad, i) => new Marka { Ad = ad, LogoAdresi = $"/img/brands/{ad.ToLowerInvariant()}.png", Sira = i }).ToDictionary(m => m.Ad);
        var subeler = new List<Sube>
        {
            new() { Ad = "İstanbul Havalimanı", Sehir = "İstanbul", Adres = "Tayakadın Mah. Terminal Cad. No:1, Arnavutköy", Telefon = "0212 555 10 10" },
            new() { Ad = "Kadıköy", Sehir = "İstanbul", Adres = "Caferağa Mah. Moda Cad. No:42, Kadıköy", Telefon = "0216 555 20 20" },
            new() { Ad = "Ankara Esenboğa", Sehir = "Ankara", Adres = "Esenboğa Havalimanı Dış Hatlar, Çubuk", Telefon = "0312 555 30 30" },
            new() { Ad = "İzmir Adnan Menderes", Sehir = "İzmir", Adres = "Dokuz Eylül Mah. Havalimanı Cad., Gaziemir", Telefon = "0232 555 40 40" },
            new() { Ad = "Antalya Havalimanı", Sehir = "Antalya", Adres = "Yenigöl Mah. Havalimanı Yolu, Muratpaşa", Telefon = "0242 555 50 50" },
        };
        var ekstralar = new List<Ekstra>
        {
            new() { Ad = "Tam Kasko", Ikon = "bi-shield-check", GunlukFiyat = 350, Aciklama = "Hasar durumunda muafiyet bedeli ödemezsiniz." },
            new() { Ad = "Ek Sürücü", Ikon = "bi-person-plus", GunlukFiyat = 150, Aciklama = "Araç ikinci bir sürücü tarafından da kullanılabilir." },
            new() { Ad = "Bebek Koltuğu", Ikon = "bi-emoji-smile", GunlukFiyat = 120, Aciklama = "0-4 yaş için ISOFIX bağlantılı koltuk." },
            new() { Ad = "Navigasyon", Ikon = "bi-geo-alt", GunlukFiyat = 90, Aciklama = "Güncel haritalı GPS cihazı." },
            new() { Ad = "Mobil Wi-Fi", Ikon = "bi-wifi", GunlukFiyat = 110, Aciklama = "Sınırsız internet paylaşımı." },
            new() { Ad = "Kar Zinciri", Ikon = "bi-snow", GunlukFiyat = 80, Aciklama = "Kış şartları için." },
        };
        vt.AddRange(kategoriler.Values);
        vt.AddRange(yakitlar.Values);
        vt.AddRange(vitesler.Values);
        vt.AddRange(markalar.Values);
        vt.AddRange(subeler);
        vt.AddRange(ekstralar);

        // ---- Araçlar (görseller wwwroot/img/cars klasöründe) ----
        var araclar = new List<Arac>();
        var plakalar = new HashSet<string>();
        string[] renkler = ["Beyaz", "Siyah", "Gri", "Gümüş", "Lacivert", "Kırmızı", "Mavi"];
        foreach (var (marka, model, kategori, koltuk, yakit, vites, gunluk, enEskiYil) in AracListesi)
        {
            var sube = subeler[rastgele.Next(subeler.Count)];
            var yil = rastgele.Next(enEskiYil, 2026);
            araclar.Add(new Arac
            {
                Marka = markalar[marka], Model = model, Yil = yil,
                Kategori = kategoriler[kategori], YakitTuru = yakitlar[yakit], VitesTuru = vitesler[vites],
                KoltukSayisi = koltuk, Renk = renkler[rastgele.Next(renkler.Length)],
                Kilometre = (2026 - yil) * rastgele.Next(12_000, 22_000) + rastgele.Next(1_000, 9_000),
                GunlukFiyat = gunluk, Depozito = kategoriler[kategori].VarsayilanDepozito,
                Sube = sube, Plaka = PlakaUret(sube.Sehir, rastgele, plakalar),
                GorselAdresi = $"/img/cars/{marka}_{model.Replace(' ', '_')}.jpg",
                Aciklama = $"{marka} {model}; {kategori.ToLower(Bicim.Tr)} sınıfında konforlu ve bakımlı bir araç.",
                EklenmeTarihi = DateTime.Now.AddYears(-1),
            });
        }
        // Her kategoriden en yeni araç ana sayfada "öne çıkan" olsun (admin panelden değiştirilebilir).
        foreach (var grup in araclar.GroupBy(a => a.Kategori))
            foreach (var a in grup.OrderByDescending(a => a.Yil).Take(2)) a.OneCikan = true;
        vt.Araclar.AddRange(araclar);
        await vt.SaveChangesAsync();

        // ---- Kullanıcılar ----
        var personel = await KullaniciEkleAsync(kullanicilar, "personel@rota.com", "Murat", "Kaya", Roller.Personel, new DateTime(1992, 9, 3), rastgele);
        await KullaniciEkleAsync(kullanicilar, "admin@rota.com", "Selin", "Aydın", Roller.Admin, new DateTime(1985, 4, 12), rastgele);
        var demoMusteri = await KullaniciEkleAsync(kullanicilar, "musteri@rota.com", "Ayşe", "Yılmaz", Roller.Musteri, new DateTime(1994, 2, 20), rastgele);
        // 20 yaşında, ehliyeti 1 yıllık → SUV / lüks kiralayamaz (iş kuralını göstermek için)
        await KullaniciEkleAsync(kullanicilar, "genc@rota.com", "Emre", "Doğan", Roller.Musteri, DateTime.Today.AddYears(-20).AddDays(-30), rastgele, ehliyetYasi: 19);
        var musteriler = new List<Kullanici> { demoMusteri };
        foreach (var (ad, soyad) in MusteriIsimleri)
            musteriler.Add(await KullaniciEkleAsync(kullanicilar, $"{Ascii(ad)}.{Ascii(soyad)}@example.com", ad, soyad, Roller.Musteri,
                new DateTime(rastgele.Next(1965, 2001), rastgele.Next(1, 13), rastgele.Next(1, 28)), rastgele));

        // ---- Kiralama geçmişi ----
        var simdi = DateTime.Now;
        var rezervasyonlar = new List<Rezervasyon>();

        Rezervasyon Ekle(Arac arac, DateTime bas, DateTime bit, RezervasyonDurumu durum)
        {
            var iade = rastgele.NextDouble() < 0.15 ? subeler[rastgele.Next(subeler.Count)] : arac.Sube!;
            var secilenEkstralar = ekstralar.OrderBy(_ => rastgele.Next()).Take(rastgele.Next(0, 3)).ToList();
            var r = new Rezervasyon
            {
                Musteri = musteriler[rastgele.Next(musteriler.Count)], Arac = arac,
                AlisSube = arac.Sube, IadeSube = iade, BaslangicTarihi = bas, BitisTarihi = bit, Durum = durum,
                OlusturmaTarihi = Min(bas.AddDays(-rastgele.Next(1, 15)), simdi.AddHours(-1)),
                IslemYapanId = durum == RezervasyonDurumu.OnayBekliyor ? null : personel.Id,
            };
            RezervasyonServisi.TeklifiUygula(r, fiyat.Hesapla(arac, bas, bit, secilenEkstralar, iade != arac.Sube));
            if (durum == RezervasyonDurumu.Kirada) { r.TeslimTarihi = bas; r.TeslimKm = arac.Kilometre; r.TeslimYakit = YakitSeviyesi.Dolu; arac.Durum = AracDurumu.Kirada; }
            rezervasyonlar.Add(r);
            return r;
        }

        for (var i = 0; i < araclar.Count; i++)
        {
            var arac = araclar[i];
            // İlk 6 aracın geçmişi erken biter; aşağıdaki "bugün" senaryolarına yer açmak için.
            var sinir = i < 6 ? simdi.AddDays(-15) : simdi.AddDays(45);
            var imlec = DateTime.Today.AddDays(-340 + rastgele.Next(0, 25));
            while (true)
            {
                var bas = imlec.AddDays(rastgele.Next(1, 10)).Date.AddHours(rastgele.Next(8, 19));
                var gun = rastgele.NextDouble() < 0.1 ? rastgele.Next(7, 15) : rastgele.Next(1, 7);
                var bit = bas.AddDays(gun);
                if (bit > sinir) break;
                imlec = bit;

                if (bit < simdi)   // geçmişte bitmiş
                {
                    var zar = rastgele.NextDouble();
                    var durum = zar < 0.05 ? RezervasyonDurumu.IptalEdildi : zar < 0.09 ? RezervasyonDurumu.Reddedildi : RezervasyonDurumu.Tamamlandi;
                    var r = Ekle(arac, bas, bit, durum);
                    if (durum == RezervasyonDurumu.Tamamlandi) GecmisIadeyiTamamla(r, arac, fiyat, rastgele, personel);
                }
                else if (bas <= simdi) Ekle(arac, bas, bit, RezervasyonDurumu.Kirada);
                else Ekle(arac, bas, bit, rastgele.NextDouble() < 0.7 ? RezervasyonDurumu.Onaylandi : RezervasyonDurumu.OnayBekliyor);
            }
        }

        // ---- "Bugün" senaryoları (gösterge panelindeki "bugünün işleri" dolu görünsün) ----
        foreach (var a in araclar.Take(2)) Ekle(a, simdi.AddDays(-6).Date.AddHours(10), simdi.AddDays(-1).Date.AddHours(10), RezervasyonDurumu.Kirada).Gecikti = true;
        foreach (var a in araclar.Skip(2).Take(2)) Ekle(a, simdi.AddHours(2), simdi.AddDays(4).AddHours(2), RezervasyonDurumu.Onaylandi);
        foreach (var a in araclar.Skip(4).Take(2)) Ekle(a, simdi.AddDays(-3), simdi.AddHours(3), RezervasyonDurumu.Kirada);
        foreach (var a in araclar.Skip(6).Take(4)) { var b = DateTime.Today.AddDays(rastgele.Next(60, 80)).AddHours(10); Ekle(a, b, b.AddDays(3), RezervasyonDurumu.OnayBekliyor); }

        // Demo müşterinin her durumdan en az bir rezervasyonu olsun.
        foreach (var durum in new[] { RezervasyonDurumu.Tamamlandi, RezervasyonDurumu.Onaylandi, RezervasyonDurumu.OnayBekliyor })
        {
            var r = rezervasyonlar.Where(x => x.Durum == durum).OrderByDescending(x => x.BaslangicTarihi).Skip(2).FirstOrDefault();
            if (r != null) r.Musteri = demoMusteri;
        }

        // Kodlar oluşturulma sırasına göre: RZ-2025-0001, RZ-2025-0002 ...
        foreach (var yil in rezervasyonlar.OrderBy(r => r.OlusturmaTarihi).GroupBy(r => r.OlusturmaTarihi.Year))
        {
            var sira = 1;
            foreach (var r in yil) r.Kod = $"RZ-{yil.Key}-{sira++:D4}";
        }
        vt.Rezervasyonlar.AddRange(rezervasyonlar);

        // ---- Bakım kayıtları: 1 araç şu an bakımda + geçmiş bakımlar ----
        var bakimdaki = araclar.Skip(6).First(a => a.Durum == AracDurumu.Musait && !rezervasyonlar.Any(r => r.Arac == a
            && MusaitlikServisi.KilitleyenDurumlar.Contains(r.Durum) && MusaitlikServisi.Cakisiyor(r.BaslangicTarihi, r.BitisTarihi, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(3))));
        bakimdaki.Durum = AracDurumu.Bakimda;
        vt.Bakimlar.Add(new Bakim { Arac = bakimdaki, Tur = BakimTuru.Onarim, BaslangicTarihi = DateTime.Today.AddDays(-1), BitisTarihi = DateTime.Today.AddDays(2).AddHours(18), Kilometre = bakimdaki.Kilometre, Maliyet = 8500, Aciklama = "Ön fren balatası ve disk değişimi." });
        foreach (var a in araclar.OrderBy(_ => rastgele.Next()).Take(18))
        {
            var bas = DateTime.Today.AddDays(-rastgele.Next(20, 330)).AddHours(9);
            var tur = (BakimTuru)rastgele.Next(0, 5);
            vt.Bakimlar.Add(new Bakim { Arac = a, Tur = tur, BaslangicTarihi = bas, BitisTarihi = bas.AddDays(1), Kilometre = Math.Max(0, a.Kilometre - rastgele.Next(2000, 15000)), Maliyet = rastgele.Next(2, 12) * 1000, Aciklama = tur.Ad(), Tamamlandi = true });
        }

        await vt.SaveChangesAsync();
    }

    // ===================== Yardımcılar =====================

    /// <summary>Geçmiş bir kiralamayı iade edilmiş hale getirir (bazılarında gecikme / yakıt / hasar).</summary>
    private static void GecmisIadeyiTamamla(Rezervasyon r, Arac arac, FiyatServisi fiyat, Random rastgele, Kullanici personel)
    {
        var yol = rastgele.Next(80, 450) * r.GunSayisi;
        r.TeslimTarihi = r.BaslangicTarihi;
        r.TeslimKm = Math.Max(0, arac.Kilometre - yol - rastgele.Next(500, 3000));
        r.IadeKm = r.TeslimKm + yol;
        r.TeslimYakit = YakitSeviyesi.Dolu;
        r.IadeYakit = rastgele.NextDouble() < 0.12 ? YakitSeviyesi.Yarim : YakitSeviyesi.Dolu;
        r.IadeTarihi = rastgele.NextDouble() < 0.1 ? r.BitisTarihi.AddHours(rastgele.Next(2, 30)) : r.BitisTarihi.AddMinutes(-rastgele.Next(0, 90));
        r.GecikmeUcreti = fiyat.GecikmeUcreti(r.GunlukFiyat, r.BitisTarihi, r.IadeTarihi.Value);
        r.YakitUcreti = fiyat.YakitUcreti(r.TeslimYakit.Value, r.IadeYakit.Value);
        if (rastgele.NextDouble() < 0.04)
        {
            r.HasarUcreti = rastgele.Next(2, 10) * 500;
            r.Hasarlar.Add(new Hasar { Aciklama = "Arka tamponda çizik.", Tutar = r.HasarUcreti, Tarih = r.IadeTarihi.Value });
        }
        RezervasyonServisi.ToplamiHesapla(r);
        r.Odemeler.Add(new Odeme { Tutar = r.ToplamTutar, Tur = OdemeTuru.KiraBedeli, Yontem = (OdemeYontemi)rastgele.Next(0, 3), Tarih = r.IadeTarihi.Value, AlanKullaniciId = personel.Id });
    }

    private static string? _demoSifreHashi;

    private static async Task<Kullanici> KullaniciEkleAsync(UserManager<Kullanici> yonetici, string eposta, string ad, string soyad,
        string rol, DateTime dogum, Random rastgele, int ehliyetYasi = 0)
    {
        var kullanici = new Kullanici
        {
            UserName = eposta, Email = eposta, EmailConfirmed = true, Ad = ad, Soyad = soyad,
            PhoneNumber = $"05{rastgele.Next(30, 56)} {rastgele.Next(100, 999)} {rastgele.Next(10, 99)} {rastgele.Next(10, 99)}",
            TcNo = TcKimlikNo.Uret(rastgele), DogumTarihi = dogum, EhliyetNo = rastgele.Next(100000, 999999).ToString(),
            EhliyetTarihi = dogum.AddYears(ehliyetYasi > 0 ? ehliyetYasi : rastgele.Next(18, 24)).AddDays(rastgele.Next(0, 200)),
            KayitTarihi = DateTime.Now.AddDays(-rastgele.Next(30, 400)),
        };
        // Şifre hash'leme bilerek yavaştır. Demo hesapların şifresi aynı olduğu için bir kez hesaplayıp
        // hepsine veriyoruz (zayıf sunucuda açılışı hızlandırır). Gerçek kayıtlarda her şifre ayrı hash'lenir.
        kullanici.PasswordHash = _demoSifreHashi ??= yonetici.PasswordHasher.HashPassword(kullanici, DemoSifresi);
        var sonuc = await yonetici.CreateAsync(kullanici);
        if (!sonuc.Succeeded) throw new InvalidOperationException(string.Join(", ", sonuc.Errors.Select(e => e.Description)));
        await yonetici.AddToRoleAsync(kullanici, rol);
        return kullanici;
    }

    private static string PlakaUret(string sehir, Random rastgele, HashSet<string> kullanilan)
    {
        var ilKodu = sehir switch { "Ankara" => "06", "İzmir" => "35", "Antalya" => "07", _ => "34" };
        const string harfler = "ABCDEFGHJKLMNPRSTUVYZ";
        while (true)
        {
            var harf = new string(Enumerable.Range(0, rastgele.Next(2, 4)).Select(_ => harfler[rastgele.Next(harfler.Length)]).ToArray());
            var plaka = $"{ilKodu} {harf} {rastgele.Next(100, 1000)}";
            if (kullanilan.Add(plaka)) return plaka;
        }
    }

    /// <summary>"İrem Şahin" → "irem.sahin" (e-posta adresinde Türkçe karakter olmasın).</summary>
    private static string Ascii(string metin)
    {
        var tablo = new Dictionary<char, char> { ['ç'] = 'c', ['ğ'] = 'g', ['ı'] = 'i', ['İ'] = 'i', ['ö'] = 'o', ['ş'] = 's', ['ü'] = 'u', ['Ç'] = 'c', ['Ş'] = 's', ['Ö'] = 'o', ['Ü'] = 'u', [' '] = '.' };
        return new string(metin.Select(c => tablo.TryGetValue(c, out var a) ? a : char.ToLowerInvariant(c)).ToArray());
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static readonly (string, string)[] MusteriIsimleri =
    [
        ("Mehmet", "Demir"), ("Zeynep", "Çelik"), ("Ahmet", "Şahin"), ("Elif", "Yıldız"), ("Mustafa", "Öztürk"),
        ("Fatma", "Arslan"), ("Can", "Koç"), ("Deniz", "Kurt"), ("Burak", "Özdemir"), ("Ece", "Aslan"),
        ("Hakan", "Polat"), ("Gizem", "Erdoğan"), ("Oğuz", "Güneş"), ("Merve", "Aksoy"), ("Kerem", "Tekin"),
        ("Seda", "Kılıç"), ("Tolga", "Bulut"), ("İrem", "Karaca"), ("Serkan", "Yavuz"), ("Buse", "Ünal"),
    ];

    // (marka, model, kategori, koltuk, yakıt, vites, günlük fiyat, en eski model yılı)
    private static readonly (string, string, string, int, string, string, decimal, int)[] AracListesi =
    [
        ("Audi", "A3", "Orta Sınıf", 5, "Benzin", "Otomatik", 1950, 2019), ("Audi", "A4", "Lüks", 5, "Dizel", "Otomatik", 2650, 2019),
        ("Audi", "A5", "Lüks", 4, "Benzin", "Otomatik", 3100, 2019), ("Audi", "A6", "Lüks", 5, "Dizel", "Otomatik", 3650, 2019),
        ("Audi", "A7", "Lüks", 4, "Benzin", "Otomatik", 4500, 2019), ("Audi", "Q3", "SUV", 5, "Benzin", "Otomatik", 2700, 2019),
        ("Audi", "Q5", "SUV", 5, "Dizel", "Otomatik", 3300, 2019), ("Audi", "Q7", "Lüks", 7, "Dizel", "Otomatik", 4800, 2019),
        ("Fiat", "500L", "Ekonomi", 5, "Benzin", "Manuel", 1150, 2019), ("Fiat", "Doblo", "Ticari", 5, "Dizel", "Manuel", 1300, 2019),
        ("Fiat", "Egea", "Ekonomi", 5, "Dizel", "Manuel", 1050, 2019), ("Fiat", "Fiorino", "Ticari", 2, "Dizel", "Manuel", 1100, 2019),
        ("Fiat", "Linea", "Ekonomi", 5, "LPG", "Manuel", 900, 2016), ("Fiat", "Palio", "Ekonomi", 5, "Benzin", "Manuel", 800, 2014),
        ("Ford", "Fiesta", "Ekonomi", 5, "Benzin", "Manuel", 1100, 2019), ("Ford", "Focus", "Orta Sınıf", 5, "Dizel", "Otomatik", 1450, 2019),
        ("Ford", "Mondeo", "Orta Sınıf", 5, "Hibrit", "Otomatik", 1850, 2019), ("Ford", "Tourneo Courier", "Ticari", 5, "Dizel", "Manuel", 1350, 2019),
        ("Ford", "Transit", "Ticari", 9, "Dizel", "Manuel", 2300, 2019), ("Hyundai", "Accent", "Ekonomi", 5, "LPG", "Manuel", 900, 2016),
        ("Hyundai", "Accent Blue", "Ekonomi", 5, "Dizel", "Otomatik", 1000, 2019), ("Hyundai", "Bayon", "SUV", 5, "Benzin", "Otomatik", 1550, 2019),
        ("Hyundai", "Tucson", "SUV", 5, "Hibrit", "Otomatik", 2350, 2019), ("Hyundai", "i20", "Ekonomi", 5, "Benzin", "Otomatik", 1150, 2019),
        ("Hyundai", "i20N", "Orta Sınıf", 5, "Benzin", "Manuel", 2200, 2019), ("Hyundai", "i30", "Orta Sınıf", 5, "Benzin", "Otomatik", 1350, 2019),
        ("Peugeot", "206", "Ekonomi", 5, "Benzin", "Manuel", 750, 2010), ("Peugeot", "208", "Ekonomi", 5, "Elektrik", "Otomatik", 1300, 2019),
        ("Peugeot", "3008", "SUV", 5, "Dizel", "Otomatik", 2250, 2019), ("Peugeot", "301", "Ekonomi", 5, "Dizel", "Manuel", 1000, 2019),
        ("Peugeot", "307", "Ekonomi", 5, "Benzin", "Manuel", 850, 2012), ("Peugeot", "308", "Orta Sınıf", 5, "Benzin", "Otomatik", 1450, 2019),
        ("Peugeot", "5008", "SUV", 7, "Dizel", "Otomatik", 2650, 2019), ("Renault", "Broadway", "Ekonomi", 5, "LPG", "Manuel", 700, 2008),
        ("Renault", "Clio", "Ekonomi", 5, "Benzin", "Manuel", 1100, 2019), ("Renault", "Express", "Ticari", 5, "Dizel", "Manuel", 1150, 2019),
        ("Renault", "Megane", "Orta Sınıf", 5, "Dizel", "Otomatik", 1400, 2019), ("Renault", "Symbol", "Ekonomi", 5, "Dizel", "Manuel", 950, 2019),
        ("Renault", "Talisman", "Orta Sınıf", 5, "Dizel", "Otomatik", 1900, 2019), ("Volkswagen", "Arteon", "Lüks", 5, "Benzin", "Otomatik", 3200, 2019),
        ("Volkswagen", "Caddy", "Ticari", 5, "Dizel", "Manuel", 1400, 2019), ("Volkswagen", "Golf", "Orta Sınıf", 5, "Benzin", "Otomatik", 1600, 2019),
        ("Volkswagen", "Jetta", "Orta Sınıf", 5, "Benzin", "Otomatik", 1550, 2019), ("Volkswagen", "Passat", "Orta Sınıf", 5, "Dizel", "Otomatik", 2100, 2019),
        ("Volkswagen", "Polo", "Ekonomi", 5, "Benzin", "Manuel", 1250, 2019), ("Volkswagen", "Tiguan", "SUV", 5, "Dizel", "Otomatik", 2500, 2019),
        ("Volkswagen", "Transporter", "Ticari", 9, "Dizel", "Manuel", 2650, 2019),
    ];
}
