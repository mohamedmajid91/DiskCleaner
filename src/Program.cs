using DiskCleaner.Services;
using DiskCleaner.UI;

namespace DiskCleaner;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // وضع صامت من سطر الأوامر (للتنظيف المجدول)
        if (Cli.WantsSilentClean(args))
            return Cli.RunSilentClean(args);

        ApplicationConfiguration.Initialize();
        try
        {
            Logger.Log($"=== Started v{App.Version} ===");
            Application.Run(new MainForm());
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Log($"FATAL: {ex}");
            MessageBox.Show(ex.Message, "Disk & RAM Cleaner", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
