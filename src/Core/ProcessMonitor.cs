using System.Diagnostics;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

public sealed record ProcInfo(int Pid, string Name, long Memory);

/// <summary>أكثر العمليات استهلاكاً للذاكرة، مع إمكانية الإنهاء.</summary>
public static class ProcessMonitor
{
    public static List<ProcInfo> Top(int count = 20)
    {
        var list = new List<ProcInfo>();
        foreach (var p in Process.GetProcesses())
        {
            try { list.Add(new ProcInfo(p.Id, p.ProcessName, p.WorkingSet64)); }
            catch { }
            finally { p.Dispose(); }
        }
        return list.OrderByDescending(p => p.Memory).Take(count).ToList();
    }

    public static bool Kill(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            p.Kill(true);
            Logger.Log($"Killed process {p.ProcessName} ({pid})");
            return true;
        }
        catch (Exception ex) { Logger.Log($"Kill {pid} failed: {ex.Message}"); return false; }
    }
}
