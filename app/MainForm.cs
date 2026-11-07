// Wplus main window: header, the side column and device tiles, drag-and-drop,
// file picking and the --screenshot option. Every number comes from
// MECHANICS.md / mechanics.json.
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace Wplus;

public sealed class MainForm : Form
{
    // ---- mechanics sheet ----------------------------------------------------
    public const float WinW = 736f;
    public const float WinH = 464f;
    const float TileRadius = 14f;
    public const int SettingsColumnW = 144;

    const float Gap = 12f;
    const float HalfW = 228f;
    const float FullW = 424f;
    const float AreaH = 376f;
    const float QuarterH = 182f;

    const float LargeIcon = 118f;
    const float LargeIconTop = 84f;
    const float LargeIconBottom = 42f;
    const float LargeTitle = 18f;
    const float SmallIcon = 64f;
    const float SmallIconTop = 36f;
    const float SmallIconBottom = 24f;
    const float SmallTitle = 14f;
    static readonly (float Top, float Bottom) ArrowLarge = (28f, 56f);
    static readonly (float Top, float Bottom) ArrowSmall = (22f, 8f);
    const float ArrowThreshold = 100f;

    // timings in milliseconds
    const double ProgressGlideMs = 900;
    const double TileResizeMs = 450;
    const double OverlayFadeMs = 150;
    const double FirstFadeDelayMs = 250;
    const double FirstFadeMs = 750;

    // empty device area (main window layout data @0x211b4f1)
    const string NoDeviceText = "Connect your device via USB or wake from sleep for Wi-Fi";
    const float NoDeviceTextSize = 18f;
    const float NoDeviceMarginSide = 80f;
    const float NoDeviceMarginBottom = 20f;
    const double RingCycleMs = 2000;
    static readonly float[] RingSizes = { 23f, 39f, 55f };
    static readonly double[] RingDelaysMs = { 0, 400, 600 };
    static readonly float[] RingScales = { 1.12f, 1.08f, 1.08f };

    public const int MaxDevices = 4;
    const double DemoSpeed = 25_000_000; // bytes per second (estimated)
    const double PreparingS = 0.8; // seconds (estimated)

    // placement: side column 228 wide; Library tile at y=194 inside it
    const float HeaderH = 64f; // estimated
    const float SideX = Gap;
    const float AreaX = Gap + HalfW + Gap;
    const float LibraryY = 194f;

    static readonly Color Success = Color.FromArgb(0x13, 0xcd, 0x00);
    static readonly Color Warning = Color.FromArgb(0xfe, 0xd6, 0x0a);
    static readonly Color Alert = Color.FromArgb(0xfd, 0x46, 0x3a);
    static readonly Color Accent = Color.FromArgb(0x17, 0x71, 0xfc);

    sealed class Palette
    {
        public Color Window, Tile, TileHover, Text, Muted, Track, Overlay, Shadow, EmptyBorder, EmptyText;
    }

    static Palette PaletteFor(Theme t) => t == Theme.Light
        ? new Palette
        {
            // WindowBackgroundColor #d9ffffff sits on a blurred backdrop; drawn on light grey (estimated)
            Window = Color.FromArgb(0xf6, 0xf6, 0xf6),
            Tile = Color.FromArgb(0xff, 0xff, 0xff),
            TileHover = Color.FromArgb(0xfa, 0xfa, 0xfa),
            Text = Color.FromArgb(0x4c, 0x4c, 0x4c),
            Muted = Color.FromArgb(0x97, 0x97, 0x97),
            Track = Color.FromArgb(0xd8, 0xd8, 0xd8),
            Overlay = Color.FromArgb(0xff, 0xff, 0xff, 0xff),
            Shadow = Color.FromArgb(28, 0, 0, 0),
            EmptyBorder = Color.FromArgb(0x97, 0x97, 0x97),
            EmptyText = Color.FromArgb(0x70, 0x70, 0x70),
        }
        : new Palette
        {
            Window = Color.FromArgb(0x1f, 0x1f, 0x1f),
            Tile = Color.FromArgb(0x2f, 0x2f, 0x2f),
            TileHover = Color.FromArgb(0x32, 0x32, 0x32),
            Text = Color.FromArgb(0xc9, 0xc9, 0xc9),
            Muted = Color.FromArgb(0x97, 0x97, 0x97),
            Track = Color.FromArgb(0x40, 0x40, 0x40),
            Overlay = Color.FromArgb(0xff, 0x2f, 0x2f, 0x2f),
            Shadow = Color.FromArgb(90, 0, 0, 0),
            EmptyBorder = Color.FromArgb(0x55, 0x55, 0x55),
            EmptyText = Color.FromArgb(0x97, 0x97, 0x97),
        };

