using DiskCleaner.Core;
using DiskCleaner.Services;

namespace DiskCleaner.UI;

public partial class MainForm
{
    private Button _btnRefUsers = null!, _btnNewUser = null!, _btnRenameUser = null!, _btnDelUser = null!, _btnResetPwd = null!, _btnEnUser = null!, _btnDisUser = null!;
    private ListView _lvUsers = null!;
    private ColumnHeader _uName = null!, _uFull = null!, _uStatus = null!, _uNever = null!, _uDesc = null!;
    private ComboBox _cmbGroups = null!;
    private Button _btnAddMember = null!, _btnRemMember = null!;
    private ListView _lvMembers = null!;
    private ColumnHeader _mName = null!;

    private void BuildUsersTab(Panel tp)
    {
        // الصف الأول
        _btnRefUsers   = MakeMini(12, 8, 90);  _btnNewUser = MakeMini(108, 8, 96);
        _btnRenameUser = MakeMini(210, 8, 104); _btnDelUser = MakeMini(320, 8, 84);
        // الصف الثاني
        _btnResetPwd = MakeMini(12, 40, 130); _btnEnUser = MakeMini(148, 40, 90); _btnDisUser = MakeMini(244, 40, 90);

        _lvUsers = MakeList(12, 74, 576, 186);
        _lvUsers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _uName = _lvUsers.Columns.Add("Name", 120); _uFull = _lvUsers.Columns.Add("Full name", 130);
        _uStatus = _lvUsers.Columns.Add("Status", 70); _uNever = _lvUsers.Columns.Add("Never expires", 90); _uDesc = _lvUsers.Columns.Add("Description", 160);

        var lblG = new Label { Text = "", ForeColor = Theme.Muted, Location = new(12, 276), Size = new(50, 22) };
        _cmbGroups = new ComboBox { Location = new(66, 273), Size = new(210, 26), DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.Panel, ForeColor = Theme.TextCol, FlatStyle = FlatStyle.Flat };
        _cmbGroups.SelectedIndexChanged += (_, _) => RefreshMembers();
        _btnAddMember = MakeMini(288, 272, 150); _btnRemMember = MakeMini(444, 272, 144);

        _lvMembers = MakeList(12, 306, 576, 176);
        _lvMembers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _mName = _lvMembers.Columns.Add("Members", 560);

        _btnRefUsers.Click += (_, _) => RefreshUsers();
        _btnNewUser.Click  += (_, _) => NewUser();
        _btnRenameUser.Click += (_, _) => RenameUser();
        _btnDelUser.Click  += (_, _) => DeleteUser();
        _btnResetPwd.Click += (_, _) => ResetPwd();
        _btnEnUser.Click   += (_, _) => SetUserEnabled(true);
        _btnDisUser.Click  += (_, _) => SetUserEnabled(false);
        _btnAddMember.Click += (_, _) => ChangeMembership(true);
        _btnRemMember.Click += (_, _) => ChangeMembership(false);

        tp.Controls.AddRange(new Control[] { _btnRefUsers, _btnNewUser, _btnRenameUser, _btnDelUser, _btnResetPwd, _btnEnUser, _btnDisUser,
            _lvUsers, lblG, _cmbGroups, _btnAddMember, _btnRemMember, _lvMembers });
        _lblGroupCaption = lblG;
    }

    private Label _lblGroupCaption = null!;

    private Button MakeMini(int x, int y, int w)
    { var b = MakeBtn(x, y, w, Theme.Gray, Theme.GrayH); b.Size = new(w, 28); b.Font = Theme.Main; return b; }

    private string? SelUser() => _lvUsers.SelectedItems.Count > 0 ? _lvUsers.SelectedItems[0].Text : null;

    private void RefreshUsers()
    {
        try
        {
            _lvUsers.Items.Clear();
            foreach (var u in UserManager.ListUsers())
            {
                var it = new ListViewItem(u.Name);
                it.SubItems.Add(u.FullName);
                it.SubItems.Add(u.Enabled ? Loc.T("enabled") : Loc.T("disabled"));
                it.SubItems.Add(u.PasswordNeverExpires ? Loc.T("yes2") : "");
                it.SubItems.Add(u.Description);
                it.ForeColor = u.Enabled ? Theme.TextCol : Theme.Muted;
                _lvUsers.Items.Add(it);
            }
            _cmbGroups.Items.Clear();
            foreach (var g in UserManager.ListGroups()) _cmbGroups.Items.Add(g.Name);
            if (_cmbGroups.Items.Count > 0 && _cmbGroups.SelectedIndex < 0) _cmbGroups.SelectedIndex = 0;
        }
        catch (Exception ex) { ShowErr(ex); }
    }

