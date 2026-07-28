using System.Diagnostics;
using System.ServiceProcess;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

public sealed record ServiceInfo(string Name, string Display, string Status, string StartType);

/// <summary>إدارة خدمات ويندوز: عرض، تشغيل/إيقاف، وتغيير نوع البدء.</summary>
public static class ServiceManager
{
    public static List<ServiceInfo> List()
    {
        var list = new List<ServiceInfo>();
        foreach (var s in ServiceController.GetServices())
        {
            try { list.Add(new ServiceInfo(s.ServiceName, string.IsNullOrEmpty(s.DisplayName) ? s.ServiceName : s.DisplayName, s.Status.ToString(), s.StartType.ToString())); }
            catch { }
            finally { s.Dispose(); }
        }
        return list.OrderBy(x => x.Display, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static void Start(string name)
    {
        try { using var s = new ServiceController(name); if (s.Status != ServiceControllerStatus.Running) { s.Start(); s.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10)); } Logger.Log($"Service start: {name}"); }
        catch (Exception ex) { Logger.Log($"Service start {name} failed: {ex.Message}"); }
    }

    public static void Stop(string name)
    {
        try { using var s = new ServiceController(name); if (s.CanStop && s.Status != ServiceControllerStatus.Stopped) { s.Stop(); s.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10)); } Logger.Log($"Service stop: {name}"); }
        catch (Exception ex) { Logger.Log($"Service stop {name} failed: {ex.Message}"); }
    }

    /// <summary>mode: auto | demand | disabled</summary>
    public static void SetStartup(string name, string mode)
    {
        try
        {
            Process.Start(new ProcessStartInfo("sc.exe", $"config \"{name}\" start= {mode}") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(8000);
            Logger.Log($"Service {name} startup = {mode}");
        }
        catch (Exception ex) { Logger.Log($"Service config {name} failed: {ex.Message}"); }
    }
}
