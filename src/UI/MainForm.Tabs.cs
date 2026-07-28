using System.Diagnostics;
using DiskCleaner.Core;
using DiskCleaner.Services;

namespace DiskCleaner.UI;

public partial class MainForm
{
    // ---- الملفات الكبيرة ----
    private ComboBox _cmbLarge = null!;
    private Button _btnScanLarge = null!, _btnDelLarge = null!;
    private Label _lblLarge = null!;
    private ListView _lvLarge = null!;
    private ColumnHeader _lgName = null!, _lgSize = null!, _lgPath = null!;

    // ---- المكرّرات ----
    private ComboBox _cmbDup = null!;
    private Button _btnScanDup = null!, _btnDelDup = null!;
    private Label _lblDup = null!;
    private ListView _lvDup = null!;
    private ColumnHeader _dName = null!, _dSize = null!, _dCopies = null!, _dPath = null!;

    // ---- بدء التشغيل ----
    private Button _btnRefStartup = null!, _btnEnStartup = null!, _btnDisStartup = null!;
    private ListView _lvStartup = null!;
    private ColumnHeader _sName = null!, _sStatus = null!, _sScope = null!, _sCmd = null!;

    // ---- العمليات ----
    private Button _btnRefProc = null!, _btnKill = null!, _btnSuspend = null!, _btnResume = null!, _btnPrioNormal = null!, _btnPrioBelow = null!, _btnPrioIdle = null!;
    private Button _btnPwrHigh = null!, _btnPwrBal = null!;
    private Label _lblPower = null!;
    private TextBox _procSearch = null!;
    private List<ProcInfo> _procList = new();
    private ListView _lvProc = null!;
    private ColumnHeader _pName = null!, _pPid = null!, _pCpu = null!, _pMem = null!;

    // ---- الجدولة ----
    private Label _lblSchedStatus = null!, _lblSchedInfo = null!;
    private Button _btnSchedOn = null!, _btnSchedOff = null!;

    // ---- السجل ----
    private Label _lblTotalFreed = null!;
    private Button _btnClearHist = null!;
    private ListView _lvHist = null!;
    private ColumnHeader _hDate = null!, _hFreed = null!, _hCats = null!;

    private CancellationTokenSource? _scanCts;

    private static readonly AnchorStyles Fill = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

    private static void StyleList(ListView lv) { lv.View = View.Details; lv.FullRowSelect = true; lv.BackColor = Theme.Panel; lv.ForeColor = Theme.TextCol; lv.Anchor = Fill; }

    private void FillDrives(ComboBox c)
    {
        c.Items.Clear();
        foreach (var d in SystemInfo.FixedDrives()) c.Items.Add(d.Name);
        if (c.Items.Count > 0) c.SelectedIndex = 0;
    }

    // ============================ الملفات الكبيرة ============================
    private void BuildLargeTab(Panel tp)
    {
        _cmbLarge = new ComboBox { Location = new(12, 12), Size = new(90, 26), DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.Panel, ForeColor = Theme.TextCol, FlatStyle = FlatStyle.Flat };
        FillDrives(_cmbLarge);
        _btnScanLarge = MakeBtn(112, 8, 110, Theme.Accent, Theme.AccentH); _btnScanLarge.Size = new(110, 30);
        _btnDelLarge  = MakeBtn(230, 8, 150, Theme.Gray, Theme.GrayH);    _btnDelLarge.Size = new(150, 30);
        _lblLarge = new Label { ForeColor = Theme.Muted, Location = new(390, 14), Size = new(200, 20) };
        _lvLarge = MakeList(12, 46, 576, 448); StyleList(_lvLarge);
        _lgName = _lvLarge.Columns.Add("Name", 200); _lgSize = _lvLarge.Columns.Add("Size", 90); _lgPath = _lvLarge.Columns.Add("Path", 280);
        _btnScanLarge.Click += (_, _) => ScanLarge();
        _btnDelLarge.Click  += (_, _) => DeleteFromList(_lvLarge, 2);
        tp.Controls.AddRange(new Control[] { _cmbLarge, _btnScanLarge, _btnDelLarge, _lblLarge, _lvLarge });
    }