    private void RefreshMembers()
    {
        _lvMembers.Items.Clear();
        if (_cmbGroups.SelectedItem is not string g) return;
        foreach (var m in UserManager.GroupMembers(g)) _lvMembers.Items.Add(new ListViewItem(m));
    }

    private void NewUser()
    {
        try
        {
            var name = InputDialog.Show(this, Loc.T("newUser"), Loc.T("promptUserName"));
            if (string.IsNullOrWhiteSpace(name)) return;
            var pwd = InputDialog.Show(this, Loc.T("newUser"), Loc.T("promptPassword"), password: true);
            if (pwd == null) return;
            var full = InputDialog.Show(this, Loc.T("newUser"), Loc.T("promptFullName")) ?? "";
            UserManager.CreateUser(name.Trim(), pwd, full, "");
            RefreshUsers();
        }
        catch (Exception ex) { ShowErr(ex); }
    }

    private void RenameUser()
    {
        var u = SelUser(); if (u == null) return;
        if (string.Equals(u, Environment.UserName, StringComparison.OrdinalIgnoreCase))
        { MessageBox.Show(Loc.T("cantRenameSelf"), Loc.T("warn"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var newName = InputDialog.Show(this, Loc.T("renameUser"), Loc.T("promptNewName"), initial: u);
        if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName.Trim(), u, StringComparison.Ordinal)) return;
        try { UserManager.RenameUser(u, newName.Trim()); RefreshUsers(); } catch (Exception ex) { ShowErr(ex); }
    }

    private void DeleteUser()
    {
        var u = SelUser(); if (u == null) return;
        if (MessageBox.Show($"{Loc.T("confirmDelUser")}\n\n{u}", Loc.T("warn"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { UserManager.DeleteUser(u); RefreshUsers(); } catch (Exception ex) { ShowErr(ex); }
    }

    private void ResetPwd()
    {
        var u = SelUser(); if (u == null) return;
        var pwd = InputDialog.Show(this, Loc.T("resetPwd"), $"{Loc.T("promptPassword")} ({u})", password: true);
        if (pwd == null) return;
        try { UserManager.SetPassword(u, pwd); MessageBox.Show(Loc.T("done"), Loc.T("resetPwd"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { ShowErr(ex); }
    }

    private void SetUserEnabled(bool enabled)
    {
        var u = SelUser(); if (u == null) return;
        try { UserManager.SetEnabled(u, enabled); RefreshUsers(); } catch (Exception ex) { ShowErr(ex); }
    }

    private void ChangeMembership(bool add)
    {
        var u = SelUser(); if (u == null || _cmbGroups.SelectedItem is not string g) return;
        try
        {
            if (add) UserManager.AddToGroup(u, g); else UserManager.RemoveFromGroup(u, g);
            RefreshMembers();
        }
        catch (Exception ex) { ShowErr(ex); }
    }

    private void ShowErr(Exception ex)
    { Logger.Log($"Users op failed: {ex.Message}"); MessageBox.Show(ex.Message, Loc.T("warn"), MessageBoxButtons.OK, MessageBoxIcon.Error); }

    private void ApplyUsersLanguage()
    {
        _btnRefUsers.Text = Loc.T("refresh"); _btnNewUser.Text = Loc.T("newUser");
        _btnRenameUser.Text = Loc.T("renameUser"); _btnDelUser.Text = Loc.T("delUser");
        _btnResetPwd.Text = Loc.T("resetPwd"); _btnEnUser.Text = Loc.T("enableUser"); _btnDisUser.Text = Loc.T("disableUser");
        _uName.Text = Loc.T("colName"); _uFull.Text = Loc.T("colFullName"); _uStatus.Text = Loc.T("colStatus");
        _uNever.Text = Loc.T("colNeverExp"); _uDesc.Text = Loc.T("colDesc");
        _lblGroupCaption.Text = Loc.T("group"); _btnAddMember.Text = Loc.T("addMember"); _btnRemMember.Text = Loc.T("removeMember");
        _mName.Text = Loc.T("members");
    }
}
