# ============================================================================
# HAZIRLA.ps1 — Yapay Zekâ Asistanı için gerekenleri İNTERNETLİ bir bilgisayarda BİR KERE indirir.
# Sonra bu klasör (projeyle birlikte) USB'ye kopyalanır; laboratuvarda internet gerekmez.
#
# Çalıştırmak için: HAZIRLA.bat dosyasına çift tıklayın (ya da PowerShell'de: .\HAZIRLA.ps1)
#
# İndirilenler (hepsi bu klasörün içine):
#   motor\vulkan, motor\cpu  → llama.cpp (llama-server.exe), ~65 MB. Kurulum istemez.
#   modeller\                → küçük model (~2,7 GB) ve orta model (~5,7 GB)
#   araclar\dotnet           → kurulumsuz .NET 10 SDK (laboratuvarda .NET yoksa kullanılır), ~300 MB
#   araclar\nuget            → projenin NuGet paketleri (internetsiz derleme için), ~300 MB
#
# Seçenekler:
#   .\HAZIRLA.ps1 -SadeceKucuk   → orta modeli indirmez (zayıf bilgisayar / küçük USB)
#   .\HAZIRLA.ps1 -DotnetYok     → taşınabilir .NET'i indirmez (laboratuvarda .NET 10 SDK kuruluysa)
#   .\HAZIRLA.ps1 -Hedef E:\automarket\AracKiralama\yapay-zeka
#                                → başka bir klasöre indirir (örn. doğrudan USB'ye). Proje OneDrive / Masaüstü
#                                  klasöründeyse önerilir: yoksa OneDrive 9 GB'ı buluta yüklemeye çalışır.
#                                  Uygulamayı o klasöre yönlendirmek: appsettings.json → YapayZeka → Klasor
# Yarım kalan indirmeler kaldığı yerden devam eder; betiği tekrar çalıştırmak güvenlidir.
# ============================================================================
param(
    [switch]$SadeceKucuk,
    [switch]$DotnetYok,
    [string]$Hedef
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$proje = Split-Path $PSScriptRoot -Parent                # ...\AracKiralama (paketleri indirilecek proje)
$kok = $PSScriptRoot                                     # indirilenlerin klasörü: ...\AracKiralama\yapay-zeka
if ($Hedef) { New-Item -ItemType Directory -Force $Hedef | Out-Null; $kok = (Resolve-Path $Hedef).Path }
Write-Host "İndirilecek klasör: $kok"
if ($kok -like "*OneDrive*") { Write-Warning "Bu klasör OneDrive içinde: model dosyaları buluta yüklenmeye çalışılabilir. Başka bir yer için -Hedef kullanın." }

# ---- Modeller: değiştirmek isterseniz sadece bu iki satırı düzenleyin (Hugging Face deposu ve dosya adı) ----
$modeller = @(
    @{ Ad = "Küçük model (panel modu için hızlı)";   Depo = "unsloth/Qwen3.5-4B-GGUF"; Dosya = "Qwen3.5-4B-Q4_K_M.gguf" },
    @{ Ad = "Orta model (kod modu için daha iyi)";   Depo = "unsloth/Qwen3.5-9B-GGUF"; Dosya = "Qwen3.5-9B-Q4_K_M.gguf" }
)
if ($SadeceKucuk) { $modeller = @($modeller[0]) }

function Baslik($metin) { Write-Host ""; Write-Host "==== $metin ====" -ForegroundColor Cyan }

# curl.exe Windows 10/11 ile gelir; -C - → yarım kalan indirmeye kaldığı yerden devam eder.
# Önce "dosya.part" adıyla iner, bitince asıl adını alır: yarım kalmış bir model asla "hazır" sanılmaz.
function Indir($adres, $hedef) {
    if (Test-Path $hedef) { Write-Host "  zaten var: $(Split-Path $hedef -Leaf)"; return }
    New-Item -ItemType Directory -Force (Split-Path $hedef -Parent) | Out-Null
    & curl.exe -L --fail --retry 3 -C - -o "$hedef.part" $adres
    if ($LASTEXITCODE -ne 0) { throw "İndirilemedi: $adres (tekrar çalıştırınca kaldığı yerden devam eder)" }
    Move-Item "$hedef.part" $hedef -Force
}

# ---- 0) Disk kontrolü: FAT32 biçimli USB 4 GB'tan büyük dosya alamaz ----
Baslik "Disk kontrolü"
$surucu = (Get-Item $kok).PSDrive.Name
$birim = $null
try { $birim = Get-Volume -DriveLetter $surucu -ErrorAction Stop } catch { Write-Host "Disk bilgisi okunamadı, devam ediliyor." }
if ($birim) {
    Write-Host ("Sürücü {0}: {1}, boş alan {2:N1} GB" -f $surucu, $birim.FileSystem, ($birim.SizeRemaining / 1GB))
    if ($birim.FileSystem -eq "FAT32" -and -not $SadeceKucuk) {
        throw "Bu sürücü FAT32 biçiminde ve 4 GB'tan büyük dosyaları alamaz. USB'yi exFAT ya da NTFS olarak biçimlendirin ya da -SadeceKucuk ile çalıştırın."
    }
    if ($birim.SizeRemaining -lt 12GB) { Write-Warning "Boş alan 12 GB'tan az; indirmeler yarıda kalabilir." }
}

# ---- 1) Motor: llama.cpp'nin en son Windows sürümü (GitHub) ----
Baslik "1/4 Yapay zekâ motoru (llama.cpp)"
# İkisi de varsa GitHub'a hiç sorulmaz (surum.txt elde olan sürümü göstermeye devam eder)
$eksikMotorlar = @("vulkan", "cpu") | Where-Object { -not (Test-Path (Join-Path $kok "motor\$_\llama-server.exe")) }
if (-not $eksikMotorlar) {
    Write-Host "Zaten var (sürüm: $(Get-Content (Join-Path $kok 'motor\surum.txt') -ErrorAction SilentlyContinue))."
} else {
    $surumler = Invoke-RestMethod "https://api.github.com/repos/ggml-org/llama.cpp/releases?per_page=10" -Headers @{ "User-Agent" = "rota-arac-kiralama" }
    $surum = $surumler | Where-Object { $_.tag_name -match '^b\d+$' -and ($_.assets | Where-Object { $_.name -like "*-bin-win-cpu-x64.zip" }) } | Select-Object -First 1
    if (-not $surum) { throw "llama.cpp sürümü bulunamadı." }
    Write-Host "Sürüm: $($surum.tag_name)"
    foreach ($tur in $eksikMotorlar) {
        $varlik = $surum.assets | Where-Object { $_.name -eq "llama-$($surum.tag_name)-bin-win-$tur-x64.zip" } | Select-Object -First 1
        if (-not $varlik) { Write-Warning "$tur paketi bulunamadı, atlanıyor."; continue }
        $klasor = Join-Path $kok "motor\$tur"
        $zip = Join-Path $kok "motor\$($varlik.name)"
        Indir $varlik.browser_download_url $zip
        Expand-Archive -Path $zip -DestinationPath $klasor -Force
        # Bazı paketlerde dosyalar alt klasörde gelir; llama-server.exe'yi bulup klasörün köküne taşı
        $exe = Get-ChildItem $klasor -Recurse -Filter "llama-server.exe" | Select-Object -First 1
        if ($exe -and $exe.DirectoryName -ne $klasor) { Get-ChildItem $exe.DirectoryName | Move-Item -Destination $klasor -Force }
        Remove-Item $zip
    }
    Set-Content -Path (Join-Path $kok "motor\surum.txt") -Value $surum.tag_name
}

# ---- 2) Modeller (Hugging Face) ----
Baslik "2/4 Modeller"
foreach ($m in $modeller) {
    $hedef = Join-Path $kok "modeller\$($m.Dosya)"
    Write-Host "$($m.Ad): $($m.Dosya)"
    Indir "https://huggingface.co/$($m.Depo)/resolve/main/$($m.Dosya)" $hedef
}

# ---- 3) Taşınabilir .NET 10 SDK (laboratuvarda .NET kurulu değilse BASLAT.bat bunu kullanır) ----
Baslik "3/4 Taşınabilir .NET 10 SDK"
$dotnetKlasoru = Join-Path $kok "araclar\dotnet"
if ($DotnetYok) { Write-Host "Atlandı (-DotnetYok)." }
elseif (Test-Path (Join-Path $dotnetKlasoru "dotnet.exe")) { Write-Host "Zaten var." }
else {
    $betik = Join-Path $env:TEMP "dotnet-install.ps1"
    Indir "https://dot.net/v1/dotnet-install.ps1" $betik
    & powershell -NoProfile -ExecutionPolicy Bypass -File $betik -Channel 10.0 -InstallDir $dotnetKlasoru -NoPath
    if ($LASTEXITCODE -ne 0) { throw ".NET SDK indirilemedi." }
}

# ---- 4) NuGet paketleri: proje internetsiz derlenebilsin diye bu klasöre indirilir ----
Baslik "4/4 Projenin paketleri (NuGet)"
$env:NUGET_PACKAGES = Join-Path $kok "araclar\nuget"
$dotnet = if (Test-Path (Join-Path $dotnetKlasoru "dotnet.exe")) { Join-Path $dotnetKlasoru "dotnet.exe" } else { "dotnet" }
& $dotnet restore (Join-Path $proje "AracKiralama.slnx")
if ($LASTEXITCODE -ne 0) { throw "Paketler indirilemedi (dotnet restore)." }

# ---- Özet ----
Baslik "HAZIR"
$boyut = (Get-ChildItem $kok -Recurse -File | Measure-Object Length -Sum).Sum / 1GB
Write-Host ("Toplam: {0:N1} GB  →  {1}" -f $boyut, $kok)
if ($kok -eq $PSScriptRoot) {
    Write-Host "Şimdi bütün automarket klasörünü USB'ye kopyalayın. Laboratuvarda: AracKiralama\yapay-zeka\BASLAT.bat" -ForegroundColor Green
} else {
    Write-Host "İndirilenler projenin dışında. USB'de bu klasörün İÇİNDEKİLERİ (motor, modeller, araclar) AracKiralama\yapay-zeka\ altına kopyalayın," -ForegroundColor Green
    Write-Host "ya da bu bilgisayarda kullanmak için appsettings.json → YapayZeka → Klasor = $kok yazın." -ForegroundColor Green
}
Write-Host "Başka bir bilgisayar için her şeyi tek klasör + zip yapmak: PAKETLE.bat (bkz. BENI-OKU.md)" -ForegroundColor Green
