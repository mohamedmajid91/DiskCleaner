<p align="center">
  <img src="docs/banner.png" alt="Disk & RAM Cleaner" width="100%">
</p>

<p align="center">
  <img src="https://img.shields.io/github/v/release/mohamedmajid91/DiskCleaner?label=version" alt="version">
  <img src="https://img.shields.io/github/downloads/mohamedmajid91/DiskCleaner/total?label=downloads" alt="downloads">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-blue" alt="platform">
  <img src="https://img.shields.io/badge/.NET-10-512BD4" alt="dotnet">
  <img src="https://img.shields.io/badge/license-MIT-green" alt="license">
</p>

# 🧹 Disk & RAM Cleaner

A safe, bilingual (Arabic / English) Windows maintenance suite: clean disk caches, free RAM, find large & duplicate files, manage startup & processes, deep-uninstall programs, and schedule automatic cleanups. Single self-contained file — no installation.

أداة صيانة ويندوز آمنة وثنائية اللغة (عربي / إنجليزي): تنظيف كاش القرص، تحرير الرام، إيجاد الملفات الكبيرة والمكرّرة، إدارة بدء التشغيل والعمليات، إزالة عميقة للبرامج، وتنظيف مجدول. ملف واحد مستقل — بدون تنصيب.

**Author / المطوّر:** Mohammed Majid

> **v2.0** is a full rewrite in **C# / .NET 10** with a clean modular architecture. The original PowerShell version is kept in [`old/`](old).

---

## ✨ Features / المزايا

| Tab / التبويب | English | العربية |
|---|---|---|
| **Clean** | Analyze & remove 11 cache categories with charts | تحليل وحذف 11 فئة كاش مع رسوم |
| **Free RAM** | Release RAM cache (+ optional auto every 10 min) | تحرير كاش الرام (+ تلقائي اختياري) |
| **Large files** | Find the biggest files on any drive | أكبر الملفات في أي قرص |
| **Duplicates** | Detect duplicate files by SHA-256 | كشف المكرّرات بالبصمة |
| **Uninstall** | Deep uninstall + leftover cleanup (registry & files) with backup | إزالة عميقة + حذف البقايا مع نسخ احتياطي |
| **Startup** | Enable/disable startup programs (reversible) | تفعيل/تعطيل برامج الإقلاع |
| **Processes** | Top memory hogs + end task | أكثر العمليات استهلاكاً + إنهاء |
| **Schedule** | Automatic weekly cleanup (Task Scheduler) | تنظيف أسبوعي تلقائي |
| **History** | Cleanup log + total space freed | سجل + إجمالي المساحة الموفّرة |

Also: live **Arabic ⇄ English** toggle, system tray, settings persistence, activity log, restore points, self-update, and a **silent CLI**: `DiskCleaner.exe /clean /silent`.

### Safe by design / آمن بالتصميم
Never touches personal files (Downloads, Documents, Desktop, Pictures, bookmarks, passwords). Deep-uninstall leftovers are moved to a **quarantine** folder and registry keys are **exported (.reg)** before removal — nothing is hard-deleted.

---

## 🚀 Usage / التشغيل
1. Download `DiskCleaner.exe` from the [latest release](../../releases/latest).
2. Double-click and approve the admin (UAC) prompt.
3. If SmartScreen appears → **More info → Run anyway** (the app is unsigned).

The download is a **self-contained** single file — no .NET install required.

### Verify / التحقق
```powershell
(Get-FileHash .\DiskCleaner.exe -Algorithm SHA256).Hash   # قارنها بـ DiskCleaner.exe.sha256
```

---

## 🛠️ Build from source / البناء من المصدر
Requires **.NET 10 SDK**.
```powershell
dotnet publish src/DiskCleaner.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```
Or push a `v*` tag — GitHub Actions builds and publishes the release automatically.

### Project layout / هيكل المشروع
```
src/
├── Core/        Cleaner, NativeMemory, SystemInfo, LargeFilesFinder,
│                DuplicateFinder, Uninstaller, StartupManager, ProcessMonitor, Scheduler
├── Services/    Logger, AppSettings, Localization, History
├── UI/          MainForm (+ partials), Theme
└── Program.cs, Cli.cs, App.cs
```

---

## 📄 License / الرخصة
MIT — see [LICENSE](LICENSE).
