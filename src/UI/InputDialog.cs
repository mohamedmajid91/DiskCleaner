using DiskCleaner.Services;

namespace DiskCleaner.UI;

/// <summary>صندوق إدخال نصّي بسيط (يدعم إخفاء كلمة السر).</summary>
public static class InputDialog
{
    public static string? Show(IWin32Window owner, string title, string prompt, bool password = false, string initial = "")
    {
        using var f = new Form
        {
            Text = title, ClientSize = new Size(400, 150), StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
            BackColor = Theme.Dark, ForeColor = Theme.TextCol, Font = Theme.Main,
            RightToLeft = Loc.IsRtl ? RightToLeft.Yes : RightToLeft.No, RightToLeftLayout = Loc.IsRtl
        };
        var lbl = new Label { Text = prompt, Location = new(16, 16), Size = new(368, 20), ForeColor = Theme.TextCol };
        var tb = new TextBox { Location = new(16, 42), Size = new(368, 26), UseSystemPasswordChar = password, Text = initial };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new(212, 100), Size = new(84, 30), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new(300, 100), Size = new(84, 30), FlatStyle = FlatStyle.Flat, BackColor = Theme.Gray, ForeColor = Color.White };
        ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
        f.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
        f.AcceptButton = ok; f.CancelButton = cancel;
        return f.ShowDialog(owner) == DialogResult.OK ? tb.Text : null;
    }
}
