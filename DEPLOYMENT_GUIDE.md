# 🚀 Deployment Guide for Hotel Billing System

This project is fully configured for instant cloud hosting (`https://hotelfoodordering.onrender.com`).

---

## 🌟 Method 1: Free Live Public Cloud URL (Render.com)

1. Upload Project to GitHub:
   ```bash
   git add .
   git commit -m "Configure hotelfoodordering deployment"
   git branch -M main
   git remote add origin https://github.com/YOUR_GITHUB_USERNAME/hotelfoodordering.git
   git push -u origin main
   ```

2. Go to [Render.com](https://render.com) and create a free account.
3. Click **New +** ➔ **Blueprint** (or **Web Service**).
4. Connect your GitHub repository `hotelfoodordering`.
5. Render will automatically build and publish your permanent live link:
   `https://hotelfoodordering.onrender.com`

---

## 📱 Method 2: Instant Public Link on Mobile (Using Ngrok)

1. Double-click `run_live_demo.bat`.
2. Open terminal and run:
   ```bash
   ngrok http 5000
   ```
3. Open the public link `https://xxxx.ngrok-free.app` on any smartphone!
