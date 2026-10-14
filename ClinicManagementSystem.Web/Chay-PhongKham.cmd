@echo off
cd /d "%~dp0"

set "ASPNETCORE_ENVIRONMENT=LocalNetwork"

if not exist "Logs" mkdir "Logs"

:restart
echo [%date% %time%] Khoi dong MVC >> "Logs\restart.log"

"ClinicManagementSystem.Web.exe" --environment LocalNetwork >> "Logs\mvc.log" 2>&1

echo [%date% %time%] MVC da dung >> "Logs\restart.log"

timeout /t 5 /nobreak >nul

goto restart