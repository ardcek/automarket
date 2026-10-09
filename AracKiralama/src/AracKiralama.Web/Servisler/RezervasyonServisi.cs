// ============================================================================
// REZERVASYON SERVİSİ: rezervasyonun bütün yaşam döngüsü burada.
//
//   Onay Bekliyor ──onayla──▶ Onaylandı ──teslim et──▶ Kirada ──iade al──▶ Tamamlandı
//        │ reddet                 │ iptal et
//        ▼                        ▼
//   Reddedildi               İptal Edildi       (Onay Bekliyor da iptal edilebilir)
//
// Bir kural çiğnenirse IsKuraliHatasi fırlatılır; controller mesajı ekranda gösterir.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

public class RezervasyonServisi
{
    private readonly VeriTabani _vt;
    private readonly FiyatServisi _fiyat;
    private readonly MusaitlikServisi _musaitlik;
    private readonly SiteAyarlari _ayarlar;

    public RezervasyonServisi(VeriTabani vt, FiyatServisi fiyat, MusaitlikServisi musaitlik, SiteAyarlari ayarlar)
    {
        _vt = vt;
        _fiyat = fiyat;
        _musaitlik = musaitlik;
        _ayarlar = ayarlar;
    }

    // ===================== KURALLAR (veritabanı gerektirmez) =====================

    /// <summary>Hangi durumdan hangi durumlara geçilebilir?</summary>
    public static readonly Dictionary<RezervasyonDurumu, RezervasyonDurumu[]> Gecisler = new()
    {
        [RezervasyonDurumu.OnayBekliyor] = [RezervasyonDurumu.Onaylandi, RezervasyonDurumu.Reddedildi, RezervasyonDurumu.IptalEdildi],
        [RezervasyonDurumu.Onaylandi] = [RezervasyonDurumu.Kirada, RezervasyonDurumu.IptalEdildi],
        [RezervasyonDurumu.Kirada] = [RezervasyonDurumu.Tamamlandi],
        [RezervasyonDurumu.Tamamlandi] = [],
        [RezervasyonDurumu.IptalEdildi] = [],
        [RezervasyonDurumu.Reddedildi] = [],
    };

    public static bool GecisMumkun(RezervasyonDurumu simdiki, RezervasyonDurumu yeni) => Gecisler[simdiki].Contains(yeni);

    private static void GecisiKontrolEt(Rezervasyon r, RezervasyonDurumu yeni)
    {
        if (!GecisMumkun(r.Durum, yeni))
            throw new IsKuraliHatasi($"\"{r.Durum.Ad()}\" durumundaki rezervasyon \"{yeni.Ad()}\" yapılamaz.");
    }

    /// <summary>Tarih kuralları. Hata yoksa boş liste döner.</summary>
    public static List<string> TarihHatalari(DateTime baslangic, DateTime bitis, DateTime simdi, int enUzunGun)
    {
        var hatalar = new List<string>();
        if (baslangic < simdi.AddMinutes(-5)) hatalar.Add("Alış tarihi geçmişte olamaz.");
        if (bitis <= baslangic) hatalar.Add("İade tarihi alış tarihinden sonra olmalıdır.");
        else if ((bitis - baslangic).TotalDays > enUzunGun) hatalar.Add($"En fazla {enUzunGun} günlük kiralama yapılabilir.");
        return hatalar;
    }

    /// <summary>Sürücü yaşı ve ehliyet süresi aracın kategorisinin şartlarını karşılıyor mu?</summary>
    public static List<string> SurucuHatalari(Kullanici kullanici, Kategori kategori, DateTime tarih)
    {
        var hatalar = new List<string>();
        if (kullanici.KaraListede)
        {
            hatalar.Add("Hesabınız rezervasyon yapmaya kapalıdır. Lütfen bizimle iletişime geçin.");
            return hatalar;
        }
        if (kullanici.DogumTarihi == null || kullanici.EhliyetTarihi == null)
        {
            hatalar.Add("Rezervasyon için profilinizde doğum tarihi ve ehliyet bilgileri eksiksiz olmalıdır.");
            return hatalar;
        }
        if (YilFarki(kullanici.DogumTarihi.Value, tarih) < kategori.MinYas)
            hatalar.Add($"{kategori.Ad} sınıfı araçlar için en az {kategori.MinYas} yaşında olmalısınız.");
        if (YilFarki(kullanici.EhliyetTarihi.Value, tarih) < kategori.MinEhliyetYili)
            hatalar.Add($"{kategori.Ad} sınıfı araçlar için en az {kategori.MinEhliyetYili} yıllık ehliyet gereklidir.");
        return hatalar;
    }

