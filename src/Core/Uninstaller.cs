using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

public sealed record InstalledApp(
    string Name, string Publisher, string Version, string InstallLocation,
    string UninstallCmd, string QuietUninstallCmd, long SizeKb, string RegSubKey, string Hive);

public enum LeftoverKind { File, Directory, Registry }
public sealed record Leftover(LeftoverKind Kind, string Path, long Size);

/// <summary>إزالة عميقة: قائمة البرامج، تشغيل الإزالة الرسمية، وفحص/حذف البقايا مع نسخ احتياطي.</summary>
public static partial class Uninstaller
{
    private const string UninstallPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","and","for","inc","llc","ltd","corporation","corp","version","edition",
        "software","update","updater","microsoft","windows","google","common","program",
        "files","x86","x64","help","tools","setup","installer","service"
    };

    // ============================ قائمة البرامج ============================
    public static List<InstalledApp> ListInstalled()
    {
        var apps = new Dictionary<string, InstalledApp>(StringComparer.OrdinalIgnoreCase);
        ReadFrom(RegistryHive.LocalMachine, RegistryView.Registry64, "HKLM", apps);
        ReadFrom(RegistryHive.LocalMachine, RegistryView.Registry32, "HKLM", apps);
        ReadFrom(RegistryHive.CurrentUser,  RegistryView.Default,     "HKCU", apps);
        return apps.Values.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void ReadFrom(RegistryHive hive, RegistryView view, string label, Dictionary<string, InstalledApp> apps)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var root = baseKey.OpenSubKey(UninstallPath);
            if (root == null) return;
            foreach (var sub in root.GetSubKeyNames())
            {
                try
                {
                    using var k = root.OpenSubKey(sub);
                    if (k == null) continue;
                    string name = k.GetValue("DisplayName") as string ?? "";
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if ((k.GetValue("SystemComponent") as int?) == 1) continue;
                    string uninst = k.GetValue("UninstallString") as string ?? "";
                    string quiet  = k.GetValue("QuietUninstallString") as string ?? "";
                    if (string.IsNullOrEmpty(uninst) && string.IsNullOrEmpty(quiet)) continue;
                    long sizeKb = (k.GetValue("EstimatedSize") as int?) ?? 0;
                    var app = new InstalledApp(
                        name,
                        k.GetValue("Publisher") as string ?? "",
                        k.GetValue("DisplayVersion") as string ?? "",
                        k.GetValue("InstallLocation") as string ?? "",
                        uninst, quiet, sizeKb, sub, label);
                    apps.TryAdd(name + "|" + app.Version, app);
                }
                catch { }
            }
        }
        catch (Exception ex) { Logger.Log($"Uninstall list {label} failed: {ex.Message}"); }
    }

    // ============================ الإزالة الرسمية ============================
    public static void RunNativeUninstaller(InstalledApp app)
    {
        var cmd = !string.IsNullOrEmpty(app.QuietUninstallCmd) ? app.QuietUninstallCmd : app.UninstallCmd;
        if (string.IsNullOrEmpty(cmd)) return;
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd) { UseShellExecute = false };
            using var p = Process.Start(psi);
            p?.WaitForExit();
            Logger.Log($"Native uninstall: {app.Name}");
        }
        catch (Exception ex) { Logger.Log($"Native uninstall failed for {app.Name}: {ex.Message}"); }
    }

    // ============================ فحص البقايا (عميق) ============================
    public static List<Leftover> ScanLeftovers(InstalledApp app)
    {
        var found = new List<Leftover>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tokens = Tokens(app.Name).ToList();
        if (tokens.Count == 0) return found;

        // 1) مجلد التثبيت
        if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
            AddDir(app.InstallLocation);

        // 2) مجلدات مطابقة في المسارات الشائعة
        string[] roots =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        };
        foreach (var r in roots)
        {
            if (string.IsNullOrEmpty(r) || !Directory.Exists(r)) continue;
            try
            {
                foreach (var d in Directory.EnumerateDirectories(r))
                    if (Match(Path.GetFileName(d), tokens)) AddDir(d);
            }
            catch { }
        }

        // 3) بقايا الرجستري (المستوى الأول من Software فقط — محافظ)
        ScanRegRoot(RegistryHive.CurrentUser,  RegistryView.Default,     @"Software", "HKCU", tokens, found, seen);
        ScanRegRoot(RegistryHive.LocalMachine, RegistryView.Registry64,  @"Software", "HKLM", tokens, found, seen);
        ScanRegRoot(RegistryHive.LocalMachine, RegistryView.Registry32,  @"Software\WOW6432Node", "HKLM", tokens, found, seen);

        // 4) مفتاح الإزالة الخاص بالبرنامج
        string ownKey = app.Hive == "HKCU"
            ? $@"HKCU\{UninstallPath}\{app.RegSubKey}"
            : $@"HKLM\{UninstallPath}\{app.RegSubKey}";
        if (seen.Add(ownKey)) found.Add(new Leftover(LeftoverKind.Registry, ownKey, 0));

        return found;

        void AddDir(string path)
        {
            if (seen.Add(path)) found.Add(new Leftover(LeftoverKind.Directory, path, DirSize(path)));
        }
    }

    private static void ScanRegRoot(RegistryHive hive, RegistryView view, string sub, string label,
        List<string> tokens, List<Leftover> found, HashSet<string> seen)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var root = baseKey.OpenSubKey(sub);
            if (root == null) return;
            foreach (var name in root.GetSubKeyNames())
            {
                if (!Match(name, tokens)) continue;
                string full = $@"{label}\{sub}\{name}";
                if (seen.Add(full)) found.Add(new Leftover(LeftoverKind.Registry, full, 0));
            }
        }
        catch { }
    }

    // ============================ حذف البقايا (مع نسخ احتياطي) ============================
    public static (int removed, string backupDir) RemoveLeftovers(IEnumerable<Leftover> items)
    {
        string backup = Path.Combine(App.DataDir, "Quarantine", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backup);
        int n = 0;
        foreach (var it in items)
        {
            try
            {
                switch (it.Kind)
                {
                    case LeftoverKind.Directory:
                        var destD = Path.Combine(backup, SafeName(it.Path));
                        Directory.Move(it.Path, destD); n++; break;
                    case LeftoverKind.File:
                        File.Move(it.Path, Path.Combine(backup, Path.GetFileName(it.Path)), true); n++; break;
                    case LeftoverKind.Registry:
                        ExportAndDeleteReg(it.Path, backup); n++; break;
                }
                Logger.Log($"Leftover removed: {it.Kind} {it.Path}");
            }
            catch (Exception ex) { Logger.Log($"Leftover remove failed {it.Path}: {ex.Message}"); }
        }
        return (n, backup);
    }

    private static void ExportAndDeleteReg(string display, string backup)
    {
        // نسخة احتياطية عبر reg export
        var regFile = Path.Combine(backup, SafeName(display) + ".reg");
        try
        {
            var psi = new ProcessStartInfo("reg.exe", $"export \"{display}\" \"{regFile}\" /y")
            { UseShellExecute = false, CreateNoWindow = true };
            Process.Start(psi)?.WaitForExit(15000);
        }
        catch { }
        // الحذف عبر الـ API
        int i = display.IndexOf('\\');
        if (i < 0) return;
        string prefix = display[..i]; string subPath = display[(i + 1)..];
        var baseKey = prefix.Equals("HKCU", StringComparison.OrdinalIgnoreCase)
            ? RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
            : RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        try { baseKey.DeleteSubKeyTree(subPath, throwOnMissingSubKey: false); } finally { baseKey.Dispose(); }
    }

    // ============================ أدوات ============================
    private static IEnumerable<string> Tokens(string name) =>
        NonAlnum().Split(name).Where(t => t.Length >= 4 && !Stop.Contains(t)).Select(t => t.ToLowerInvariant()).Distinct();

    private static bool Match(string candidate, List<string> tokens)
    {
        var n = candidate.ToLowerInvariant();
        return tokens.Any(t => n.Contains(t));
    }

    private static long DirSize(string path)
    {
        long s = 0;
        try { foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) { try { s += new FileInfo(f).Length; } catch { } } }
        catch { }
        return s;
    }

    private static string SafeName(string path)
    {
        var name = path.Replace('\\', '_').Replace(':', '_').Replace('/', '_');
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Length > 120 ? name[^120..] : name;
    }

    [GeneratedRegex(@"[^A-Za-z0-9]+")]
    private static partial Regex NonAlnum();
}
