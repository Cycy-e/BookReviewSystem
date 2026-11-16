using BookReviewSystem.Controllers;
using BookReviewSystem.Models;

namespace BookReviewSystem.Views;

/// <summary>Administrators view all users and deactivate or reactivate accounts (FR-16).</summary>
public class AdminUsersForm : BaseForm
{
    private readonly AdminController _admin = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly Button _toggle = new() { Text = "Deactivate / reactivate", Width = 170, Height = 30 };

    public AdminUsersForm()
    {
        Text = "Manage users";
        ClientSize = new Size(700, 420);
        StartPosition = FormStartPosition.CenterParent;

        Ui.StyleGrid(_grid);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(8, 8, 8, 0) };
        bar.Controls.Add(_toggle);

        Controls.Add(_grid);
        Controls.Add(bar);

        _toggle.Click += (_, _) => Toggle();
        LoadUsers();
    }

    private void LoadUsers()
    {
        var users = _admin.GetUsers();
        _grid.DataSource = null;
        _grid.DataSource = users;
        if (_grid.Columns.Count == 0) return;

        _grid.Columns["Id"].Visible = false;
        _grid.Columns["IsAdmin"].Visible = false;
        _grid.Columns["FullName"].HeaderText = "Full name";
        _grid.Columns["IsActive"].HeaderText = "Active";
        _grid.Columns["CreatedAt"].HeaderText = "Registered";
    }

    private void Toggle()
    {
        if (_grid.CurrentRow?.DataBoundItem is not User user)
        {
            Ui.Info("Please select a user first.");
            return;
        }

        string action = user.IsActive ? "deactivate" : "reactivate";
        if (!Ui.Confirm($"Do you want to {action} the account of {user.FullName}?")) return;

        var result = _admin.SetActive(user.Id, !user.IsActive);
        if (!result.Success) { Ui.Error(result.Message); return; }
        LoadUsers();
    }
}