    /// <summary>İki tarih arasındaki tam yıl (yaş hesabı).</summary>
    public static int YilFarki(DateTime ilk, DateTime son)
    {
        var yil = son.Year - ilk.Year;
        if (ilk.Date > son.Date.AddYears(-yil)) yil--;
        return yil;
    }

    /// <summary>Fiyat teklifini rezervasyona kopyalar.</summary>
    public static void TeklifiUygula(Rezervasyon r, FiyatTeklifi t)
    {
        r.GunSayisi = t.GunSayisi;
        r.GunlukFiyat = t.GunlukFiyat;
        r.KiraTutari = t.KiraTutari;
        r.Indirim = t.Indirim;
        r.EkstraTutari = t.EkstraTutari;
        r.TekYonUcreti = t.TekYonUcreti;
        r.Depozito = t.Depozito;
        r.Ekstralar = t.Ekstralar
            .Select(e => new RezervasyonEkstrasi { EkstraId = e.EkstraId, GunlukFiyat = e.GunlukFiyat, Tutar = e.Tutar })
            .ToList();
        ToplamiHesapla(r);
    }

    /// <summary>Toplam = kira − indirim + ekstralar + tek yön + gecikme + yakıt + hasar</summary>
    public static void ToplamiHesapla(Rezervasyon r)
    {
        r.ToplamTutar = r.KiraTutari - r.Indirim + r.EkstraTutari + r.TekYonUcreti
                        + r.GecikmeUcreti + r.YakitUcreti + r.HasarUcreti;
    }

    // ===================== İŞLEMLER =====================

    /// <summary>Seçilen araç, tarih ve ekstralar için fiyat teklifi.</summary>
    public async Task<(Arac arac, FiyatTeklifi teklif)> TeklifAsync(int aracId, DateTime baslangic, DateTime bitis,
        List<int> ekstraIdleri, int alisSubeId, int iadeSubeId)
    {
        var arac = await _vt.Araclar.Include(a => a.Marka).Include(a => a.Kategori).FirstOrDefaultAsync(a => a.Id == aracId)
                   ?? throw new IsKuraliHatasi("Araç bulunamadı.");
        var ekstralar = await _vt.Ekstralar.Where(e => e.Aktif && ekstraIdleri.Contains(e.Id)).OrderBy(e => e.Ad).ToListAsync();

        var teklif = _fiyat.Hesapla(arac, baslangic, bitis, ekstralar, alisSubeId != iadeSubeId);
        return (arac, teklif);
    }

    /// <summary>Müşterinin yeni rezervasyon talebi.</summary>
    public async Task<Rezervasyon> OlusturAsync(string musteriId, int aracId, int alisSubeId, int iadeSubeId,
        DateTime baslangic, DateTime bitis, List<int> ekstraIdleri, string? not)
    {
        if (!_ayarlar.RezervasyonaAcik) throw new IsKuraliHatasi("Şu anda yeni rezervasyon alınmamaktadır.");

        var tarihHatalari = TarihHatalari(baslangic, bitis, DateTime.Now, _ayarlar.EnUzunKiralama);
        if (tarihHatalari.Count > 0) throw new IsKuraliHatasi(tarihHatalari[0]);

        var musteri = await _vt.Users.FindAsync(musteriId) ?? throw new IsKuraliHatasi("Müşteri bulunamadı.");
        if (!await _vt.Subeler.AnyAsync(s => s.Id == alisSubeId) || !await _vt.Subeler.AnyAsync(s => s.Id == iadeSubeId))
            throw new IsKuraliHatasi("Lütfen geçerli bir alış ve iade şubesi seçin.");
        var (arac, teklif) = await TeklifAsync(aracId, baslangic, bitis, ekstraIdleri, alisSubeId, iadeSubeId);

        var surucuHatalari = SurucuHatalari(musteri, arac.Kategori!, baslangic);
        if (surucuHatalari.Count > 0) throw new IsKuraliHatasi(surucuHatalari[0]);

        if (!await _musaitlik.MusaitMiAsync(aracId, baslangic, bitis))
            throw new IsKuraliHatasi("Üzgünüz, araç seçtiğiniz tarihlerde müsait değil.");

        // Aynı müşteri aynı araca aynı tarihlerde ikinci bir talep açamasın.
        var ayniTalepVar = await _vt.Rezervasyonlar.AnyAsync(r => r.MusteriId == musteriId && r.AracId == aracId
            && r.Durum == RezervasyonDurumu.OnayBekliyor && r.BaslangicTarihi < bitis && baslangic < r.BitisTarihi);
        if (ayniTalepVar) throw new IsKuraliHatasi("Bu araç için bu tarihlerde zaten bekleyen bir talebiniz var.");

        var rezervasyon = new Rezervasyon
        {
            Kod = await KodUretAsync(),
            MusteriId = musteriId,
            AracId = aracId,
            AlisSubeId = alisSubeId,
            IadeSubeId = iadeSubeId,
            BaslangicTarihi = baslangic,
            BitisTarihi = bitis,
            Not = not,
            // Ayarlar → Rezervasyon → "Talepler otomatik onaylansın" açıksa doğrudan onaylanır.
            Durum = _ayarlar.OtomatikOnay ? RezervasyonDurumu.Onaylandi : RezervasyonDurumu.OnayBekliyor,
        };
        TeklifiUygula(rezervasyon, teklif);

        _vt.Rezervasyonlar.Add(rezervasyon);
        await _vt.SaveChangesAsync();
        return rezervasyon;
    }