    static double EaseOut(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return 1 - Math.Pow(1 - t, 3);
    }

    sealed class Anim
    {
        float _from, _to;
        double _t0;
        readonly double _dur;
        public Anim(float v, double durMs) { _from = _to = v; _dur = durMs / 1000.0; }
        public float Target => _to;
        public float Get(double now) => _dur <= 0 ? _to : _from + (_to - _from) * (float)EaseOut((now - _t0) / _dur);
        public void Set(float v, double now)
        {
            if (Math.Abs(v - _to) > float.Epsilon) { _from = Get(now); _to = v; _t0 = now; }
        }
        public void Jump(float v) { _from = _to = v; }
    }

    /// <summary>Device tile positions inside the 468 x 376 device area.</summary>
    static RectangleF[] DeviceLayout(int n)
    {
        float x2 = HalfW + Gap, y2 = QuarterH + Gap;
        return n switch
        {
            1 => new[] { new RectangleF(0, 0, FullW, AreaH) },
            2 => new[] { new RectangleF(0, 0, HalfW, AreaH), new RectangleF(x2, 0, HalfW, AreaH) },
            3 => new[] { new RectangleF(0, 0, HalfW, AreaH), new RectangleF(x2, 0, HalfW, QuarterH), new RectangleF(x2, y2, HalfW, QuarterH) },
            4 => new[]
            {
                new RectangleF(0, 0, HalfW, QuarterH), new RectangleF(x2, 0, HalfW, QuarterH),
                new RectangleF(0, y2, HalfW, QuarterH), new RectangleF(x2, y2, HalfW, QuarterH),
            },
            _ => Array.Empty<RectangleF>(),
        };
    }

    // ---- model ------------------------------------------------------------------
    public sealed class Device
    {
        public int Id;
        public string Name = "";
        public ulong Capacity, Free;
        public bool Wifi, Locked;
    }

    enum State { Normal, Preparing, Transferring, Success, Error, NotEnoughSpace, Untrusted }

    sealed class SimWork { public ulong Total; public double Done; public int Files; }

    sealed class Tile
    {
        public string Key = "";            // "folder", "library" or "dev:<id>"
        public int DeviceId = -1;
        public bool IsDevice => DeviceId >= 0;
        public State State = State.Normal;
        public double PrepUntil;
        public string ErrorText = "";
        public CopyJob? Copy;
        public SimWork? Sim;
        public List<string> Pending = new();
        public int LastCount;
        public string? LastDir;
        public string? Note;
        public readonly Anim Progress = new(0, ProgressGlideMs);
        public readonly Anim Overlay = new(0, OverlayFadeMs);
        public readonly Anim[] R = { new(0, TileResizeMs), new(0, TileResizeMs), new(0, TileResizeMs), new(0, TileResizeMs) };
        public double Born;
        public bool First;
        public bool Busy => State is State.Preparing or State.Transferring;

        public void SetRect(RectangleF r, double now, bool snap)
        {
            float[] v = { r.X, r.Y, r.Width, r.Height };
            for (int i = 0; i < 4; i++) { if (snap) R[i].Jump(v[i]); else R[i].Set(v[i], now); }
        }
        public RectangleF Rect(double now) => new(R[0].Get(now), R[1].Get(now), R[2].Get(now), R[3].Get(now));
        public float Appear(double now)
        {
            double delay = First ? FirstFadeDelayMs / 1000 : 0, dur = (First ? FirstFadeMs : TileResizeMs) / 1000;
            return (float)EaseOut((now - Born - delay) / dur);
        }
    }

