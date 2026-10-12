@echo off
title Smart Hotel Billing System - Live Public Link Launcher
color 0A
echo ===================================================================
echo     SMART HOTEL BILLING SYSTEM - 1-CLICK LIVE LINK LAUNCHER
echo ===================================================================
echo.
echo [1/2] Starting ASP.NET Core Web Server on http://0.0.0.0:5000 ...

start /b dotnet run --project "%~dp0HotelBillingSystem\HotelBillingSystem.csproj" --urls "http://0.0.0.0:5000" > nul 2>&1

timeout /t 5 /nobreak > nul

echo [2/2] Generating Live Public URL for Customers...
echo.

echo Starting tunnel with fixed domain (https://hotelfoodordering.loca.lt)...
if exist "C:\Program Files (x86)\cloudflared\cloudflared.exe" (
    "C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --url http://localhost:5000
) else (
    npx localtunnel --port 5000 --subdomain hotelfoodordering
)

pause
