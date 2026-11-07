// Text rules, file-type rules and saved settings (values in MECHANICS.md).
using System.Globalization;

namespace Wplus;

public enum Kind { Music, Video, Subtitle, AudioBook, Ringtone, Book }

public enum Theme { Light, Dark }

public static class Logic
{
    // ---- sizes ----------------------------------------------------------------
    static readonly string[] Units = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    public const double SizeBase = 1000.0;
    public const int SizeDecimals = 1;
    public const int SizeDecimalsAboveGb = 2;

    static double RoundTo(double v, int d) => Math.Round(v, d, MidpointRounding.ToEven);

    /// <summary>Divide by 1000 while the rounded value is &gt;= 1000; trailing zeros dropped.</summary>
    public static string FormatSize(ulong bytes)
    {
        int d = SizeDecimals;
        double v = bytes;
        int unit = 0;
        while (RoundTo(v, d) >= SizeBase && unit < Units.Length - 1)
        {
            v /= SizeBase;
            unit++;
        }
        if (unit > 3) d = SizeDecimalsAboveGb;
        string fmt = "0." + new string('#', d);
        return RoundTo(v, d).ToString(fmt, CultureInfo.InvariantCulture) + " " + Units[unit];
    }

    // ---- device capacity label -------------------------------------------------
    public const double Gib = 1073741824.0;
    static readonly (long Max, string Label)[] CapacityBuckets =
    {
        (8, "8 Gb"), (16, "16 Gb"), (32, "32 Gb"), (64, "64 Gb"), (128, "128 Gb"),
        (256, "256 Gb"), (512, "512 Gb"), (1024, "1 Tb"), (2048, "2 Tb"),
    };

    public static string CapacityText(ulong totalBytes)
    {
        long gib = (long)Math.Round(totalBytes / Gib, MidpointRounding.ToEven);
        if (gib <= 0) return "";
        foreach (var (max, label) in CapacityBuckets)
            if (gib <= max) return label;
        return "";
    }

    // ---- names and counts -------------------------------------------------------
    public const int NameMax = 28;
    public const int NameKeep = 25;

    public static string ShortName(string name) =>
        name.Length > NameMax ? name.Substring(0, NameKeep) + "..." : name;

    public static string FilesText(int n) => n == 1 ? "1 file" : $"{n} files";

    // ---- content types -------------------------------------------------------
    public static readonly string[] Audio = { "mp3", "aac", "m4a", "wav", "flac", "ape", "aif", "aiff", "wma", "ogg", "oga", "dff", "tta", "tak", "wv", "dsf", "cue" };
    public static readonly string[] Video = { "mp4", "m4v", "mov", "avi", "mkv", "wmv", "dv", "m2v", "webm", "mpeg", "mpg", "flv", "3gp", "m2ts", "ts", "mpe", "mts", "rm", "rmvb", "vob" };
    public static readonly string[] Subtitles = { "srt", "ass", "ssa", "sub" };
    public static readonly string[] AudioBooks = { "m4b" };
    public static readonly string[] Ringtones = { "m4r" };
    public static readonly string[] Books = { "epub", "pdf" };

    public static Kind? KindOf(string path)
    {
        string e = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        if (Audio.Contains(e)) return Kind.Music;
        if (Video.Contains(e)) return Kind.Video;
        if (Subtitles.Contains(e)) return Kind.Subtitle;
        if (AudioBooks.Contains(e)) return Kind.AudioBook;
        if (Ringtones.Contains(e)) return Kind.Ringtone;
        if (Books.Contains(e)) return Kind.Book;
        return null;
    }

    public static IEnumerable<string> AllSupported() =>
        Audio.Concat(AudioBooks).Concat(Video).Concat(Subtitles).Concat(Ringtones).Concat(Books);

    static string Pattern(IEnumerable<string> exts) => string.Join(";", exts.Select(e => "*." + e));

    /// <summary>File-picker filters for library/device tiles.</summary>
    public static string SupportedFilter() =>
        $"All supported files|{Pattern(AllSupported())}|Audio files|{Pattern(Audio)}|Audiobooks|{Pattern(AudioBooks)}|Video files|{Pattern(Video)}|All files (*.*)|*.*";

    // ---- folders --------------------------------------------------------------
    static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string DefaultLocalFolder() => Path.Combine(Home, "Documents", "Wplus");

    /// <summary>The Library tile files each kind into the user's own folders.</summary>
    public static string LibraryFolder(Kind kind) => kind switch
    {
        Kind.Music => Path.Combine(Home, "Music", "Wplus"),
        Kind.AudioBook => Path.Combine(Home, "Music", "Wplus", "Audiobooks"),
        Kind.Ringtone => Path.Combine(Home, "Music", "Wplus", "Ringtones"),
        Kind.Video or Kind.Subtitle => Path.Combine(Home, "Videos", "Wplus"),
        _ => Path.Combine(Home, "Documents", "Wplus Books"),
    };

    public static List<string> Expand(IEnumerable<string> paths)
    {
        var outList = new List<string>();
        foreach (var p in paths)
        {
            if (Directory.Exists(p))
            {
                try { outList.AddRange(Expand(Directory.GetFileSystemEntries(p).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))); }
                catch { /* unreadable folder: skip */ }
            }
            else if (File.Exists(p)) outList.Add(p);
        }
        return outList;
    }
}

public sealed class Settings
{
    public Theme Theme = Theme.Light;
    public string LocalFolder = Logic.DefaultLocalFolder();
    public bool ShowLibrary = true;

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wplus", "settings.txt");

    public static Settings Load()
    {
        var s = new Settings();
        try
        {
            foreach (var line in File.ReadAllLines(FilePath))
            {
                int i = line.IndexOf('=');
                if (i < 0) continue;
                string k = line[..i].Trim(), v = line[(i + 1)..].Trim();
                switch (k)
                {
                    case "theme": s.Theme = v == "dark" ? Theme.Dark : Theme.Light; break;
                    case "local_folder" when v.Length > 0: s.LocalFolder = v; break;
                    case "show_library": s.ShowLibrary = v != "false"; break;
                }
            }
        }
        catch { /* first run */ }
        return s;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath,
                $"theme={(Theme == Theme.Dark ? "dark" : "light")}\nlocal_folder={LocalFolder}\nshow_library={(ShowLibrary ? "true" : "false")}\n");
        }
        catch { /* not fatal */ }
    }
}
