using System.Diagnostics;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

public sealed record TaskInfo(string Name, string Status, string NextRun);

/// <summary>مدير المهام المجدولة (طرف ثالث) عبر schtasks: عرض، تفعيل/تعطيل، حذف.</summary>
public static class ScheduledTasks
{
    public static List<TaskInfo> List()
    {
        var outp = Run("/query /fo CSV /nh /v", capture: true);
        var map = new Dictionary<string, TaskInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in outp.Split('\n'))
        {
            var line = raw.Trim(); if (line.Length == 0) continue;
            var p = ParseCsv(line);
            if (p.Count < 4) continue;
            string name = p[1];                       // العمود الثاني = TaskName (مع /v)
            if (string.IsNullOrWhiteSpace(name) || name.Equals("TaskName", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.StartsWith(@"\Microsoft", StringComparison.OrdinalIgnoreCase)) continue;  // تخطّي مهام النظام
            string next = p[2]; string status = p[3];
            map[name] = new TaskInfo(name, status, next);
        }
        return map.Values.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static void Enable(string name, bool enable) { Run($"/change /tn \"{name}\" {(enable ? "/enable" : "/disable")}"); Logger.Log($"Task {name} {(enable ? "enabled" : "disabled")}"); }
    public static void Delete(string name) { Run($"/delete /tn \"{name}\" /f"); Logger.Log($"Task deleted: {name}"); }

    private static string Run(string args, bool capture = false)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks.exe", args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = capture };
            using var p = Process.Start(psi);
            string o = capture ? (p?.StandardOutput.ReadToEnd() ?? "") : "";
            p?.WaitForExit(15000);
            return o;
        }
        catch (Exception ex) { Logger.Log($"schtasks failed: {ex.Message}"); return ""; }
    }

    private static List<string> ParseCsv(string line)
    {
        var res = new List<string>(); var cur = new System.Text.StringBuilder(); bool q = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { if (q && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; } else q = !q; }
            else if (c == ',' && !q) { res.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(c);
        }
        res.Add(cur.ToString());
        return res;
    }
}
