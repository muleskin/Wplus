// Background file copying for the Local Folder and Library tiles.
namespace Wplus;

public sealed class CopyJob
{
    public ulong Total { get; }
    public int Files { get; }
    long _done;
    volatile bool _finished;
    volatile bool _cancel;
    volatile string? _error;

    CopyJob(List<(string Src, string Dir)> pairs)
    {
        Files = pairs.Count;
        Total = (ulong)pairs.Sum(p => FileLen(p.Src));
    }

    public static CopyJob Start(List<(string Src, string Dir)> pairs)
    {
        var job = new CopyJob(pairs);
        var t = new Thread(() =>
        {
            foreach (var (src, dir) in pairs)
            {
                if (job._cancel) break;
                try { job.CopyOne(src, dir); }
                catch (Exception e)
                {
                    job._error = $"{Path.GetFileName(src)}: {e.Message}";
                    break;
                }
            }
            job._finished = true;
        }) { IsBackground = true, Name = "Wplus copy" };
        t.Start();
        return job;
    }

    public float Fraction =>
        Total == 0 ? (_finished ? 1f : 0f) : (float)Math.Min(1.0, Interlocked.Read(ref _done) / (double)Total);
    public bool IsFinished => _finished;
    public bool WasCancelled => _cancel;
    public string? Error => _error;
    public void Cancel() => _cancel = true;

    public static long FileLen(string p)
    {
        try { return new FileInfo(p).Length; } catch { return 0; }
    }

    /// <summary>"name.ext", then "name (2).ext", "name (3).ext"... so nothing is overwritten.</summary>
    static string FreeName(string dir, string src)
    {
        string stem = Path.GetFileNameWithoutExtension(src), ext = Path.GetExtension(src);
        string candidate = Path.Combine(dir, stem + ext);
        for (int n = 2; File.Exists(candidate); n++)
            candidate = Path.Combine(dir, $"{stem} ({n}){ext}");
        return candidate;
    }

    void CopyOne(string src, string dir)
    {
        Directory.CreateDirectory(dir);
        string dest = FreeName(dir, src);
        var buf = new byte[1 << 20];
        using (var input = File.OpenRead(src))
        using (var output = File.Create(dest))
        {
            int n;
            while ((n = input.Read(buf, 0, buf.Length)) > 0)
            {
                if (_cancel) break;
                output.Write(buf, 0, n);
                Interlocked.Add(ref _done, n);
            }
        }
        if (_cancel) { try { File.Delete(dest); } catch { } }
    }
}
