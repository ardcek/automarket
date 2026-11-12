@echo off
rem ==========================================================================
rem BASLAT.bat - Siteyi ve Yapay Zeka Asistanini internetsiz baslatir (cift tiklayin).
rem   1) Bilgisayarda .NET yoksa USB'deki tasinabilir .NET'i kullanir (araclar\dotnet)
rem   2) NuGet paketlerini USB'den okur (araclar\nuget) -> internet gerekmez
rem   3) Uygulamayi acar, tarayiciyi Yapay Zeka Asistani sayfasinda acar
rem   4) Kod modu "yeniden baslat" derse (cikis kodu 42) projeyi yeniden derleyip acar
rem   5) Uygulama hata ile kapanirsa son yapay zeka degisikligini geri almayi teklif eder
rem Kapatmak: bu pencerede Ctrl+C, sonra "C".
rem ==========================================================================
setlocal
title Rota Arac Kiralama - Yapay Zeka Asistani
set "KOK=%~dp0"
set "PROJE=%KOK%..\src\AracKiralama.Web"

rem ---- Yapay zeka dosyalari (motor, modeller, araclar) nerede? ----
rem Once bu klasor; yoksa YapayZeka__Klasor degiskeni, C:\Users\<ad>\rota-yapay-zeka, C:\rota-yapay-zeka.
rem (~9 GB; proje Masaustu/OneDrive'daysa disarida durmalari daha iyi: KUR.bat oraya kurar.)
set "YZ="
if exist "%KOK%modeller\*.gguf" set "YZ=%KOK:~0,-1%"
if not defined YZ if defined YapayZeka__Klasor if exist "%YapayZeka__Klasor%\modeller\*.gguf" set "YZ=%YapayZeka__Klasor%"
if not defined YZ if exist "%USERPROFILE%\rota-yapay-zeka\modeller\*.gguf" set "YZ=%USERPROFILE%\rota-yapay-zeka"
if not defined YZ if exist "C:\rota-yapay-zeka\modeller\*.gguf" set "YZ=C:\rota-yapay-zeka"
if defined YZ set "YapayZeka__Klasor=%YZ%"
if defined YZ echo Yapay zeka dosyalari: %YZ%
if not defined YZ echo Yapay zeka dosyalari bulunamadi: site acilir ama asistan "kurulu degil" der. USB'deki KUR.bat'i calistirin.
if not defined YZ set "YZ=%KOK:~0,-1%"

rem ---- .NET: tasinabilir surum varsa o kullanilir; bilgisayardaki baska .NET / Visual Studio ayarlari karismasin ----
if exist "%YZ%\araclar\dotnet\dotnet.exe" (
    set "DOTNET_ROOT=%YZ%\araclar\dotnet"
    set "PATH=%YZ%\araclar\dotnet;%PATH%"
    set "MSBuildSDKsPath="
    set "MSBuildExtensionsPath="
    set "MSBUILD_EXE_PATH="
    set "DOTNET_HOST_PATH="
)
where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET bulunamadi. USB'deki KUR.bat'i calistirin ya da .NET 10 SDK kurun.
    pause
    exit /b 1
)

rem ---- Paketler yapay zeka klasorunden (internetsiz derleme) ----
if exist "%YZ%\araclar\nuget" set "NUGET_PACKAGES=%YZ%\araclar\nuget"
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_NOLOGO=1
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
rem Uygulama bu degiskeni gorunce "yeniden baslat" dugmesini acar (KodModu.BaslaticidanMi)
set ROTA_BASLATICI=1
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5046

echo Site: http://localhost:5046   Panel: http://localhost:5046/Yonetim/YapayZeka   (admin@rota.com / Rota123!)
echo Ilk acilista proje derlenir (1-2 dakika). Tarayici, site hazir olunca kendiliginden acilir.
rem Tarayiciyi site cevap verince ac (arka planda /health adresini yoklar, en cok ~10 dakika).
rem Yeni bir bilgisayarda ilk derleme uzun surer; sabit bir bekleme yetmez. ROTA_TARAYICI_ACMA=1 ise acilmaz.
if not defined ROTA_TARAYICI_ACMA start "" /min powershell -NoProfile -WindowStyle Hidden -Command "for ($i = 0; $i -lt 200; $i++) { try { $null = Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 'http://localhost:5046/health'; break } catch { Start-Sleep 1 } }; Start-Process 'http://localhost:5046/Yonetim/YapayZeka'"

:dongu
dotnet run --project "%PROJE%" --no-launch-profile
set KOD=%ERRORLEVEL%
if "%KOD%"=="42" (
    echo.
    echo ==== Kod degisti, proje yeniden derleniyor ====
    goto dongu
)
if "%KOD%"=="0" goto son

echo.
echo Uygulama hata ile kapandi (kod %KOD%). Yukaridaki kirmizi satirlari okuyun.
echo   G = son yapay zeka kod degisikligini geri al ve yeniden dene
echo   C = cik
choice /c GC /n /m "Seciminiz (G/C): "
if errorlevel 2 goto son
call "%KOK%GERI-AL.bat" /sessiz
goto dongu

:son
endlocal
