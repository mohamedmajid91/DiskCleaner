using System.Diagnostics;

namespace DiskCleaner;

/// <summary>ثوابت التطبيق ومساراته.</summary>
public static class App
{
    public const string Version   = "2.1.2";
    public const string RepoOwner = "mohamedmajid91";
    public const string RepoName  = "DiskCleaner";
    public const string Author    = "Mohammed Majid";

    public static string DataDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiskCleaner");

    public static string SettingsFile => Path.Combine(DataDir, "settings.json");
    public static string LogFile      => Path.Combine(DataDir, "log.txt");
    public static string HistoryFile  => Path.Combine(DataDir, "history.json");

    // Environment.ProcessPath يرجّع مسار الـ EXE الحقيقي (يعمل بشكل صحيح مع single-file)
    public static string ExePath => Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";

    static App()
    {
        try { Directory.CreateDirectory(DataDir); } catch { }
    }
}
