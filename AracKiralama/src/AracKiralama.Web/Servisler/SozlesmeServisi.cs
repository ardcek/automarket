// ============================================================================
// SÖZLEŞME SERVİSİ: kiralama sözleşmesini PDF olarak üretir (QuestPDF kütüphanesi).
// Sayfa düzeni C# ile tarif edilir: Üst bilgi → İçerik → Alt bilgi.
// Firma adı, adres ve fiyat maddeleri site ayarlarından gelir.
// Sözleşme maddelerini değiştirmek için aşağıdaki "maddeler" listesini düzenleyin.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Veri;
using AracKiralama.Yardimcilar;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AracKiralama.Servisler;

public class SozlesmeServisi
{
    private const string Gri = "#6B7280";
    private const string Cizgi = "#E5E7EB";
    private readonly VeriTabani _vt;
    private readonly SiteAyarlari _ayarlar;

    public SozlesmeServisi(VeriTabani vt, SiteAyarlari ayarlar)
    {
        _vt = vt;
        _ayarlar = ayarlar;
    }

    /// <summary>PDF dosyasının baytlarını döndürür; rezervasyon yoksa null.</summary>
    public async Task<byte[]?> OlusturAsync(int rezervasyonId)
    {
        var r = await _vt.Rezervasyonlar
            .Include(x => x.Musteri).Include(x => x.Arac).ThenInclude(a => a!.Marka)
            .Include(x => x.Arac).ThenInclude(a => a!.YakitTuru).Include(x => x.Arac).ThenInclude(a => a!.VitesTuru)
            .Include(x => x.AlisSube).Include(x => x.IadeSube)
            .Include(x => x.Ekstralar).ThenInclude(e => e.Ekstra).Include(x => x.Hasarlar)
            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == rezervasyonId);
        if (r == null) return null;

        var a = _ayarlar;
        var renk = a.AnaRenk;
        string[] maddeler =
        [
            "Araç, teslim alındığı yakıt seviyesiyle iade edilmelidir; eksik her çeyrek depo için " + Bicim.Para(a.YakitCeyrekUcreti) + " alınır.",
            $"Planlanan iade saatinden {a.GecikmeToleransi} dakika sonra başlayan her gün için günlük ücretin {a.GecikmeCarpani.ToString("0.#", Bicim.Tr)} katı geç iade bedeli uygulanır.",
            "Kiracı, trafik cezalarından ve kira süresince oluşan hasarlardan (kasko kapsamı dışındakiler) sorumludur.",
            "Araç, sözleşmede adı geçmeyen kişilerce kullanılamaz ve yurt dışına çıkarılamaz.",
            "Depozito, araç hasarsız ve eksiksiz iade edildiğinde kiracıya geri ödenir.",
        ];

