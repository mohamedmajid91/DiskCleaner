namespace DiskCleaner.Core;

public sealed record FileEntry(string Path, long Size);

/// <summary>يبحث عن أكبر الملفات في قرص/مجلد (تعداد آمن يتجاوز مجلدات ممنوعة).</summary>
public static class LargeFilesFinder
{
    public static List<FileEntry> Find(string root, int top = 50, CancellationToken ct = default)
    {
        var all = new List<FileEntry>(4096);
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
                try { all.Add(new FileEntry(f, new FileInfo(f).Length)); } catch { }
            }
        }

        return all.OrderByDescending(x => x.Size).Take(top).ToList();
    }
}