    private void ScanLarge()
    {
        if (_cmbLarge.SelectedItem is not string drive) return;
        _lvLarge.Items.Clear(); _lblLarge.Text = Loc.T("scanning"); _btnScanLarge.Enabled = false;
        _scanCts = new CancellationTokenSource(); var ct = _scanCts.Token;
        Task.Run(() => { try { return LargeFilesFinder.Find(drive, 100, ct); } catch { return null; } })
            .ContinueWith(t =>
            {
                var res = t.Result;
                BeginInvoke((MethodInvoker)(() =>
                {
                    _btnScanLarge.Enabled = true;
                    if (res == null) { _lblLarge.Text = Loc.T("scanDone"); return; }
                    foreach (var f in res)
                    {
                        var it = new ListViewItem(Path.GetFileName(f.Path));
                        it.SubItems.Add(Theme.FormatSize(f.Size)); it.SubItems.Add(f.Path);
                        _lvLarge.Items.Add(it);
                    }
                    _lblLarge.Text = res.Count == 0 ? Loc.T("noItems") : Loc.T("scanDone");
                }));
            });
    }

    // ============================ المكرّرات ============================
    private void BuildDuplicatesTab(Panel tp)
    {
        _cmbDup = new ComboBox { Location = new(12, 12), Size = new(90, 26), DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.Panel, ForeColor = Theme.TextCol, FlatStyle = FlatStyle.Flat };
        FillDrives(_cmbDup);
        _btnScanDup = MakeBtn(112, 8, 110, Theme.Accent, Theme.AccentH); _btnScanDup.Size = new(110, 30);
        _btnDelDup  = MakeBtn(230, 8, 150, Theme.Gray, Theme.GrayH);    _btnDelDup.Size = new(150, 30);
        _lblDup = new Label { ForeColor = Theme.Muted, Location = new(390, 14), Size = new(200, 20) };
        _lvDup = MakeList(12, 46, 576, 448); StyleList(_lvDup);
        _dName = _lvDup.Columns.Add("Name", 200); _dSize = _lvDup.Columns.Add("Size", 80); _dCopies = _lvDup.Columns.Add("Copies", 60); _dPath = _lvDup.Columns.Add("Path", 230);
        _btnScanDup.Click += (_, _) => ScanDup();
        _btnDelDup.Click  += (_, _) => DeleteFromList(_lvDup, 3);
        tp.Controls.AddRange(new Control[] { _cmbDup, _btnScanDup, _btnDelDup, _lblDup, _lvDup });
    }

    private void ScanDup()
    {
        if (_cmbDup.SelectedItem is not string drive) return;
        _lvDup.Items.Clear(); _lblDup.Text = Loc.T("scanning"); _btnScanDup.Enabled = false;
        _scanCts = new CancellationTokenSource(); var ct = _scanCts.Token;
        Task.Run(() => { try { return DuplicateFinder.Find(drive, 1_048_576, ct); } catch { return null; } })
            .ContinueWith(t =>
            {
                var res = t.Result;
                BeginInvoke((MethodInvoker)(() =>
                {
                    _btnScanDup.Enabled = true;
                    if (res == null) { _lblDup.Text = Loc.T("scanDone"); return; }
                    foreach (var g in res)
                        foreach (var f in g.Files)
                        {
                            var it = new ListViewItem(Path.GetFileName(f));
                            it.SubItems.Add(Theme.FormatSize(g.Size)); it.SubItems.Add(g.Files.Count.ToString()); it.SubItems.Add(f);
                            _lvDup.Items.Add(it);
                        }
                    _lblDup.Text = res.Count == 0 ? Loc.T("noItems") : Loc.T("scanDone");
                }));
            });
    }

