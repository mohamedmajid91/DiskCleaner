namespace DiskCleaner.Core;

public enum SpecialKind { None, RecycleBin, DeliveryOptimization }

/// <summary>فئة تنظيف واحدة: اسمها، مساراتها، وسلوكها الخاص.</summary>
public class CleanCategory
{
    public required string Key { get; init; }
    public required string NameEn { get; init; }
    public required string NameAr { get; init; }

    /// <summary>مسارات مجلدات يُحذف محتواها.</summary>
    public string[] Paths { get; init; } = Array.Empty<string>();

    /// <summary>أنماط ملفات مفردة (wildcards).</summary>
    public string[] Files { get; init; } = Array.Empty<string>();

    /// <summary>مسارات ديناميكية (مثل بروفايلات Firefox).</summary>
    public Func<IEnumerable<string>>? Dynamic { get; init; }

    /// <summary>خدمات توقف قبل الحذف وترجع بعده.</summary>
    public string[] Services { get; init; } = Array.Empty<string>();

    public SpecialKind Special { get; init; } = SpecialKind.None;

    public string Name(string lang) => lang == "ar" ? NameAr : NameEn;

    public IEnumerable<string> ResolvePaths()
    {
        foreach (var p in Paths) if (!string.IsNullOrEmpty(p)) yield return p;
        if (Dynamic != null)
            foreach (var p in Dynamic()) if (!string.IsNullOrEmpty(p)) yield return p;
    }
}
