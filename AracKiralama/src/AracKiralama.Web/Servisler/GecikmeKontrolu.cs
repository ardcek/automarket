// ============================================================================
// GECİKME KONTROLÜ (arka planda sürekli çalışan servis)
// Uygulama açılınca ve sonra her saat başı, iade tarihi geçtiği halde hâlâ
// müşteride olan kiralamaları "Gecikti" olarak işaretler.
// Gösterge panelindeki kırmızı uyarı ve takvimdeki kırmızı bloklar buradan beslenir.
// ============================================================================
using AracKiralama.Modeller;
using AracKiralama.Veri;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Servisler;

public class GecikmeKontrolu : BackgroundService
{
    private readonly IServiceScopeFactory _kapsamOlusturucu;
    private readonly ILogger<GecikmeKontrolu> _log;

    public GecikmeKontrolu(IServiceScopeFactory kapsamOlusturucu, ILogger<GecikmeKontrolu> log)
    {
        _kapsamOlusturucu = kapsamOlusturucu;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        using var saatlik = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                // Bu servis tek örnek çalışır, veritabanı bağlantısı için kısa bir kapsam açıyoruz.
                using var kapsam = _kapsamOlusturucu.CreateScope();
                var vt = kapsam.ServiceProvider.GetRequiredService<VeriTabani>();
                var simdi = DateTime.Now;

                var sayi = await vt.Rezervasyonlar
                    .Where(r => r.Durum == RezervasyonDurumu.Kirada && !r.Gecikti && r.BitisTarihi < simdi)
                    .ExecuteUpdateAsync(x => x.SetProperty(r => r.Gecikti, true), durdur);
                if (sayi > 0) _log.LogInformation("{Sayi} kiralama gecikti olarak işaretlendi.", sayi);
            }
            catch (Exception hata) when (hata is not OperationCanceledException)
            {
                _log.LogError(hata, "Gecikme kontrolünde hata.");
            }
        } while (await saatlik.WaitForNextTickAsync(durdur));
    }
}
