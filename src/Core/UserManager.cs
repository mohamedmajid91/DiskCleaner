using System.DirectoryServices.AccountManagement;
using DiskCleaner.Services;

namespace DiskCleaner.Core;

public sealed record LocalUserInfo(string Name, string FullName, string Description, bool Enabled, bool PasswordNeverExpires, DateTime? LastLogon);
public sealed record LocalGroupInfo(string Name, string Description);

/// <summary>إدارة المستخدمين والمجموعات المحليين (مثل lusrmgr) عبر AccountManagement.</summary>
public static class UserManager
{
    private static PrincipalContext Machine() => new(ContextType.Machine);

    // ---------------- المستخدمون ----------------
    public static List<LocalUserInfo> ListUsers()
    {
        var list = new List<LocalUserInfo>();
        try
        {
            using var ctx = Machine();
            using var searcher = new PrincipalSearcher(new UserPrincipal(ctx));
            foreach (var p in searcher.FindAll())
            {
                if (p is not UserPrincipal u) continue;
                try
                {
                    list.Add(new LocalUserInfo(
                        u.SamAccountName ?? u.Name ?? "",
                        u.DisplayName ?? "",
                        u.Description ?? "",
                        u.Enabled ?? true,
                        u.PasswordNeverExpires,
                        u.LastLogon));
                }
                catch { }
                finally { u.Dispose(); }
            }
        }
        catch (Exception ex) { Logger.Log($"ListUsers failed: {ex.Message}"); }
        return list.OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static void CreateUser(string name, string password, string fullName, string description)
    {
        using var ctx = Machine();
        using var u = new UserPrincipal(ctx) { Name = name, DisplayName = fullName, Description = description, Enabled = true };
        u.SetPassword(password);
        u.Save();
        Logger.Log($"User created: {name}");
    }

    public static void DeleteUser(string name)
    {
        using var ctx = Machine();
        using var u = UserPrincipal.FindByIdentity(ctx, name) ?? throw new InvalidOperationException("User not found");
        u.Delete();
        Logger.Log($"User deleted: {name}");
    }

    public static void SetPassword(string name, string password)
    {
        using var ctx = Machine();
        using var u = UserPrincipal.FindByIdentity(ctx, name) ?? throw new InvalidOperationException("User not found");
        u.SetPassword(password);
        u.Save();
        Logger.Log($"Password reset: {name}");
    }

    public static void SetEnabled(string name, bool enabled)
    {
        using var ctx = Machine();
        using var u = UserPrincipal.FindByIdentity(ctx, name) ?? throw new InvalidOperationException("User not found");
        u.Enabled = enabled; u.Save();
        Logger.Log($"User {name} enabled={enabled}");
    }

    public static void SetPasswordNeverExpires(string name, bool value)
    {
        using var ctx = Machine();
        using var u = UserPrincipal.FindByIdentity(ctx, name) ?? throw new InvalidOperationException("User not found");
        u.PasswordNeverExpires = value; u.Save();
        Logger.Log($"User {name} pwdNeverExpires={value}");
    }

    // ---------------- المجموعات ----------------
    public static List<LocalGroupInfo> ListGroups()
    {
        var list = new List<LocalGroupInfo>();
        try
        {
            using var ctx = Machine();
            using var searcher = new PrincipalSearcher(new GroupPrincipal(ctx));
            foreach (var p in searcher.FindAll())
            {
                if (p is not GroupPrincipal g) continue;
                try { list.Add(new LocalGroupInfo(g.SamAccountName ?? g.Name ?? "", g.Description ?? "")); }
                catch { } finally { g.Dispose(); }
            }
        }
        catch (Exception ex) { Logger.Log($"ListGroups failed: {ex.Message}"); }
        return list.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static List<string> GroupMembers(string group)
    {
        var members = new List<string>();
        try
        {
            using var ctx = Machine();
            using var g = GroupPrincipal.FindByIdentity(ctx, group);
            if (g == null) return members;
            foreach (var m in g.GetMembers()) { try { members.Add(m.SamAccountName ?? m.Name ?? ""); } catch { } finally { m.Dispose(); } }
        }
        catch (Exception ex) { Logger.Log($"GroupMembers failed: {ex.Message}"); }
        return members.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static void AddToGroup(string user, string group)
    {
        using var ctx = Machine();
        using var g = GroupPrincipal.FindByIdentity(ctx, group) ?? throw new InvalidOperationException("Group not found");
        g.Members.Add(ctx, IdentityType.SamAccountName, user);
        g.Save();
        Logger.Log($"Added {user} to {group}");
    }

    public static void RemoveFromGroup(string user, string group)
    {
        using var ctx = Machine();
        using var g = GroupPrincipal.FindByIdentity(ctx, group) ?? throw new InvalidOperationException("Group not found");
        g.Members.Remove(ctx, IdentityType.SamAccountName, user);
        g.Save();
        Logger.Log($"Removed {user} from {group}");
    }
}
