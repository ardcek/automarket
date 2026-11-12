@echo off
rem ==========================================================================
rem HAZIRLA.bat - HAZIRLA.ps1'i calistirir (cift tiklamak icin).
rem Internetli bilgisayarda BIR KERE calistirin. Ayrintilar: BENI-OKU.md
rem Secenekler: HAZIRLA.bat -SadeceKucuk   /   HAZIRLA.bat -DotnetYok
rem ==========================================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0HAZIRLA.ps1" %*
echo.
pause
