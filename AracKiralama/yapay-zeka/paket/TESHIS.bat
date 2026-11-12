@echo off
rem ==========================================================================
rem TESHIS.bat - Kurulum ya da "derleme basarisiz" sorununu incelemek icin bilgi toplar.
rem Sorun olan bilgisayarda cift tiklayin. Sonuc USB'ye yazilir: teshis-<BILGISAYARADI>.txt
rem Bu dosyayi ekibe gosterin. Hicbir seyi degistirmez; sadece bir deneme derlemesi yapar.
rem ==========================================================================
setlocal
title Rota - Teshis
set "CIKTI=%~dp0teshis-%COMPUTERNAME%.txt"
set "MASAUSTU="
for /f "usebackq delims=" %%d in (`powershell -NoProfile -Command "[Environment]::GetFolderPath('Desktop')"`) do set "MASAUSTU=%%d"

rem ---- Proje ve yapay zeka (tasinabilir .NET) nerede? ----
set "PROJE="
if exist "%MASAUSTU%\automarket\AracKiralama\src\AracKiralama.Web\AracKiralama.Web.csproj" set "PROJE=%MASAUSTU%\automarket\AracKiralama"
if not defined PROJE if exist "C:\Rota\AracKiralama\src\AracKiralama.Web\AracKiralama.Web.csproj" set "PROJE=C:\Rota\AracKiralama"
if not defined PROJE if exist "%USERPROFILE%\Rota\AracKiralama\src\AracKiralama.Web\AracKiralama.Web.csproj" set "PROJE=%USERPROFILE%\Rota\AracKiralama"
set "YZ="
if defined PROJE if exist "%PROJE%\yapay-zeka\araclar\dotnet\dotnet.exe" set "YZ=%PROJE%\yapay-zeka"
if not defined YZ if exist "%USERPROFILE%\rota-yapay-zeka\araclar\dotnet\dotnet.exe" set "YZ=%USERPROFILE%\rota-yapay-zeka"
if not defined YZ if exist "C:\rota-yapay-zeka\araclar\dotnet\dotnet.exe" set "YZ=C:\rota-yapay-zeka"

echo Bilgi toplaniyor ve deneme derlemesi yapiliyor (1-3 dakika)...
> "%CIKTI%" echo ==== Rota teshis: %COMPUTERNAME%   %DATE% %TIME%
>>"%CIKTI%" ver
>>"%CIKTI%" echo Mimari: %PROCESSOR_ARCHITECTURE%   Kullanici klasoru: %USERPROFILE%
>>"%CIKTI%" echo Masaustu: %MASAUSTU%
>>"%CIKTI%" echo Proje: %PROJE%
>>"%CIKTI%" echo Yapay zeka: %YZ%
>>"%CIKTI%" powershell -NoProfile -Command "Get-PSDrive -PSProvider FileSystem | ForEach-Object { '{0}: bos {1:N1} GB' -f $_.Name, ($_.Free / 1GB) }; 'RAM: {0:N0} GB' -f ((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB); 'Ekran karti: ' + ((Get-CimInstance Win32_VideoController | ForEach-Object Name) -join ', '); 'Antivirus: ' + ((Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct -ErrorAction SilentlyContinue | ForEach-Object displayName) -join ', ')"
>>"%CIKTI%" echo ==== ortam degiskenleri (dotnet / msbuild / nuget)
>>"%CIKTI%" 2>nul set DOTNET
>>"%CIKTI%" 2>nul set MSBuild
>>"%CIKTI%" 2>nul set NUGET
>>"%CIKTI%" echo ==== bilgisayarda kurulu dotnet
>>"%CIKTI%" 2>&1 where dotnet
if not defined PROJE (
    >>"%CIKTI%" echo PROJE BULUNAMADI
    goto son
)
if not defined YZ (
    >>"%CIKTI%" echo TASINABILIR .NET BULUNAMADI
    goto son
)

rem ---- BASLAT.bat'in kurdugu ortamin aynisi ----
set "DOTNET_ROOT=%YZ%\araclar\dotnet"
set "PATH=%YZ%\araclar\dotnet;%PATH%"
set "NUGET_PACKAGES=%YZ%\araclar\nuget"
set "MSBuildSDKsPath="
set "MSBuildExtensionsPath="
set "MSBUILD_EXE_PATH="
set "DOTNET_HOST_PATH="
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_NOLOGO=1
>>"%CIKTI%" echo ==== dotnet --info
>>"%CIKTI%" 2>&1 "%YZ%\araclar\dotnet\dotnet.exe" --info
>>"%CIKTI%" echo ==== derleme
>>"%CIKTI%" 2>&1 "%YZ%\araclar\dotnet\dotnet.exe" build "%PROJE%\src\AracKiralama.Web" -v minimal -nodeReuse:false
>>"%CIKTI%" echo ==== derleme cikis kodu: %ERRORLEVEL%

:son
echo.
echo Bitti. Sonuc USB'ye yazildi:
echo   %CIKTI%
echo Bu dosyayi gosterin.
pause
