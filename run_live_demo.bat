@echo off
title Pannaum Smart Hotel System - Live Server
echo ===================================================================
echo   PANNAUM SMART HOTEL SYSTEM - STARTING LIVE WEB SERVER
echo ===================================================================
echo.
echo Starting ASP.NET Core server on http://0.0.0.0:5000...
echo Devices on your Local Network / Wi-Fi can access this portal!
echo.

cd /d "%~dp0HotelBillingSystem"
dotnet run --urls "http://0.0.0.0:5000"

pause