    // ---- state --------------------------------------------------------------------
    public readonly Settings AppSettings = Settings.Load();
    public readonly List<Device> Devices = new();
    readonly List<Tile> _tiles = new();
    int _nextId = 1;
    string? _lastHover;
    PointF _mouse = new(-1, -1);
    bool _dragging;
    PointF _dragPoint;
    readonly List<(RectangleF Rect, Action Act)> _links = new();
    readonly List<(RectangleF Rect, Tile Tile)> _tileHits = new();
    readonly Stopwatch _clock = Stopwatch.StartNew();
    double _lastNow;
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    readonly string? _shotPath;
    readonly bool _demo;
    int _frame;
    readonly Bitmap _logo;
    readonly float _scale;
    readonly Dictionary<(float, bool), Font> _fonts = new();

    double Now => _clock.Elapsed.TotalSeconds;

    public MainForm(string? shotPath, bool demo, bool empty)
    {
        _shotPath = shotPath;
        _demo = demo;
        Text = "Wplus";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        DoubleBuffered = true;
        AllowDrop = true;
        _scale = DeviceDpi / 96f;
        ClientSize = new Size((int)Math.Round(WinW * _scale), (int)Math.Round(WinH * _scale));

        using (var s = typeof(MainForm).Assembly.GetManifestResourceStream("wplus.ico")!)
        {
            Icon = new Icon(s);
        }
        using (var s = typeof(MainForm).Assembly.GetManifestResourceStream("wplus.ico")!)
        using (var big = new Icon(s, 64, 64))
        {
            _logo = big.ToBitmap();
        }

        _tiles.Add(new Tile { Key = "folder" });
        _tiles.Add(new Tile { Key = "library" });
        if (!empty) AddDevice("Demo Phone", 128_000_000_000, 41_700_000_000, false, 0);
        else Relayout(0, true);

        if (demo && Devices.Count > 0)
        {
            AddDevice("A Very Long Tablet Name For Testing", 256_000_000_000, 190_300_000_000, true, 0);
            var t = _tiles[2];
            t.Sim = new SimWork { Total = 3_400_000_000, Done = 1_500_000_000, Files = 12 };
            t.LastCount = 12;
            t.State = State.Transferring;
            _tiles[0].State = State.Success;
            _tiles[0].LastCount = 3;
        }

        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
    }

    // ---- devices and layout -----------------------------------------------------------
    public void AddDevice(string name, ulong capacity, ulong free, bool wifi, double now)
    {
        if (Devices.Count >= MaxDevices) return;
        int id = _nextId++;
        bool first = Devices.Count == 0;
        Devices.Add(new Device { Id = id, Name = name, Capacity = capacity, Free = free, Wifi = wifi });
        _tiles.Add(new Tile { Key = $"dev:{id}", DeviceId = id, Born = now, First = first });
        Relayout(now, true);
    }

    public void AddDemoDevice()
    {
        int n = _nextId;
        AddDevice($"Demo Device {n}", 64_000_000_000, 22_400_000_000, n % 2 == 0, Now);
    }

    public void RemoveDevice(int id)
    {
        Devices.RemoveAll(d => d.Id == id);
        _tiles.RemoveAll(t => t.DeviceId == id);
        Relayout(Now, false);
    }

    /// <summary>Local Folder is 376 tall alone, 182 with the Library tile below it at y=194 (450 ms).</summary>
    public void Relayout(double now, bool snapSide)
    {
        float folderH = AppSettings.ShowLibrary ? QuarterH : AreaH;
        var side0 = new RectangleF(SideX, HeaderH, HalfW, folderH);
        var side1 = new RectangleF(SideX, HeaderH + LibraryY, HalfW, QuarterH);
        var slots = DeviceLayout(Devices.Count);
        int di = 0;
        foreach (var t in _tiles)
        {
            if (t.Key == "folder") t.SetRect(side0, now, snapSide);
            else if (t.Key == "library") t.SetRect(side1, now, snapSide);
            else
            {
                var r = slots[di++];
                r.Offset(AreaX, HeaderH);
                bool fresh = t.R[2].Target == 0;
                t.SetRect(r, now, fresh);
            }
        }
    }

    public void SettingsChanged(bool relayout)
    {
        AppSettings.Save();
        if (relayout) Relayout(Now, false);
        Invalidate();
    }

