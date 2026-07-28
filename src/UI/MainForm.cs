using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Runtime.InteropServices;
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

    // الهيكل الجديد
    private Panel _titleBar = null!, _sidebar = null!, _content = null!;
    private Label _titleLbl = null!, _credit = null!;
    private Button _btnMin = null!, _btnMax = null!, _btnClose = null!, _btnLang = null!;
    private LinkLabel _lnkUpdate = null!;
    private readonly List<(Button nav, Panel panel, string key)> _sections = new();

    // أقسام (لوحات)
    private Panel _tpDashboard = null!, _tpClean = null!, _tpLarge = null!, _tpDup = null!, _tpUninstall = null!,
                 _tpStartup = null!, _tpProc = null!, _tpSched = null!, _tpHistory = null!, _tpUsers = null!,
                 _tpServices = null!, _tpTasks = null!;

    // عناصر تبويب التنظيف
    private Panel _ramBar = null!, _cpuBar = null!, _catPanel = null!, _chart = null!;
    private Label _info = null!, _total = null!, _status = null!;
    private Button _btnAnalyze = null!, _btnClean = null!, _btnRam = null!;
    private CheckBox _chkAuto = null!, _chkRestore = null!;
    private ProgressBar _progress = null!;

    // اللوحة الرئيسية
    private Label _cardDiskVal = null!, _cardRamVal = null!, _cardCpuVal = null!, _cardFreedVal = null!;
    private Panel _cpuGraph = null!;
    private Label _cpuInfoLbl = null!;
    private Button _btnBoost = null!;
    private readonly List<int> _cpuHistory = new();
    private int _highLoadStreak;
    private string? _prevScheme;
    private bool _boosted;

    private NotifyIcon _tray = null!;
    private ToolStripMenuItem _miShow = null!, _miRam = null!, _miExit = null!;
    private System.Windows.Forms.Timer _ramTimer = null!, _cpuTimer = null!;

    private int _ramPct, _cpuPct;
    private bool _analyzed;
    private long _lastTotal;

    // Win32 للسحب وتغيير الحجم
    [LibraryImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static partial bool ReleaseCapture();
    [LibraryImport("user32.dll")] private static partial IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public MainForm()
    {
        _settings = AppSettings.Load();
        Loc.Lang = _settings.Lang;
        BuildUi();
        ApplySettings();
        ApplyLanguage();
        Shown += async (_, _) => { ApplyLanguage(); ShowSection(0); await CheckUpdate(announce: false); };
    }

    // ===================== الهيكل =====================
    private void BuildUi()
    {
        Text = "Disk & RAM Cleaner";
        ClientSize = new Size(920, 660);
        MinimumSize = new Size(860, 660);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Theme.Dark; ForeColor = Theme.TextCol; Font = Theme.Main;
        try { Icon = Icon.ExtractAssociatedIcon(App.ExePath); } catch { }

        // ---- شريط العنوان المخصّص ----
        _titleBar = new Panel { Location = new(0, 0), Size = new(920, 46), Dock = DockStyle.Top };
        _titleBar.Paint += (_, e) =>
        {
            var r = _titleBar.ClientRectangle; if (r.Width <= 0) return;
            using (var b = new SolidBrush(Theme.TitleBar)) e.Graphics.FillRectangle(b, r);
            using (var a = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(a, 0, r.Height - 2, r.Width, 2);
        };
        _titleBar.MouseDown += TitleDrag;
        _titleBar.MouseDoubleClick += (_, _) => ToggleMaximize();
        Controls.Add(_titleBar);

        _titleLbl = new Label { Text = "Disk & RAM Cleaner", Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.White, BackColor = Color.Transparent, Location = new(16, 9), Size = new(360, 28) };
        _titleLbl.MouseDown += TitleDrag;
        _titleBar.Controls.Add(_titleLbl);

        _btnClose = WinBtn("\u2715", Color.FromArgb(210, 70, 70));
        _btnMax   = WinBtn("\u25A1", Theme.GrayH);
        _btnMin   = WinBtn("\u2014", Theme.GrayH);
        _btnClose.Click += (_, _) => Close();
        _btnMax.Click   += (_, _) => ToggleMaximize();
        _btnMin.Click   += (_, _) => WindowState = FormWindowState.Minimized;
        _titleBar.Controls.AddRange(new Control[] { _btnMin, _btnMax, _btnClose });
        LayoutWinButtons();
        _titleBar.Resize += (_, _) => { LayoutWinButtons(); _titleBar.Invalidate(); };

        // ---- القائمة الجانبية ----
        _sidebar = new Panel { Location = new(0, 46), Size = new(180, 614), Dock = DockStyle.Left, BackColor = Theme.Panel };
        Controls.Add(_sidebar);

        // ---- منطقة المحتوى ----
        _content = new Panel { BackColor = Theme.Dark, Dock = DockStyle.Fill };
        Controls.Add(_content);

        // أنشئ الأقسام
        _tpDashboard = NewSection(); _tpClean = NewSection(); _tpLarge = NewSection(); _tpDup = NewSection();
        _tpUninstall = NewSection(); _tpStartup = NewSection(); _tpProc = NewSection(); _tpUsers = NewSection();
        _tpServices = NewSection(); _tpTasks = NewSection(); _tpSched = NewSection(); _tpHistory = NewSection();

        BuildDashboard(_tpDashboard);
        BuildCleanTab(_tpClean);
        BuildLargeTab(_tpLarge);
        BuildDuplicatesTab(_tpDup);
        BuildUninstallTab(_tpUninstall);
        BuildStartupTab(_tpStartup);
        BuildProcessTab(_tpProc);
        BuildUsersTab(_tpUsers);
        BuildServicesTab(_tpServices);
        BuildTasksTab(_tpTasks);
        BuildScheduleTab(_tpSched);
        BuildHistoryTab(_tpHistory);

        // أزرار التنقّل
        AddNav("tabDashboard", _tpDashboard);
        AddNav("tabClean",     _tpClean);
        AddNav("tabLarge",     _tpLarge);
        AddNav("tabDup",       _tpDup);
        AddNav("tabUninstall", _tpUninstall);
        AddNav("tabStartup",   _tpStartup);
        AddNav("tabProc",      _tpProc);
        AddNav("tabServices",  _tpServices);
        AddNav("tabTasks",     _tpTasks);
        AddNav("tabUsers",     _tpUsers);
        AddNav("tabSchedule",  _tpSched);
        AddNav("tabHistory",   _tpHistory);

        // أسفل القائمة: اللغة + التحديث + الإصدار
        _btnLang = new Button { Size = new(150, 30), Location = new(15, 470), FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Gray, ForeColor = Theme.TextCol, Font = Theme.Main, Cursor = Cursors.Hand, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        _btnLang.FlatAppearance.BorderSize = 0;
        _btnLang.Click += (_, _) => { Loc.Lang = Loc.Lang == "ar" ? "en" : "ar"; ApplyLanguage(); SaveSettings(); };
        _sidebar.Controls.Add(_btnLang);

        _lnkUpdate = new LinkLabel { LinkColor = Theme.Link, ActiveLinkColor = Theme.AccentH, Font = Theme.Main,
            Location = new(15, 506), Size = new(160, 20), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        _lnkUpdate.LinkClicked += async (_, _) => await CheckUpdate();
        _sidebar.Controls.Add(_lnkUpdate);

        _credit = new Label { Text = $"Developer by\nMohammed Majid\nv{App.Version}", ForeColor = Theme.Muted, Font = new Font("Segoe UI", 8F),
            Location = new(15, 532), Size = new(160, 50), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        _sidebar.Controls.Add(_credit);

        // مؤقتات
        _ramTimer = new System.Windows.Forms.Timer { Interval = 600000 };
        _ramTimer.Tick += (_, _) => { DiskCleaner.Core.NativeMemory.FreeAll(); UpdateHeader(); _status.Text = $"{Loc.T("ramDone")} @ {DateTime.Now:HH:mm}"; Logger.Log("Auto RAM free");
            if (!Visible) _tray.ShowBalloonTip(1500, "Disk & RAM Cleaner", Loc.T("ramDone"), ToolTipIcon.Info); };
        _cpuTimer = new System.Windows.Forms.Timer { Interval = 1500 };
        _cpuTimer.Tick += (_, _) =>
        {
            _cpuPct = SystemInfo.GetCpuUsage(); _cpuBar.Invalidate();
            _cpuHistory.Add(_cpuPct); if (_cpuHistory.Count > 120) _cpuHistory.RemoveAt(0);
            if (_tpDashboard.Visible) { _cpuGraph.Invalidate(); RefreshDashboard(); }
            if (_cpuPct >= 90) { _highLoadStreak++; if (_highLoadStreak == 6 && !Visible) _tray.ShowBalloonTip(2500, "Disk & RAM Cleaner", $"{Loc.T("cpuHigh")} ({_cpuPct}%)", ToolTipIcon.Warning); }
            else _highLoadStreak = 0;
        };
        _cpuTimer.Start();

        BuildTray();
        Resize += (_, _) => { if (WindowState == FormWindowState.Minimized) { Hide(); _tray.ShowBalloonTip(1500, "Disk & RAM Cleaner", Loc.T("trayMin"), ToolTipIcon.Info); } };
        FormClosing += (_, _) => { SaveSettings(); Logger.Log("Closed"); try { _cpuTimer.Stop(); _tray.Visible = false; _tray.Dispose(); } catch { } };

        // ترتيب الإرساء: العنوان يمتد بعرض النافذة كامل فوق القائمة والمحتوى
        Controls.SetChildIndex(_content, 0);
        Controls.SetChildIndex(_sidebar, 1);
        Controls.SetChildIndex(_titleBar, 2);
    }

    private Panel NewSection()
    {
        // حجم صريح مطابق لمنطقة المحتوى حتى تُحسب مراجع الإرساء (Anchor) بشكل صحيح
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Dark, Visible = false, Size = new Size(740, 614), Padding = new Padding(16, 12, 16, 12) };
        _content.Controls.Add(p);
        return p;
    }

    private void AddNav(string key, Panel panel)
    {
        int idx = _sections.Count;
        var b = new Button { Size = new(180, 36), Location = new(0, idx * 38), FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Panel, ForeColor = Theme.Muted, Font = Theme.Main, Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0) };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Theme.GrayH;
        int captured = idx;
        b.Click += (_, _) => ShowSection(captured);
        _sidebar.Controls.Add(b);
        _sections.Add((b, panel, key));
    }

    private void ShowSection(int index)
    {
        for (int i = 0; i < _sections.Count; i++)
        {
            bool on = i == index;
            _sections[i].panel.Visible = on;
            _sections[i].nav.BackColor = on ? Theme.Accent : Theme.Panel;
            _sections[i].nav.ForeColor = on ? Color.White : Theme.Muted;
        }
        if (index >= 0 && index < _sections.Count) _sections[index].panel.BringToFront();
        if (_tpDashboard.Visible) RefreshDashboard();
    }

    private Button WinBtn(string text, Color hover)
    {
        var b = new Button { Size = new(46, 46), Text = text, FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent,
            ForeColor = Color.White, Font = new Font("Segoe UI", 11F), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = hover;
        return b;
    }
    private void LayoutWinButtons()
    {
        int w = _titleBar.ClientSize.Width;
        _btnClose.Location = new(w - 46, 0);
        _btnMax.Location = new(w - 92, 0);
        _btnMin.Location = new(w - 138, 0);
    }
    private void TitleDrag(object? s, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessageW(Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero); // WM_NCLBUTTONDOWN, HTCAPTION
    }
    private void ToggleMaximize()
    {
        if (WindowState == FormWindowState.Normal) { MaximizedBounds = Screen.FromHandle(Handle).WorkingArea; WindowState = FormWindowState.Maximized; }
        else WindowState = FormWindowState.Normal;
    }

    // تغيير الحجم من الحواف (نافذة بلا إطار)
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
        {
            base.WndProc(ref m);
            int x = (short)(m.LParam.ToInt64() & 0xFFFF);
            int y = (short)((m.LParam.ToInt64() >> 16) & 0xFFFF);
            var p = PointToClient(new Point(x, y));
            const int g = 6; int w = ClientSize.Width, h = ClientSize.Height;
            bool l = p.X <= g, r = p.X >= w - g, t = p.Y <= g, bo = p.Y >= h - g;
            if (t && l) m.Result = (IntPtr)13; else if (t && r) m.Result = (IntPtr)14;
            else if (bo && l) m.Result = (IntPtr)16; else if (bo && r) m.Result = (IntPtr)17;
            else if (l) m.Result = (IntPtr)10; else if (r) m.Result = (IntPtr)11;
            else if (t) m.Result = (IntPtr)12; else if (bo) m.Result = (IntPtr)15;
            return;
        }
        base.WndProc(ref m);
    }

    // ===================== اللوحة الرئيسية =====================
    private void BuildDashboard(Panel tp)
    {
        var head = new Label { Text = "", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Theme.TextCol,
            Location = new(4, 4), Size = new(600, 34), Name = "dashHead" };
        tp.Controls.Add(head);

        _cardDiskVal  = AddCard(tp, 4, 52, Theme.Accent, "cardDisk", out _);
        _cardRamVal   = AddCard(tp, 250, 52, Color.FromArgb(45,140,110), "cardRam", out _);
        _cardCpuVal   = AddCard(tp, 4, 168, Color.FromArgb(84,158,168), "cardCpu", out _);
        _cardFreedVal = AddCard(tp, 250, 168, Color.FromArgb(205,160,60), "cardFreed", out _);

        // رسم بياني حيّ للمعالج
        _cpuGraph = new Panel { Location = new(500, 52), Size = new(232, 220), BackColor = Theme.Panel };
        _cpuGraph.Paint += CpuGraph_Paint;
        _cpuGraph.Resize += (_, _) => _cpuGraph.Invalidate();
        tp.Controls.Add(_cpuGraph);

        _cpuInfoLbl = new Label { ForeColor = Theme.Muted, Font = Theme.Main, Location = new(4, 284), Size = new(728, 22) };
        tp.Controls.Add(_cpuInfoLbl);

        _btnBoost = MakeBtn(4, 316, 300, Color.FromArgb(45,140,110), Color.FromArgb(56,160,126)); _btnBoost.Name = "dashBoost";
        _btnBoost.Click += (_, _) => RunBoost();
        var bRam = MakeBtn(312, 316, 200, Theme.Purple, Theme.PurpleH); bRam.Name = "dashRam";
        bRam.Click += (_, _) => RunFreeRam();
        var bClean = MakeBtn(520, 316, 210, Theme.Accent, Theme.AccentH); bClean.Name = "dashClean";
        bClean.Click += (_, _) => ShowSection(1);
        tp.Controls.AddRange(new Control[] { _btnBoost, bRam, bClean });
    }

    private Label AddCard(Panel parent, int x, int y, Color accent, string titleName, out Label title)
    {
        var card = new Panel { Location = new(x, y), Size = new(236, 104), BackColor = Theme.Panel };
        var strip = new Panel { Location = new(0, 0), Size = new(5, 104), BackColor = accent };
        title = new Label { Name = titleName, ForeColor = Theme.Muted, Font = Theme.Main, Location = new(18, 16), Size = new(210, 22) };
        var val = new Label { ForeColor = Theme.TextCol, Font = new Font("Segoe UI", 22F, FontStyle.Bold), Location = new(16, 44), Size = new(214, 44) };
        card.Controls.AddRange(new Control[] { strip, title, val });
        parent.Controls.Add(card);
        return val;
    }

    private void RefreshDashboard()
    {
        _cardDiskVal.Text = $"{SystemInfo.GetFreeGB()} GB";
        var r = SystemInfo.GetRam();
        _cardRamVal.Text = $"{r.usedPct}%";
        _cardCpuVal.Text = $"{_cpuPct}%";
        _cardFreedVal.Text = $"{History.TotalFreedGb():N1} GB";
        _cpuInfoLbl.Text = $"{SystemInfo.CpuName()}  ·  {Environment.ProcessorCount} {Loc.T("cores")}";
    }

    private void CpuGraph_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics; int w = _cpuGraph.ClientSize.Width, h = _cpuGraph.ClientSize.Height;
        if (w <= 0 || h <= 0) return;
        using (var bg = new SolidBrush(Theme.Panel)) g.FillRectangle(bg, 0, 0, w, h);
        using (var grid = new Pen(Color.FromArgb(60, 64, 72))) for (int i = 1; i < 4; i++) { int gy = h * i / 4; g.DrawLine(grid, 0, gy, w, gy); }
        using (var f = new Font("Segoe UI", 8F, FontStyle.Bold)) using (var tb = new SolidBrush(Theme.Muted))
            g.DrawString($"{Loc.T("cpuHistory")}  {_cpuPct}%", f, tb, 6, 4);
        if (_cpuHistory.Count < 2) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int n = _cpuHistory.Count; var pts = new PointF[n];
        for (int i = 0; i < n; i++) { float x = (float)i / (n - 1) * (w - 4) + 2; float y = h - 2 - (_cpuHistory[i] / 100f * (h - 26)); pts[i] = new PointF(x, y); }
        using var pen = new Pen(Theme.Accent, 2f); g.DrawLines(pen, pts);
    }

    // ===================== تبويب التنظيف =====================
    private void BuildCleanTab(Panel tp)
    {
        var wideLR = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _info = new Label { ForeColor = Theme.Muted, Location = new(4, 4), Size = new(680, 20), Anchor = wideLR };
        tp.Controls.Add(_info);

        _ramBar = new Panel { Location = new(4, 28), Size = new(680, 18), Anchor = wideLR };
        _ramBar.Paint += RamBar_Paint; _ramBar.Resize += (_, _) => _ramBar.Invalidate();
        tp.Controls.Add(_ramBar);

        _cpuBar = new Panel { Location = new(4, 48), Size = new(680, 18), Anchor = wideLR };
        _cpuBar.Paint += CpuBar_Paint; _cpuBar.Resize += (_, _) => _cpuBar.Invalidate();
        tp.Controls.Add(_cpuBar);

        _catPanel = new Panel { Location = new(4, 72), Size = new(680, 210), BackColor = Theme.Panel, AutoScroll = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        tp.Controls.Add(_catPanel);
        int y = 10;
        foreach (var c in _cats)
        {
            var cb = new CheckBox { ForeColor = Theme.TextCol, Font = Theme.Main, Location = new(14, y), Size = new(340, 24), Checked = true };
            cb.CheckedChanged += (_, _) => SaveSettings();
            _catPanel.Controls.Add(cb); _checks[c.Key] = cb;
            var sl = new Label { Text = "--", ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleRight, Location = new(400, y), Size = new(160, 24) };
            _catPanel.Controls.Add(sl); _sizeLabels[c.Key] = sl;
            y += 30;
        }

        _total = new Label { Font = Theme.Bold, ForeColor = Theme.AccentH, Location = new(4, 296), Size = new(680, 22),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        tp.Controls.Add(_total);

        _chart = new Panel { Location = new(4, 322), Size = new(680, 20), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        _chart.Paint += Chart_Paint; _chart.Resize += (_, _) => _chart.Invalidate();
        tp.Controls.Add(_chart);

        _progress = new ProgressBar { Location = new(4, 348), Size = new(680, 14), Style = ProgressBarStyle.Continuous, Visible = false,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        tp.Controls.Add(_progress);

        _status = new Label { ForeColor = Theme.Muted, Location = new(4, 366), Size = new(680, 20),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        tp.Controls.Add(_status);

        _btnAnalyze = MakeBtn(4, 392, 210, Theme.Gray, Theme.GrayH);
        _btnClean   = MakeBtn(224, 392, 220, Theme.Accent, Theme.AccentH);
        _btnRam     = MakeBtn(454, 392, 230, Theme.Purple, Theme.PurpleH);
        foreach (var b in new[] { _btnAnalyze, _btnClean, _btnRam }) b.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnAnalyze.Click += (_, _) => RunAnalyze();
        _btnClean.Click   += (_, _) => RunClean();
        _btnRam.Click     += (_, _) => RunFreeRam();
        tp.Controls.AddRange(new Control[] { _btnAnalyze, _btnClean, _btnRam });

        _chkAuto = new CheckBox { ForeColor = Theme.TextCol, Font = Theme.Main, Location = new(4, 444), Size = new(680, 22), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        _chkAuto.CheckedChanged += Auto_Changed;
        tp.Controls.Add(_chkAuto);
        _chkRestore = new CheckBox { ForeColor = Theme.TextCol, Font = Theme.Main, Location = new(4, 470), Size = new(680, 22), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        _chkRestore.CheckedChanged += (_, _) => SaveSettings();
        tp.Controls.Add(_chkRestore);
    }

    // ===================== أدوات مشتركة =====================
    private Button MakeBtn(int x, int y, int w, Color baseCol, Color hover)
    {
        var b = new Button { Size = new(w, 42), Location = new(x, y), FlatStyle = FlatStyle.Flat, BackColor = baseCol,
            ForeColor = Color.White, Font = Theme.Bold, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = 0;
        b.MouseEnter += (_, _) => b.BackColor = hover;
        b.MouseLeave += (_, _) => b.BackColor = baseCol;
        return b;
    }
    private static ListView MakeList(int x, int y, int w, int h)
        => new() { Location = new(x, y), Size = new(w, h), View = View.Details, FullRowSelect = true, BackColor = Theme.Panel, ForeColor = Theme.TextCol, BorderStyle = BorderStyle.FixedSingle };

    private void BuildTray()
    {
        var ctx = new ContextMenuStrip();
        _miShow = new ToolStripMenuItem("Show app", null, (_, _) => ShowApp());
        _miRam  = new ToolStripMenuItem("Free RAM now", null, (_, _) => { DiskCleaner.Core.NativeMemory.FreeAll(); _ramPct = SystemInfo.GetRam().usedPct; _ramBar.Invalidate();
            _tray.ShowBalloonTip(1500, "Disk & RAM Cleaner", Loc.T("ramDone"), ToolTipIcon.Info); });
        _miExit = new ToolStripMenuItem("Exit", null, (_, _) => Close());
        ctx.Items.AddRange(new ToolStripItem[] { _miShow, _miRam, new ToolStripSeparator(), _miExit });
        _tray = new NotifyIcon { Icon = Icon ?? SystemIcons.Application, Text = "Disk & RAM Cleaner", Visible = true, ContextMenuStrip = ctx };
        _tray.DoubleClick += (_, _) => ShowApp();
    }
    private void ShowApp() { Show(); WindowState = FormWindowState.Normal; Activate(); }

    // ===================== الرسوم =====================
    private void RamBar_Paint(object? sender, PaintEventArgs e) => DrawBar(e, _ramBar, _ramPct, "RAM", _ramPct < 70 ? Color.FromArgb(0,180,120) : _ramPct < 88 ? Color.FromArgb(230,160,40) : Color.FromArgb(215,70,70));
    private void CpuBar_Paint(object? sender, PaintEventArgs e) => DrawBar(e, _cpuBar, _cpuPct, "CPU", _cpuPct < 60 ? Color.FromArgb(0,170,200) : _cpuPct < 85 ? Color.FromArgb(230,160,40) : Color.FromArgb(215,70,70));
    private static void DrawBar(PaintEventArgs e, Panel bar, int pct, string label, Color col)
    {
        int w = bar.ClientSize.Width, h = bar.ClientSize.Height; if (w <= 0 || h <= 0) return;
        var g = e.Graphics;
        using (var bg = new SolidBrush(Theme.Gray)) g.FillRectangle(bg, 0, 0, w, h);
        int fw = w * pct / 100;
        if (fw > 0) using (var fb = new SolidBrush(col)) g.FillRectangle(fb, 0, 0, fw, h);
        using var f = new Font("Segoe UI", 8F, FontStyle.Bold);
        using var tb = new SolidBrush(Color.White);
        g.DrawString($"{label}  {pct}%", f, tb, 6, 1);
    }

    private void Chart_Paint(object? sender, PaintEventArgs e)
    {
        int w = _chart.ClientSize.Width, h = _chart.ClientSize.Height; if (w <= 0 || h <= 0) return;
        var g = e.Graphics;
        using (var bg = new SolidBrush(Theme.Gray)) g.FillRectangle(bg, 0, 0, w, h);
        if (!_analyzed || _lastTotal <= 0) return;
        double x = 0; int idx = 0;
        foreach (var c in _cats)
        {
            long sz = _sizes.TryGetValue(c.Key, out var v) ? v : 0;
            if (sz > 0) { double seg = (double)w * sz / _lastTotal; using var b = new SolidBrush(Theme.Palette[idx % Theme.Palette.Length]); g.FillRectangle(b, (int)x, 0, Math.Max(1, (int)seg), h); x += seg; }
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
        _titleLbl.Text = Loc.T("title");
        _btnLang.Text = Loc.T("langBtn"); _lnkUpdate.Text = Loc.T("checkUpdate");
        foreach (var s in _sections) s.nav.Text = "  " + Loc.T(s.key);
        _miShow.Text = Loc.T("trayShow"); _miRam.Text = Loc.T("trayRam"); _miExit.Text = Loc.T("trayExit");

        // اللوحة الرئيسية
        SetText(_tpDashboard, "dashHead", Loc.T("title"));
        SetText(_tpDashboard, "cardDisk", Loc.T("diskFree"));
        SetText(_tpDashboard, "cardRam", Loc.T("ramUsed"));
        SetText(_tpDashboard, "cardCpu", "CPU");
        SetText(_tpDashboard, "cardFreed", Loc.T("totalFreed"));
        _btnBoost.Text = _boosted ? Loc.T("restoreBoost") : Loc.T("boost");
        SetBtn(_tpDashboard, "dashRam", Loc.T("freeRam"));
        SetBtn(_tpDashboard, "dashClean", Loc.T("tabClean"));
        RefreshDashboard();

        // تبويب التنظيف
        _btnAnalyze.Text = Loc.T("analyze"); _btnClean.Text = Loc.T("cleanSel"); _btnRam.Text = Loc.T("freeRam");
        _chkAuto.Text = Loc.T("autoRam"); _chkRestore.Text = Loc.T("restore");
        foreach (var c in _cats) _checks[c.Key].Text = c.Name(Loc.Lang);
        _total.Text = _analyzed ? $"{Loc.T("totalClean")}: {Theme.FormatSize(_lastTotal)}" : Loc.T("pressAnalyze");

        ApplyTabsLanguage(); ApplyUninstallLanguage(); ApplyUsersLanguage(); ApplySystemLanguage();
        UpdateHeader();
        ResumeLayout(); Refresh();
    }

    private static void SetText(Panel p, string name, string text) { var c = p.Controls.Find(name, true); if (c.Length > 0) c[0].Text = text; }
    private static void SetBtn(Panel p, string name, string text) { var c = p.Controls.Find(name, true); if (c.Length > 0) c[0].Text = text; }

    // ===================== إجراءات التنظيف =====================
    private void RunAnalyze()
    {
        _btnAnalyze.Enabled = _btnClean.Enabled = false;
        _progress.Maximum = _cats.Count; _progress.Value = 0; _progress.Visible = true;
        long total = 0; int i = 0;
        foreach (var c in _cats)
        {
            i++; _status.Text = $"{Loc.T("analyzing")}: {c.Name(Loc.Lang)}..."; Application.DoEvents();
            long s = Cleaner.GetSize(c); _sizes[c.Key] = s; _sizeLabels[c.Key].Text = Theme.FormatSize(s);
            total += s; _progress.Value = i; Application.DoEvents();
        }
        _analyzed = true; _lastTotal = total;
        int idx = 0; foreach (var c in _cats) { _sizeLabels[c.Key].ForeColor = _sizes[c.Key] > 0 ? Theme.Palette[idx % Theme.Palette.Length] : Theme.Muted; idx++; }
        _total.Text = $"{Loc.T("totalClean")}: {Theme.FormatSize(total)}";
        _status.Text = Loc.T("doneAnalyze"); UpdateHeader(); _chart.Invalidate();
        Logger.Log($"Analyzed. Total: {Theme.FormatSize(total)}");
        _progress.Value = 0; _progress.Visible = false;
        _btnAnalyze.Enabled = _btnClean.Enabled = true;
    }

    private void RunClean()
    {
        var sel = _cats.Where(c => _checks[c.Key].Checked).ToList();
        if (sel.Count == 0) { MessageBox.Show(Loc.T("noSelect"), Loc.T("warn"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        string names = string.Join("\n", sel.Select(c => "- " + c.Name(Loc.Lang)));
        if (MessageBox.Show($"{Loc.T("willDelete")}\n\n{names}\n\n{Loc.T("permanent")}", Loc.T("confirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        double before = SystemInfo.GetFreeGB();
        _btnAnalyze.Enabled = _btnClean.Enabled = false;
        if (_chkRestore.Checked) { _status.Text = Loc.T("restoring"); Application.DoEvents(); CreateRestorePoint(); }
        _progress.Maximum = sel.Count; _progress.Value = 0; _progress.Visible = true; int i = 0;
        foreach (var c in sel)
        {
            i++; _status.Text = $"{Loc.T("cleaning")}: {c.Name(Loc.Lang)}..."; Application.DoEvents();
            Cleaner.Clean(c); _sizeLabels[c.Key].Text = "0 B"; _sizes[c.Key] = 0; _progress.Value = i; Application.DoEvents();
        }
        double after = SystemInfo.GetFreeGB(); double freed = Math.Round(after - before, 2);
        UpdateHeader(); _chart.Invalidate(); _status.Text = Loc.T("doneClean");
        _progress.Value = 0; _progress.Visible = false;
        History.Add(freed, sel.Select(c => c.Key)); RefreshHistory();
        Logger.Log($"Cleaned [{string.Join(", ", sel.Select(c => c.Key))}]. Freed {freed} GB");
        _btnAnalyze.Enabled = _btnClean.Enabled = true;
        MessageBox.Show($"{Loc.T("cleanOk")}\n\n{Loc.T("before")}: {before} GB\n{Loc.T("after")}: {after} GB\n{Loc.T("freed")}: {freed} GB", Loc.T("resultTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RunFreeRam()
    {
        _btnRam.Enabled = false;
        var b = SystemInfo.GetRam(); _status.Text = Loc.T("freeingRam"); Application.DoEvents();
        DiskCleaner.Core.NativeMemory.FreeAll(); Thread.Sleep(600);
        var a = SystemInfo.GetRam(); UpdateHeader(); _status.Text = Loc.T("ramDone"); _btnRam.Enabled = true;
        double freed = Math.Round(a.freeGb - b.freeGb, 2); Logger.Log($"RAM freed: {freed} GB");
        MessageBox.Show($"{Loc.T("ramDone")}\n\n{Loc.T("before")}: {b.freeGb} GB ({b.usedPct}% {Loc.T("used")})\n{Loc.T("after")}: {a.freeGb} GB ({a.usedPct}% {Loc.T("used")})\n{Loc.T("freed")}: {freed} GB", Loc.T("ramTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RunBoost()
    {
        if (!_boosted)
        {
            _prevScheme = PowerPlan.GetActiveScheme();     // احفظ الخطة الحالية للرجوع
            PowerPlan.HighPerformance();
            DiskCleaner.Core.NativeMemory.FreeAll();
            _boosted = true; _btnBoost.Text = Loc.T("restoreBoost");
            _cpuPct = SystemInfo.GetCpuUsage(); UpdateHeader(); RefreshDashboard();
            Logger.Log("Boost applied");
            MessageBox.Show(Loc.T("boostDone"), Loc.T("boost"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            if (!string.IsNullOrEmpty(_prevScheme)) PowerPlan.RestoreScheme(_prevScheme); else PowerPlan.Balanced();
            _boosted = false; _btnBoost.Text = Loc.T("boost");
            Logger.Log("Boost reverted");
            MessageBox.Show(Loc.T("boostRestored"), Loc.T("boost"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void Auto_Changed(object? sender, EventArgs e)
    {
        if (_chkAuto.Checked) { _ramTimer.Start(); _status.Text = Loc.T("autoOn"); } else { _ramTimer.Stop(); _status.Text = Loc.T("autoOff"); }
        SaveSettings();
    }

    private static void CreateRestorePoint()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"Enable-ComputerRestore -Drive 'C:\\'; Checkpoint-Computer -Description 'DiskCleaner' -RestorePointType 'MODIFY_SETTINGS'\"")
            { UseShellExecute = false, CreateNoWindow = true };
            Process.Start(psi)?.WaitForExit(60000); Logger.Log("Restore point requested");
        }
        catch (Exception ex) { Logger.Log($"Restore point failed: {ex.Message}"); }
    }

    private async Task CheckUpdate(bool announce = true)
    {
        try
        {
            string? latest = await Updater.GetLatestVersionAsync();
            if (latest == null) { if (announce) MessageBox.Show(Loc.T("updFail"), Loc.T("updTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!(Version.TryParse(latest, out var lv) && Version.TryParse(App.Version, out var cv) && lv > cv))
            { if (announce) MessageBox.Show(Loc.T("updLatest"), Loc.T("updTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show($"{Loc.T("updAvail")}: {latest}\n\n{Loc.T("updInstall")}", Loc.T("updTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;

            string exe = App.ExePath;
            bool canSelf = !string.IsNullOrEmpty(exe) && exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(exe).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase);
            if (!canSelf) { Process.Start(new ProcessStartInfo($"https://github.com/{App.RepoOwner}/{App.RepoName}/releases/latest") { UseShellExecute = true }); return; }

            ShowSection(1);
            _lnkUpdate.Enabled = false;
            _progress.Maximum = 100; _progress.Value = 0; _progress.Visible = true; _status.Text = Loc.T("downloading");
            string newExe = Path.Combine(App.DataDir, "DiskCleaner_new.exe");
            var progress = new Progress<int>(p => { _progress.Value = Math.Clamp(p, 0, 100); _status.Text = $"{Loc.T("downloading")} {p}%"; });
            await Updater.DownloadAsync(newExe, progress);
            _status.Text = Loc.T("updReady"); Logger.Log($"Update {App.Version} -> {latest}");
            Updater.ApplyAndRestart(newExe);
            Close();
        }
        catch (Exception ex) { _lnkUpdate.Enabled = true; Logger.Log($"Update failed: {ex.Message}"); if (announce) MessageBox.Show(Loc.T("updFail"), Loc.T("updTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void ApplySettings()
    {
        foreach (var c in _cats) _checks[c.Key].Checked = !_settings.Unchecked.Contains(c.Key);
        _chkRestore.Checked = _settings.RestorePoint; _chkAuto.Checked = _settings.AutoRam;
    }
    private void SaveSettings()
    {
        _settings.Lang = Loc.Lang; _settings.AutoRam = _chkAuto.Checked; _settings.RestorePoint = _chkRestore.Checked;
        _settings.Unchecked = _cats.Where(c => !_checks[c.Key].Checked).Select(c => c.Key).ToList();
        _settings.Save();
    }
}
