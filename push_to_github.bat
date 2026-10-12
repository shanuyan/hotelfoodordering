@echo off
title 1-Click Push to GitHub for hotelfoodordering
color 0B
echo ===================================================================
echo     PUSHING PROJECT TO GITHUB (hotelfoodordering)
echo ===================================================================
echo.
cd /d "%~dp0"
echo [1/3] Checking Git Status...
git status -s
echo.
echo [2/3] Adding files and committing...
git add .
git commit -m "Configure hotelfoodordering for permanent Render deployment" > nul 2>&1
git branch -M main
echo.
echo [3/3] Pushing to GitHub (https://github.com/shanuyan/hotelfoodordering.git)...
echo If a GitHub login window appears, please click 'Sign in with your browser'.
echo.
git push -u origin main
echo.
echo ===================================================================
if %errorlevel% equ 0 (
    echo SUCCESS! Your code is live on GitHub!
    echo Now open Render.com to complete the 1-click deploy.
) else (
    echo Note: If the repository doesn't exist yet on GitHub, please create
    echo https://github.com/new with name 'hotelfoodordering' first, then re-run this file!
)
echo ===================================================================
echo.
pause
