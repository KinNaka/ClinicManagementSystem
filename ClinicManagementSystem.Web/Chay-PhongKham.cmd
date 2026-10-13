@echo off
cd /d "%~dp0"
set "ASPNETCORE_ENVIRONMENT=LocalNetwork"
"ClinicManagementSystem.Web.exe" --environment LocalNetwork
echo.
echo Ung dung da dung. Kiem tra thong bao loi phia tren neu co.
pause
