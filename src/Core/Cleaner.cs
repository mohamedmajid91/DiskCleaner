using System.Runtime.InteropServices;
using System.ServiceProcess;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

/// <summary>محرّك التنظيف: تعريف الفئات، حساب الأحجام، والحذف الآمن.</summary>
public static partial class Cleaner
{
    // ---- Shell API لسلة المحذوفات ----
    [StructLayout(LayoutKind.Sequential)]
    private struct SHQUERYRBINFO { public int cbSize; public long i64Size; public long i64NumItems; }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHQueryRecycleBinW(string? path, ref SHQUERYRBINFO info);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHEmptyRecycleBinW(IntPtr hwnd, string? path, uint flags);

    private static string L(Environment.SpecialFolder f) => Environment.GetFolderPath(f);

    public static List<CleanCategory> Build()
    {
        string lu   = L(Environment.SpecialFolder.LocalApplicationData);
        string ro   = L(Environment.SpecialFolder.ApplicationData);
        string prof = L(Environment.SpecialFolder.UserProfile);
        string ll   = Path.Combine(prof, "AppData", "LocalLow");
        string temp = Path.GetTempPath();
        string win  = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        return new()
        {
            new() { Key="temp", NameEn="Temporary files", NameAr="ملفات مؤقتة (Temp)",
                Paths=[temp, Path.Combine(lu,"Temp"), Path.Combine(win,"Temp"), Path.Combine(win,"Prefetch")] },
            new() { Key="winupdate", NameEn="Windows Update cache", NameAr="كاش تحديثات ويندوز",
                Paths=[Path.Combine(win,"SoftwareDistribution","Download")], Services=["wuauserv","bits"] },
            new() { Key="chrome", NameEn="Chrome cache", NameAr="كاش متصفح Chrome",
                Paths=[Path.Combine(lu,@"Google\Chrome\User Data\Default\Cache"), Path.Combine(lu,@"Google\Chrome\User Data\Default\Code Cache"), Path.Combine(lu,@"Google\Chrome\User Data\Default\GPUCache")] },
            new() { Key="edge", NameEn="Edge cache", NameAr="كاش متصفح Edge",
                Paths=[Path.Combine(lu,@"Microsoft\Edge\User Data\Default\Cache"), Path.Combine(lu,@"Microsoft\Edge\User Data\Default\Code Cache"), Path.Combine(lu,@"Microsoft\Edge\User Data\Default\GPUCache")] },
            new() { Key="firefox", NameEn="Firefox cache", NameAr="كاش متصفح Firefox",
                Dynamic=() => {
                    var p = Path.Combine(lu, @"Mozilla\Firefox\Profiles");
                    return Directory.Exists(p) ? Directory.GetDirectories(p).Select(d => Path.Combine(d,"cache2")) : Enumerable.Empty<string>();
                } },
            new() { Key="teams", NameEn="Microsoft Teams cache", NameAr="كاش Microsoft Teams",
                Paths=[Path.Combine(ro,@"Microsoft\Teams\Cache"), Path.Combine(ro,@"Microsoft\Teams\Code Cache"), Path.Combine(ro,@"Microsoft\Teams\GPUCache")] },
            new() { Key="discord", NameEn="Discord cache", NameAr="كاش Discord",
                Paths=[Path.Combine(ro,@"discord\Cache"), Path.Combine(ro,@"discord\Code Cache"), Path.Combine(ro,@"discord\GPUCache")] },
            new() { Key="nvidia", NameEn="NVIDIA shader cache", NameAr="كاش كرت NVIDIA",
                Paths=[Path.Combine(lu,@"NVIDIA\DXCache"), Path.Combine(lu,@"NVIDIA\GLCache"), Path.Combine(ll,@"NVIDIA\PerDriverVersion\DXCache")] },
            new() { Key="directx", NameEn="DirectX shader cache", NameAr="كاش DirectX",
                Paths=[Path.Combine(lu,"D3DSCache")] },
            new() { Key="thumbnails", NameEn="Thumbnail cache", NameAr="كاش الصور المصغّرة",
                Files=[Path.Combine(lu,@"Microsoft\Windows\Explorer\thumbcache_*.db")] },
            new() { Key="crashdumps", NameEn="Crash dumps & error reports", NameAr="تقارير الأخطاء والكراش",
                Paths=[@"C:\ProgramData\Microsoft\Windows\WER\ReportQueue", @"C:\ProgramData\Microsoft\Windows\WER\ReportArchive", Path.Combine(win,"Minidump")],
                Files=[Path.Combine(win,"MEMORY.DMP")] },
            new() { Key="winlogs", NameEn="Old Windows logs", NameAr="سجلّات ويندوز القديمة",
                Paths=[Path.Combine(win,"Logs","CBS")], Files=[Path.Combine(win,"Logs","DISM","*.log")] },
            new() { Key="recyclebin", NameEn="Recycle Bin", NameAr="سلة المحذوفات", Special=SpecialKind.RecycleBin },
        };
    }

