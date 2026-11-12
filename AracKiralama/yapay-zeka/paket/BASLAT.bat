@echo off
rem ==========================================================================
rem BASLAT.bat - Rota Arac Kiralama: siteyi ve internetsiz Yapay Zeka Asistanini acar.
rem Cift tiklayin. Ilk acilista proje derlenir (1-2 dakika). Ayrintilar: KURULUM-OKU.txt
rem Bu dosyayi PAKETLE.ps1 paketin kokune koyar; asil baslatici AracKiralama\yapay-zeka\BASLAT.bat
rem ==========================================================================
if not exist "%~dp0AracKiralama\yapay-zeka\BASLAT.bat" (
    echo AracKiralama klasoru bulunamadi.
    echo Zip'e sag tiklayin, "Tumunu ayikla" ile C:\Rota gibi bir klasore cikarin ve BASLAT.bat'i oradan calistirin.
    pause
    exit /b 1
)
call "%~dp0AracKiralama\yapay-zeka\BASLAT.bat"
