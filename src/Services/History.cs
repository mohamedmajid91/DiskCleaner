using System.Text.Json;

namespace DiskCleaner.Services;

public sealed record CleanRecord(DateTime Date, double FreedGb, string Categories);

/// <summary>سجل عمليات التنظيف + إجمالي المساحة الموفّرة.</summary>
public static class History
{
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static List<CleanRecord> Load()
    {
        try
        {
            if (File.Exists(App.HistoryFile))
                return JsonSerializer.Deserialize<List<CleanRecord>>(File.ReadAllText(App.HistoryFile)) ?? new();
        }
        catch (Exception ex) { Logger.Log($"History load failed: {ex.Message}"); }
        return new();
    }

    public static void Add(double freedGb, IEnumerable<string> categories)
    {
        try
        {
            var list = Load();
            list.Insert(0, new CleanRecord(DateTime.Now, Math.Round(freedGb, 2), string.Join(", ", categories)));
            if (list.Count > 200) list = list.Take(200).ToList();
            File.WriteAllText(App.HistoryFile, JsonSerializer.Serialize(list, _opts));
        }
        catch (Exception ex) { Logger.Log($"History add failed: {ex.Message}"); }
    }

    public static double TotalFreedGb() => Load().Sum(r => r.FreedGb);
}
