// ============================================================================
// YAPAY ZEKÂ SUNUCUSU: bilgisayardaki yapay zekâ motorunu (llama.cpp → llama-server.exe)
// açar, kapatır ve durumunu söyler. İnternet gerekmez; model dosyası bilgisayardadır.
//
// Klasör düzeni (AracKiralama/yapay-zeka/, HAZIRLA.ps1 ile bir kere indirilir):
//   motor/vulkan/llama-server.exe   → ekran kartı varsa onu kullanan motor (yoksa işlemciyle çalışır)
//   motor/cpu/llama-server.exe      → sadece işlemci (Vulkan motoru açılmazsa bu denenir)
//   modeller/*.gguf                 → model dosyaları (küçük ve orta boy)
//
// Model seçimi: appsettings.json → YapayZeka:Model boşsa, bilgisayarın RAM'i 16 GB ve üzeriyse
// en büyük model, değilse en küçük model seçilir. Panelden de değiştirilebilir.
//
// Uygulama motoru kendi başlatır (Yapay Zekâ sayfası açılınca) ve uygulama kapanınca kapatır.
// Render gibi sunucularda motor dosyaları olmadığı için Durum = "Yok" olur; site etkilenmez.
// ============================================================================
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace AracKiralama.Servisler;

/// <summary>appsettings.json → "YapayZeka" bölümü.</summary>
public class YapayZekaAyarlari
{
    public string Klasor { get; set; } = "../../yapay-zeka";   // uygulama klasörüne göre
    public string? Adres { get; set; }                          // hazır çalışan bir sunucu (Ollama, LM Studio...) varsa adresi
    public string? Model { get; set; }                          // modeller klasöründeki dosya adı; boşsa otomatik
    public int Port { get; set; } = 8089;
    public int BaglamUzunlugu { get; set; } = 16384;            // modelin aynı anda okuyabileceği metin uzunluğu (token)
    public string? EkParametreler { get; set; }                 // llama-server'a eklenecek parametreler
}

public enum SunucuDurumu
{
    Yok,        // motor ya da model dosyası yok (Render'da, ya da HAZIRLA.ps1 çalıştırılmamış)
    Kapali,     // dosyalar var, motor çalışmıyor
    Aciliyor,   // motor başlatıldı, model belleğe yükleniyor
    Hazir,      // soru sorulabilir
    Hata,       // açılamadı (Mesaj'da sebebi yazar)
}

public class YapayZekaSunucusu : IDisposable
{
    private readonly YapayZekaAyarlari _ayarlar;
    private readonly ILogger<YapayZekaSunucusu> _log;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
    private readonly SemaphoreSlim _kilit = new(1, 1);                 // aynı anda iki kez başlatılmasın
    private readonly ConcurrentQueue<string> _sonSatirlar = new();      // motorun son çıktıları (hata ayıklamak için)
    private Process? _surec;

    public YapayZekaSunucusu(IOptions<YapayZekaAyarlari> ayarlar, IWebHostEnvironment ortam, ILogger<YapayZekaSunucusu> log, IHostApplicationLifetime omur)
    {
        _ayarlar = ayarlar.Value;
        _log = log;
        Klasor = Path.GetFullPath(Path.Combine(ortam.ContentRootPath, _ayarlar.Klasor));
        omur.ApplicationStopping.Register(Durdur);   // uygulama kapanınca motor da kapanır
        Durum = Kurulu ? SunucuDurumu.Kapali : SunucuDurumu.Yok;
        Mesaj = Kurulu ? "Yapay zekâ kapalı. İlk soruda kendiliğinden açılır." : "Bu bilgisayarda yapay zekâ kurulu değil (yapay-zeka/BENI-OKU.md).";
    }

    public string Klasor { get; }
    public SunucuDurumu Durum { get; private set; }
    public string Mesaj { get; private set; }
    public string? ModelAdi { get; private set; }
    public string? Motor { get; private set; }   // "Ekran kartı (Vulkan)", "İşlemci (CPU)", "Dış sunucu"

