using DiskCleaner.Core;
using DiskCleaner.Services;

namespace DiskCleaner.UI;

public partial class MainForm
{
    // ---- الخدمات ----
    private Button _btnRefSvc = null!, _btnSvcStart = null!, _btnSvcStop = null!, _btnSvcAuto = null!, _btnSvcManual = null!, _btnSvcDisable = null!;
    private TextBox _svcSearch = null!;
    private ListView _lvSvc = null!;
    private ColumnHeader _svcDisp = null!, _svcName = null!, _svcStatus = null!, _svcStart = null!;
    private List<ServiceInfo> _svcList = new();

    // ---- المهام المجدولة ----
    private Button _btnRefTask = null!, _btnTaskEn = null!, _btnTaskDis = null!, _btnTaskDel = null!;
    private ListView _lvTask = null!;
    private ColumnHeader _tkName = null!, _tkStatus = null!, _tkNext = null!;

    // ============================ الخدمات ============================
    private void BuildServicesTab(Panel tp)
    {
        _btnRefSvc     = MakeMini(4, 6, 90);  _btnSvcStart = MakeMini(100, 6, 90); _btnSvcStop = MakeMini(196, 6, 90);
        _btnSvcAuto    = MakeMini(4, 38, 110); _btnSvcManual = MakeMini(120, 38, 90); _btnSvcDisable = MakeMini(216, 38, 100);
        _svcSearch = new TextBox { Location = new(500, 8), Size = new(228, 26), BackColor = Theme.Panel, ForeColor = Theme.TextCol,
            BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _svcSearch.TextChanged += (_, _) => PopulateSvc(_svcSearch.Text);

        _lvSvc = MakeList(4, 74, 724, 470); StyleList(_lvSvc);
        _svcDisp = _lvSvc.Columns.Add("Service", 300); _svcName = _lvSvc.Columns.Add("Name", 160);
        _svcStatus = _lvSvc.Columns.Add("Status", 100); _svcStart = _lvSvc.Columns.Add("Startup", 120);

        _btnRefSvc.Click     += (_, _) => RefreshSvc();
        _btnSvcStart.Click   += (_, _) => SvcAction(n => ServiceManager.Start(n));
        _btnSvcStop.Click    += (_, _) => SvcAction(n => ServiceManager.Stop(n));
        _btnSvcAuto.Click    += (_, _) => SvcAction(n => ServiceManager.SetStartup(n, "auto"));
        _btnSvcManual.Click  += (_, _) => SvcAction(n => ServiceManager.SetStartup(n, "demand"));
        _btnSvcDisable.Click += (_, _) => SvcAction(n => ServiceManager.SetStartup(n, "disabled"));

        tp.Controls.AddRange(new Control[] { _btnRefSvc, _btnSvcStart, _btnSvcStop, _btnSvcAuto, _btnSvcManual, _btnSvcDisable, _svcSearch, _lvSvc });
    }

    private void RefreshSvc()
    {
        _btnRefSvc.Enabled = false;
        Task.Run(() => ServiceManager.List()).ContinueWith(t =>
            BeginInvoke((MethodInvoker)(() => { _svcList = t.Result; PopulateSvc(_svcSearch.Text); _btnRefSvc.Enabled = true; })));
    }

    private void PopulateSvc(string? filter)
    {
        _lvSvc.Items.Clear();
        string f = (filter ?? "").Trim();
        foreach (var s in _svcList)
        {
            if (f.Length > 0 && !(s.Display.Contains(f, StringComparison.OrdinalIgnoreCase) || s.Name.Contains(f, StringComparison.OrdinalIgnoreCase))) continue;
            var it = new ListViewItem(s.Display);
            it.SubItems.Add(s.Name); it.SubItems.Add(s.Status); it.SubItems.Add(s.StartType);
            if (s.Status == "Running") it.ForeColor = Theme.TextCol; else it.ForeColor = Theme.Muted;
            _lvSvc.Items.Add(it);
        }
    }

    private void SvcAction(Action<string> act)
    {
        if (_lvSvc.SelectedItems.Count == 0) return;
        string name = _lvSvc.SelectedItems[0].SubItems[1].Text;
        try { act(name); } catch (Exception ex) { ShowErr(ex); }
        RefreshSvc();
    }

    // ============================ المهام المجدولة ============================
    private void BuildTasksTab(Panel tp)
    {
        _btnRefTask = MakeMini(4, 6, 90); _btnTaskEn = MakeMini(100, 6, 90); _btnTaskDis = MakeMini(196, 6, 90); _btnTaskDel = MakeMini(292, 6, 90);
        _lvTask = MakeList(4, 44, 724, 500); StyleList(_lvTask);
        _tkName = _lvTask.Columns.Add("Task", 420); _tkStatus = _lvTask.Columns.Add("Status", 120); _tkNext = _lvTask.Columns.Add("Next run", 170);

        _btnRefTask.Click += (_, _) => RefreshTasks();
        _btnTaskEn.Click  += (_, _) => TaskAction(n => ScheduledTasks.Enable(n, true));
        _btnTaskDis.Click += (_, _) => TaskAction(n => ScheduledTasks.Enable(n, false));
        _btnTaskDel.Click += (_, _) =>
        {
            if (_lvTask.SelectedItems.Count == 0) return;
            if (MessageBox.Show(Loc.T("confirmDelTask"), Loc.T("warn"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            TaskAction(ScheduledTasks.Delete);
        };
        tp.Controls.AddRange(new Control[] { _btnRefTask, _btnTaskEn, _btnTaskDis, _btnTaskDel, _lvTask });
    }

    private void RefreshTasks()
    {
        _btnRefTask.Enabled = false;
        Task.Run(() => ScheduledTasks.List()).ContinueWith(t =>
            BeginInvoke((MethodInvoker)(() =>
            {
                _lvTask.Items.Clear();
                foreach (var tk in t.Result)
                {
                    var it = new ListViewItem(tk.Name);
                    it.SubItems.Add(tk.Status); it.SubItems.Add(tk.NextRun);
                    if (tk.Status.Contains("Disabled", StringComparison.OrdinalIgnoreCase)) it.ForeColor = Theme.Muted;
                    _lvTask.Items.Add(it);
                }
                _btnRefTask.Enabled = true;
            })));
    }

    private void TaskAction(Action<string> act)
    {
        if (_lvTask.SelectedItems.Count == 0) return;
        try { act(_lvTask.SelectedItems[0].Text); } catch (Exception ex) { ShowErr(ex); }
        RefreshTasks();
    }

    // ============================ اللغة ============================
    private void ApplySystemLanguage()
    {
        _btnRefSvc.Text = Loc.T("refresh"); _btnSvcStart.Text = Loc.T("svcStart"); _btnSvcStop.Text = Loc.T("svcStop");
        _btnSvcAuto.Text = Loc.T("svcAuto"); _btnSvcManual.Text = Loc.T("svcManual"); _btnSvcDisable.Text = Loc.T("svcDisable");
        _svcSearch.PlaceholderText = Loc.T("searchSvc");
        _svcDisp.Text = Loc.T("colDisplay"); _svcName.Text = Loc.T("colName"); _svcStatus.Text = Loc.T("colStatus"); _svcStart.Text = Loc.T("colStartType");

        _btnRefTask.Text = Loc.T("refresh"); _btnTaskEn.Text = Loc.T("taskEnable"); _btnTaskDis.Text = Loc.T("taskDisable"); _btnTaskDel.Text = Loc.T("taskDelete");
        _tkName.Text = Loc.T("colTask"); _tkStatus.Text = Loc.T("colStatus"); _tkNext.Text = Loc.T("colNextRun");
    }
}
