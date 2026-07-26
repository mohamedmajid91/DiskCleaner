using System.Drawing;

namespace DiskCleaner.UI;

/// <summary>ألوان وخطوط الواجهة (ثيم غامق حالياً، قابل للتوسّع).</summary>
public static class Theme
{
    // لوحة ألوان احترافية محايدة: أزرق أساسي هادئ + رماديات
    public static Color Dark    = Color.FromArgb(30, 31, 34);   // خلفية
    public static Color Panel   = Color.FromArgb(43, 45, 49);   // ألواح
    public static Color TitleBar= Color.FromArgb(34, 37, 45);   // شريط العنوان
    public static Color Accent  = Color.FromArgb(47, 111, 235); // أزرق أساسي
    public static Color AccentH = Color.FromArgb(66, 133, 244);
    public static Color Purple  = Color.FromArgb(45, 122, 90);  // أخضر (زر الرام) - اسم قديم محتفظ به
    public static Color PurpleH = Color.FromArgb(56, 146, 110);
    public static Color Gray    = Color.FromArgb(55, 58, 64);
    public static Color GrayH   = Color.FromArgb(72, 76, 84);
    public static Color TextCol = Color.White;
    public static Color Muted   = Color.FromArgb(150, 156, 166);
    public static Color Link    = Color.FromArgb(96, 165, 240);

    public static readonly Font Main = new("Segoe UI", 10F);
    public static readonly Font Bold = new("Segoe UI", 11F, FontStyle.Bold);
    public static readonly Font TitleFont = new("Segoe UI", 15F, FontStyle.Bold);

    // ألوان الرسوم: مجموعة هادئة احترافية (مو زاهية)
    public static readonly Color[] Palette =
    {
        Color.FromArgb(47,111,235),  Color.FromArgb(45,140,110),  Color.FromArgb(205,160,60),
        Color.FromArgb(196,92,92),   Color.FromArgb(96,124,196),  Color.FromArgb(118,166,120),
        Color.FromArgb(150,112,180), Color.FromArgb(158,158,102), Color.FromArgb(84,158,168),
        Color.FromArgb(186,120,84),  Color.FromArgb(110,138,196),  Color.FromArgb(102,166,146),
        Color.FromArgb(198,176,92),
    };

    public static string FormatSize(long b) => b switch
    {
        >= 1L << 30 => $"{b / 1073741824.0:N2} GB",
        >= 1L << 20 => $"{b / 1048576.0:N1} MB",
        >= 1L << 10 => $"{b / 1024.0:N0} KB",
        _ => $"{b} B"
    };
}
