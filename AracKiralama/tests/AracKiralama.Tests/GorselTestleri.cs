// ============================================================================
// GÖRSEL TESTLERİ: fotoğrafın genişliği dosya başlığından doğru okunuyor mu?
// (Gorseller.cs srcset'e özgün fotoğrafın gerçek genişliğini yazar; yanlış okunursa tarayıcı yanlış dosyayı seçer.)
// ============================================================================
using AracKiralama.Yardimcilar;

namespace AracKiralama.Testler;

public class GorselTestleri : IDisposable
{
    private readonly string _klasor = Directory.CreateTempSubdirectory("gorsel-testi-").FullName;

    public void Dispose() => Directory.Delete(_klasor, true);

    private string Yaz(string ad, byte[] icerik)
    {
        var yol = Path.Combine(_klasor, ad);
        File.WriteAllBytes(yol, icerik);
        return yol;
    }

    [Fact]
    public void Jpeg_genisligi_SOF_bolumunden_okunur()
    {
        byte[] jpeg =
        [
            0xFF, 0xD8,                                                       // dosya başı
            0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,   // APP0 (atlanır)
            0xFF, 0xC0, 0x00, 0x11, 0x08, 0x01, 0xF4, 0x03, 0xC0,             // SOF0: yükseklik 500, genişlik 960
            0x03, 0x01, 0x22, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01,
            0xFF, 0xD9,
        ];
        Assert.Equal(960, Gorseller.GenislikOku(Yaz("araba.jpg", jpeg)));
    }

    [Fact]
    public void Png_genisligi_IHDR_bolumunden_okunur()
    {
        byte[] png =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,                   // imza
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,                   // IHDR
            0x00, 0x00, 0x06, 0x40, 0x00, 0x00, 0x03, 0x20,                   // 1600 x 800
            0x08, 0x02, 0x00, 0x00, 0x00,
        ];
        Assert.Equal(1600, Gorseller.GenislikOku(Yaz("logo.png", png)));
    }

    [Fact]
    public void Tanimadigi_ya_da_olmayan_dosyada_null_doner()
    {
        Assert.Null(Gorseller.GenislikOku(Yaz("bozuk.jpg", [0x00, 0x01, 0x02, 0x03, 0x04])));
        Assert.Null(Gorseller.GenislikOku(Path.Combine(_klasor, "yok.jpg")));
    }
}
