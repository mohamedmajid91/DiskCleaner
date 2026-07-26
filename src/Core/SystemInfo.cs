using System.Runtime.InteropServices;

namespace DiskCleaner.Core;

/// <summary>معلومات القرص والذاكرة.</summary>
public static class SystemInfo
{
    [StructLayout(LayoutKind.Sequential)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

    private static long _prevIdle, _prevKernel, _prevUser;
    private static bool _havePrev;

    /// <summary>نسبة استخدام المعالج الكلي % (يُستدعى دورياً؛ أول استدعاء يرجّع 0 للتهيئة).</summary>
    public static int GetCpuUsage()
    {
        if (!GetSystemTimes(out long idle, out long kernel, out long user)) return 0;
        if (!_havePrev) { _prevIdle = idle; _prevKernel = kernel; _prevUser = user; _havePrev = true; return 0; }
        long idleDiff = idle - _prevIdle, kernDiff = kernel - _prevKernel, userDiff = user - _prevUser;
        _prevIdle = idle; _prevKernel = kernel; _prevUser = user;
        long total = kernDiff + userDiff;          // kernel يتضمّن وقت الخمول
        if (total <= 0) return 0;
        double usage = (total - idleDiff) * 100.0 / total;
        return (int)Math.Clamp(usage, 0, 100);
    }

    public static double GetFreeGB(string drive = "C:\\")
    {
        try { return Math.Round(new DriveInfo(drive).AvailableFreeSpace / 1073741824.0, 2); }
        catch { return 0; }
    }

    /// <summary>يرجّع (نسبة الاستخدام %, الرام الفارغة GB).</summary>
    public static (int usedPct, double freeGb) GetRam()
    {
        var m = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(m))
            return ((int)m.dwMemoryLoad, Math.Round(m.ullAvailPhys / 1073741824.0, 2));
        return (0, 0);
    }

    /// <summary>أقراص ثابتة متاحة (C:, D:, ...).</summary>
    public static IEnumerable<DriveInfo> FixedDrives() =>
        DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady);
}
