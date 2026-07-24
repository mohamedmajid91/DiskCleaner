using DiskCleaner.Core;
using DiskCleaner.Services;

namespace DiskCleaner;

/// <summary>وضع التشغيل الصامت من سطر الأوامر: DiskCleaner.exe /clean /silent</summary>
public static class Cli
{
    public static bool WantsSilentClean(string[] args) =>
        args.Any(a => a.Equals("/clean", StringComparison.OrdinalIgnoreCase));

    public static int RunSilentClean(string[] args)
    {
        try
        {
            Logger.Log("CLI silent clean started");
            var settings = AppSettings.Load();
            var cats = Cleaner.Build().Where(c => !settings.Unchecked.Contains(c.Key)).ToList();

            double before = SystemInfo.GetFreeGB();
            foreach (var c in cats) Cleaner.Clean(c);
            double after = SystemInfo.GetFreeGB();
            double freed = Math.Round(after - before, 2);

            History.Add(freed, cats.Select(c => c.Key));
            Logger.Log($"CLI clean done. Freed {freed} GB");
            return 0;
        }
        catch (Exception ex) { Logger.Log($"CLI error: {ex}"); return 1; }
    }
}
