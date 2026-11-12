# Kullanım: pdfe-cevir.ps1 -Girdi <dosya> -Klasor <çıktı klasörü> [-Tur pdf]
# Bu LibreOffice sürümü aynı profille ikinci dönüşümde takıldığı için her seferinde yeni, geçici bir profil kullanır.
param([string]$Girdi, [string]$Klasor, [string]$Tur = 'pdf')
$kok = $env:TEMP
$profilYolu = Join-Path $kok ("lo-gecici-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
$profil = 'file:///' + ($profilYolu -replace '\\', '/')
$t = Get-Date
$p = Start-Process -FilePath 'C:\Program Files\LibreOffice\program\soffice.exe' -ArgumentList @("-env:UserInstallation=$profil", '--headless', '--norestore', '--nolockcheck', '--convert-to', $Tur, '--outdir', "`"$Klasor`"", "`"$Girdi`"") -PassThru -WindowStyle Hidden
try { Wait-Process -Id $p.Id -Timeout 120 -ErrorAction Stop; "Çevrildi ($Tur): " + [int]((Get-Date) - $t).TotalSeconds + ' sn' }
catch { 'ZAMAN AŞIMI'; Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
Remove-Item -Recurse -Force $profilYolu -ErrorAction SilentlyContinue

