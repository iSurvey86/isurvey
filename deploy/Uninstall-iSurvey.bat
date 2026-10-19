@echo off
cd /d "%~dp0"
echo.
echo === iSurvey — Go cai dat ===
echo Dong AutoCAD / Civil 3D truoc khi tiep tuc.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-iSurvey.ps1"
echo.
pause
