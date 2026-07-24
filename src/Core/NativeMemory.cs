using System.Runtime.InteropServices;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

/// <summary>تحرير ذاكرة النظام: تقليم working sets ومسح standby list.</summary>
public static partial class NativeMemory
{
    [LibraryImport("psapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyWorkingSet(IntPtr hProcess);

    [LibraryImport("ntdll.dll")]
    private static partial int NtSetSystemInformation(int infoClass, IntPtr info, int length);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(IntPtr h, uint access, out IntPtr token);

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool LookupPrivilegeValueW(string? host, string name, ref long luid);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AdjustTokenPrivileges(IntPtr token, [MarshalAs(UnmanagedType.Bool)] bool disableAll,
        ref TOKEN_PRIVILEGES newState, int len, IntPtr prev, IntPtr retLen);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetCurrentProcess();

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TOKEN_PRIVILEGES { public int Count; public long Luid; public int Attr; }

    private const int SystemMemoryListInformation = 0x50;
    private const int MemoryPurgeStandbyList = 4;

    private static void EnablePrivilege(string priv)
    {
        if (!OpenProcessToken(GetCurrentProcess(), 0x28, out var tok)) return;
        var tp = new TOKEN_PRIVILEGES { Count = 1, Attr = 0x2, Luid = 0 };
        LookupPrivilegeValueW(null, priv, ref tp.Luid);
        AdjustTokenPrivileges(tok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>يحرّر أكبر قدر ممكن من الرام المخزّنة كاش.</summary>
    public static void FreeAll()
    {
        foreach (var p in System.Diagnostics.Process.GetProcesses())
        {
            try { EmptyWorkingSet(p.Handle); } catch { }
            finally { p.Dispose(); }
        }
        PurgeStandbyList();
    }

    private static void PurgeStandbyList()
    {
        try
        {
            EnablePrivilege("SeProfileSingleProcessPrivilege");
            int cmd = MemoryPurgeStandbyList;
            var h = GCHandle.Alloc(cmd, GCHandleType.Pinned);
            try { NtSetSystemInformation(SystemMemoryListInformation, h.AddrOfPinnedObject(), Marshal.SizeOf<int>()); }
            finally { h.Free(); }
        }
        catch (Exception ex) { Logger.Log($"PurgeStandbyList failed: {ex.Message}"); }
    }
}
