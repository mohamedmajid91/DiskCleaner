using System.Text.Json;

namespace DiskCleaner.Services;

/// <summary>إعدادات المستخدم المحفوظة بين الجلسات (JSON في %AppData%).</summary>
public class AppSettings
{
    public string Lang { get; set; } = "en";
    public bool AutoRam { get; set; } = false;
    public bool RestorePoint { get; set; } = false;
    public List<string> Unchecked { get; set; } = new();   // فئات ألغى المستخدم اختيارها
    public string Theme { get; set; } = "dark";

    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(App.SettingsFile))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(App.SettingsFile)) ?? new AppSettings();
        }
        catch (Exception ex) { Logger.Log($"Settings load failed: {ex.Message}"); }
        return new AppSettings();
    }

    public void Save()
    {
        try { File.WriteAllText(App.SettingsFile, JsonSerializer.Serialize(this, _opts)); }
        catch (Exception ex) { Logger.Log($"Settings save failed: {ex.Message}"); }
    }
}
