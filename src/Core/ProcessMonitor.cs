using System.Diagnostics;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

public sealed record ProcInfo(int Pid, string Name, long Memory, double Cpu);

/// <summary>مراقبة العمليات: أعلى استهلاك للمعالج/الذاكرة، تغيير الأولوية، والإنهاء.</summary>
public static class ProcessMonitor
{
    /// <summary>يرجّع أعلى العمليات مع نسبة CPU% (عيّنة قصيرة).</summary>
    public static List<ProcInfo> Top(int count = 30, int sampleMs = 400)
    {
        var snap = new Dictionary<int, (TimeSpan cpu, Process p, string name, long mem)>();
        foreach (var p in Process.GetProcesses())
        {
            try { snap[p.Id] = (p.TotalProcessorTime, p, p.ProcessName, p.WorkingSet64); }
            catch { p.Dispose(); }
        }

        Thread.Sleep(sampleMs);
        int cores = Environment.ProcessorCount;
        var list = new List<ProcInfo>();
        foreach (var kv in snap)
        {
            var (cpu0, p, name, _) = kv.Value;
            try
            {
                p.Refresh();
                double ms = (p.TotalProcessorTime - cpu0).TotalMilliseconds;
                double pct = Math.Max(0, Math.Round(ms / (sampleMs * (double)cores) * 100.0, 1));
                list.Add(new ProcInfo(kv.Key, name, p.WorkingSet64, pct));
            }
            catch { }
            finally { p.Dispose(); }
        }
        return list.OrderByDescending(x => x.Cpu).ThenByDescending(x => x.Memory).Take(count).ToList();
    }

    public static bool SetPriority(int pid, ProcessPriorityClass cls)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            p.PriorityClass = cls;
            Logger.Log($"Priority {p.ProcessName} ({pid}) = {cls}");
            return true;
        }
        catch (Exception ex) { Logger.Log($"SetPriority {pid} failed: {ex.Message}"); return false; }
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
