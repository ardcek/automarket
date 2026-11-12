@echo off
rem ==========================================================================
rem PAKETLE.bat - Projeyi ve internetsiz yapay zekayi TEK KLASOR + ZIP yapar (cift tiklayin).
rem Cikan zip baska bir Windows bilgisayarda kurulumsuz ve internetsiz calisir.
rem Ornek:  PAKETLE.bat -Ad Rota-Hafta5        Kucuk paket:  PAKETLE.bat -Ad Rota-Hafta5 -SadeceKucuk
rem Ayrintilar: PAKETLE.ps1'in basi ve BENI-OKU.md
rem ==========================================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0PAKETLE.ps1" %*
echo.
pause