        return Document.Create(belge => belge.Page(sayfa =>
        {
            sayfa.Size(PageSizes.A4);
            sayfa.Margin(36);
            sayfa.DefaultTextStyle(t => t.FontSize(9.5f));

            // ---- Üst bilgi: firma solda, sözleşme kodu sağda ----
            sayfa.Header().Row(satir =>
            {
                satir.RelativeItem().Column(s =>
                {
                    s.Item().Text(a.FirmaAdi).FontSize(22).Bold().FontColor(renk);
                    s.Item().Text(a.Adres).FontColor(Gri);
                    s.Item().Text($"{a.Telefon} · {a.Eposta}").FontColor(Gri);
                });
                satir.ConstantItem(170).AlignRight().Column(s =>
                {
                    s.Item().AlignRight().Text("ARAÇ KİRALAMA SÖZLEŞMESİ").Bold();
                    s.Item().AlignRight().Text(r.Kod).FontSize(16).Bold().FontColor(renk);
                    s.Item().AlignRight().Text($"Durum: {r.Durum.Ad()}").FontColor(Gri);
                });
            });

            // ---- İçerik ----
            sayfa.Content().PaddingTop(16).Column(icerik =>
            {
                icerik.Spacing(14);

                icerik.Item().Row(satir =>
                {
                    satir.Spacing(14);
                    satir.RelativeItem().Element(Kutu).Column(s =>
                    {
                        Baslik(s, "KİRACI", renk);
                        Alan(s, "Ad Soyad", r.Musteri!.TamAd);
                        Alan(s, "T.C. Kimlik No", r.Musteri.TcNo ?? "-");
                        Alan(s, "Telefon", r.Musteri.PhoneNumber ?? "-");
                        Alan(s, "Ehliyet No", r.Musteri.EhliyetNo ?? "-");
                    });
                    satir.RelativeItem().Element(Kutu).Column(s =>
                    {
                        Baslik(s, "ARAÇ", renk);
                        Alan(s, "Araç", $"{r.Arac!.TamAd} ({r.Arac.Yil})");
                        Alan(s, "Plaka", r.Arac.Plaka);
                        Alan(s, "Yakıt / Vites", $"{r.Arac.YakitTuru?.Ad} / {r.Arac.VitesTuru?.Ad}");
                        Alan(s, "Teslim / İade Km", $"{r.TeslimKm?.ToString() ?? "-"} / {r.IadeKm?.ToString() ?? "-"}");
                    });
                });

                icerik.Item().Element(Kutu).Row(satir =>
                {
                    satir.RelativeItem().Column(s => { Baslik(s, "ALIŞ", renk); s.Item().Text(r.AlisSube!.Ad).Bold(); s.Item().Text(Bicim.TarihSaat(r.BaslangicTarihi)); });
                    satir.RelativeItem().Column(s => { Baslik(s, "İADE", renk); s.Item().Text(r.IadeSube!.Ad).Bold(); s.Item().Text(Bicim.TarihSaat(r.BitisTarihi)); });
                });

                icerik.Item().Element(Kutu).Column(s =>
                {
                    Baslik(s, "ÜCRET DÖKÜMÜ", renk);
                    UcretSatiri(s, $"Kira bedeli ({r.GunSayisi} gün × {Bicim.Para(r.GunlukFiyat)})", r.KiraTutari);
                    if (r.Indirim > 0) UcretSatiri(s, "Uzun dönem indirimi", -r.Indirim);
                    foreach (var e in r.Ekstralar) UcretSatiri(s, e.Ekstra!.Ad, e.Tutar);
                    if (r.TekYonUcreti > 0) UcretSatiri(s, "Farklı şubeye iade", r.TekYonUcreti);
                    if (r.GecikmeUcreti > 0) UcretSatiri(s, "Geç iade ücreti", r.GecikmeUcreti);
                    if (r.YakitUcreti > 0) UcretSatiri(s, "Eksik yakıt ücreti", r.YakitUcreti);
                    if (r.HasarUcreti > 0) UcretSatiri(s, "Hasar bedeli", r.HasarUcreti);
                    s.Item().PaddingTop(6).Row(t =>
                    {
                        t.RelativeItem().Text("GENEL TOPLAM").Bold();
                        t.ConstantItem(120).AlignRight().Text(Bicim.Para(r.ToplamTutar)).Bold().FontSize(12).FontColor(renk);
                    });
                    s.Item().Text($"Depozito (iadede geri ödenir): {Bicim.Para(r.Depozito)}").FontColor(Gri);
                });

                icerik.Item().Column(s =>
                {
                    Baslik(s, "GENEL ŞARTLAR", renk);
                    for (var i = 0; i < maddeler.Length; i++)
                        s.Item().PaddingBottom(2).Text($"{i + 1}. {maddeler[i]}").FontColor(Gri);
                });

                icerik.Item().PaddingTop(24).Row(satir =>
                {
                    satir.Spacing(40);
                    satir.RelativeItem().Column(s => Imza(s, "KİRAYA VEREN", a.FirmaAdi));
                    satir.RelativeItem().Column(s => Imza(s, "KİRACI", r.Musteri!.TamAd));
                });
            });

            sayfa.Footer().AlignCenter().Text($"{a.FirmaAdi} · {Bicim.TarihSaat(DateTime.Now)} tarihinde sistem tarafından üretilmiştir").FontSize(8).FontColor(Gri);
        })).GeneratePdf();
    }

    // ---- Tekrar eden küçük çizim parçaları ----
    private static IContainer Kutu(IContainer c) => c.Border(0.75f).BorderColor(Cizgi).Padding(10);
    private static void Baslik(ColumnDescriptor s, string metin, string renk) => s.Item().PaddingBottom(4).Text(metin).FontSize(8).Bold().FontColor(renk);
    private static void Alan(ColumnDescriptor s, string etiket, string deger) => s.Item().Row(r => { r.ConstantItem(90).Text(etiket).FontColor(Gri); r.RelativeItem().Text(deger); });
    private static void UcretSatiri(ColumnDescriptor s, string ad, decimal tutar) => s.Item().BorderBottom(0.5f).BorderColor(Cizgi).PaddingVertical(4).Row(r => { r.RelativeItem().Text(ad); r.ConstantItem(120).AlignRight().Text(Bicim.Para(tutar)); });
    private static void Imza(ColumnDescriptor s, string baslik, string ad)
    {
        s.Item().Height(40);
        s.Item().BorderTop(0.75f).PaddingTop(4).Text(baslik).Bold().FontSize(8);
        s.Item().Text(ad).FontColor(Gri);
    }
}
