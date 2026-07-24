# 🧹 Disk & RAM Cleaner

A safe, bilingual (Arabic / English) Windows cleaner that frees disk space and RAM cache — single file, no installation.

أداة تنظيف ويندوز آمنة وثنائية اللغة (عربي / إنجليزي) تحرّر مساحة القرص وكاش الذاكرة — ملف واحد، بدون تنصيب.

**Author / المطوّر:** Mohammed Majid

---

## ✨ Features / المزايا

| English | العربية |
|---|---|
| Analyzes cleanable sizes before deleting | يحلّل الأحجام قبل الحذف |
| Select exactly what to clean | تختار بالضبط شنو تنظّف |
| Frees RAM cache (working sets + standby list) | يحرّر كاش الرام |
| Auto-free RAM every 10 min (optional) | تحرير رام تلقائي كل 10 دقائق (اختياري) |
| Live Arabic / English toggle | تبديل فوري عربي / إنجليزي |
| Built-in update check | فحص تحديثات مدمج |
| **Never** touches personal files | **لا يمس** ملفاتك الشخصية |

### Safe by design / آمن بالتصميم
Does **not** touch: Downloads, Documents, Desktop, Pictures, browser bookmarks or saved passwords. Only caches and temporary files are removed.

لا يمس: التنزيلات، المستندات، سطح المكتب، الصور، بوكماركس المتصفح أو كلمات السر المحفوظة. يحذف فقط الكاش والملفات المؤقتة.

---

## 🚀 Usage / التشغيل

Download `DiskCleaner.exe` from [Releases](../../releases) and double-click it. Approve the admin (UAC) prompt.

نزّل `DiskCleaner.exe` من [الإصدارات](../../releases) واضغط عليه دبل-كليك. وافق على طلب صلاحية الأدمن.

---

## 🛠️ Build from source / البناء من المصدر

Requires the [ps2exe](https://www.powershellgallery.com/packages/ps2exe) module:

```powershell
Install-Module ps2exe -Scope CurrentUser
.\build.ps1
```

---

## 🔄 Updates / التحديثات
The app checks `version.txt` in this repo and notifies you when a newer version is released. To publish an update: bump the version in `CleanApp.ps1` and `version.txt`, rebuild, and attach the new `.exe` to a GitHub Release.

البرنامج يفحص `version.txt` بالمستودع وينبّهك عند صدور إصدار أحدث.

---

## 📄 License / الرخصة
MIT — see [LICENSE](LICENSE).
