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
