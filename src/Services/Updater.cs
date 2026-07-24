using System.Diagnostics;
using System.Net.Http;

namespace DiskCleaner.Services;

/// <summary>تحديث ذاتي: يقارن الإصدار، ينزّل النسخة الجديدة، ويستبدل نفسه ثم يعيد التشغيل.</summary>
public static class Updater
{
    private static string VersionUrl => $"https://raw.githubusercontent.com/{App.RepoOwner}/{App.RepoName}/main/version.txt";
    private static string ExeUrl     => $"https://github.com/{App.RepoOwner}/{App.RepoName}/releases/latest/download/DiskCleaner.exe";

    /// <summary>يرجّع أحدث إصدار من GitHub، أو null عند الفشل.</summary>
    public static async Task<string?> GetLatestVersionAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            return (await http.GetStringAsync(VersionUrl)).Trim();
        }
        catch (Exception ex) { Logger.Log($"Update check failed: {ex.Message}"); return null; }
    }

    /// <summary>ينزّل الملف التنفيذي الجديد إلى المسار المحدّد مع تقدّم.</summary>
    public static async Task DownloadAsync(string dest, IProgress<int>? progress, CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        using var resp = await http.GetAsync(ExeUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        long total = resp.Content.Headers.ContentLength ?? -1L;

        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using var fs = File.Create(dest);
        var buffer = new byte[81920];
        long read = 0; int n;
        while ((n = await src.ReadAsync(buffer, ct)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, n), ct);
            read += n;
            if (total > 0) progress?.Report((int)(read * 100 / total));
        }
    }

    /// <summary>يكتب سكربت استبدال ينتظر خروج البرنامج ثم يبدّل الملف ويعيد التشغيل.</summary>
    public static void ApplyAndRestart(string newExe)
    {
        string exe = App.ExePath;
        string cmd = Path.Combine(App.DataDir, "update.cmd");
        string script =
            "@echo off\r\n" +
            "timeout /t 1 /nobreak >nul\r\n" +
            ":retry\r\n" +
            $"move /y \"{newExe}\" \"{exe}\" >nul 2>&1\r\n" +
            "if errorlevel 1 ( timeout /t 1 /nobreak >nul & goto retry )\r\n" +
            $"start \"\" \"{exe}\"\r\n" +
            "del \"%~f0\"\r\n";
        File.WriteAllText(cmd, script);
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{cmd}\"") { UseShellExecute = false, CreateNoWindow = true });
        Logger.Log("Update: applying and restarting");
    }
}
