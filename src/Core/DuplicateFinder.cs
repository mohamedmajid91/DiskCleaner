using System.Security.Cryptography;

namespace DiskCleaner.Core;

public sealed record DuplicateGroup(long Size, List<string> Files)
{
    public long Wasted => Size * (Files.Count - 1);
}

/// <summary>يكشف الملفات المكرّرة: يجمّع حسب الحجم ثم يؤكّد بالبصمة (SHA256).</summary>
public static class DuplicateFinder
{
    public static List<DuplicateGroup> Find(string root, long minSize = 1_048_576, CancellationToken ct = default)
    {
        // 1) اجمع الملفات حسب الحجم
        var bySize = new Dictionary<long, List<string>>();
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var dir = stack.Pop();
            try { foreach (var d in Directory.EnumerateDirectories(dir)) stack.Push(d); } catch { }
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(dir); } catch { continue; }
            foreach (var f in files)
            {
                try
                {
                    long len = new FileInfo(f).Length;
                    if (len < minSize) continue;
                    if (!bySize.TryGetValue(len, out var l)) bySize[len] = l = new List<string>();
                    l.Add(f);
                }
                catch { }
            }
        }

        // 2) للمجموعات ذات نفس الحجم، أكّد بالبصمة
        var result = new List<DuplicateGroup>();
        foreach (var (size, files) in bySize)
        {
            if (files.Count < 2) continue;
            ct.ThrowIfCancellationRequested();
            var byHash = new Dictionary<string, List<string>>();
            foreach (var f in files)
            {
                var h = Hash(f);
                if (h == null) continue;
                if (!byHash.TryGetValue(h, out var l)) byHash[h] = l = new List<string>();
                l.Add(f);
            }
            foreach (var g in byHash.Values)
                if (g.Count > 1) result.Add(new DuplicateGroup(size, g));
        }

        return result.OrderByDescending(g => g.Wasted).ToList();
    }

    private static string? Hash(string path)
    {
        try
        {
            using var s = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(s));
        }
        catch { return null; }
    }
}
