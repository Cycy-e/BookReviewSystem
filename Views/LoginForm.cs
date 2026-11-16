using BookReviewSystem.Controllers;
using BookReviewSystem.Data;

namespace BookReviewSystem.Views;

public class LoginForm : Form
{
    private readonly AuthController _auth = new();
    private readonly TextBox _email = new() { Left = 120, Top = 90, Width = 230 };
    private readonly TextBox _password = new() { Left = 120, Top = 125, Width = 230, UseSystemPasswordChar = true };

    public LoginForm()
    {
        Text = "Book Review System - Login";
        ClientSize = new Size(390, 290);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var title = new Label
        {
            Text = "Book Review & Management",
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Left = 20, Top = 20, Width = 350, Height = 30
        };
        var hint = new Label
        {
            Text = "Log in, or continue as a visitor to browse books.",
            Left = 20, Top = 52, Width = 350, ForeColor = Color.DimGray
        };

        var login = new Button { Text = "Log in", Left = 120, Top = 165, Width = 110, Height = 30 };
        var guest = new Button { Text = "Continue as visitor", Left = 240, Top = 165, Width = 110, Height = 30 };
        var register = new LinkLabel { Text = "Create a new account", Left = 120, Top = 210, Width = 200 };
        var adminNote = new Label
        {
            Text = $"Demo administrator: {Database.DefaultAdminEmail} / {Database.DefaultAdminPassword}",
            Left = 20, Top = 250, Width = 360, ForeColor = Color.DimGray, Font = new Font(Font.FontFamily, 8)
        };

        login.Click += (_, _) => DoLogin();
        guest.Click += (_, _) => { Session.Clear(); DialogResult = DialogResult.OK; };
        register.LinkClicked += (_, _) => OpenRegister();

        AcceptButton = login;
        Controls.AddRange(new Control[]
        {
            title, hint,
            Ui.Label("Email", 20, 90), _email,
            Ui.Label("Password", 20, 125), _password,
            login, guest, register, adminNote
        });
    }

    private void DoLogin()
    {
        var result = _auth.Login(_email.Text, _password.Text);
        if (!result.Success)
        {
            Ui.Error(result.Message);
            return;
        }
        DialogResult = DialogResult.OK;
    }

    private void OpenRegister()
    {
        using var form = new RegisterForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _email.Text = form.RegisteredEmail;
            _password.Clear();
            _password.Focus();
        }
    }
}