    Device? Dev(int id) => Devices.FirstOrDefault(d => d.Id == id);
    bool IsShown(Tile t) => t.Key != "library" || AppSettings.ShowLibrary;

    string Title(Tile t) => t.Key switch
    {
        "folder" => "Local Folder",
        "library" => "Library",
        _ => Logic.ShortName(Dev(t.DeviceId)?.Name ?? ""),
    };

    // ---- transfers -----------------------------------------------------------------
    void Accept(Tile t, IEnumerable<string> paths, double now)
    {
        var files = Logic.Expand(paths);
        if (files.Count == 0 || t.Busy) return;
        List<string> ok, skipped;
        if (t.Key == "folder") { ok = files; skipped = new(); }
        else
        {
            ok = files.Where(f => Logic.KindOf(f) != null).ToList();
            skipped = files.Where(f => Logic.KindOf(f) == null).ToList();
        }
        t.Note = skipped.Count == 0 ? null : $"{Logic.FilesText(skipped.Count)} not supported";
        if (ok.Count == 0) { t.State = State.Error; t.ErrorText = "These files aren't supported."; return; }
        if (t.IsDevice && Dev(t.DeviceId)?.Locked == true) { t.State = State.Untrusted; return; }
        t.Progress.Jump(0);
        t.LastCount = ok.Count;
        switch (t.Key)
        {
            case "folder":
                t.LastDir = AppSettings.LocalFolder;
                t.Copy = CopyJob.Start(ok.Select(p => (p, AppSettings.LocalFolder)).ToList());
                t.State = State.Transferring;
                break;
            case "library":
                var pairs = ok.Select(p => (p, Logic.LibraryFolder(Logic.KindOf(p) ?? Kind.Music))).ToList();
                t.LastDir = pairs[0].Item2;
                t.Copy = CopyJob.Start(pairs);
                t.State = State.Transferring;
                break;
            default:
                t.Pending = ok;
                t.PrepUntil = now + PreparingS;
                t.State = State.Preparing;
                break;
        }
    }

    void Step(double now, double dt)
    {
        foreach (var t in _tiles)
        {
            if (t.State == State.Preparing && now >= t.PrepUntil)
            {
                var files = t.Pending;
                t.Pending = new();
                ulong total = (ulong)files.Sum(CopyJob.FileLen);
                ulong free = t.IsDevice ? Dev(t.DeviceId)?.Free ?? 0 : ulong.MaxValue;
                if (total > free) t.State = State.NotEnoughSpace;
                else
                {
                    t.Sim = new SimWork { Total = Math.Max(total, 1), Files = files.Count };
                    t.State = State.Transferring;
                }
            }
            else if (t.State == State.Transferring)
            {
                if (t.Copy is { } job)
                {
                    t.Progress.Set(job.Fraction, now);
                    if (job.IsFinished)
                    {
                        t.Copy = null;
                        if (job.WasCancelled) t.State = State.Normal;
                        else if (job.Error is { } e) { t.State = State.Error; t.ErrorText = e; }
                        else t.State = State.Success;
                    }
                }
                else if (t.Sim is { } sim)
                {
                    sim.Done += DemoSpeed * dt;
                    t.Progress.Set((float)Math.Min(1.0, sim.Done / sim.Total), now);
                    if (sim.Done >= sim.Total && t.Progress.Get(now) >= 0.999f)
                    {
                        t.Sim = null;
                        t.State = State.Success;
                        if (Dev(t.DeviceId) is { } d) d.Free = d.Free > sim.Total ? d.Free - sim.Total : 0;
                    }
                }
                else t.State = State.Normal;
            }
        }
    }

    void Cancel(Tile t)
    {
        if (t.Copy != null) { t.Copy.Cancel(); return; }
        t.Sim = null;
        t.Pending.Clear();
        t.State = State.Normal;
    }

    void Browse(Tile t)
    {
        using var dlg = new OpenFileDialog
        {
            Title = $"{Title(t)} Browse files",
            Multiselect = true,
            Filter = t.Key == "folder" ? "All files (*.*)|*.*" : Logic.SupportedFilter(),
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) Accept(t, dlg.FileNames, Now);
    }

    public static void OpenInExplorer(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
        catch { /* nothing to open */ }
    }