    // حذف الملفات المحددة (عمود المسار = pathCol)
    private void DeleteFromList(ListView lv, int pathCol)
    {
        if (lv.SelectedItems.Count == 0) return;
        if (MessageBox.Show(Loc.T("confirmDelete"), Loc.T("warn"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        foreach (ListViewItem it in lv.SelectedItems.Cast<ListViewItem>().ToList())
        {
            try { File.SetAttributes(it.SubItems[pathCol].Text, FileAttributes.Normal); File.Delete(it.SubItems[pathCol].Text); it.Remove(); }
            catch (Exception ex) { Logger.Log($"Delete {it.SubItems[pathCol].Text} failed: {ex.Message}"); }
        }
        UpdateHeader();
    }

    // ============================ بدء التشغيل ============================
    private void BuildStartupTab(Panel tp)
    {
        _btnRefStartup = MakeBtn(12, 8, 110, Theme.Accent, Theme.AccentH); _btnRefStartup.Size = new(110, 30);
        _btnEnStartup  = MakeBtn(130, 8, 110, Theme.Gray, Theme.GrayH);    _btnEnStartup.Size = new(110, 30);
        _btnDisStartup = MakeBtn(248, 8, 110, Theme.Gray, Theme.GrayH);    _btnDisStartup.Size = new(110, 30);
        _lvStartup = MakeList(12, 46, 576, 448); StyleList(_lvStartup);
        _sName = _lvStartup.Columns.Add("Name", 160); _sStatus = _lvStartup.Columns.Add("Status", 90); _sScope = _lvStartup.Columns.Add("Scope", 70); _sCmd = _lvStartup.Columns.Add("Command", 250);
        _btnRefStartup.Click += (_, _) => RefreshStartup();
        _btnEnStartup.Click  += (_, _) => ToggleStartup(true);
        _btnDisStartup.Click += (_, _) => ToggleStartup(false);
        tp.Controls.AddRange(new Control[] { _btnRefStartup, _btnEnStartup, _btnDisStartup, _lvStartup });
    }

    private void RefreshStartup()
    {
        _lvStartup.Items.Clear();
        foreach (var s in StartupManager.List())
        {
            var it = new ListViewItem(s.Name);
            it.SubItems.Add(s.Suspicious ? "⚠ " + Loc.T("suspicious") : (s.Enabled ? Loc.T("enabled") : Loc.T("disabled")));
            it.SubItems.Add(s.Scope); it.SubItems.Add(s.Command);
            it.ForeColor = s.Suspicious ? Color.FromArgb(230, 130, 90) : (s.Enabled ? Theme.TextCol : Theme.Muted);
            _lvStartup.Items.Add(it);
        }
    }

    private void ToggleStartup(bool enable)
    {
        foreach (ListViewItem it in _lvStartup.SelectedItems) StartupManager.SetEnabled(it.Text, enable);
        RefreshStartup();
    }

    // ============================ العمليات ============================
    private void BuildProcessTab(Panel tp)
    {
        // الصف الأول: تحديث + إنهاء + تعليق/استئناف
        _btnRefProc = MakeBtn(4, 6, 90, Theme.Accent, Theme.AccentH); _btnRefProc.Size = new(90, 28);
        _btnKill    = MakeBtn(98, 6, 84, Theme.Gray, Theme.GrayH);    _btnKill.Size = new(84, 28);
        _btnSuspend = MakeBtn(186, 6, 92, Theme.Gray, Theme.GrayH);   _btnSuspend.Size = new(92, 28);
        _btnResume  = MakeBtn(282, 6, 92, Theme.Gray, Theme.GrayH);   _btnResume.Size = new(92, 28);
        // الصف الثاني: الأولوية + بحث
        _btnPrioNormal = MakeBtn(4, 38, 110, Theme.Gray, Theme.GrayH);  _btnPrioNormal.Size = new(110, 28);
        _btnPrioBelow  = MakeBtn(120, 38, 100, Theme.Gray, Theme.GrayH); _btnPrioBelow.Size = new(100, 28);
        _btnPrioIdle   = MakeBtn(226, 38, 96, Theme.Gray, Theme.GrayH);  _btnPrioIdle.Size = new(96, 28);
        _procSearch = new TextBox { Location = new(500, 38), Size = new(228, 26), BackColor = Theme.Panel, ForeColor = Theme.TextCol,
            BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _procSearch.TextChanged += (_, _) => PopulateProc(_procSearch.Text);
        // الصف الثالث: خطة الطاقة
        _lblPower  = new Label { ForeColor = Theme.Muted, Location = new(4, 72), Size = new(60, 24), TextAlign = ContentAlignment.MiddleLeft };
        _btnPwrHigh = MakeBtn(68, 70, 140, Theme.Purple, Theme.PurpleH); _btnPwrHigh.Size = new(140, 26);
        _btnPwrBal  = MakeBtn(214, 70, 120, Theme.Gray, Theme.GrayH);    _btnPwrBal.Size = new(120, 26);

        _btnSuspend.Click += (_, _) => SuspendProc(true);
        _btnResume.Click  += (_, _) => SuspendProc(false);

        _lvProc = MakeList(4, 104, 724, 496); StyleList(_lvProc);
        _pName = _lvProc.Columns.Add("Name", 250); _pPid = _lvProc.Columns.Add("PID", 70);
        _pCpu = _lvProc.Columns.Add("CPU", 70); _pMem = _lvProc.Columns.Add("Memory", 110);

        _btnRefProc.Click    += (_, _) => RefreshProc();
        _btnKill.Click       += (_, _) => KillProc();
        _btnPrioNormal.Click += (_, _) => SetProcPriority(ProcessPriorityClass.Normal);
        _btnPrioBelow.Click  += (_, _) => SetProcPriority(ProcessPriorityClass.BelowNormal);
        _btnPrioIdle.Click   += (_, _) => SetProcPriority(ProcessPriorityClass.Idle);
        _btnPwrHigh.Click    += (_, _) => { PowerPlan.HighPerformance(); _lblStatusFlash(_lblPower); };
        _btnPwrBal.Click     += (_, _) => { PowerPlan.Balanced(); _lblStatusFlash(_lblPower); };

        tp.Controls.AddRange(new Control[] { _btnRefProc, _btnKill, _btnSuspend, _btnResume, _btnPrioNormal, _btnPrioBelow, _btnPrioIdle,
            _lblPower, _btnPwrHigh, _btnPwrBal, _procSearch, _lvProc });
    }

    private void SuspendProc(bool suspend)
    {
        if (_lvProc.SelectedItems.Count == 0) return;
        if (int.TryParse(_lvProc.SelectedItems[0].SubItems[1].Text, out var pid)) ProcessMonitor.Suspend(pid, suspend);
    }

    private static void _lblStatusFlash(Label l) { l.ForeColor = Theme.AccentH; }

    private void RefreshProc()
    {
        _btnRefProc.Enabled = false;
        Task.Run(() => ProcessMonitor.Top(60)).ContinueWith(t =>
        {
            var list = t.Result;
            BeginInvoke((MethodInvoker)(() => { _procList = list; PopulateProc(_procSearch.Text); _btnRefProc.Enabled = true; }));
        });
    }

    private void PopulateProc(string? filter)
    {
        _lvProc.Items.Clear();
        string f = (filter ?? "").Trim();
        foreach (var p in _procList)
        {
            if (f.Length > 0 && !p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)) continue;
            var it = new ListViewItem(p.Name);
            it.SubItems.Add(p.Pid.ToString());
            it.SubItems.Add($"{p.Cpu}%");
            it.SubItems.Add(Theme.FormatSize(p.Memory));
            if (p.Cpu >= 25) it.ForeColor = Color.FromArgb(235, 150, 60);
            _lvProc.Items.Add(it);
        }
    }

    private void SetProcPriority(ProcessPriorityClass cls)
    {
        if (_lvProc.SelectedItems.Count == 0) return;
        if (int.TryParse(_lvProc.SelectedItems[0].SubItems[1].Text, out var pid)) ProcessMonitor.SetPriority(pid, cls);
    }

    private void KillProc()
    {
        if (_lvProc.SelectedItems.Count == 0) return;
        if (MessageBox.Show(Loc.T("confirmKill"), Loc.T("warn"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        foreach (ListViewItem it in _lvProc.SelectedItems)
            if (int.TryParse(it.SubItems[1].Text, out var pid)) ProcessMonitor.Kill(pid);
        RefreshProc();
    }

    // ============================ الجدولة ============================
    private void BuildScheduleTab(Panel tp)
    {
        _lblSchedStatus = new Label { Font = Theme.Bold, ForeColor = Theme.AccentH, Location = new(16, 24), Size = new(560, 26) };
        _lblSchedInfo = new Label { ForeColor = Theme.Muted, Location = new(16, 56), Size = new(560, 40) };
        _btnSchedOn  = MakeBtn(16, 110, 260, Theme.Accent, Theme.AccentH);
        _btnSchedOff = MakeBtn(288, 110, 260, Theme.Gray, Theme.GrayH);
        _btnSchedOn.Click  += (_, _) => { Scheduler.Enable(App.ExePath); RefreshSched(); };
        _btnSchedOff.Click += (_, _) => { Scheduler.Disable(); RefreshSched(); };
        tp.Controls.AddRange(new Control[] { _lblSchedStatus, _lblSchedInfo, _btnSchedOn, _btnSchedOff });
    }

    private void RefreshSched()
    {
        bool on = Scheduler.Exists();
        _lblSchedStatus.Text = on ? Loc.T("schedOn") : Loc.T("schedOff");
        _lblSchedStatus.ForeColor = on ? Theme.AccentH : Theme.Muted;
    }

    // ============================ السجل ============================
    private void BuildHistoryTab(Panel tp)
    {
        _lblTotalFreed = new Label { Font = Theme.Bold, ForeColor = Theme.AccentH, Location = new(12, 12), Size = new(400, 26) };
        _btnClearHist = MakeBtn(430, 8, 158, Theme.Gray, Theme.GrayH); _btnClearHist.Size = new(158, 30);
        _lvHist = MakeList(12, 46, 576, 448); StyleList(_lvHist);
        _hDate = _lvHist.Columns.Add("Date", 150); _hFreed = _lvHist.Columns.Add("Freed", 90); _hCats = _lvHist.Columns.Add("Items", 320);
        _btnClearHist.Click += (_, _) => { try { if (File.Exists(App.HistoryFile)) File.Delete(App.HistoryFile); } catch { } RefreshHistory(); };
        tp.Controls.AddRange(new Control[] { _lblTotalFreed, _btnClearHist, _lvHist });
        RefreshHistory();
    }

    private void RefreshHistory()
    {
        if (_lvHist == null) return;
        _lvHist.Items.Clear();
        foreach (var r in History.Load())
        {
            var it = new ListViewItem(r.Date.ToString("yyyy-MM-dd HH:mm"));
            it.SubItems.Add($"{r.FreedGb} GB"); it.SubItems.Add(r.Categories);
            _lvHist.Items.Add(it);
        }
        _lblTotalFreed.Text = $"{Loc.T("totalFreed")}: {History.TotalFreedGb():N2} GB";
    }

    // ============================ اللغة للتبويبات ============================
    private void ApplyTabsLanguage()
    {
        _btnScanLarge.Text = Loc.T("scan"); _btnDelLarge.Text = Loc.T("deleteSel");
        _lgName.Text = Loc.T("colName"); _lgSize.Text = Loc.T("colSize"); _lgPath.Text = Loc.T("colPath");

        _btnScanDup.Text = Loc.T("scan"); _btnDelDup.Text = Loc.T("deleteSel");
        _dName.Text = Loc.T("colName"); _dSize.Text = Loc.T("colSize"); _dCopies.Text = Loc.T("colCount"); _dPath.Text = Loc.T("colPath");

        _btnRefStartup.Text = Loc.T("refresh"); _btnEnStartup.Text = Loc.T("enableItem"); _btnDisStartup.Text = Loc.T("disableItem");
        _sName.Text = Loc.T("colName"); _sStatus.Text = Loc.T("colStatus"); _sScope.Text = Loc.T("colScope"); _sCmd.Text = Loc.T("colCommand");

        _btnRefProc.Text = Loc.T("refresh"); _btnKill.Text = Loc.T("kill");
        _btnSuspend.Text = Loc.T("suspend"); _btnResume.Text = Loc.T("resume");
        _btnPrioNormal.Text = Loc.T("prioNormal"); _btnPrioBelow.Text = Loc.T("prioBelow"); _btnPrioIdle.Text = Loc.T("prioIdle");
        _lblPower.Text = Loc.T("powerPlan"); _btnPwrHigh.Text = Loc.T("powerHigh"); _btnPwrBal.Text = Loc.T("powerBalanced");
        _procSearch.PlaceholderText = Loc.T("searchProc");
        _pName.Text = Loc.T("colName"); _pPid.Text = Loc.T("colPid"); _pCpu.Text = Loc.T("colCpu"); _pMem.Text = Loc.T("colMem");

        _lblSchedInfo.Text = Loc.T("schedInfo"); _btnSchedOn.Text = Loc.T("enableWeekly"); _btnSchedOff.Text = Loc.T("disableWeekly");
        RefreshSched();

        _btnClearHist.Text = Loc.T("clearHistory");
        _hDate.Text = Loc.T("colDate"); _hFreed.Text = Loc.T("colFreed"); _hCats.Text = Loc.T("colCats");
        RefreshHistory();
    }
}
