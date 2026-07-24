using Microsoft.Win32;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

public sealed record StartupItem(string Name, string Command, string Scope, bool Enabled);

/// <summary>يقرأ ويعطّل برامج بدء التشغيل (مفاتيح Run + StartupApproved، تعطيل قابل للتراجع).</summary>
public static class StartupManager
{
    private const string RunHkcu = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunHklm = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedHkcu = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public static List<StartupItem> List()
    {
        var items = new List<StartupItem>();
        Read(Registry.CurrentUser, RunHkcu, "HKCU", items);
        Read(Registry.LocalMachine, RunHklm, "HKLM", items);
        return items;
    }

    private static void Read(RegistryKey root, string sub, string scope, List<StartupItem> items)
    {
        try
        {
            using var run = root.OpenSubKey(sub);
            if (run == null) return;
            using var approved = Registry.CurrentUser.OpenSubKey(ApprovedHkcu);
            foreach (var name in run.GetValueNames())
            {
                string cmd = run.GetValue(name)?.ToString() ?? "";
                bool enabled = true;
                var b = approved?.GetValue(name) as byte[];
                if (b is { Length: > 0 }) enabled = (b[0] & 0x01) == 0;   // فردي => معطّل
                items.Add(new StartupItem(name, cmd, scope, enabled));
            }
        }
        catch (Exception ex) { Logger.Log($"Startup read {scope} failed: {ex.Message}"); }
    }

    /// <summary>يفعّل/يعطّل عنصر بدء تشغيل (يكتب StartupApproved، بدون حذف مفتاح Run).</summary>
    public static void SetEnabled(string name, bool enable)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(ApprovedHkcu, true);
            var b = (k?.GetValue(name) as byte[]) ?? new byte[12];
            if (b.Length < 12) b = new byte[12];
            b[0] = (byte)(enable ? 0x02 : 0x03);
            k?.SetValue(name, b, RegistryValueKind.Binary);
            Logger.Log($"Startup '{name}' {(enable ? "enabled" : "disabled")}");
        }
        catch (Exception ex) { Logger.Log($"Startup set '{name}' failed: {ex.Message}"); }
    }
}
