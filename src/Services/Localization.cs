namespace DiskCleaner.Services;

/// <summary>الترجمة ثنائية اللغة (قابلة للتوسّع للغات أخرى).</summary>
public static class Loc
{
    public static string Lang { get; set; } = "en";
    public static bool IsRtl => Lang == "ar";

    // key => (en, ar)
    private static readonly Dictionary<string, (string en, string ar)> S = new()
    {
        ["title"]        = ("Disk & RAM Cleaner", "منظّف القرص والذاكرة"),
        ["diskFree"]     = ("C: free", "قرص C: فارغ"),
        ["ramUsed"]      = ("RAM used", "الرام مستخدمة"),
        ["free"]         = ("free", "فارغ"),
        ["analyze"]      = ("Analyze", "تحليل"),
        ["cleanSel"]     = ("Clean Selected", "تنظيف المحدد"),
        ["freeRam"]      = ("Free RAM", "تحرير الذاكرة (RAM)"),
        ["close"]        = ("Close", "إغلاق"),
        ["autoRam"]      = ("Auto-free RAM every 10 min (while app is open)", "تحرير الرام تلقائياً كل 10 دقائق (طالما البرنامج مفتوح)"),
        ["restore"]      = ("Create a restore point before cleaning", "إنشاء نقطة استعادة قبل التنظيف"),
        ["totalClean"]   = ("Total cleanable", "المجموع القابل للتنظيف"),
        ["pressAnalyze"] = ("Press (Analyze) to calculate sizes", "اضغط (تحليل) لحساب الأحجام"),
        ["analyzing"]    = ("Analyzing", "جاري تحليل"),
        ["cleaning"]     = ("Cleaning", "جاري تنظيف"),
        ["doneAnalyze"]  = ("Analysis done. Select items and clean.", "التحليل خلص. اختر الفئات واضغط تنظيف."),
        ["doneClean"]    = ("Cleaning done.", "خلص التنظيف."),
        ["freeingRam"]   = ("Freeing RAM...", "جاري تحرير الذاكرة..."),
        ["ramDone"]      = ("RAM freed.", "تم تحرير الذاكرة."),
        ["restoring"]    = ("Creating restore point...", "جاري إنشاء نقطة استعادة..."),
        ["autoOn"]       = ("Auto-free enabled (every 10 min).", "التحرير التلقائي مُفعّل (كل 10 دقائق)."),
        ["autoOff"]      = ("Auto-free disabled.", "التحرير التلقائي متوقّف."),
        ["noSelect"]     = ("No category selected.", "ما اخترت ولا فئة."),
        ["warn"]         = ("Warning", "تنبيه"),
        ["confirmTitle"] = ("Confirm cleanup", "تأكيد التنظيف"),
        ["willDelete"]   = ("Will be deleted:", "راح ينحذف:"),
        ["permanent"]    = ("This is permanent (Recycle Bin emptied). Continue?", "هذا حذف نهائي (سلة المحذوفات تنفرغ). متأكد؟"),
        ["resultTitle"]  = ("Result", "النتيجة"),
        ["cleanOk"]      = ("Cleanup successful.", "تم التنظيف بنجاح."),
        ["before"]       = ("Before", "قبل"),
        ["after"]        = ("After", "بعد"),
        ["freed"]        = ("Freed", "تم تحرير"),
        ["ramTitle"]     = ("Memory (RAM)", "الذاكرة (RAM)"),
        ["used"]         = ("used", "مستخدمة"),
        ["checkUpdate"]  = ("Check for updates", "التحقق من التحديثات"),
        ["updTitle"]     = ("Updates", "التحديثات"),
        ["updAvail"]     = ("A new version is available", "يتوفّر إصدار جديد"),
        ["updLatest"]    = ("You are on the latest version.", "أنت على أحدث إصدار."),
        ["updFail"]      = ("Could not check for updates.", "تعذّر التحقق من التحديثات."),
        ["updInstall"]   = ("Download and install the update now? The app will restart.", "تنزيل وتثبيت التحديث الآن؟ سيُعاد تشغيل البرنامج تلقائياً."),
        ["downloading"]  = ("Downloading update", "جاري تنزيل التحديث"),
        ["updReady"]     = ("Installing and restarting...", "جاري التثبيت وإعادة التشغيل..."),
        ["langBtn"]      = ("عربي", "English"),
        ["trayShow"]     = ("Show app", "إظهار البرنامج"),
        ["trayRam"]      = ("Free RAM now", "تحرير الذاكرة الآن"),
        ["trayExit"]     = ("Exit", "خروج"),
        ["trayMin"]      = ("Running in the background", "يعمل بالخلفية"),

        // التبويبات
        ["tabDashboard"] = ("Dashboard", "الرئيسية"),
        ["tabClean"]     = ("Clean", "تنظيف"),
        ["tabLarge"]     = ("Large files", "الملفات الكبيرة"),
        ["tabDup"]       = ("Duplicates", "المكرّرات"),
        ["tabStartup"]   = ("Startup", "بدء التشغيل"),
        ["tabProc"]      = ("Processes", "العمليات"),
        ["tabSchedule"]  = ("Schedule", "الجدولة"),
        ["tabHistory"]   = ("History", "السجل"),

        // أزرار وأعمدة مشتركة
        ["drive"]        = ("Drive", "القرص"),
        ["scan"]         = ("Scan", "فحص"),
        ["stop"]         = ("Stop", "إيقاف"),
        ["deleteSel"]    = ("Delete selected", "حذف المحدد"),
        ["refresh"]      = ("Refresh", "تحديث"),
        ["kill"]         = ("End task", "إنهاء العملية"),
        ["enableItem"]   = ("Enable", "تفعيل"),
        ["disableItem"]  = ("Disable", "تعطيل"),
        ["scanning"]     = ("Scanning...", "جاري الفحص..."),
        ["scanDone"]     = ("Scan done.", "خلص الفحص."),
        ["noItems"]      = ("Nothing found.", "ما في نتائج."),
        ["confirmDelete"]= ("Permanently delete the selected files?", "حذف الملفات المحددة نهائياً؟"),
        ["confirmKill"]  = ("End the selected process?", "إنهاء العملية المحددة؟"),
        ["enabled"]      = ("Enabled", "مُفعّل"),
        ["disabled"]     = ("Disabled", "معطّل"),

        // أعمدة
        ["colName"]      = ("Name", "الاسم"),
        ["colSize"]      = ("Size", "الحجم"),
        ["colPath"]      = ("Path", "المسار"),
        ["colMem"]       = ("Memory", "الذاكرة"),
        ["colPid"]       = ("PID", "المعرّف"),
        ["colCount"]     = ("Copies", "نسخ"),
        ["colWasted"]    = ("Wasted", "مهدور"),
        ["colStatus"]    = ("Status", "الحالة"),
        ["colCommand"]   = ("Command", "الأمر"),
        ["colScope"]     = ("Scope", "النطاق"),
        ["colDate"]      = ("Date", "التاريخ"),
        ["colFreed"]     = ("Freed", "المحرّر"),
        ["colCats"]      = ("Items", "العناصر"),

        // الجدولة
        ["schedOn"]      = ("Weekly cleanup is ON (Sundays 02:00).", "التنظيف الأسبوعي مُفعّل (الأحد 02:00)."),
        ["schedOff"]     = ("Weekly cleanup is OFF.", "التنظيف الأسبوعي متوقّف."),
        ["enableWeekly"] = ("Enable weekly cleanup", "تفعيل التنظيف الأسبوعي"),
        ["disableWeekly"]= ("Disable weekly cleanup", "تعطيل التنظيف الأسبوعي"),
        ["schedInfo"]    = ("Runs a silent cleanup automatically every week.", "ينفّذ تنظيفاً صامتاً تلقائياً كل أسبوع."),

        // السجل
        ["totalFreed"]   = ("Total space freed", "إجمالي المساحة الموفّرة"),
        ["clearHistory"] = ("Clear history", "مسح السجل"),

        // إلغاء التثبيت (إزالة عميقة)
        ["tabUninstall"] = ("Uninstall", "إلغاء التثبيت"),
        ["uRefresh"]     = ("Refresh list", "تحديث القائمة"),
        ["uUninstall"]   = ("Uninstall", "إلغاء التثبيت"),
        ["uScanLeft"]    = ("Scan leftovers", "فحص البقايا"),
        ["uRemoveLeft"]  = ("Remove leftovers (backup)", "حذف البقايا (نسخ احتياطي)"),
        ["colPublisher"] = ("Publisher", "الناشر"),
        ["colVersion"]   = ("Version", "الإصدار"),
        ["colType"]      = ("Type", "النوع"),
        ["confirmUninstall"] = ("Run the official uninstaller for this program?", "تشغيل أداة الإزالة الرسمية لهذا البرنامج؟"),
        ["uninstalling"] = ("Uninstalling...", "جاري الإزالة..."),
        ["scanningLeft"] = ("Scanning for leftovers...", "جاري فحص البقايا..."),
        ["leftFound"]    = ("Leftovers found - review and remove", "بقايا موجودة - راجعها واحذفها"),
        ["noLeft"]       = ("No leftovers found.", "ما في بقايا."),
        ["confirmRemoveLeft"] = ("Move the selected leftovers to backup (quarantine)?\nA registry backup (.reg) is saved first.", "نقل البقايا المحددة للنسخ الاحتياطي (حجر)؟\nيُحفظ نسخ احتياطي للرجستري (.reg) أولاً."),
        ["removedLeft"]  = ("Removed {0} leftovers. Backup:", "تم حذف {0} من البقايا. النسخة الاحتياطية:"),
        ["selectAppFirst"] = ("Select a program first.", "اختر برنامجاً أولاً."),
        ["kindFile"]     = ("File", "ملف"),
        ["kindDir"]      = ("Folder", "مجلد"),
        ["kindReg"]      = ("Registry", "رجستري"),

        // المستخدمون والمجموعات
        ["tabUsers"]     = ("Users & Groups", "المستخدمون والمجموعات"),
        ["newUser"]      = ("New user", "مستخدم جديد"),
        ["delUser"]      = ("Delete", "حذف"),
        ["resetPwd"]     = ("Reset password", "تغيير كلمة السر"),
        ["enableUser"]   = ("Enable", "تفعيل"),
        ["disableUser"]  = ("Disable", "تعطيل"),
        ["colFullName"]  = ("Full name", "الاسم الكامل"),
        ["colNeverExp"]  = ("Never expires", "لا تنتهي"),
        ["colDesc"]      = ("Description", "الوصف"),
        ["promptUserName"]= ("New user name:", "اسم المستخدم الجديد:"),
        ["promptPassword"]= ("Password:", "كلمة السر:"),
        ["promptFullName"]= ("Full name (optional):", "الاسم الكامل (اختياري):"),
        ["confirmDelUser"]= ("Delete this local user account?", "حذف حساب المستخدم المحلي هذا؟"),
        ["group"]        = ("Group", "المجموعة"),
        ["members"]      = ("Members", "الأعضاء"),
        ["addMember"]    = ("Add selected user", "إضافة المستخدم المحدد"),
        ["removeMember"] = ("Remove selected member", "إزالة العضو المحدد"),
        ["done"]         = ("Done.", "تم."),
        ["yes2"]         = ("Yes", "نعم"),
        ["renameUser"]   = ("Rename", "إعادة تسمية"),
        ["promptNewName"]= ("New account name:", "اسم الحساب الجديد:"),
        ["cantRenameSelf"]=("You can't rename the account you're currently signed in with.", "ما تگدر تعيد تسمية الحساب اللي داخل بيه حالياً."),

        // المعالج (CPU)
        ["colCpu"]       = ("CPU", "المعالج"),
        ["prioNormal"]   = ("Normal priority", "أولوية عادية"),
        ["prioBelow"]    = ("Lower priority", "أولوية أقل"),
        ["prioIdle"]     = ("Idle priority", "أولوية خاملة"),
        ["powerPlan"]    = ("Power plan:", "خطة الطاقة:"),
        ["powerHigh"]    = ("High performance", "أداء عالٍ"),
        ["powerBalanced"]= ("Balanced", "متوازن"),
        ["cpuHigh"]      = ("High CPU load", "حمل عالٍ على المعالج"),
        ["cores"]        = ("cores", "نواة"),
        ["cpuHistory"]   = ("CPU usage", "استخدام المعالج"),
        ["searchProc"]   = ("Search process...", "بحث عن عملية..."),
    };

    /// <summary>يرجّع النص حسب اللغة الحالية.</summary>
    public static string T(string key)
    {
        if (S.TryGetValue(key, out var v))
            return Lang == "ar" ? v.ar : v.en;
        return key;
    }
}
