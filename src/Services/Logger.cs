namespace DiskCleaner.Services;

/// <summary>سجل بسيط يكتب الأحداث والأخطاء إلى ملف نصّي.</summary>
public static class Logger
{
    private static readonly object _lock = new();

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
                File.AppendAllText(App.LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
        }
        catch { /* التسجيل لا يجب أن يُسقط التطبيق أبداً */ }
    }
}
