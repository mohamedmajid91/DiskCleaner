using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net.Http;
using DiskCleaner.Core;
using DiskCleaner.Services;

namespace DiskCleaner.UI;

public partial class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly List<CleanCategory> _cats = Cleaner.Build();
    private readonly Dictionary<string, CheckBox> _checks = new();
    private readonly Dictionary<string, Label> _sizeLabels = new();
    private readonly Dictionary<string, long> _sizes = new();

    private Panel _header = null!, _ramBar = null!, _catPanel = null!, _chart = null!;
    private Label _title = null!, _info = null!, _total = null!, _status = null!, _credit = null!;
    private Button _btnLang = null!, _btnAnalyze = null!, _btnClean = null!, _btnRam = null!;
    private CheckBox _chkAuto = null!, _chkRestore = null!;
    private LinkLabel _lnkUpdate = null!;
    private ProgressBar _progress = null!;
    private NotifyIcon _tray = null!;
    private ToolStripMenuItem _miShow = null!, _miRam = null!, _miExit = null!;
    private System.Windows.Forms.Timer _ramTimer = null!;

    private TabControl _tabs = null!;
    private TabPage _tpClean = null!, _tpLarge = null!, _tpDup = null!, _tpStartup = null!, _tpProc = null!, _tpSched = null!, _tpHistory = null!, _tpUninstall = null!;

    private int _ramPct;
    private bool _analyzed;
    private long _lastTotal;

    public MainForm()
    {
        _settings = AppSettings.Load();
        Loc.Lang = _settings.Lang;
        BuildUi();
        ApplySettings();
        ApplyLanguage();
        Shown += (_, _) => { ApplyLanguage(); RunAnalyze(); };
    }

    // ===================== الهيكل العام =====================
    private void BuildUi()
    {
        Text = "Disk & RAM Cleaner";
        ClientSize = new Size(624, 648);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Theme.Dark; ForeColor = Theme.TextCol; Font = Theme.Main;
        try { Icon = Icon.ExtractAssociatedIcon(App.ExePath); } catch { }

        _header = new Panel { Location = new(0, 0), Size = new(624, 56) };
        _header.Paint += (_, e) =>
        {
            var r = _header.ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0) return;
            using var b = new LinearGradientBrush(r, Theme.Accent, Theme.Purple, 0f);
            e.Graphics.FillRectangle(b, r);
        };
        Controls.Add(_header);

        _title = new Label { Font = Theme.TitleFont, ForeColor = Theme.TextCol, BackColor = Color.Transparent,
            Location = new(18, 12), Size = new(420, 34) };
        _header.Controls.Add(_title);

        _btnLang = new Button { Size = new(88, 30), Location = new(516, 13), FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent, ForeColor = Theme.TextCol, Font = Theme.Main, Cursor = Cursors.Hand };
        _btnLang.FlatAppearance.BorderColor = Color.White; _btnLang.FlatAppearance.BorderSize = 1;
        _btnLang.Click += (_, _) => { Loc.Lang = Loc.Lang == "ar" ? "en" : "ar"; ApplyLanguage(); SaveSettings(); };
        _header.Controls.Add(_btnLang);

        _tabs = new TabControl { Location = new(8, 62), Size = new(608, 548) };
        Controls.Add(_tabs);

        _tpClean     = new TabPage();
        _tpLarge     = new TabPage();
        _tpDup       = new TabPage();
        _tpUninstall = new TabPage();
        _tpStartup   = new TabPage();
        _tpProc      = new TabPage();
        _tpSched     = new TabPage();
        _tpHistory   = new TabPage();
        foreach (var tp in new[] { _tpClean, _tpLarge, _tpDup, _tpUninstall, _tpStartup, _tpProc, _tpSched, _tpHistory })
        { tp.BackColor = Theme.Dark; tp.UseVisualStyleBackColor = false; _tabs.TabPages.Add(tp); }

        BuildCleanTab(_tpClean);
        BuildLargeTab(_tpLarge);
        BuildDuplicatesTab(_tpDup);
        BuildUninstallTab(_tpUninstall);
        BuildStartupTab(_tpStartup);
        BuildProcessTab(_tpProc);
        BuildScheduleTab(_tpSched);
        BuildHistoryTab(_tpHistory);

        _lnkUpdate = new LinkLabel { LinkColor = Theme.Link, ActiveLinkColor = Theme.AccentH, Font = Theme.Main,
            Location = new(12, 618), Size = new(240, 22) };
        _lnkUpdate.LinkClicked += (_, _) => CheckUpdate();
        Controls.Add(_lnkUpdate);

        _credit = new Label { Text = $"v{App.Version}  -  by {App.Author}", ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 8F), TextAlign = ContentAlignment.MiddleRight, Location = new(360, 618), Size = new(252, 22) };
        Controls.Add(_credit);

        _ramTimer = new System.Windows.Forms.Timer { Interval = 600000 };
        _ramTimer.Tick += (_, _) => { NativeMemory.FreeAll(); UpdateHeader(); _status.Text = $"{Loc.T("ramDone")} @ {DateTime.Now:HH:mm}"; Logger.Log("Auto RAM free");
            if (!Visible) _tray.ShowBalloonTip(1500, "Disk & RAM Cleaner", Loc.T("ramDone"), ToolTipIcon.Info); };

        BuildTray();
        Resize += (_, _) => { if (WindowState == FormWindowState.Minimized) { Hide(); _tray.ShowBalloonTip(1500, "Disk & RAM Cleaner", Loc.T("trayMin"), ToolTipIcon.Info); } };
        FormClosing += (_, _) => { SaveSettings(); Logger.Log("Closed"); try { _tray.Visible = false; _tray.Dispose(); } catch { } };
    }

    // ===================== تبويب التنظيف =====================
    private void BuildCleanTab(TabPage tp)
    {
        _info = new Label { ForeColor = Theme.Muted, Location = new(12, 8), Size = new(576, 20) };
        tp.Controls.Add(_info);

        _ramBar = new Panel { Location = new(12, 30), Size = new(576, 18) };
        _ramBar.Paint += RamBar_Paint;
        tp.Controls.Add(_ramBar);

        _catPanel = new Panel { Location = new(12, 54), Size = new(576, 208), BackColor = Theme.Panel, AutoScroll = true };
        tp.Controls.Add(_catPanel);
        int y = 10;
        foreach (var c in _cats)
        {
            var cb = new CheckBox { ForeColor = Theme.TextCol, Font = Theme.Main, Location = new(14, y), Size = new(340, 24), Checked = true };
            cb.CheckedChanged += (_, _) => SaveSettings();
            _catPanel.Controls.Add(cb); _checks[c.Key] = cb;
            var sl = new Label { Text = "--", ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleRight,
                Location = new(396, y), Size = new(150, 24) };
            _catPanel.Controls.Add(sl); _sizeLabels[c.Key] = sl;
            y += 30;
        }

        _total = new Label { Font = Theme.Bold, ForeColor = Theme.AccentH, Location = new(12, 268), Size = new(576, 22) };
        tp.Controls.Add(_total);

        _chart = new Panel { Location = new(12, 294), Size = new(576, 22) };
        _chart.Paint += Chart_Paint;
        tp.Controls.Add(_chart);

        _progress = new ProgressBar { Location = new(12, 322), Size = new(576, 14), Style = ProgressBarStyle.Continuous };
        tp.Controls.Add(_progress);

        _status = new Label { ForeColor = Theme.Muted, Location = new(12, 340), Size = new(576, 20) };
        tp.Controls.Add(_status);

        _btnAnalyze = MakeBtn(12, 366, 184, Theme.Gray, Theme.GrayH);
        _btnClean   = MakeBtn(204, 366, 184, Theme.Accent, Theme.AccentH);
        _btnRam     = MakeBtn(396, 366, 192, Theme.Purple, Theme.PurpleH);
        _btnAnalyze.Click += (_, _) => RunAnalyze();
        _btnClean.Click   += (_, _) => RunClean();
        _btnRam.Click     += (_, _) => RunFreeRam();
        tp.Controls.AddRange(new Control[] { _btnAnalyze, _btnClean, _btnRam });

        _chkAuto = new CheckBox { ForeColor = Theme.TextCol, Font = Theme.Main, Location = new(12, 418), Size = new(576, 22) };
        _chkAuto.CheckedChanged += Auto_Changed;
        tp.Controls.Add(_chkAuto);

        _chkRestore = new CheckBox { ForeColor = Theme.TextCol, Font = Theme.Main, Location = new(12, 444), Size = new(576, 22) };
        _chkRestore.CheckedChanged += (_, _) => SaveSettings();
        tp.Controls.Add(_chkRestore);
    }

    // ===================== أدوات مساعدة =====================
    private Button MakeBtn(int x, int y, int w, Color baseCol, Color hover)
    {
        var b = new Button { Size = new(w, 42), Location = new(x, y), FlatStyle = FlatStyle.Flat,
            BackColor = baseCol, ForeColor = Color.White, Font = Theme.Bold, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = 0;
        b.MouseEnter += (_, _) => b.BackColor = hover;
        b.MouseLeave += (_, _) => b.BackColor = baseCol;
        return b;
    }

    private static ListView MakeList(int x, int y, int w, int h)
        => new()
        {
            Location = new(x, y), Size = new(w, h), View = View.Details, FullRowSelect = true,
            GridLines = false, BackColor = Theme.Panel, ForeColor = Theme.TextCol, BorderStyle = BorderStyle.FixedSingle
        };

    private void BuildTray()
    {
        var ctx = new ContextMenuStrip();
        _miShow = new ToolStripMenuItem("Show app", null, (_, _) => ShowApp());
        _miRam  = new ToolStripMenuItem("Free RAM now", null, (_, _) => { NativeMemory.FreeAll(); _ramPct = SystemInfo.GetRam().usedPct; _ramBar.Invalidate();
            _tray.ShowBalloonTip(1500, "Disk & RAM Cleaner", Loc.T("ramDone"), ToolTipIcon.Info); Logger.Log("Tray RAM free"); });
        _miExit = new ToolStripMenuItem("Exit", null, (_, _) => Close());
        ctx.Items.AddRange(new ToolStripItem[] { _miShow, _miRam, new ToolStripSeparator(), _miExit });
        _tray = new NotifyIcon { Icon = Icon ?? SystemIcons.Application, Text = "Disk & RAM Cleaner", Visible = true, ContextMenuStrip = ctx };
        _tray.DoubleClick += (_, _) => ShowApp();
    }

    private void ShowApp() { Show(); WindowState = FormWindowState.Normal; Activate(); }

    // ===================== الرسوم =====================
    private void RamBar_Paint(object? sender, PaintEventArgs e)
    {
        int w = _ramBar.ClientSize.Width, h = _ramBar.ClientSize.Height;
        if (w <= 0 || h <= 0) return;
        var g = e.Graphics;
        using (var bg = new SolidBrush(Theme.Gray)) g.FillRectangle(bg, 0, 0, w, h);
        int fw = w * _ramPct / 100;
        var col = _ramPct < 70 ? Color.FromArgb(0, 180, 120) : _ramPct < 88 ? Color.FromArgb(230, 160, 40) : Color.FromArgb(215, 70, 70);
        if (fw > 0) using (var fb = new SolidBrush(col)) g.FillRectangle(fb, 0, 0, fw, h);
        using var f = new Font("Segoe UI", 8F, FontStyle.Bold);
        using var tb = new SolidBrush(Color.White);
        g.DrawString($"RAM  {_ramPct}%", f, tb, 6, 1);
    }

    private void Chart_Paint(object? sender, PaintEventArgs e)
    {
        int w = _chart.ClientSize.Width, h = _chart.ClientSize.Height;
        if (w <= 0 || h <= 0) return;
        var g = e.Graphics;
        using (var bg = new SolidBrush(Theme.Gray)) g.FillRectangle(bg, 0, 0, w, h);
        if (!_analyzed || _lastTotal <= 0) return;
        double x = 0; int idx = 0;
        foreach (var c in _cats)
        {
            long sz = _sizes.TryGetValue(c.Key, out var v) ? v : 0;
            if (sz > 0)
            {
                double seg = (double)w * sz / _lastTotal;
                using var b = new SolidBrush(Theme.Palette[idx % Theme.Palette.Length]);
                g.FillRectangle(b, (int)x, 0, Math.Max(1, (int)seg), h);
                x += seg;
            }
            idx++;
        }
    }

    // ===================== اللغة =====================
    private void UpdateHeader()
    {
        var (pct, freeGb) = SystemInfo.GetRam();
        _ramPct = pct;
        _info.Text = $"{Loc.T("diskFree")} {SystemInfo.GetFreeGB()} GB    |    {Loc.T("ramUsed")} {pct}%  ({Loc.T("free")} {freeGb} GB)";
        _ramBar.Invalidate();
    }

    private void ApplyLanguage()
    {
        SuspendLayout();
        RightToLeft = Loc.IsRtl ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = Loc.IsRtl;
        _header.RightToLeft = RightToLeft;
        _title.Text = Loc.T("title"); _btnLang.Text = Loc.T("langBtn");
        _btnAnalyze.Text = Loc.T("analyze"); _btnClean.Text = Loc.T("cleanSel");
        _btnRam.Text = Loc.T("freeRam");
        _chkAuto.Text = Loc.T("autoRam"); _chkRestore.Text = Loc.T("restore");
        _lnkUpdate.Text = Loc.T("checkUpdate");
        _miShow.Text = Loc.T("trayShow"); _miRam.Text = Loc.T("trayRam"); _miExit.Text = Loc.T("trayExit");
        _tpClean.Text = Loc.T("tabClean"); _tpLarge.Text = Loc.T("tabLarge"); _tpDup.Text = Loc.T("tabDup");
        _tpUninstall.Text = Loc.T("tabUninstall");
        _tpStartup.Text = Loc.T("tabStartup"); _tpProc.Text = Loc.T("tabProc"); _tpSched.Text = Loc.T("tabSchedule"); _tpHistory.Text = Loc.T("tabHistory");
        foreach (var c in _cats) _checks[c.Key].Text = c.Name(Loc.Lang);
        _total.Text = _analyzed ? $"{Loc.T("totalClean")}: {Theme.FormatSize(_lastTotal)}" : Loc.T("pressAnalyze");
        ApplyTabsLanguage();
        ApplyUninstallLanguage();
        UpdateHeader();
        ResumeLayout(); Refresh();
    }

    // ===================== إجراءات التنظيف =====================
    private void RunAnalyze()
    {
        _btnAnalyze.Enabled = _btnClean.Enabled = false;
        _progress.Value = 0; _progress.Maximum = _cats.Count;
        long total = 0; int i = 0;
        foreach (var c in _cats)
        {
            i++; _status.Text = $"{Loc.T("analyzing")}: {c.Name(Loc.Lang)}...";
            Application.DoEvents();
            long s = Cleaner.GetSize(c);
            _sizes[c.Key] = s; _sizeLabels[c.Key].Text = Theme.FormatSize(s);
            total += s; _progress.Value = i; Application.DoEvents();
        }
        _analyzed = true; _lastTotal = total;
        int idx = 0;
        foreach (var c in _cats) { _sizeLabels[c.Key].ForeColor = _sizes[c.Key] > 0 ? Theme.Palette[idx % Theme.Palette.Length] : Theme.Muted; idx++; }
        _total.Text = $"{Loc.T("totalClean")}: {Theme.FormatSize(total)}";
        _status.Text = Loc.T("doneAnalyze"); UpdateHeader(); _chart.Invalidate();
        Logger.Log($"Analyzed. Total: {Theme.FormatSize(total)}");
        _btnAnalyze.Enabled = _btnClean.Enabled = true;
    }

    private void RunClean()
    {
        var sel = _cats.Where(c => _checks[c.Key].Checked).ToList();
        if (sel.Count == 0) { MessageBox.Show(Loc.T("noSelect"), Loc.T("warn"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        string names = string.Join("\n", sel.Select(c => "- " + c.Name(Loc.Lang)));
        if (MessageBox.Show($"{Loc.T("willDelete")}\n\n{names}\n\n{Loc.T("permanent")}", Loc.T("confirmTitle"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        double before = SystemInfo.GetFreeGB();
        _btnAnalyze.Enabled = _btnClean.Enabled = false;
        if (_chkRestore.Checked) { _status.Text = Loc.T("restoring"); Application.DoEvents(); CreateRestorePoint(); }

        _progress.Value = 0; _progress.Maximum = sel.Count; int i = 0;
        foreach (var c in sel)
        {
            i++; _status.Text = $"{Loc.T("cleaning")}: {c.Name(Loc.Lang)}...";
            Application.DoEvents();
            Cleaner.Clean(c);
            _sizeLabels[c.Key].Text = "0 B"; _sizes[c.Key] = 0; _progress.Value = i; Application.DoEvents();
        }
        double after = SystemInfo.GetFreeGB(); double freed = Math.Round(after - before, 2);
        UpdateHeader(); _chart.Invalidate(); _status.Text = Loc.T("doneClean");
        History.Add(freed, sel.Select(c => c.Key));
        RefreshHistory();
        Logger.Log($"Cleaned [{string.Join(", ", sel.Select(c => c.Key))}]. Freed {freed} GB");
        _btnAnalyze.Enabled = _btnClean.Enabled = true;
        MessageBox.Show($"{Loc.T("cleanOk")}\n\n{Loc.T("before")}: {before} GB\n{Loc.T("after")}: {after} GB\n{Loc.T("freed")}: {freed} GB",
            Loc.T("resultTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RunFreeRam()
    {
        _btnRam.Enabled = false;
        var b = SystemInfo.GetRam(); _status.Text = Loc.T("freeingRam"); Application.DoEvents();
        NativeMemory.FreeAll(); Thread.Sleep(600);
        var a = SystemInfo.GetRam(); UpdateHeader(); _status.Text = Loc.T("ramDone"); _btnRam.Enabled = true;
        double freed = Math.Round(a.freeGb - b.freeGb, 2); Logger.Log($"RAM freed: {freed} GB");
        MessageBox.Show($"{Loc.T("ramDone")}\n\n{Loc.T("before")}: {b.freeGb} GB ({b.usedPct}% {Loc.T("used")})\n{Loc.T("after")}: {a.freeGb} GB ({a.usedPct}% {Loc.T("used")})\n{Loc.T("freed")}: {freed} GB",
            Loc.T("ramTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Auto_Changed(object? sender, EventArgs e)
    {
        if (_chkAuto.Checked) { _ramTimer.Start(); _status.Text = Loc.T("autoOn"); }
        else { _ramTimer.Stop(); _status.Text = Loc.T("autoOff"); }
        SaveSettings();
    }

    private static void CreateRestorePoint()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -Command \"Enable-ComputerRestore -Drive 'C:\\'; Checkpoint-Computer -Description 'DiskCleaner' -RestorePointType 'MODIFY_SETTINGS'\"")
            { UseShellExecute = false, CreateNoWindow = true };
            Process.Start(psi)?.WaitForExit(60000);
            Logger.Log("Restore point requested");
        }
        catch (Exception ex) { Logger.Log($"Restore point failed: {ex.Message}"); }
    }

    private void CheckUpdate()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            string latest = http.GetStringAsync($"https://raw.githubusercontent.com/{App.RepoOwner}/{App.RepoName}/main/version.txt").Result.Trim();
            if (Version.TryParse(latest, out var lv) && Version.TryParse(App.Version, out var cv) && lv > cv)
            {
                if (MessageBox.Show($"{Loc.T("updAvail")}: {latest}", Loc.T("updTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo($"https://github.com/{App.RepoOwner}/{App.RepoName}/releases/latest") { UseShellExecute = true });
            }
            else MessageBox.Show(Loc.T("updLatest"), Loc.T("updTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Logger.Log($"Update check failed: {ex.Message}"); MessageBox.Show(Loc.T("updFail"), Loc.T("updTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void ApplySettings()
    {
        foreach (var c in _cats) _checks[c.Key].Checked = !_settings.Unchecked.Contains(c.Key);
        _chkRestore.Checked = _settings.RestorePoint;
        _chkAuto.Checked = _settings.AutoRam;
    }

    private void SaveSettings()
    {
        _settings.Lang = Loc.Lang;
        _settings.AutoRam = _chkAuto.Checked;
        _settings.RestorePoint = _chkRestore.Checked;
        _settings.Unchecked = _cats.Where(c => !_checks[c.Key].Checked).Select(c => c.Key).ToList();
        _settings.Save();
    }
}