    public async Task OnaylaAsync(int id, string personelId)
    {
        // Transaction: kontrol ve kayıt birlikte yapılır → aynı araç aynı tarihte iki kişiye onaylanamaz.
        await using var islem = await _vt.Database.BeginTransactionAsync();

        var r = await GetirAsync(id);
        GecisiKontrolEt(r, RezervasyonDurumu.Onaylandi);
        if (r.BaslangicTarihi < DateTime.Now.AddHours(-1))
            throw new IsKuraliHatasi("Alış tarihi geçmiş bir talep onaylanamaz. Lütfen reddedin.");
        if (!await _musaitlik.MusaitMiAsync(r.AracId, r.BaslangicTarihi, r.BitisTarihi, haricRezervasyonId: r.Id))
            throw new IsKuraliHatasi("Araç bu tarihlerde başka bir rezervasyona ayrılmış veya bakımda.");

        r.Durum = RezervasyonDurumu.Onaylandi;
        r.IslemYapanId = personelId;

        // Aynı araç için çakışan diğer bekleyen talepler otomatik reddedilir.
        var cakisanlar = await _vt.Rezervasyonlar.Where(x => x.Id != r.Id && x.AracId == r.AracId
            && x.Durum == RezervasyonDurumu.OnayBekliyor && x.BaslangicTarihi < r.BitisTarihi && r.BaslangicTarihi < x.BitisTarihi).ToListAsync();
        foreach (var x in cakisanlar)
        {
            x.Durum = RezervasyonDurumu.Reddedildi;
            x.DurumAciklamasi = $"Araç aynı tarihler için başka bir rezervasyona ({r.Kod}) ayrıldı.";
            x.IslemYapanId = personelId;
        }

        await _vt.SaveChangesAsync();
        await islem.CommitAsync();
    }

    public async Task ReddetAsync(int id, string personelId, string? sebep)
    {
        var r = await GetirAsync(id);
        GecisiKontrolEt(r, RezervasyonDurumu.Reddedildi);
        r.Durum = RezervasyonDurumu.Reddedildi;
        r.DurumAciklamasi = string.IsNullOrWhiteSpace(sebep) ? "Talep uygun bulunmadı." : sebep.Trim();
        r.IslemYapanId = personelId;
        await _vt.SaveChangesAsync();
    }

    /// <summary>İptal. Müşteri sadece kendi rezervasyonunu iptal edebilir; personel iptalinde ücret kesilmez.</summary>
    public async Task IptalEtAsync(int id, string kullaniciId, bool personelMi, string? sebep = null)
    {
        var r = await GetirAsync(id);
        if (!personelMi && r.MusteriId != kullaniciId) throw new IsKuraliHatasi("Bu rezervasyonu iptal etme yetkiniz yok.");
        GecisiKontrolEt(r, RezervasyonDurumu.IptalEdildi);

        r.IptalUcreti = personelMi ? 0 : _fiyat.IptalUcreti(r, DateTime.Now);
        r.Durum = RezervasyonDurumu.IptalEdildi;
        r.DurumAciklamasi = sebep ?? (personelMi ? "Personel tarafından iptal edildi." : "Müşteri tarafından iptal edildi.");
        if (personelMi) r.IslemYapanId = kullaniciId;
        await _vt.SaveChangesAsync();
    }

