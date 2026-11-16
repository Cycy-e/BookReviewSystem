using BookReviewSystem.Controllers;
using BookReviewSystem.Models;

namespace BookReviewSystem.Views;

/// <summary>Administrators see every review and remove inappropriate ones (FR-15).</summary>
public class AdminReviewsForm : Form
{
    private readonly ReviewController _reviews = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly Button _remove = new() { Text = "Remove selected review", Width = 180, Height = 30 };

    public AdminReviewsForm()
    {
        Text = "Manage reviews";
        ClientSize = new Size(780, 440);
        StartPosition = FormStartPosition.CenterParent;

        Ui.StyleGrid(_grid);
        _grid.RowTemplate.Height = 40;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(8, 8, 8, 0) };
        bar.Controls.Add(_remove);

        Controls.Add(_grid);
        Controls.Add(bar);

        _remove.Click += (_, _) => Remove();
        LoadReviews();
    }

    private void LoadReviews()
    {
        _grid.DataSource = null;
        _grid.DataSource = _reviews.GetAll();
        if (_grid.Columns.Count == 0) return;

        foreach (var hidden in new[] { "Id", "UserId", "BookId" })
            _grid.Columns[hidden].Visible = false;
        ShowColumn("BookTitle", "Book", 30);
        ShowColumn("UserName", "Reader", 20);
        ShowColumn("CreatedAt", "Date", 20);
        ShowColumn("Content", "Review", 80);
        _grid.Columns["Content"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
    }

    private void ShowColumn(string name, string header, float weight)
    {
        var col = _grid.Columns[name];
        col.Visible = true;
        col.HeaderText = header;
        col.FillWeight = weight;
    }

    private void Remove()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Review review)
        {
            Ui.Info("Please select a review first.");
            return;
        }
        if (!Ui.Confirm($"Remove the review by {review.UserName} on \"{review.BookTitle}\"?")) return;

        var result = _reviews.AdminDelete(review.Id);
        if (!result.Success) { Ui.Error(result.Message); return; }
        LoadReviews();
    }
}
