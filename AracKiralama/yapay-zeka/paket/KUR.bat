@echo off
rem ==========================================================================
rem KUR.bat - Rota'yi bu bilgisayara USB'den kurar (cift tiklayin). Internet gerekmez.
rem   Proje       -> Masaustu\automarket\AracKiralama\         (automarket.zip, ~40 MB)
rem   Yapay zeka  -> C:\Users\<ad>\rota-yapay-zeka\              (Rota-HaftaN.zip icinden, ~9 GB)
rem   Calistirmak -> Masaustu\automarket\AracKiralama\yapay-zeka\BASLAT.bat
rem Yapay zeka dosyalari bilerek masaustune konmaz: masaustu OneDrive'a bagliysa 9 GB'i
rem buluta yuklemeye calisir. BASLAT.bat onlari C:\Users\<ad>\rota-yapay-zeka'da kendisi bulur.
rem Tekrar calistirilirsa: yapay zeka dosyalari varsa atlanir; proje USB'deki surumle guncellenir
rem (veritabani korunur). Masaustu\automarket git ile indirildiyse projeye dokunulmaz.
rem USB'de olmasi gerekenler: KUR.bat, automarket.zip, Rota-HaftaN.zip (PAKETLE.ps1 uretir)
rem ==========================================================================
setlocal
title Rota Arac Kiralama - Kurulum
set "USB=%~dp0"
set "YZ=%USERPROFILE%\rota-yapay-zeka"
set "MASAUSTU="
for /f "usebackq delims=" %%d in (`powershell -NoProfile -Command "[Environment]::GetFolderPath('Desktop')"`) do set "MASAUSTU=%%d"
if not defined MASAUSTU set "MASAUSTU=%USERPROFILE%\Desktop"
set "PROJE=%MASAUSTU%\automarket"
rem Deneme / ozel kurulum icin hedefler degistirilebilir
if defined ROTA_YZ set "YZ=%ROTA_YZ%"
if defined ROTA_PROJE set "PROJE=%ROTA_PROJE%"

set "PROJEZIP="
if exist "%USB%automarket.zip" set "PROJEZIP=%USB%automarket.zip"
set "BUYUKZIP="
for %%f in ("%USB%Rota-*.zip") do set "BUYUKZIP=%%~ff"

echo.
echo  Proje:       %PROJE%
echo  Yapay zeka:  %YZ%
echo.

rem ==== 1) Yapay zeka dosyalari: motor, modeller, tasinabilir .NET, paketler ====
if exist "%YZ%\modeller\*.gguf" if exist "%YZ%\araclar\dotnet\dotnet.exe" if exist "%YZ%\araclar\nuget" if exist "%YZ%\motor" goto yzvar
if not exist "%YZ%" mkdir "%YZ%"
rem Eski kurulum (C:\Rota) varsa 9 GB'i yeniden acmak yerine oradan tasi (ayni diskte aninda biter)
if exist "C:\Rota\AracKiralama\yapay-zeka\modeller\*.gguf" if not exist "%YZ%\modeller" (
    echo Eski C:\Rota kurulumundaki yapay zeka dosyalari tasiniyor...
    move "C:\Rota\AracKiralama\yapay-zeka\motor" "%YZ%\motor" >nul
    move "C:\Rota\AracKiralama\yapay-zeka\modeller" "%YZ%\modeller" >nul
    move "C:\Rota\AracKiralama\yapay-zeka\araclar" "%YZ%\araclar" >nul
)
if exist "%YZ%\modeller\*.gguf" if exist "%YZ%\araclar\dotnet\dotnet.exe" if exist "%YZ%\araclar\nuget" if exist "%YZ%\motor" goto yzvar
if not defined BUYUKZIP goto zipyok
echo Yapay zeka dosyalari aciliyor: ~9 GB, USB hizina gore 5-10 dakika. Pencereyi kapatmayin.
echo En sondaki iki model dosyasi acilirken ekran 1-2 dakika durgun gorunebilir.
tar -xvf "%BUYUKZIP%" -C "%YZ%" --strip-components 2 AracKiralama/yapay-zeka/motor AracKiralama/yapay-zeka/modeller AracKiralama/yapay-zeka/araclar
if errorlevel 1 goto hata
if not exist "%YZ%\modeller\*.gguf" goto hata
if not exist "%YZ%\araclar\dotnet\dotnet.exe" goto hata
echo Yapay zeka dosyalari kuruldu.
goto proje
:yzvar
echo Yapay zeka dosyalari zaten var, atlaniyor.

rem ==== 2) Proje ====
:proje
if exist "%PROJE%\.git" goto gitvar
if not exist "%PROJE%" mkdir "%PROJE%"
if not defined PROJEZIP goto projebuyuk
echo Proje aciliyor: automarket.zip
tar -xf "%PROJEZIP%" -C "%PROJE%"
goto projekontrol
:projebuyuk
if not defined BUYUKZIP goto zipyok
echo Proje aciliyor: buyuk paketten
tar -xf "%BUYUKZIP%" -C "%PROJE%" --exclude "AracKiralama/yapay-zeka/motor" --exclude "AracKiralama/yapay-zeka/modeller" --exclude "AracKiralama/yapay-zeka/araclar"
:projekontrol
if errorlevel 1 goto hata
if not exist "%PROJE%\AracKiralama\yapay-zeka\BASLAT.bat" goto hata
goto bitti
:gitvar
echo Masaustu\automarket git ile indirilmis: projeye dokunulmadi. Guncellemek icin: git pull

:bitti
echo.
echo  ==== KURULUM TAMAM ====
echo  Calistirmak icin:  %PROJE%\AracKiralama\yapay-zeka\BASLAT.bat
echo  Ilk acilista proje derlenir (1-2 dakika); tarayici kendiliginden acilir.
echo.
if defined ROTA_BASLATMA exit /b 0
choice /c EH /t 20 /d E /m "Simdi baslatilsin mi? 20 saniye sonra kendiliginden E"
if errorlevel 2 exit /b 0
call "%PROJE%\AracKiralama\yapay-zeka\BASLAT.bat"
exit /b

:zipyok
echo USB'de gereken zip bulunamadi. KUR.bat'in yaninda automarket.zip ve Rota-HaftaN.zip olmali.
pause
exit /b 1

:hata
echo.
echo Kurulum tamamlanamadi. Diskte en az 12 GB bos yer olmali.
echo KUR.bat'i tekrar calistirin. Sorun surerse USB'deki TESHIS.bat'i calistirip olusan dosyayi gosterin.
pause
exit /b 1
