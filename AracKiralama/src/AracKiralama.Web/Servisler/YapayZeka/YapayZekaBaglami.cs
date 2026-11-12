// ============================================================================
// YAPAY ZEKÂ BAĞLAMI: modele her soruda başta gönderilen "sistem mesajı".
// İçinde: modelin rolü, kurallar, yapabileceği işlemler (PanelIslemleri.Hepsi),
// SİTENİN ŞU ANKİ DURUMU (kategoriler, kampanyalar, ayarlar, yazılar...) ve örnekler.
// Model siteyi göremez; ne biliyorsa bu metinden bilir. Bu yüzden kısa ve düzenli tutulur
// (küçük modellerin okuyabileceği uzunlukta: yaklaşık 4-6 bin token).
// ============================================================================
using System.Reflection;
using System.Text;
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

public class YapayZekaBaglami
{
    private readonly VeriTabani _vt;

    public YapayZekaBaglami(VeriTabani vt)
    {
        _vt = vt;
    }

    /// <summary>Panel modu için sistem mesajı.</summary>
    public async Task<string> PanelSistemiAsync()
    {
        var m = new StringBuilder();
        m.AppendLine("""
            Sen "Rota Rent a Car" araç kiralama sitesinin yönetim panelindeki yardımcı asistansın.
            Kullanıcı (sitenin yöneticisi) Türkçe bir değişiklik ister. Sen bu isteği aşağıdaki İŞLEMLER listesinden işlemlere çevirip JSON yazarsın.
            Kod yazmazsın; sadece listedeki işlemleri kullanırsın.

            KURALLAR:
            - "anladigim": isteği 1-2 cümleyle Türkçe özetle. Kullanıcı bir sayı vermediyse makul bir değer seç ve bunu burada söyle.
            - Bir istek birden fazla işlem gerektirebilir; hepsini sırayla yaz (örn. önce müşteri bilgisi ekle, sonra onu kullanan kampanyaları ekle).
            - Var olan şeyleri SİTENİN DURUMU bölümündeki adlarla aynen yaz. Zaten var olan bir şeyi yeniden ekleme.
            - Müşteri grubuna (meslek, öğrenci, yaş grubu...) indirim = musteri_bilgisi_ekle + her grup için ayrı kampanya_ekle.
            - Araç kategorisi, marka, gün sayısı ya da tarih koşulu için müşteri bilgisi EKLEME; kampanya_ekle'nin kategori / marka / en_az_gun / baslangic / bitis alanlarını kullan.
            - Var olan bir şeyi değiştirmek için "_guncelle" işlemlerini kullan (kampanya_guncelle, ekstra_guncelle); aynı adla yeniden ekleme.
            - Ana sayfadaki büyük başlığın tamamını değiştirmek için ana_baslik_degistir kullan (parçalara kendisi böler).
            - Kullanıcı "filtrede görünsün / indirimli araçlar" derse ayrıca ayar_degistir ile FiltreIndirimli = true yap.
            - Yüzdeler sayı olarak yazılır: %15 → 15.
            - Kullanıcı soru soruyorsa cevap_ver kullan. Listedeki işlemlerle yapılamıyorsa yapilamaz kullan ve sebebini yaz.
            - Kullanıcının istemediği hiçbir şeyi değiştirme.
            """);

        // ---- İşlemler (her işlemin açıklaması ve parametreleri) ----
        m.AppendLine("İŞLEMLER:");
        foreach (var islem in PanelIslemleri.Hepsi)
        {
            var (ozellikler, zorunlular) = islem.Parametreler();
            var parametreler = string.Join(", ", ozellikler.Select(o => zorunlular.Contains(o.Key) ? o.Key : o.Key + "?"));
            m.AppendLine($"- {islem.Tur} ({parametreler}): {islem.Aciklama}");
        }

        // ---- Sitenin durumu ----
        m.AppendLine();
        m.AppendLine("SİTENİN DURUMU:");
        m.AppendLine("Kategoriler: " + Liste(await _vt.Kategoriler.OrderBy(k => k.Sira).Select(k => k.Ad).ToListAsync()));
        m.AppendLine("Markalar: " + Liste(await _vt.Markalar.OrderBy(k => k.Ad).Select(k => k.Ad).ToListAsync()));
        m.AppendLine($"Araç sayısı: {await _vt.Araclar.CountAsync(a => a.Durum != AracDurumu.Pasif)}");
        m.AppendLine("Araç özellikleri: " + Liste((await _vt.AracOzellikleri.AsNoTracking().ToListAsync()).Select(o => $"{o.Ad} ({o.Tur.Ad()}{(o.Tur == OzellikTuru.Secim ? ": " + string.Join("|", o.SecenekListesi) : "")})")));
        m.AppendLine("Müşteri bilgileri: " + Liste((await _vt.MusteriAlanlari.AsNoTracking().ToListAsync()).Select(a => $"{a.Ad} ({a.Tur.Ad()}{(a.Tur == OzellikTuru.Secim ? ": " + string.Join("|", a.SecenekListesi) : "")})")));
        var kampanyalar = await _vt.Kampanyalar.Include(k => k.MusteriAlani).Include(k => k.Kategori).Include(k => k.Marka).Include(k => k.AracOzelligi).AsNoTracking().ToListAsync();
        m.AppendLine("Kampanyalar: " + Liste(kampanyalar.Select(k => $"{k.Ad} ({k.OranYazisi}, {k.KosulOzeti()}{(k.Aktif ? "" : ", kapalı")})")));
        m.AppendLine("Ek hizmetler: " + Liste((await _vt.Ekstralar.AsNoTracking().ToListAsync()).Select(e => $"{e.Ad} ({Bicim.Para(e.GunlukFiyat)}/gün{(e.Aktif ? "" : ", kapalı")})")));
        m.AppendLine("Şubeler: " + Liste(await _vt.Subeler.Select(s => s.Ad).ToListAsync()));
        m.AppendLine("Sayfalar: " + Liste((await _vt.Sayfalar.AsNoTracking().ToListAsync()).Select(s => $"{s.Adres} \"{s.Baslik}\"{(s.Aktif ? "" : " (kapalı)")}")));

        // ---- Ayarlar: ad = değer · grup / açıklama ----
        var ayarlar = await _vt.SiteAyarlari.AsNoTracking().FirstAsync();
        m.AppendLine();
        m.AppendLine("AYARLAR (ad = şu anki değer · ne işe yarar):");
        foreach (var ozellik in AyarDegistir.Ayarlar())
        {
            var bilgi = ozellik.GetCustomAttribute<AyarAttribute>()!;
            m.AppendLine($"{ozellik.Name} = {AyarCevirici.Yazi(ozellik.GetValue(ayarlar))} · {bilgi.Grup} / {bilgi.Ad}{(bilgi.Aciklama != null ? " (" + bilgi.Aciklama + ")" : "")}");
        }

        // ---- Yazılar: anahtar = "şu anki yazı" (sayfadaki yeri) ----
        var degisenler = await _vt.Metinler.AsNoTracking().ToDictionaryAsync(x => x.Anahtar, x => x.Deger);
        m.AppendLine();
        m.AppendLine("YAZILAR (anahtar = \"şu anki yazı\" (sayfadaki yeri)):");
        foreach (var (anahtar, varsayilan) in VarsayilanMetinler.Liste)
        {
            var (bolum, ad) = DuzenleyiciTanimlari.Aciklama(anahtar);
            m.AppendLine($"{anahtar} = \"{Kisalt(degisenler.GetValueOrDefault(anahtar, varsayilan), 50)}\" ({bolum} › {ad})");
        }

        // ---- Örnekler ----
        m.AppendLine("""

            ÖRNEK 1
            İstek: Meslek gruplarına göre indirim yap, filtrede de indirimli araçlar görünsün
            Cevap: {"anladigim":"Müşterilere meslekleri sorulacak; öğrenci, öğretmen ve sağlık çalışanlarına indirim yapılacak ve araç listesinde indirim filtresi açılacak. Oranları ben seçtim: %10, %15, %12.","islemler":[{"tur":"musteri_bilgisi_ekle","ad":"Meslek","bilgi_turu":"secim","secenekler":["Öğrenci","Öğretmen","Sağlık çalışanı","Diğer"],"kayitta_sor":true,"zorunlu":false},{"tur":"kampanya_ekle","ad":"Öğrenci indirimi","indirim_yuzdesi":10,"rozet":"Öğrencilere %10","musteri_bilgisi":"Meslek","musteri_degeri":"Öğrenci"},{"tur":"kampanya_ekle","ad":"Öğretmen indirimi","indirim_yuzdesi":15,"rozet":"Öğretmenlere %15","musteri_bilgisi":"Meslek","musteri_degeri":"Öğretmen"},{"tur":"kampanya_ekle","ad":"Sağlık çalışanı indirimi","indirim_yuzdesi":12,"rozet":"Sağlıkçılara %12","musteri_bilgisi":"Meslek","musteri_degeri":"Sağlık çalışanı"},{"tur":"ayar_degistir","ayar":"FiltreIndirimli","deger":"true"}]}

            ÖRNEK 2
            İstek: Ana sayfadaki büyük başlık "Hayalindeki araç burada" olsun, butonlar turuncu olsun
            Cevap: {"anladigim":"Ana sayfanın büyük başlığı değişecek (\"araç\" renkli olacak) ve ana renk turuncu yapılacak.","islemler":[{"tur":"ana_baslik_degistir","metin":"Hayalindeki araç burada","renkli_kisim":"araç"},{"tur":"ayar_degistir","ayar":"AnaRenk","deger":"#f97316"}]}

            ÖRNEK 3
            İstek: Lüks araçlarda 5 gün ve üzeri kiralamalara %8 indirim yap
            Cevap: {"anladigim":"Lüks kategorisinde 5 gün ve üzeri kiralamalara %8 indirim yapılacak.","islemler":[{"tur":"kampanya_ekle","ad":"Lüks uzun kiralama indirimi","indirim_yuzdesi":8,"rozet":"5+ gün %8","kategori":"Lüks","en_az_gun":5}]}

            ÖRNEK 4
            İstek: Araçlara klima bilgisi ekle, bütün araçlarda olsun, filtrede seçilebilsin
            Cevap: {"anladigim":"Araçlara Var/Yok türünde Klima özelliği eklenecek, bütün araçlara Var verilecek ve filtrede görünecek.","islemler":[{"tur":"arac_ozelligi_ekle","ad":"Klima","ozellik_turu":"evet_hayir","filtrede_goster":true,"kartta_goster":false,"ikon":"bi-snow"},{"tur":"arac_ozellik_degeri_ver","ozellik":"Klima","deger":"true"}]}
            """);
        return m.ToString();
    }

    private static string Liste(IEnumerable<string> ogeler)
    {
        var liste = ogeler.ToList();
        return liste.Count == 0 ? "(yok)" : string.Join(", ", liste);
    }

    private static string Kisalt(string metin, int uzunluk)
    {
        metin = metin.Replace("\r", " ").Replace("\n", " ");
        return metin.Length <= uzunluk ? metin : metin[..uzunluk] + "…";
    }
}
