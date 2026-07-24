using System.Diagnostics;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

/// <summary>تنظيف مجدول أسبوعي عبر Windows Task Scheduler (schtasks).</summary>
public static class Scheduler
{
    private const string TaskName = "DiskCleaner Weekly";

    public static bool Exists() => Run($"/Query /TN \"{TaskName}\"") == 0;

    public static bool Enable(string exePath, string day = "SUN", string time = "02:00")
    {
        // يشغّل التنظيف الصامت أسبوعياً بأعلى صلاحية
        string args = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\" /clean /silent\" " +
                      $"/SC WEEKLY /D {day} /ST {time} /RL HIGHEST /F";
        bool ok = Run(args) == 0;
        Logger.Log($"Schedule enable: {ok}");
        return ok;
    }

    public static bool Disable()
    {
        bool ok = Run($"/Delete /TN \"{TaskName}\" /F") == 0;
        Logger.Log($"Schedule disable: {ok}");
        return ok;
    }

    private static int Run(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks.exe", args)
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            using var p = Process.Start(psi);
            if (p == null) return -1;
            p.WaitForExit(15000);
            return p.ExitCode;
        }
        catch (Exception ex) { Logger.Log($"schtasks failed: {ex.Message}"); return -1; }
    }
}
