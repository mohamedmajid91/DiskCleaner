using DiskCleaner.Core;
using DiskCleaner.Services;

namespace DiskCleaner.UI;

public partial class MainForm
{
    private Button _btnRefApps = null!, _btnUninstall = null!, _btnScanLeft = null!, _btnRemoveLeft = null!;
    private Label _lblUnin = null!;
    private ListView _lvApps = null!, _lvLeft = null!;
    private ColumnHeader _aName = null!, _aPub = null!, _aVer = null!, _aSize = null!;
    private ColumnHeader _lType = null!, _lSize = null!, _lPath = null!;

    private void BuildUninstallTab(TabPage tp)
    {
        _btnRefApps   = MakeBtn(12, 8, 120, Theme.Accent, Theme.AccentH);  _btnRefApps.Size = new(120, 30);
        _btnUninstall = MakeBtn(140, 8, 120, Theme.Gray, Theme.GrayH);     _btnUninstall.Size = new(120, 30);
        _btnScanLeft  = MakeBtn(268, 8, 120, Theme.Gray, Theme.GrayH);     _btnScanLeft.Size = new(120, 30);
        _lblUnin = new Label { ForeColor = Theme.Muted, Location = new(396, 14), Size = new(196, 20) };

        _lvApps = MakeList(12, 46, 576, 210);
        _lvApps.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _aName = _lvApps.Columns.Add("Name", 220); _aPub = _lvApps.Columns.Add("Publisher", 150);
        _aVer = _lvApps.Columns.Add("Version", 90); _aSize = _lvApps.Columns.Add("Size", 100);

        _lvLeft = MakeList(12, 264, 576, 182);
        _lvLeft.CheckBoxes = true;
        _lvLeft.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _lType = _lvLeft.Columns.Add("Type", 80); _lSize = _lvLeft.Columns.Add("Size", 90); _lPath = _lvLeft.Columns.Add("Path", 390);

        _btnRemoveLeft = MakeBtn(12, 452, 576, Theme.Purple, Theme.PurpleH); _btnRemoveLeft.Size = new(576, 40);
        _btnRemoveLeft.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _btnRefApps.Click    += (_, _) => RefreshApps();
        _btnUninstall.Click  += (_, _) => DoUninstall();
        _btnScanLeft.Click   += (_, _) => DoScanLeftovers();
        _btnRemoveLeft.Click += (_, _) => DoRemoveLeftovers();

        tp.Controls.AddRange(new Control[] { _btnRefApps, _btnUninstall, _btnScanLeft, _lblUnin, _lvApps, _lvLeft, _btnRemoveLeft });
    }

    private InstalledApp? SelApp() =>
        _lvApps.SelectedItems.Count > 0 ? _lvApps.SelectedItems[0].Tag as InstalledApp : null;

    private void RefreshApps()
    {
        _lvApps.Items.Clear();
        foreach (var a in Uninstaller.ListInstalled())
        {
            var it = new ListViewItem(a.Name) { Tag = a };
            it.SubItems.Add(a.Publisher);
            it.SubItems.Add(a.Version);
            it.SubItems.Add(a.SizeKb > 0 ? Theme.FormatSize(a.SizeKb * 1024) : "-");
            _lvApps.Items.Add(it);
        }
    }

    private void DoUninstall()
    {
        var app = SelApp();
        if (app == null) { MessageBox.Show(Loc.T("selectAppFirst"), Loc.T("warn"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (MessageBox.Show($"{Loc.T("confirmUninstall")}\n\n{app.Name}", Loc.T("tabUninstall"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        _lblUnin.Text = Loc.T("uninstalling"); SetUninBusy(true);
        Task.Run(() => { Uninstaller.RunNativeUninstaller(app); return Uninstaller.ScanLeftovers(app); })
            .ContinueWith(t => BeginInvoke((MethodInvoker)(() => { SetUninBusy(false); PopulateLeftovers(t.Result); })));
    }

    private void DoScanLeftovers()
    {
        var app = SelApp();
        if (app == null) { MessageBox.Show(Loc.T("selectAppFirst"), Loc.T("warn"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        _lblUnin.Text = Loc.T("scanningLeft"); SetUninBusy(true);
        Task.Run(() => Uninstaller.ScanLeftovers(app))
            .ContinueWith(t => BeginInvoke((MethodInvoker)(() => { SetUninBusy(false); PopulateLeftovers(t.Result); })));
    }

    private void PopulateLeftovers(List<Leftover> items)
    {
        _lvLeft.Items.Clear();
        foreach (var lo in items)
        {
            var it = new ListViewItem(KindText(lo.Kind)) { Tag = lo, Checked = true };
            it.SubItems.Add(lo.Kind == LeftoverKind.Registry ? "-" : Theme.FormatSize(lo.Size));
            it.SubItems.Add(lo.Path);
            _lvLeft.Items.Add(it);
        }
        _lblUnin.Text = items.Count == 0 ? Loc.T("noLeft") : Loc.T("leftFound");
    }

    private void DoRemoveLeftovers()
    {
        var list = _lvLeft.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => (Leftover)i.Tag!).ToList();
        if (list.Count == 0) return;
        if (MessageBox.Show(Loc.T("confirmRemoveLeft"), Loc.T("warn"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var (n, backup) = Uninstaller.RemoveLeftovers(list);
        _lvLeft.Items.Clear();
        RefreshApps(); UpdateHeader();
        MessageBox.Show($"{string.Format(Loc.T("removedLeft"), n)}\n{backup}", Loc.T("tabUninstall"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SetUninBusy(bool busy)
    { _btnUninstall.Enabled = _btnScanLeft.Enabled = _btnRefApps.Enabled = _btnRemoveLeft.Enabled = !busy; }

    private static string KindText(LeftoverKind k) => k switch
    {
        LeftoverKind.File => Loc.T("kindFile"),
        LeftoverKind.Directory => Loc.T("kindDir"),
        _ => Loc.T("kindReg"),
    };

    private void ApplyUninstallLanguage()
    {
        _btnRefApps.Text = Loc.T("uRefresh"); _btnUninstall.Text = Loc.T("uUninstall");
        _btnScanLeft.Text = Loc.T("uScanLeft"); _btnRemoveLeft.Text = Loc.T("uRemoveLeft");
        _aName.Text = Loc.T("colName"); _aPub.Text = Loc.T("colPublisher"); _aVer.Text = Loc.T("colVersion"); _aSize.Text = Loc.T("colSize");
        _lType.Text = Loc.T("colType"); _lSize.Text = Loc.T("colSize"); _lPath.Text = Loc.T("colPath");
    }
}