    /// <summary>Ayarlarda bir adres yazılıysa (Ollama vb.) motor açılmaz, o adres kullanılır.</summary>
    public bool DisSunucu => !string.IsNullOrWhiteSpace(_ayarlar.Adres);
    public string Adres => DisSunucu ? _ayarlar.Adres!.TrimEnd('/') : $"http://127.0.0.1:{_ayarlar.Port}";

    /// <summary>Motor ve en az bir model dosyası var mı?</summary>
    public bool Kurulu => DisSunucu || (Motorlar().Count > 0 && Modeller().Count > 0);

    /// <summary>Bilgisayarın toplam belleği (GB).</summary>
    public static double RamGb => Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024d / 1024 / 1024, 1);

    /// <summary>Modeller klasöründeki .gguf dosyaları, küçükten büyüğe (görüntü eki "mmproj" dosyaları hariç).</summary>
    public List<FileInfo> Modeller()
    {
        var klasor = new DirectoryInfo(Path.Combine(Klasor, "modeller"));
        if (!klasor.Exists) return new();
        return klasor.GetFiles("*.gguf").Where(f => !f.Name.StartsWith("mmproj", StringComparison.OrdinalIgnoreCase)).OrderBy(f => f.Length).ToList();
    }

    /// <summary>Kullanılabilecek motorlar, denenme sırasıyla: önce ekran kartlı (Vulkan), sonra işlemci.</summary>
    public List<(string Ad, string Yol)> Motorlar()
    {
        var liste = new List<(string, string)>();
        foreach (var (klasor, ad) in new[] { ("vulkan", "Ekran kartı (Vulkan)"), ("cuda", "Ekran kartı (CUDA)"), ("cpu", "İşlemci (CPU)") })
        {
            var yol = Path.Combine(Klasor, "motor", klasor, "llama-server.exe");
            if (File.Exists(yol)) liste.Add((ad, yol));
        }
        return liste;
    }

    /// <summary>Otomatik model: ayarda yazılıysa o, değilse RAM 16 GB ve üzeriyse en büyük, değilse en küçük.</summary>
    public FileInfo? OtomatikModel()
    {
        var modeller = Modeller();
        if (modeller.Count == 0) return null;
        var ayardaki = modeller.FirstOrDefault(m => m.Name.Equals(_ayarlar.Model, StringComparison.OrdinalIgnoreCase));
        return ayardaki ?? (RamGb >= 16 ? modeller[^1] : modeller[0]);
    }

    /// <summary>Motorun son çıktı satırları (açılmazsa sebebini görmek için).</summary>
    public string SonCikti => string.Join("\n", _sonSatirlar);

    /// <summary>
    /// Motoru başlatır ve model yüklenene kadar bekler. Zaten açıksa hemen döner.
    /// modelAdi verilirse ve çalışan model farklıysa motor o modelle yeniden açılır.
    /// </summary>
    public async Task BaslatAsync(string? modelAdi = null)
    {
        await _kilit.WaitAsync();
        try
        {
            var istenen = modelAdi == null ? null : Modeller().FirstOrDefault(m => m.Name == modelAdi);
            if (Durum == SunucuDurumu.Hazir && (istenen == null || istenen.Name == ModelAdi) && await SaglikliMiAsync()) return;

            // Dış sunucu ya da önceki çalıştırmadan kalmış bir motor bu adreste zaten çalışıyor mu?
            if ((DisSunucu || _surec == null) && istenen == null && await SaglikliMiAsync())
            {
                ModelAdi = await CalisanModelAsync();
                Motor = DisSunucu ? "Dış sunucu" : "Önceden açılmış motor";
                if (!DisSunucu) KalmisMotorlariBagla();   // artık bu uygulamayla birlikte kapansın
                (Durum, Mesaj) = (SunucuDurumu.Hazir, "Hazır.");
                return;
            }
            if (DisSunucu)
            {
                (Durum, Mesaj) = (SunucuDurumu.Hata, $"{Adres} adresinde yapay zekâ sunucusu yanıt vermiyor. Ollama / LM Studio açık mı?");
                return;
            }

            var model = istenen ?? OtomatikModel();
            var motorlar = Motorlar();
            if (model == null || motorlar.Count == 0)
            {
                (Durum, Mesaj) = (SunucuDurumu.Yok, "Motor ya da model dosyası bulunamadı. yapay-zeka/HAZIRLA.ps1 ile indirin (BENI-OKU.md).");
                return;
            }

            DurdurIcerden();
            foreach (var (motorAdi, yol) in motorlar)
            {
                (Durum, Mesaj, Motor, ModelAdi) = (SunucuDurumu.Aciliyor, $"{model.Name} belleğe yükleniyor ({motorAdi})… İlk açılış 10-60 saniye sürebilir.", motorAdi, model.Name);
                if (await DeneAsync(yol, model.FullName))
                {
                    (Durum, Mesaj) = (SunucuDurumu.Hazir, "Hazır.");
                    _log.LogInformation("Yapay zekâ hazır: {Model} · {Motor}", model.Name, motorAdi);
                    return;
                }
                _log.LogWarning("Yapay zekâ motoru açılamadı ({Motor}). Son çıktı:\n{Cikti}", motorAdi, SonCikti);
                DurdurIcerden();
            }
            (Durum, Mesaj) = (SunucuDurumu.Hata, "Yapay zekâ motoru açılamadı. Son çıktı: " + string.Join(" ", _sonSatirlar.TakeLast(3)));
        }
        finally
        {
            _kilit.Release();
        }
    }

    /// <summary>Motoru verilen modelle açar, en çok 5 dakika hazır olmasını bekler.</summary>
    private async Task<bool> DeneAsync(string motorYolu, string modelYolu)
    {
        _sonSatirlar.Clear();
        var bilgi = new ProcessStartInfo(motorYolu)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(motorYolu)!,
        };
        // llama-server parametreleri: model, bağlam uzunluğu, sadece bu bilgisayardan erişim, port, web arayüzü kapalı
        foreach (var p in new[] { "-m", modelYolu, "-c", _ayarlar.BaglamUzunlugu.ToString(), "--host", "127.0.0.1", "--port", _ayarlar.Port.ToString(), "--no-webui" })
            bilgi.ArgumentList.Add(p);
        foreach (var p in (_ayarlar.EkParametreler ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            bilgi.ArgumentList.Add(p);

        try
        {
            _surec = Process.Start(bilgi)!;
        }
        catch (Exception hata)
        {
            SatirEkle("Başlatılamadı: " + hata.Message);
            return false;
        }
        BirlikteKapat.Ekle(_surec);   // uygulama nasıl kapanırsa kapansın (pencere kapatma, çökme) motor da kapansın
        _surec.OutputDataReceived += (_, e) => SatirEkle(e.Data);
        _surec.ErrorDataReceived += (_, e) => SatirEkle(e.Data);
        _surec.BeginOutputReadLine();
        _surec.BeginErrorReadLine();

        var bitis = DateTime.Now.AddMinutes(5);
        while (DateTime.Now < bitis)
        {
            if (_surec.HasExited) return false;          // çöktü (örn. ekran kartı sürücüsü yok)
            if (await SaglikliMiAsync()) return true;     // /health → 200 {"status":"ok"}
            await Task.Delay(1000);
        }
        return false;
    }

    /// <summary>Motor soru almaya hazır mı? (llama-server: model yüklenirken 503, hazırken 200 döner)</summary>
    public async Task<bool> SaglikliMiAsync()
    {
        try
        {
            using var cevap = await _http.GetAsync(Adres + "/health");
            if (cevap.IsSuccessStatusCode) return true;
            // Ollama'da /health yok; /v1/models cevap veriyorsa hazırdır
            if (DisSunucu) { using var modeller = await _http.GetAsync(Adres + "/v1/models"); return modeller.IsSuccessStatusCode; }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Çalışan sunucudaki modelin adı (/v1/models).</summary>
    private async Task<string?> CalisanModelAsync()
    {
        try
        {
            var json = JsonNode.Parse(await _http.GetStringAsync(Adres + "/v1/models"));
            return Path.GetFileName(json?["data"]?[0]?["id"]?.GetValue<string>());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Motoru kapatır (panelden "Durdur" ya da uygulama kapanırken).</summary>
    public void Durdur()
    {
        DurdurIcerden();
        if (Kurulu) (Durum, Mesaj) = (SunucuDurumu.Kapali, "Yapay zekâ kapatıldı.");
    }

    /// <summary>Önceki bir çalıştırmadan kalan (bu klasördeki) motorları bu uygulamanın iş nesnesine bağlar.</summary>
    private void KalmisMotorlariBagla()
    {
        foreach (var surec in Process.GetProcessesByName("llama-server"))
        {
            try
            {
                if (surec.MainModule?.FileName?.StartsWith(Klasor, StringComparison.OrdinalIgnoreCase) == true) BirlikteKapat.Ekle(surec);
            }
            catch { /* başka kullanıcının süreci olabilir; dokunma */ }
        }
    }

    private void DurdurIcerden()
    {
        try
        {
            if (_surec is { HasExited: false })
            {
                _surec.Kill(entireProcessTree: true);
                _log.LogInformation("Yapay zekâ motoru kapatıldı.");
            }
        }
        catch (Exception hata)
        {
            _log.LogWarning("Yapay zekâ motoru kapatılamadı: {Hata}", hata.Message);
        }
        _surec?.Dispose();
        _surec = null;
    }

    private void SatirEkle(string? satir)
    {
        if (string.IsNullOrWhiteSpace(satir)) return;
        _sonSatirlar.Enqueue(satir);
        while (_sonSatirlar.Count > 40) _sonSatirlar.TryDequeue(out _);
    }

    public void Dispose()
    {
        DurdurIcerden();
        _http.Dispose();
    }
}

/// <summary>
/// Windows "iş nesnesi" (Job Object): içine konan süreçler, uygulama kapanınca (normal kapanma, pencereyi X ile
/// kapatma, çökme — hangisi olursa) Windows tarafından otomatik kapatılır. Böylece laboratuvarda pencere kapatılınca
/// yapay zekâ motoru arka planda belleği doldurmaya devam etmez. Windows dışında bir şey yapmaz.
/// </summary>
internal static class BirlikteKapat
{
    private const int GenisletilmisSinirBilgisi = 9;                // JobObjectExtendedLimitInformation
    private const uint IsKapanincaHepsiniKapat = 0x2000;            // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
    private static IntPtr _isNesnesi = IntPtr.Zero;
    private static readonly object _kilit = new();

    public static void Ekle(Process surec)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            lock (_kilit)
            {
                if (_isNesnesi == IntPtr.Zero)
                {
                    _isNesnesi = CreateJobObject(IntPtr.Zero, null);
                    var bilgi = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION { BasicLimitInformation = { LimitFlags = IsKapanincaHepsiniKapat } };
                    var boyut = Marshal.SizeOf(bilgi);
                    var bellek = Marshal.AllocHGlobal(boyut);
                    try
                    {
                        Marshal.StructureToPtr(bilgi, bellek, false);
                        SetInformationJobObject(_isNesnesi, GenisletilmisSinirBilgisi, bellek, (uint)boyut);
                    }
                    finally { Marshal.FreeHGlobal(bellek); }
                }
                AssignProcessToJobObject(_isNesnesi, surec.Handle);
            }
        }
        catch { /* olmazsa uygulama kapanırken Durdur() yine motoru kapatmayı dener */ }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateJobObject(IntPtr guvenlik, string? ad);
    [DllImport("kernel32.dll")] private static extern bool SetInformationJobObject(IntPtr isNesnesi, int bilgiTuru, IntPtr bilgi, uint boyut);
    [DllImport("kernel32.dll")] private static extern bool AssignProcessToJobObject(IntPtr isNesnesi, IntPtr surec);

    // Windows'un beklediği yapılar (alan adları Windows belgeleriyle aynı bırakıldı)
    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass, SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }
}
