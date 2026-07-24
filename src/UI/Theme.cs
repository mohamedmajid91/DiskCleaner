using System.Drawing;

namespace DiskCleaner.UI;

/// <summary>ألوان وخطوط الواجهة (ثيم غامق حالياً، قابل للتوسّع).</summary>
public static class Theme
{
    public static Color Dark    = Color.FromArgb(28, 30, 38);
    public static Color Panel   = Color.FromArgb(40, 43, 52);
    public static Color Accent  = Color.FromArgb(0, 150, 136);
    public static Color AccentH = Color.FromArgb(0, 180, 164);
    public static Color Purple  = Color.FromArgb(120, 80, 200);
    public static Color PurpleH = Color.FromArgb(140, 100, 220);
    public static Color Gray    = Color.FromArgb(55, 60, 70);
    public static Color GrayH   = Color.FromArgb(70, 76, 88);
    public static Color TextCol = Color.White;
    public static Color Muted   = Color.FromArgb(165, 172, 185);
    public static Color Link    = Color.FromArgb(90, 170, 255);

    public static readonly Font Main = new("Segoe UI", 10F);
    public static readonly Font Bold = new("Segoe UI", 11F, FontStyle.Bold);
    public static readonly Font TitleFont = new("Segoe UI", 15F, FontStyle.Bold);

    public static readonly Color[] Palette =
    {
        Color.FromArgb(0,180,164),  Color.FromArgb(140,100,220), Color.FromArgb(235,170,50),
        Color.FromArgb(225,90,90),  Color.FromArgb(80,165,225),  Color.FromArgb(120,205,120),
        Color.FromArgb(210,120,185),Color.FromArgb(170,165,90),  Color.FromArgb(90,200,195),
        Color.FromArgb(215,110,70), Color.FromArgb(150,130,225),  Color.FromArgb(110,190,160),
        Color.FromArgb(225,195,90),
    };

    public static string FormatSize(long b) => b switch
    {
        >= 1L << 30 => $"{b / 1073741824.0:N2} GB",
        >= 1L << 20 => $"{b / 1048576.0:N1} MB",
        >= 1L << 10 => $"{b / 1024.0:N0} KB",
        _ => $"{b} B"
    };
}
