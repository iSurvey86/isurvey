@echo off
cd /d "%~dp0"
echo.
echo === iSurvey — Cai dat ===
echo Dong AutoCAD / Civil 3D truoc khi tiep tuc.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-iSurvey.ps1"
echo.
pause
