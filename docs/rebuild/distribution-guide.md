# Distribution & CI/CD Guide

How we build, bundle, and deliver SaveState Reborn to the world.

---

[← Back to README](./README.md)

---

## **📦 Publishing Strategy**

We target **Native AOT (Ahead-of-Time)** for the core logic and **Trimmed Self-Contained** for the UI, ensuring the fastest possible startup with no .NET runtime requirement on the user's machine.

### **Publish Command**

```bash
dotnet publish src/SaveState.App \
    -c Release \
    -r win-x64 \
    -p:PublishAot=true \
    -p:PublishSingleFile=true \
    -p:SelfContained=true \
    --output ./publish
```

---

## **🔄 Update System: Velopack**

We use [Velopack](https://velopack.io/) for updates. It provides:

- Multiple update channels (Stable/Beta).
- Delta updates (user only downloads changed bytes).
- Native Windows installers (.exe / .msi).

### **Velopack Build Script**

```bash
# 1. Install Velopack CLI
dotnet tool install -g vpk

# 2. Package the published output
vpk pack \
    --packid SaveStateReborn \
    --packv 1.0.0 \
    --packdir ./publish \
    --mainexe SaveState.App.exe \
    --icon ./assets/app.ico
```

---

## **🚀 CI/CD Pipeline (GitHub Actions)**

📁 Create: `.github/workflows/build-deploy.yml`

```yaml
name: Build & Release

on:
  push:
    tags: ['v*']

jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Run Tests
      run: dotnet test --configuration Release --no-restore

    - name: Publish App
      run: dotnet publish src/SaveState.App -c Release -r win-x64 -p:PublishAot=true -o ./dist

    - name: Package with Velopack
      run: |
        dotnet tool install -g vpk
        vpk pack --packid SaveStateReborn --packv ${{ github.ref_name }} --packdir ./dist

    - name: Create Release
      uses: softprops/action-gh-release@v2
      with:
        files: releases/*
```

---

## **🛠️ Signing & Security**

1. **Code Signing**: All releases must be signed using a certificate (Certum/Digicert) to prevent Windows SmartScreen warnings.
2. **VirusTotal Scan**: Every build is automatically scanned by the pipeline to ensure zero false positives.

---

## **🔧 Update Channels**

| Channel | Frequency | Stability | Target |
|:---|:---|:---|:---|
| **Canary** | Every Commit | Experimental | Devs / Brave Users |
| **Beta** | Weekly | Stable-ish | Power Users |
| **Stable** | Monthly | Rock Solid | General Public |
