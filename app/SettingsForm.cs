// Settings: General (demo devices), View (theme, Library tile), Locations, About.
namespace Wplus;

sealed class SettingsForm : Form
{
    readonly MainForm _main;
    readonly ListBox _tabs = new();
    readonly Panel _page = new();

    public SettingsForm(MainForm main)
    {
        _main = main;
        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(600, 320);

        _tabs.Dock = DockStyle.Left;
        _tabs.Width = MainForm.SettingsColumnW;
        _tabs.BorderStyle = BorderStyle.None;
        _tabs.IntegralHeight = false;
        _tabs.ItemHeight = 30;
        _tabs.DrawMode = DrawMode.OwnerDrawFixed;
        _tabs.Items.AddRange(new object[] { "General", "View", "Locations", "About" });
        _tabs.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using var bg = new SolidBrush(sel ? Color.FromArgb(0xe8, 0xf0, 0xfe) : SystemColors.Window);
            e.Graphics.FillRectangle(bg, e.Bounds);
            TextRenderer.DrawText(e.Graphics, _tabs.Items[e.Index].ToString(), Font,
                new Rectangle(e.Bounds.X + 14, e.Bounds.Y, e.Bounds.Width - 14, e.Bounds.Height),
                sel ? Color.FromArgb(0x17, 0x71, 0xfc) : SystemColors.ControlText, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        };
        _tabs.SelectedIndexChanged += (_, _) => ShowPage();

        _page.Dock = DockStyle.Fill;
        _page.Padding = new Padding(18, 14, 18, 14);
        _page.AutoScroll = true;

        Controls.Add(_page);
        Controls.Add(new Label { Dock = DockStyle.Left, Width = 1, BackColor = Color.Gainsboro });
        Controls.Add(_tabs);
        _tabs.SelectedIndex = 0;
    }

    FlowLayoutPanel NewFlow()
    {
        var f = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };
        _page.Controls.Clear();
        _page.Controls.Add(f);
        return f;
    }

    static Label Heading(string s) => new() { Text = s, AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Margin = new Padding(0, 0, 0, 8) };
    static Label Text_(string s, int w = 400) => new() { Text = s, AutoSize = true, MaximumSize = new Size(w, 0), Margin = new Padding(0, 0, 0, 8) };

    void ShowPage()
    {
        var f = NewFlow();
        switch (_tabs.SelectedIndex)
        {
            case 0:
                f.Controls.Add(Heading("Devices"));
                f.Controls.Add(Text_("Real phone and tablet transfer isn't available in this build. Demo devices show how transfers work."));
                foreach (var d in _main.Devices.ToList())
                {
                    var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 2, 0, 2) };
                    row.Controls.Add(new Label { Text = Logic.ShortName(d.Name), AutoSize = true, Width = 200, Margin = new Padding(0, 6, 10, 0) });
                    var wifi = new CheckBox { Text = "Wi-Fi", Checked = d.Wifi, AutoSize = true };
                    wifi.CheckedChanged += (_, _) => { d.Wifi = wifi.Checked; _main.Invalidate(); };
                    var locked = new CheckBox { Text = "Locked", Checked = d.Locked, AutoSize = true };
                    locked.CheckedChanged += (_, _) => { d.Locked = locked.Checked; };
                    var remove = new Button { Text = "Remove", AutoSize = true };
                    remove.Click += (_, _) => { _main.RemoveDevice(d.Id); ShowPage(); };
                    row.Controls.AddRange(new Control[] { wifi, locked, remove });
                    f.Controls.Add(row);
                }
                if (_main.Devices.Count < MainForm.MaxDevices)
                {
                    var add = new Button { Text = "Add demo device", AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
                    add.Click += (_, _) => { _main.AddDemoDevice(); ShowPage(); };
                    f.Controls.Add(add);
                }
                break;

            case 1:
                f.Controls.Add(Heading("Appearance"));
                var light = new RadioButton { Text = "Light", Checked = _main.AppSettings.Theme == Theme.Light, AutoSize = true };
                var dark = new RadioButton { Text = "Dark", Checked = _main.AppSettings.Theme == Theme.Dark, AutoSize = true };
                light.CheckedChanged += (_, _) => { if (light.Checked) { _main.AppSettings.Theme = Theme.Light; _main.SettingsChanged(false); } };
                dark.CheckedChanged += (_, _) => { if (dark.Checked) { _main.AppSettings.Theme = Theme.Dark; _main.SettingsChanged(false); } };
                var lib = new CheckBox { Text = "Show the Library tile", Checked = _main.AppSettings.ShowLibrary, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
                lib.CheckedChanged += (_, _) => { _main.AppSettings.ShowLibrary = lib.Checked; _main.SettingsChanged(true); };
                f.Controls.AddRange(new Control[] { light, dark, lib });
                break;

            case 2:
                f.Controls.Add(Heading("Local Folder"));
                f.Controls.Add(Text_("Files dropped on the Local Folder tile are copied here:"));
                var path = new Label { Text = _main.AppSettings.LocalFolder, AutoSize = true, MaximumSize = new Size(400, 0), Font = new Font("Consolas", 9.5f), Margin = new Padding(0, 0, 0, 10) };
                f.Controls.Add(path);
                var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
                var change = new Button { Text = "Change…", AutoSize = true };
                change.Click += (_, _) =>
                {
                    using var dlg = new FolderBrowserDialog { Description = "Choose Local Folder", SelectedPath = _main.AppSettings.LocalFolder, UseDescriptionForTitle = true };
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _main.AppSettings.LocalFolder = dlg.SelectedPath;
                        _main.SettingsChanged(false);
                        path.Text = dlg.SelectedPath;
                    }
                };
                var open = new Button { Text = "Open", AutoSize = true };
                open.Click += (_, _) => MainForm.OpenInExplorer(_main.AppSettings.LocalFolder);
                buttons.Controls.AddRange(new Control[] { change, open });
                f.Controls.Add(buttons);
                break;

            default:
                f.Controls.Add(Heading("Wplus 1.0"));
                f.Controls.Add(Text_("Free to use — no licence, trial or activation."));
                f.Controls.Add(Text_("Audio: " + string.Join(", ", Logic.Audio)));
                f.Controls.Add(Text_("Video: " + string.Join(", ", Logic.Video)));
                f.Controls.Add(Text_("Subtitles: srt, ass, ssa, sub · Audiobooks: m4b · Ringtones: m4r · Books: epub, pdf"));
                break;
        }
    }
}
