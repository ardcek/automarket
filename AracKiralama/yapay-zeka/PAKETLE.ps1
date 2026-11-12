# ============================================================================
# PAKETLE.ps1 — Projeyi ve internetsiz Yapay Zekâ Asistanını TEK KLASÖR + ZIP yapar.
# Zip başka bir Windows 10/11 bilgisayara götürülür; orada HİÇBİR ŞEY KURMADAN ve İNTERNETSİZ
# çalışır: zip'i C:\Rota gibi bir klasöre çıkar → BASLAT.bat'a çift tıkla.
#
# Çalıştırmak için: PAKETLE.bat'a çift tıklayın  ya da  PAKETLE.bat -Ad Rota-Hafta5
#
# Ne yapar?
#   1) Yapay zekâ dosyalarını bulur ve kontrol eder: motor, modeller, araclar\dotnet (taşınabilir
#      .NET 10 SDK), araclar\nuget (paketler). Eksik varsa önce internetle HAZIRLA.bat çalıştırılır.
#   2) <Hedef>\<Ad>\ klasörünü kurar:
#        BASLAT.bat, KURULUM-OKU.txt   → kullanıcının ilk göreceği iki dosya (yapay-zeka\paket\)
#        AracKiralama\                 → projenin dosyaları: git'teki dosyalar + git'e eklenmemiş
#                                        belgeler (bin, obj, veritabanı, yedekler girmez)
#        AracKiralama\yapay-zeka\      → motor, modeller, .NET, NuGet paketleri
#   3) <Hedef>\<Ad>.zip dosyasını yapar. Modeller sıkıştırılmaz (zaten sıkışmazlar). Zip 4 GB'tan
#      büyük olabilir → USB exFAT ya da NTFS olmalı (FAT32 almaz).
#
# Seçenekler:
#   -Ad Rota-Hafta5                → klasörün ve zip'in adı (varsayılan: Rota-Kurulum)
#   -Hedef D:\Paketler             → nereye (varsayılan: kullanıcı klasörü, örn. C:\Users\ad).
#                                    OneDrive / Masaüstü içine koymayın: ~10 GB buluta yüklenir.
#   -YapayZeka D:\rota-yapay-zeka  → yapay zekâ dosyalarının klasörü (verilmezse kendisi arar)
#   -SadeceKucuk                   → orta modeli koymaz: zip ~3,5 GB olur, FAT32 USB'ye de sığar
#   -ZipYok                        → sadece klasörü kurar
# ============================================================================
param(
    [string]$Ad = "Rota-Kurulum",
    [string]$Hedef = $env:USERPROFILE,
    [string]$YapayZeka,
    [switch]$SadeceKucuk,
    [switch]$ZipYok
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$saat = [Diagnostics.Stopwatch]::StartNew()
$tr = [Globalization.CultureInfo]::GetCultureInfo("tr-TR")
$proje = Split-Path $PSScriptRoot -Parent                 # ...\AracKiralama

function Baslik($metin) { Write-Host ""; Write-Host "==== $metin ====" -ForegroundColor Cyan }
function GB($bayt) { ($bayt / 1GB).ToString("N1", $tr) + " GB" }
function Toplam($dosyalar) { ($dosyalar | Measure-Object Length -Sum).Sum }

# git'i çalıştırır, çıktıyı UTF-8 okur (Türkçe dosya adları bozulmasın). git yoksa / hata verirse $null.
function GitCalistir([string[]]$argumanlar) {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) { return $null }
    $psi = New-Object Diagnostics.ProcessStartInfo $git.Source
    $psi.Arguments = ($argumanlar | ForEach-Object { '"' + $_ + '"' }) -join " "
    $psi.WorkingDirectory = $proje
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = [Text.Encoding]::UTF8
    $surec = [Diagnostics.Process]::Start($psi)
    $hata = $surec.StandardError.ReadToEndAsync()
    $cikti = $surec.StandardOutput.ReadToEnd()
    $surec.WaitForExit()
    $null = $hata.Result
    if ($surec.ExitCode -ne 0) { return $null }
    return $cikti
}

