# 🚀 1-Click Deployment Guide for Pannaum Hotel System

This project is fully configured for instant free cloud hosting (giving you a live `https://pannaum-hotel.onrender.com` URL) as well as instant local mobile testing.

---

## 🌟 Method 1: Free Live Public Cloud URL (Render.com)

We have added `Dockerfile` and `render.yaml` to the project repository. Follow these 3 steps:

### Step 1: Upload Project to GitHub
1. Open terminal in `hotelbillingsystem001` folder and run:
   ```bash
   git init
   git add .
   git commit -m "Deploy Pannaum Hotel System"
   git branch -M main
   git remote add origin https://github.com/YOUR_GITHUB_USERNAME/pannaum-hotel.git
   git push -u origin main
   ```

### Step 2: Connect to Render.com (100% Free)
1. Go to [Render.com](https://render.com) and create a free account.
2. Click **New +** ➔ **Blueprint** (or **Web Service**).
3. Connect your GitHub repository `pannaum-hotel`.
4. Render will automatically detect `render.yaml` & `Dockerfile` and deploy your app!

### Step 3: Get Your Live Link
In 2-3 minutes, Render will give you your official live website link:
`https://pannaum-hotel-billing.onrender.com`

---

## 📱 Method 2: Instant Public Link on Mobile (Using Ngrok)

If you want an instant public HTTPS link right now from your PC without uploading to GitHub:

1. Double-click `run_live_demo.bat` to start the server.
2. Download free [ngrok.exe](https://ngrok.com/download).
3. Open terminal and run:
   ```bash
   ngrok http 5000
   ```
4. Ngrok will give you an instant public HTTPS URL like `https://a1b2c3d4.ngrok-free.app` that you can open on any mobile phone or send to anyone!

---

## 🏨 Method 3: Hotel Wi-Fi Network Access

1. Connect your PC and mobile phones to the same Wi-Fi.
2. Double-click `run_live_demo.bat`.
3. Open `cmd` and type `ipconfig` to find your PC's IP address (e.g. `192.168.1.50`).
4. On any smartphone connected to the Wi-Fi, open: `http://192.168.1.50:5000`