    // ---- حساب الحجم ----
    public static long GetSize(CleanCategory c)
    {
        if (c.Special == SpecialKind.RecycleBin)
        {
            var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
            return SHQueryRecycleBinW(null, ref info) == 0 ? info.i64Size : 0;
        }
        long total = 0;
        foreach (var p in c.ResolvePaths()) total += DirSize(p);
        foreach (var pat in c.Files) total += WildcardSize(pat);
        return total;
    }

    private static long DirSize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        try
        {
            foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            { try { size += new FileInfo(f).Length; } catch { } }
        }
        catch { }
        return size;
    }

    private static long WildcardSize(string pattern)
    {
        try
        {
            var dir = Path.GetDirectoryName(pattern); var pat = Path.GetFileName(pattern);
            if (dir == null || !Directory.Exists(dir)) return 0;
            long s = 0;
            foreach (var f in Directory.EnumerateFiles(dir, pat)) { try { s += new FileInfo(f).Length; } catch { } }
            return s;
        }
        catch { return 0; }
    }

    // ---- الحذف ----
    public static void Clean(CleanCategory c)
    {
        foreach (var svc in c.Services) SetService(svc, start: false);
        try
        {
            if (c.Special == SpecialKind.RecycleBin) { SHEmptyRecycleBinW(IntPtr.Zero, null, 0x7); return; }
            foreach (var p in c.ResolvePaths()) DeleteContents(p);
            foreach (var pat in c.Files) DeleteWildcard(pat);
        }
        finally { foreach (var svc in c.Services) SetService(svc, start: true); }
    }

    private static void DeleteContents(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var f in SafeEnum(() => Directory.EnumerateFiles(path))) TryDelete(f);
        foreach (var d in SafeEnum(() => Directory.EnumerateDirectories(path)))
        { try { Directory.Delete(d, true); } catch { } }
    }

    private static void DeleteWildcard(string pattern)
    {
        var dir = Path.GetDirectoryName(pattern); var pat = Path.GetFileName(pattern);
        if (dir == null || !Directory.Exists(dir)) return;
        foreach (var f in SafeEnum(() => Directory.EnumerateFiles(dir, pat))) TryDelete(f);
    }

    private static IEnumerable<string> SafeEnum(Func<IEnumerable<string>> f)
    { try { return f().ToList(); } catch { return Enumerable.Empty<string>(); } }

    private static void TryDelete(string file)
    { try { File.SetAttributes(file, FileAttributes.Normal); File.Delete(file); } catch { } }

    private static void SetService(string name, bool start)
    {
        try
        {
            using var sc = new ServiceController(name);
            if (start && sc.Status != ServiceControllerStatus.Running) sc.Start();
            else if (!start && sc.Status == ServiceControllerStatus.Running)
            { sc.Stop(); sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8)); }
        }
        catch (Exception ex) { Logger.Log($"Service {name} {(start ? "start" : "stop")} failed: {ex.Message}"); }
    }
}
