using System.Diagnostics;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

/// <summary>تبديل خطة الطاقة عبر powercfg (أسماء مخططات ويندوز المدمجة).</summary>
public static class PowerPlan
{
    public static void HighPerformance() => Set("SCHEME_MIN");
    public static void Balanced()        => Set("SCHEME_BALANCED");
    public static void PowerSaver()      => Set("SCHEME_MAX");

    private static void Set(string scheme)
    {
        try
        {
            Process.Start(new ProcessStartInfo("powercfg.exe", "/setactive " + scheme)
            { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000);
            Logger.Log($"Power plan -> {scheme}");
        }
        catch (Exception ex) { Logger.Log($"Power plan failed: {ex.Message}"); }
    }
}
