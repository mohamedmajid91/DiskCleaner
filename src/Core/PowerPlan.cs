using System.Diagnostics;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

/// <summary>تبديل خطة الطاقة عبر powercfg (أسماء مخططات ويندوز المدمجة).</summary>
public static class PowerPlan
{
    public static void HighPerformance() => Set("SCHEME_MIN");
    public static void Balanced()        => Set("SCHEME_BALANCED");
    public static void PowerSaver()      => Set("SCHEME_MAX");

    /// <summary>GUID خطة الطاقة النشطة حالياً (للاستعادة لاحقاً).</summary>
    public static string? GetActiveScheme()
    {
        try
        {
            var psi = new ProcessStartInfo("powercfg.exe", "/getactivescheme") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            string o = p?.StandardOutput.ReadToEnd() ?? ""; p?.WaitForExit(5000);
            var m = System.Text.RegularExpressions.Regex.Match(o, @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}");
            return m.Success ? m.Value : null;
        }
        catch { return null; }
    }

    public static void RestoreScheme(string guid)
    {
        try { Process.Start(new ProcessStartInfo("powercfg.exe", "/setactive " + guid) { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000); Logger.Log($"Power plan restored -> {guid}"); }
        catch (Exception ex) { Logger.Log($"Restore plan failed: {ex.Message}"); }
    }

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
