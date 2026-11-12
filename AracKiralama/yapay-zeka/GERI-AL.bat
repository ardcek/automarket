@echo off
rem ==========================================================================
rem GERI-AL.bat - Yapay zeka KOD MODUNUN son degisikligini geri alir.
rem Uygulama acilamiyorsa kullanin. Panel aciliyorsa: Yapay Zeka Asistani -> Gecmis -> Geri al.
rem BASLAT.bat hata durumunda bunu "/sessiz" ile kendisi cagirir (soru sormadan).
rem ==========================================================================
if /i "%~1"=="/sessiz" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0GERI-AL.ps1" -Sessiz
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0GERI-AL.ps1"
    pause
)
