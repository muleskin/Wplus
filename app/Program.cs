// Wplus - drag-and-drop media transfer utility (C#).
// No licensing, trial or activation of any kind.
//
//   Wplus.exe                         normal start
//   Wplus.exe --screenshot out.png    draw ~120 frames, save the window picture, exit
//   Wplus.exe --demo                  start with busy example states
//   Wplus.exe --empty                 start with no device
//   Wplus.exe --selftest              check the text, file-type and copy rules, exit 0/1
namespace Wplus;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Contains("--selftest")) return SelfTest.Run();

        string? shot = null;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--screenshot") shot = args[i + 1];

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(shot, args.Contains("--demo"), args.Contains("--empty")));
        return 0;
    }
}

static class SelfTest
{
    static int _failed;

    static void Eq<T>(T actual, T expected, string what)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {what}: {actual}{(ok ? "" : $" (expected {expected})")}");
        if (!ok) _failed++;
    }

    public static int Run()
    {
        // size text
        Eq(Logic.FormatSize(512), "512 bytes", "size 512");
        Eq(Logic.FormatSize(38_400_000), "38.4 MB", "size 38.4 MB");
        Eq(Logic.FormatSize(842_000_000), "842 MB", "size 842 MB");
        Eq(Logic.FormatSize(999_960), "1 MB", "size rounds up a unit");
        Eq(Logic.FormatSize(128_000_000_000), "128 GB", "size 128 GB");
        Eq(Logic.FormatSize(1_234_000_000_000), "1.23 TB", "size 2 decimals past GB");
        // capacity label
        Eq(Logic.CapacityText(128_000_000_000), "128 Gb", "capacity 128");
        Eq(Logic.CapacityText(64_000_000_000), "64 Gb", "capacity 64");
        Eq(Logic.CapacityText(15_000_000_000), "16 Gb", "capacity 16");
        Eq(Logic.CapacityText(1_000_000_000_000), "1 Tb", "capacity 1 Tb");
        Eq(Logic.CapacityText(0), "", "capacity none");
        // names and kinds
        Eq(Logic.ShortName("Short"), "Short", "short name");
        Eq(Logic.ShortName("A Very Long Device Name That Goes On"), "A Very Long Device Name T...", "long name cut");
        Eq(Logic.KindOf("a.M4R"), Kind.Ringtone, "m4r");
        Eq(Logic.KindOf("a.m4b"), Kind.AudioBook, "m4b");
        Eq(Logic.KindOf("a.epub"), Kind.Book, "epub");
        Eq(Logic.KindOf("a.docx"), (Kind?)null, "docx unsupported");
        // copying never overwrites
        string baseDir = Path.Combine(Path.GetTempPath(), $"wplus-selftest-{Environment.ProcessId}");
        try
        {
            string src = Path.Combine(baseDir, "src", "song.mp3"), dst = Path.Combine(baseDir, "dst");
            Directory.CreateDirectory(Path.GetDirectoryName(src)!);
            File.WriteAllBytes(src, new byte[3_000_000]);
            for (int i = 0; i < 2; i++)
            {
                var job = CopyJob.Start(new() { (src, dst) });
                while (!job.IsFinished) Thread.Sleep(5);
                Eq(job.Error, null, $"copy {i + 1} error");
                Eq(job.Fraction, 1f, $"copy {i + 1} complete");
            }
            Eq(CopyJob.FileLen(Path.Combine(dst, "song.mp3")), 3_000_000L, "first copy");
            Eq(CopyJob.FileLen(Path.Combine(dst, "song (2).mp3")), 3_000_000L, "second copy renamed");
        }
        finally { try { Directory.Delete(baseDir, true); } catch { } }

        Console.WriteLine(_failed == 0 ? "all checks passed" : $"{_failed} check(s) failed");
        return _failed == 0 ? 0 : 1;
    }
}
