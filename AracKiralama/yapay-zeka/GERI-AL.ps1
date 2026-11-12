# ============================================================================
# GERI-AL.ps1 — Yapay zekâ KOD MODUNUN yaptığı son değişikliği geri alır.
# Uygulama açılamıyorsa (panele girilemiyorsa) kullanılır. Panel açılıyorsa
# Yönetim → Yapay Zekâ Asistanı → Geçmiş → "Geri al" daha kolaydır.
#
# Yedekler: AracKiralama\.yapay-zeka-yedek\<tarih>\ (her birinde degisiklik.json)
# En yeni ve henüz geri alınmamış yedek bulunur, dosyalar eski haline döner,
# kod modunun eklediği yeni dosyalar silinir.
# ============================================================================
param([switch]$Sessiz)
$ErrorActionPreference = "Stop"
$proje = Split-Path $PSScriptRoot -Parent                       # ...\AracKiralama
$kaynak = Join-Path $proje "src\AracKiralama.Web"
$yedekler = Join-Path $proje ".yapay-zeka-yedek"

$son = Get-ChildItem $yedekler -Directory -ErrorAction SilentlyContinue |
    Where-Object { (Test-Path (Join-Path $_.FullName "degisiklik.json")) -and -not (Test-Path (Join-Path $_.FullName "geri-alindi.txt")) } |
    Sort-Object Name -Descending | Select-Object -First 1
if (-not $son) { Write-Host "Geri alınacak bir kod değişikliği yok."; exit 0 }

$bilgi = Get-Content (Join-Path $son.FullName "degisiklik.json") -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Host "Son değişiklik: $($son.Name)  —  $($bilgi.istek)"
if (-not $Sessiz) {
    $cevap = Read-Host "Geri alınsın mı? (E/H)"
    if ($cevap -notin @("E", "e")) { exit 0 }
}

$veritabani = $false
foreach ($yol in $bilgi.degisenler) {
    Copy-Item (Join-Path $son.FullName $yol) (Join-Path $kaynak $yol) -Force
    # Kopya eski tarihini taşır; derleyici "değişmemiş" sanmasın diye tarihi şimdi yapılır
    (Get-Item (Join-Path $kaynak $yol)).LastWriteTime = Get-Date
    Write-Host "  geri yüklendi: $yol"
    if ($yol -like "Modeller/*" -or $yol -like "Veri/*") { $veritabani = $true }
}
foreach ($yol in $bilgi.yeniler) {
    $tam = Join-Path $kaynak $yol
    if (Test-Path $tam) { Remove-Item $tam -Force; Write-Host "  silindi: $yol" }
    if ($yol -like "Modeller/*" -or $yol -like "Veri/*") { $veritabani = $true }
}
# Model dosyası değiştiyse yerel veritabanı açılışta yeniden oluşturulsun
if ($veritabani) { Set-Content (Join-Path $kaynak "veritabanini-yenile.isaret") "GERI-AL ile geri alındı." }
Set-Content (Join-Path $son.FullName "geri-alindi.txt") ("GERI-AL.bat ile geri alındı: " + (Get-Date))
Write-Host "Tamam. Uygulama yeniden açılınca eski haliyle çalışır." -ForegroundColor Green