# ---- 1) Yapay zekâ dosyaları ----
Baslik "1/4 Yapay zekâ dosyaları"
$adaylar = @($YapayZeka, $env:YapayZeka__Klasor, [Environment]::GetEnvironmentVariable("YapayZeka__Klasor", "User"),
             $PSScriptRoot, (Join-Path $env:USERPROFILE "rota-yapay-zeka")) | Where-Object { $_ }
$ai = $adaylar | Where-Object { Test-Path (Join-Path $_ "modeller\*.gguf") } | Select-Object -First 1
if (-not $ai) { throw "Model dosyaları bulunamadı. Önce HAZIRLA.bat'ı çalıştırın ya da -YapayZeka <klasör> verin." }
$ai = (Resolve-Path $ai).Path
Write-Host "Klasör: $ai"
$eksik = @()
if (-not (Get-ChildItem (Join-Path $ai "motor") -Recurse -Filter "llama-server.exe" -ErrorAction SilentlyContinue)) { $eksik += "motor (llama-server.exe)" }
if (-not (Test-Path (Join-Path $ai "araclar\dotnet\dotnet.exe"))) { $eksik += "taşınabilir .NET (araclar\dotnet)" }
if (-not (Test-Path (Join-Path $ai "araclar\nuget"))) { $eksik += "NuGet paketleri (araclar\nuget)" }
if ($eksik) { throw ("Eksik: " + ($eksik -join ", ") + ". İnternetli bilgisayarda bir kere çalıştırın:  HAZIRLA.bat -Hedef `"$ai`"") }
$modeller = @(Get-ChildItem (Join-Path $ai "modeller") -Filter "*.gguf" | Sort-Object Length)
if ($SadeceKucuk) { $modeller = @($modeller[0]) }
$modeller | ForEach-Object { Write-Host ("  model: {0} ({1})" -f $_.Name, (GB $_.Length)) }
$dotnetSurumu = (Get-ChildItem (Join-Path $ai "araclar\dotnet\sdk") -Directory | Sort-Object Name | Select-Object -Last 1).Name
$motorSurumu = Get-Content (Join-Path $ai "motor\surum.txt") -ErrorAction SilentlyContinue | Select-Object -First 1
Write-Host "  .NET SDK $dotnetSurumu, llama.cpp $motorSurumu"

# Hedef: yer var mı, daha önce yapılmış mı?
New-Item -ItemType Directory -Force $Hedef | Out-Null
$Hedef = (Resolve-Path $Hedef).Path
$kok = Join-Path $Hedef $Ad
$zipYolu = Join-Path $Hedef "$Ad.zip"
if ($Hedef -like "*OneDrive*") { Write-Warning "Hedef OneDrive içinde: ~10 GB buluta yüklenmeye çalışılır. Başka bir yer için -Hedef verin." }
if (Test-Path $kok) { throw "Bu klasör zaten var: $kok  (silin ya da başka bir -Ad verin)" }
if (-not $ZipYok -and (Test-Path $zipYolu)) { throw "Bu zip zaten var: $zipYolu  (silin ya da başka bir -Ad verin)" }
$aiBoyutu = (Toplam (Get-ChildItem (Join-Path $ai "motor"), (Join-Path $ai "araclar") -Recurse -File)) + (Toplam $modeller)
$gerekli = $aiBoyutu * $(if ($ZipYok) { 1 } else { 2 }) + 1GB
$bos = $null
try { $bos = (Get-PSDrive ($Hedef.Substring(0, 1)) -ErrorAction Stop).Free } catch { }   # ağ yolunda ölçülemez
if ($bos -and $bos -lt $gerekli) { throw "Diskte yer yetmiyor: $(GB $gerekli) gerekli, $(GB $bos) boş." }

# ---- 2) Projenin dosyaları ----
Baslik "2/4 Projenin dosyaları → $kok\AracKiralama"
$hedefProje = Join-Path $kok "AracKiralama"
$liste = GitCalistir @("ls-files", "-z", "--cached", "--others", "--exclude-standard")
if ($liste) {
    # Git'in bildiği dosyalar + git'e eklenmemiş ama yok sayılmayan dosyalar (.gitignore'dakiler girmez)
    $dosyalar = @($liste.Split([char]0) | Where-Object { $_ -and $_ -notmatch '^yapay-zeka/(motor|modeller|araclar)/' })
    $atlanan = 0
    foreach ($d in $dosyalar) {
        $kaynak = Join-Path $proje $d
        if (-not (Test-Path -LiteralPath $kaynak -PathType Leaf)) { $atlanan++; continue }   # silinmiş dosya
        $hedefDosya = Join-Path $hedefProje $d
        $klasor = Split-Path $hedefDosya -Parent
        if (-not (Test-Path -LiteralPath $klasor)) { New-Item -ItemType Directory -Force $klasor | Out-Null }
        Copy-Item -LiteralPath $kaynak -Destination $hedefDosya
    }
    Write-Host ("{0} dosya kopyalandı (git listesine göre){1}" -f ($dosyalar.Count - $atlanan), $(if ($atlanan) { ", $atlanan silinmiş dosya atlandı" } else { "" }))
} else {
    # git yok (örn. paketin içinden çalıştırılıyor): derleme çıktıları, veritabanı ve büyük klasörler hariç her şey
    $haric = @("bin", "obj", ".vs", "node_modules", "TestResults", ".yapay-zeka-yedek", (Join-Path $proje "src\AracKiralama.Web\anahtarlar"),
               (Join-Path $PSScriptRoot "motor"), (Join-Path $PSScriptRoot "modeller"), (Join-Path $PSScriptRoot "araclar"))
    & robocopy $proje $hedefProje /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD @haric /XF "*.db" "*.db-shm" "*.db-wal" "*.yedek-*" "veritabanini-yenile.isaret" | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Proje kopyalanamadı (robocopy kodu $LASTEXITCODE)" }
    Write-Host "Kopyalandı (git yok: bin, obj, veritabanı ve büyük klasörler hariç)"
}

# ---- 3) Yapay zekâ dosyaları + kökteki iki dosya ----
Baslik "3/4 Motor, modeller, .NET ve paketler ($(GB $aiBoyutu))"
$hedefAi = Join-Path $hedefProje "yapay-zeka"
foreach ($alt in "motor", "araclar") {
    & robocopy (Join-Path $ai $alt) (Join-Path $hedefAi $alt) /E /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Kopyalanamadı: $alt (robocopy kodu $LASTEXITCODE)" }
    Write-Host "  $alt"
}
foreach ($m in $modeller) {
    & robocopy $m.DirectoryName (Join-Path $hedefAi "modeller") $m.Name /J /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Kopyalanamadı: $($m.Name) (robocopy kodu $LASTEXITCODE)" }
    Write-Host "  modeller\$($m.Name)"
}
Copy-Item (Join-Path $PSScriptRoot "paket\BASLAT.bat") $kok
$surum = GitCalistir @("log", "-1", "--format=%h  %s  (%ad)", "--date=short")
$metin = [IO.File]::ReadAllText((Join-Path $PSScriptRoot "paket\KURULUM-OKU.txt"), [Text.Encoding]::UTF8)
$metin = $metin.Replace("{AD}", $Ad).Replace("{TARIH}", (Get-Date).ToString("d MMMM yyyy HH:mm", $tr))
$metin = $metin.Replace("{SURUM}", $(if ($surum) { $surum.Trim() } else { "-" })).Replace("{DOTNET}", $dotnetSurumu).Replace("{MOTOR}", "$motorSurumu")
$metin = $metin.Replace("{MODELLER}", (($modeller | ForEach-Object { "{0} ({1})" -f $_.Name, (GB $_.Length) }) -join ", "))
$metin = $metin -replace "`r?`n", "`r`n"                  # Not Defteri için Windows satır sonu
[IO.File]::WriteAllText((Join-Path $kok "KURULUM-OKU.txt"), $metin, (New-Object Text.UTF8Encoding $true))

$hepsi = @(Get-ChildItem $kok -Recurse -File -Force)
$klasorBoyutu = Toplam $hepsi
$enUzun = ($hepsi | ForEach-Object { $_.FullName.Length - $kok.Length - 1 } | Measure-Object -Maximum).Maximum
Write-Host ("Klasör hazır: {0} dosya, {1}. En uzun yol {2} karakter (C:\Rota\ altında {3})." -f $hepsi.Count, (GB $klasorBoyutu), $enUzun, ($enUzun + 8))
if ($enUzun + 8 -gt 240) { Write-Warning "Yollar uzun: zip'i C:\Rota gibi kısa bir klasöre çıkarın." }

# ---- 4) Zip ----
if (-not $ZipYok) {
    Baslik "4/4 Zip: $zipYolu"
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    # Modeller en sona (diğer dosyalar zip'in başında kalır); 4 GB'tan büyük dosyalar Zip64 ile yazılır
    $modelDosyalari = @($hepsi | Where-Object { $_.Extension -eq ".gguf" } | Sort-Object Length)
    $digerleri = @($hepsi | Where-Object { $_.Extension -ne ".gguf" } | Sort-Object FullName)
    $sikismaz = @(".gguf", ".zip", ".nupkg", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".woff", ".woff2", ".pdf", ".gz", ".7z")
    $gecici = "$zipYolu.yarim"
    if (Test-Path $gecici) { Remove-Item $gecici }
    $akim = [IO.File]::Open($gecici, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $arsiv = New-Object IO.Compression.ZipArchive($akim, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $sira = 0
        foreach ($d in ($digerleri + $modelDosyalari)) {
            $sira++
            $adi = $d.FullName.Substring($kok.Length + 1).Replace("\", "/")
            $seviye = if ($sikismaz -contains $d.Extension.ToLowerInvariant()) { [IO.Compression.CompressionLevel]::NoCompression } else { [IO.Compression.CompressionLevel]::Optimal }
            if ($d.Length -gt 500MB) { Write-Host ("  {0} ({1})" -f $adi, (GB $d.Length)) }
            elseif ($sira % 2000 -eq 0) { Write-Host ("  {0} / {1} dosya" -f $sira, $hepsi.Count) }
            $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arsiv, $d.FullName, $adi, $seviye)
        }
    } finally { $arsiv.Dispose(); $akim.Dispose() }

    # Doğrulama: zip yeniden açılır; dosya sayısı ve modellerin boyutu tutmalı
    $okunan = [IO.Compression.ZipFile]::OpenRead($gecici)
    try {
        if ($okunan.Entries.Count -ne $hepsi.Count) { throw "Zip eksik: $($okunan.Entries.Count) / $($hepsi.Count) dosya" }
        foreach ($m in $modelDosyalari) {
            $giris = $okunan.GetEntry($m.FullName.Substring($kok.Length + 1).Replace("\", "/"))
            if (-not $giris -or $giris.Length -ne $m.Length) { throw "Zip'te model boyutu tutmuyor: $($m.Name)" }
        }
    } finally { $okunan.Dispose() }
    Move-Item $gecici $zipYolu
    Write-Host ("Zip doğrulandı: {0} dosya" -f $hepsi.Count)
}

Baslik "HAZIR"
Write-Host "Klasör: $kok  ($(GB $klasorBoyutu))"
if (-not $ZipYok) { Write-Host "Zip:    $zipYolu  ($(GB (Get-Item $zipYolu).Length))" }
Write-Host "USB için (exFAT / NTFS): zip + yapay-zeka\paket\KUR.bat + paket\TESHIS.bat + klasördeki KURULUM-OKU.txt" -ForegroundColor Green
Write-Host "Başka bilgisayarda: USB'deki KUR.bat → proje Masaüstü\automarket'e, yapay zekâ C:\Users\<ad>\rota-yapay-zeka'ya" -ForegroundColor Green
Write-Host ("Süre: {0} dk {1} sn" -f [int][Math]::Floor($saat.Elapsed.TotalMinutes), $saat.Elapsed.Seconds)