    // ---- frame loop -------------------------------------------------------------------
    void OnTick()
    {
        double now = Now, dt = Math.Clamp(now - _lastNow, 0, 0.1);
        _lastNow = now;
        Step(now, dt);

        Tile? target = DropTarget();
        string? demoDrop = _demo ? _tiles.LastOrDefault()?.Key : null;
        foreach (var t in _tiles)
            t.Overlay.Set((_dragging && t == target) || t.Key == demoDrop ? 1f : 0f, now);

        Invalidate();

        if (_shotPath != null)
        {
            _frame++;
            if (_frame >= 120 && now >= (FirstFadeDelayMs + FirstFadeMs) / 1000 + 0.3)
            {
                SaveScreenshot(_shotPath);
                _timer.Stop();
                Close();
            }
        }
    }

    void SaveScreenshot(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using var bmp = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp)) Render(g);
            bmp.Save(path, ImageFormat.Png);
        }
        catch (Exception e) { Console.Error.WriteLine("screenshot failed: " + e.Message); }
    }

    Tile? TileAt(PointF p) =>
        _tiles.FirstOrDefault(t => IsShown(t) && t.Rect(Now).Contains(p));

    Tile? DropTarget()
    {
        if (_dragging && TileAt(_dragPoint) is { } under) { _lastHover = under.Key; return under; }
        return _tiles.FirstOrDefault(t => t.Key == _lastHover && IsShown(t))
            ?? _tiles.FirstOrDefault(t => t.IsDevice)
            ?? _tiles[0];
    }

    // ---- input ------------------------------------------------------------------------
    PointF Logical(Point p) => new(p.X / _scale, p.Y / _scale);

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mouse = Logical(e.Location);
        bool hand = _links.Any(l => l.Rect.Contains(_mouse))
            || _tileHits.Any(h => h.Rect.Contains(_mouse) && h.Tile.State == State.Normal);
        Cursor = hand ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _mouse = new PointF(-1, -1);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        var p = Logical(e.Location);
        for (int i = _links.Count - 1; i >= 0; i--)
        {
            if (_links[i].Rect.Contains(p)) { _links[i].Act(); return; }
        }
        foreach (var (rect, t) in _tileHits)
        {
            if (!rect.Contains(p)) continue;
            switch (t.State)
            {
                case State.Normal: Browse(t); break;
                case State.Success or State.NotEnoughSpace or State.Untrusted:
                    t.State = State.Normal; t.Note = null; break;
            }
            return;
        }
    }

    protected override void OnDragEnter(DragEventArgs e)
    {
        base.OnDragEnter(e);
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
            _dragging = true;
            _dragPoint = Logical(PointToClient(new Point(e.X, e.Y)));
        }
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        if (!_dragging) return;
        e.Effect = DragDropEffects.Copy;
        _dragPoint = Logical(PointToClient(new Point(e.X, e.Y)));
    }

    protected override void OnDragLeave(EventArgs e)
    {
        base.OnDragLeave(e);
        _dragging = false;
    }

    protected override void OnDragDrop(DragEventArgs e)
    {
        base.OnDragDrop(e);
        _dragPoint = Logical(PointToClient(new Point(e.X, e.Y)));
        var target = DropTarget();
        _dragging = false;
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && target != null)
            Accept(target, files, Now);
    }

    // ---- drawing ----------------------------------------------------------------------
    protected override void OnPaint(PaintEventArgs e) => Render(e.Graphics);

    Font F(float px, bool bold = false)
    {
        if (!_fonts.TryGetValue((px, bold), out var f))
            _fonts[(px, bold)] = f = new Font("Segoe UI", px, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        return f;
    }

    static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Color Fade(Color c, float a) => Color.FromArgb(Math.Clamp((int)(c.A * a), 0, 255), c.R, c.G, c.B);
    static Color Mul(Color c, float k) => Color.FromArgb(c.A, (int)(c.R * k), (int)(c.G * k), (int)(c.B * k));

    static readonly StringFormat Center = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
    static readonly StringFormat CenterWrap = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisWord };
    static readonly StringFormat LeftMid = new() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };

    void Line(Graphics g, string s, float px, bool bold, Color c, float cx, float cy, float w)
    {
        using var b = new SolidBrush(c);
        g.DrawString(s, F(px, bold), b, new RectangleF(cx - w / 2, cy - px, w, px * 2), Center);
    }

    void Wrapped(Graphics g, string s, float px, bool bold, Color c, float cx, float top, float w)
    {
        using var b = new SolidBrush(c);
        g.DrawString(s, F(px, bold), b, new RectangleF(cx - w / 2, top, w, 400), CenterWrap);
    }

    /// <summary>A clickable line of text; registered for the click handler.</summary>
    void Link(Graphics g, string s, float px, Color c, float cx, float cy, Action act)
    {
        var size = g.MeasureString(s, F(px));
        var r = new RectangleF(cx - size.Width / 2 - 4, cy - size.Height / 2 - 2, size.Width + 8, size.Height + 4);
        bool hover = r.Contains(_mouse);
        Line(g, s, px, false, hover ? Mul(c, 0.8f) : c, cx, cy, size.Width + 8);
        _links.Add((r, act));
    }

    void Render(Graphics g)
    {
        double now = Now;
        var pal = PaletteFor(AppSettings.Theme);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.ResetTransform();
        g.ScaleTransform(_scale, _scale);
        g.Clear(pal.Window);
        _links.Clear();
        _tileHits.Clear();

        // header: logo + name, demo-device button, settings gear
        var mark = new PointF(SideX + 14, HeaderH / 2);
        g.DrawImage(_logo, new RectangleF(mark.X - 14, mark.Y - 14, 28, 28));
        using (var b = new SolidBrush(pal.Text))
            g.DrawString("Wplus", F(17, true), b, new RectangleF(mark.X + 20, mark.Y - 14, 200, 28), LeftMid);

        var gearC = new PointF(WinW - 30, HeaderH / 2);
        var gearR = new RectangleF(gearC.X - 15, gearC.Y - 15, 30, 30);
        var gc = gearR.Contains(_mouse) ? Accent : pal.Muted;
        using (var pen = new Pen(gc, 3f))
        {
            for (int k = 0; k < 8; k++)
            {
                double a = k * Math.PI / 4;
                g.DrawLine(pen, gearC.X + (float)Math.Cos(a) * 6, gearC.Y + (float)Math.Sin(a) * 6,
                    gearC.X + (float)Math.Cos(a) * 10, gearC.Y + (float)Math.Sin(a) * 10);
            }
        }
        using (var pen = new Pen(gc, 2.5f)) g.DrawEllipse(pen, gearC.X - 6.5f, gearC.Y - 6.5f, 13, 13);
        _links.Add((gearR, () => new SettingsForm(this).ShowDialog(this)));

        if (Devices.Count < MaxDevices)
        {
            var addR = new RectangleF(WinW - 110 - 56, HeaderH / 2 - 13, 112, 26);
            bool hov = addR.Contains(_mouse);
            using (var path = RoundRect(addR, 13))
            using (var pen = new Pen(hov ? Accent : pal.Track, 1f)) g.DrawPath(pen, path);
            Line(g, "+ Demo device", 12.5f, false, hov ? Accent : pal.Muted, addR.X + addR.Width / 2, addR.Y + addR.Height / 2, addR.Width);
            _links.Add((addR, AddDemoDevice));
        }

        // empty device area: pulsing rings + wording
        if (Devices.Count == 0)
        {
            var area = new RectangleF(AreaX, HeaderH, HalfW * 2 + Gap, AreaH);
            using (var path = RoundRect(area, TileRadius))
            using (var pen = new Pen(pal.EmptyBorder, 1.5f) { DashPattern = new[] { 4f, 3.3f } })
                g.DrawPath(pen, path);
            var c = new PointF(area.X + area.Width / 2, area.Y + area.Height / 2 - 36);
            for (int k = 0; k < 3; k++)
            {
                double phase = (((now * 1000 - RingDelaysMs[k]) / RingCycleMs) % 1.0 + 1.0) % 1.0 * Math.PI * 2;
                float s = 1 + (RingScales[k] - 1) * (float)(0.5 - 0.5 * Math.Cos(phase));
                float rad = RingSizes[k] / 2 * s;
                using var pen = new Pen(Fade(pal.EmptyText, 0.4f + 0.2f * k), 1.5f);
                g.DrawEllipse(pen, c.X - rad, c.Y - rad, rad * 2, rad * 2);
            }
            using (var b = new SolidBrush(Accent)) g.FillEllipse(b, c.X - 4, c.Y - 4, 8, 8);
            Wrapped(g, NoDeviceText, NoDeviceTextSize, false, pal.EmptyText, c.X, c.Y + 55 + NoDeviceMarginBottom - 10,
                area.Width - 2 * NoDeviceMarginSide);
        }

        foreach (var t in _tiles)
            if (IsShown(t)) DrawTile(g, t, pal, now);
    }

    void DrawTile(Graphics g, Tile t, Palette pal, double now)
    {
        var rect = t.Rect(now);
        float alpha = t.Appear(now);
        if (alpha <= 0 || rect.Width < 1) return;
        _tileHits.Add((rect, t));
        Color A(Color c) => Fade(c, alpha);

        // soft shadow + body
        for (int i = 6; i >= 1; i--)
        {
            var sr = rect;
            sr.Inflate(i, i);
            sr.Offset(0, 3);
            using var sp = RoundRect(sr, TileRadius + i);
            using var sb = new SolidBrush(Fade(pal.Shadow, alpha * (1 - i / 7f) * 0.45f));
            g.FillPath(sb, sp);
        }
        bool hovered = rect.Contains(_mouse) && !_dragging;
        using (var body = RoundRect(rect, TileRadius))
        using (var bb = new SolidBrush(A(hovered ? pal.TileHover : pal.Tile)))
            g.FillPath(bb, body);

        bool large = rect.Height > QuarterH + Gap;
        float iconH = large ? LargeIcon : SmallIcon;
        float iconTop = large ? LargeIconTop : SmallIconTop;
        float iconBottom = large ? LargeIconBottom : SmallIconBottom;
        float titleSize = large ? LargeTitle : SmallTitle;
        var iconC = new PointF(rect.X + rect.Width / 2, rect.Y + iconTop + iconH / 2);
        float cx = iconC.X;
        float titleY = rect.Y + iconTop + iconH + iconBottom - titleSize * 0.6f;
        float subY = titleY + titleSize * 0.95f + 4;
        float textW = rect.Width - 28;
        float subPx = large ? 13f : 11.5f;

        void MainIcon()
        {
            switch (t.Key)
            {
                case "folder": Icons.Folder(g, iconC, iconH * 0.8f, A(Color.FromArgb(0x5f, 0x9b, 0xf5))); break;
                case "library": Icons.Library(g, iconC, iconH * 0.85f, A(Color.FromArgb(0xe7, 0x3c, 0x7e))); break;
                default: Icons.Device(g, iconC, iconH, A(pal.Text)); break;
            }
        }
        void TitleText() => Line(g, Title(t), titleSize, true, A(pal.Text), cx, titleY, textW);
        void Sub(float y, string s, Color c) => Line(g, s, subPx, false, A(c), cx, y, textW);

        string subtitle = t.Key switch
        {
            "folder" => AppSettings.LocalFolder,
            "library" => "Music, Videos & Books folders",
            _ => Logic.CapacityText(Dev(t.DeviceId)?.Capacity ?? 0),
        };

        switch (t.State)
        {
            case State.Normal:
                MainIcon();
                TitleText();
                Sub(subY, subtitle, pal.Muted);
                if (t.IsDevice)
                {
                    string badge = Dev(t.DeviceId)?.Wifi == true ? "Wi-Fi" : "USB";
                    var sz = g.MeasureString(badge, F(10.5f));
                    var br = new RectangleF(rect.Right - sz.Width - 26, rect.Y + 12, sz.Width + 14, sz.Height + 2);
                    using (var path = RoundRect(br, br.Height / 2))
                    using (var pen = new Pen(A(pal.Track), 1f)) g.DrawPath(pen, path);
                    Line(g, badge, 10.5f, false, A(pal.Muted), br.X + br.Width / 2, br.Y + br.Height / 2, br.Width + 4);
                }
                break;

            case State.Preparing:
                Icons.Spinner(g, iconC, iconH, now);
                TitleText();
                Sub(subY, t.IsDevice ? "Preparing files..." : "Preparing to convert...", pal.Muted);
                Link(g, "Cancel transferring.", subPx, A(Alert), cx, subY + 20, () => Cancel(t));
                break;

            case State.Transferring:
            {
                MainIcon();
                TitleText();
                float f = Math.Clamp(t.Progress.Get(now), 0, 1);
                var bar = new RectangleF(cx - (rect.Width - 56) / 2, subY - 3, rect.Width - 56, 6);
                using (var path = RoundRect(bar, 3))
                using (var b = new SolidBrush(A(pal.Track))) g.FillPath(b, path);
                var fill = bar;
                fill.Width = Math.Max(bar.Width * f, 6);
                using (var path = RoundRect(fill, 3))
                using (var b = new SolidBrush(A(Accent))) g.FillPath(b, path);
                ulong total = t.Copy?.Total ?? t.Sim?.Total ?? 0;
                int files = t.Copy?.Files ?? t.Sim?.Files ?? 0;
                ulong done = (ulong)(total * (double)f);
                Sub(subY + 18, $"{Logic.FilesText(files)} · {Logic.FormatSize(done)} of {Logic.FormatSize(total)}", pal.Muted);
                Link(g, "Cancel transferring.", subPx, A(Alert), cx, subY + 38, () => Cancel(t));
                break;
            }

            case State.Success:
            {
                Icons.Check(g, iconC, iconH * 0.8f, alpha);
                TitleText();
                Sub(subY, t.IsDevice ? $"{Logic.FilesText(t.LastCount)} transferred" : $"{Logic.FilesText(t.LastCount)} added", Success);
                float y = subY + 20;
                if (t.Note != null) { Sub(y, t.Note, pal.Muted); y += 18; }
                if (t.IsDevice)
                {
                    if (large)
                        Wrapped(g, "Press here to find out where the transferred files are located on your device.",
                            11.5f, false, A(pal.Muted), cx, y - 8, textW - 20);
                }
                else if (t.LastDir is { } dir)
                {
                    Link(g, "Show in folder.", subPx, A(Accent), cx, y, () => OpenInExplorer(dir));
                }
                break;
            }

            case State.Error:
                Icons.Warning(g, iconC, iconH * 0.75f, A(Warning));
                TitleText();
                Sub(subY, t.ErrorText, pal.Text);
                if (t.Note != null) Sub(subY + 18, t.Note, pal.Muted);
                Link(g, "Try again.", subPx, A(Accent), cx, subY + 38, () => { t.State = State.Normal; t.Note = null; });
                break;

            case State.NotEnoughSpace:
                Icons.Warning(g, iconC, iconH * 0.75f, A(Alert));
                TitleText();
                Sub(subY, "Not enough free space", pal.Text);
                break;

            case State.Untrusted:
                Icons.Lock(g, iconC, iconH * 0.7f, A(pal.Text));
                TitleText();
                Sub(subY, "Unlock device to continue.", pal.Text);
                break;
        }

        // drop overlay (150 ms fade)
        float ov = t.Overlay.Get(now) * alpha;
        if (ov > 0.01f)
        {
            using (var path = RoundRect(rect, TileRadius))
            using (var b = new SolidBrush(Fade(pal.Overlay, ov))) g.FillPath(b, path);
            var inner = rect;
            inner.Inflate(-1, -1);
            using (var path = RoundRect(inner, TileRadius))
            using (var pen = new Pen(Fade(Accent, ov), 2f)) g.DrawPath(pen, path);
            var (aTop, aBottom) = iconH > ArrowThreshold ? ArrowLarge : ArrowSmall;
            float aLen = iconH * 0.45f;
            var top = new PointF(cx, rect.Y + iconTop + aTop);
            Icons.Arrow(g, top, aLen, Fade(Accent, ov));
            string drop = t.Key switch
            {
                "folder" => "Drop files here to add to Local Folder",
                "library" => "Drop files here to add to Library",
                _ => $"Drop files here to convert & transfer into {Title(t)}",
            };
            Wrapped(g, drop, large ? 16f : 13f, true, Fade(pal.Text, ov), cx, top.Y + aLen + aBottom + 10, textW);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        base.OnFormClosed(e);
    }
}