    /// <summary>Araç müşteriye verildi.</summary>
    public async Task TeslimEtAsync(int id, string personelId, int teslimKm, YakitSeviyesi teslimYakit)
    {
        var r = await GetirAsync(id);
        GecisiKontrolEt(r, RezervasyonDurumu.Kirada);
        var arac = r.Arac!;
        if (arac.Durum == AracDurumu.Kirada)
            throw new IsKuraliHatasi("Araç şu anda başka bir müşteride görünüyor; önce o kiralamanın iadesini alın.");
        if (teslimKm < arac.Kilometre)
            throw new IsKuraliHatasi($"Teslim kilometresi aracın kayıtlı kilometresinden ({Bicim.Sayi(arac.Kilometre)}) küçük olamaz.");

        r.Durum = RezervasyonDurumu.Kirada;
        r.TeslimTarihi = DateTime.Now;
        r.TeslimKm = teslimKm;
        r.TeslimYakit = teslimYakit;
        r.IslemYapanId = personelId;
        arac.Durum = AracDurumu.Kirada;
        arac.Kilometre = teslimKm;
        await _vt.SaveChangesAsync();
    }

    /// <summary>Araç geri geldi. Gecikme, yakıt ve hasar ücretleri otomatik eklenir.</summary>
    public async Task<Rezervasyon> IadeAlAsync(int id, string personelId, int iadeKm, YakitSeviyesi iadeYakit,
        DateTime iadeTarihi, string? hasarAciklamasi, decimal hasarTutari, string? not)
    {
        var r = await GetirAsync(id);
        GecisiKontrolEt(r, RezervasyonDurumu.Tamamlandi);
        if (iadeKm < (r.TeslimKm ?? 0)) throw new IsKuraliHatasi($"İade kilometresi teslim kilometresinden ({Bicim.Sayi(r.TeslimKm ?? 0)}) küçük olamaz.");
        if (hasarTutari < 0) throw new IsKuraliHatasi("Hasar tutarı negatif olamaz.");

        r.Durum = RezervasyonDurumu.Tamamlandi;
        r.IadeTarihi = iadeTarihi;
        r.IadeKm = iadeKm;
        r.IadeYakit = iadeYakit;
        r.Gecikti = false;
        r.IslemYapanId = personelId;

        r.GecikmeUcreti = _fiyat.GecikmeUcreti(r.GunlukFiyat, r.BitisTarihi, iadeTarihi);
        r.YakitUcreti = _fiyat.YakitUcreti(r.TeslimYakit ?? YakitSeviyesi.Dolu, iadeYakit);
        r.HasarUcreti = hasarTutari;
        if (hasarTutari > 0 || !string.IsNullOrWhiteSpace(hasarAciklamasi))
            r.Hasarlar.Add(new Hasar { Aciklama = string.IsNullOrWhiteSpace(hasarAciklamasi) ? "Hasar" : hasarAciklamasi.Trim(), Tutar = hasarTutari });
        if (!string.IsNullOrWhiteSpace(not)) r.Not = string.IsNullOrWhiteSpace(r.Not) ? not : r.Not + "\n" + not;
        ToplamiHesapla(r);

        // Araç iade şubesinde, yeni kilometresiyle tekrar müsait.
        r.Arac!.Durum = AracDurumu.Musait;
        r.Arac.Kilometre = iadeKm;
        r.Arac.SubeId = r.IadeSubeId;

        await _vt.SaveChangesAsync();
        return r;
    }

    public async Task OdemeEkleAsync(int id, string personelId, decimal tutar, OdemeYontemi yontem, OdemeTuru tur, string? not)
    {
        if (tutar <= 0) throw new IsKuraliHatasi("Ödeme tutarı sıfırdan büyük olmalıdır.");
        var r = await GetirAsync(id);
        if (r.Durum == RezervasyonDurumu.Reddedildi) throw new IsKuraliHatasi("Reddedilmiş rezervasyona ödeme eklenemez.");

        _vt.Odemeler.Add(new Odeme { RezervasyonId = id, Tutar = tutar, Yontem = yontem, Tur = tur, Not = not, AlanKullaniciId = personelId });
        await _vt.SaveChangesAsync();
    }

    // ===================== YARDIMCI =====================

    private async Task<Rezervasyon> GetirAsync(int id)
    {
        return await _vt.Rezervasyonlar.Include(r => r.Arac).Include(r => r.Hasarlar).FirstOrDefaultAsync(r => r.Id == id)
               ?? throw new IsKuraliHatasi("Rezervasyon bulunamadı.");
    }

    /// <summary>"RZ-2026-0001" biçiminde, yıl içinde artan kod.</summary>
    private async Task<string> KodUretAsync()
    {
        var onEk = $"RZ-{DateTime.Now.Year}-";
        var sonKod = await _vt.Rezervasyonlar.Where(r => r.Kod.StartsWith(onEk)).OrderByDescending(r => r.Kod).Select(r => r.Kod).FirstOrDefaultAsync();
        var siradaki = sonKod == null ? 1 : int.Parse(sonKod[onEk.Length..]) + 1;
        return onEk + siradaki.ToString("D4");
    }
}
