using BookReviewSystem.Controllers;

namespace BookReviewSystem.Views;

public class RegisterForm : BaseForm
{
    private readonly AuthController _auth = new();
    private readonly TextBox _name = new() { Left = 140, Top = 20, Width = 220 };
    private readonly TextBox _email = new() { Left = 140, Top = 55, Width = 220 };
    private readonly TextBox _password = new() { Left = 140, Top = 90, Width = 220, UseSystemPasswordChar = true };
    private readonly TextBox _confirm = new() { Left = 140, Top = 125, Width = 220, UseSystemPasswordChar = true };

    public string RegisteredEmail { get; private set; }

    public RegisterForm()
    {
        Text = "Create an account";
        ClientSize = new Size(390, 240);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var hint = new Label
        {
            Text = "Password: at least 8 characters, with letters and numbers.",
            Left = 20, Top = 158, Width = 350, ForeColor = Color.DimGray
        };
        var create = new Button { Text = "Create account", Left = 140, Top = 190, Width = 110, Height = 30 };
        var cancel = new Button { Text = "Cancel", Left = 260, Top = 190, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };

        create.Click += (_, _) => Submit();
        AcceptButton = create;
        CancelButton = cancel;

        Controls.AddRange(new Control[]
        {
            Ui.Label("Full name", 20, 20), _name,
            Ui.Label("Email", 20, 55), _email,
            Ui.Label("Password", 20, 90), _password,
            Ui.Label("Confirm password", 20, 125, 115), _confirm,
            hint, create, cancel
        });
    }

    private void Submit()
    {
        if (_password.Text != _confirm.Text)
        {
            Ui.Error("The two passwords do not match.");
            return;
        }

        var result = _auth.Register(_name.Text, _email.Text, _password.Text);
        if (!result.Success)
        {
            Ui.Error(result.Message);
            return;
        }

        RegisteredEmail = _email.Text.Trim();
        Ui.Info(result.Message);
        DialogResult = DialogResult.OK;
    }
}
