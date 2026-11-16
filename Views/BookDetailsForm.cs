using BookReviewSystem.Controllers;
using BookReviewSystem.Models;

namespace BookReviewSystem.Views;

/// <summary>Book details page: information, average rating, reviews, and reader actions.</summary>
public class BookDetailsForm : Form
{
    private readonly BookController _books = new();
    private readonly ReviewController _reviews = new();
    private readonly int _bookId;
    private int _editingReviewId; // 0 = writing a new review

    private readonly Label _title = new() { Left = 20, Top = 12, Width = 640, Height = 28, Font = new Font(SystemFonts.DefaultFont.FontFamily, 14, FontStyle.Bold) };
    private readonly Label _meta = new() { Left = 20, Top = 44, Width = 640, Height = 20 };
    private readonly Label _average = new() { Left = 20, Top = 66, Width = 640, Height = 20, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
    private readonly TextBox _description = new() { Left = 20, Top = 92, Width = 640, Height = 60, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly DataGridView _grid = new() { Left = 20, Top = 186, Width = 640, Height = 170, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };

    private readonly Panel _readerPanel = new() { Left = 20, Top = 362, Width = 640, Height = 175, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
    private readonly ComboBox _stars = new() { Left = 105, Top = 4, Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _myRating = new() { Left = 360, Top = 7, Width = 270 };
    private readonly TextBox _reviewText = new() { Left = 0, Top = 62, Width = 640, Height = 70, Multiline = true, MaxLength = ReviewController.MaxReviewLength, ScrollBars = ScrollBars.Vertical };
    private readonly Button _post = new() { Left = 0, Top = 140, Width = 120, Height = 28, Text = "Post review" };
    private readonly Button _cancelEdit = new() { Left = 128, Top = 140, Width = 100, Height = 28, Text = "Cancel edit", Visible = false };
    private readonly Button _editMine = new() { Left = 380, Top = 140, Width = 120, Height = 28, Text = "Edit my review" };
    private readonly Button _deleteMine = new() { Left = 508, Top = 140, Width = 132, Height = 28, Text = "Delete my review" };
    private readonly Label _loginNote = new() { Left = 20, Top = 362, Width = 640, Height = 40, ForeColor = Color.DimGray };

    public BookDetailsForm(int bookId)
    {
        _bookId = bookId;
        Text = "Book details";
        ClientSize = new Size(680, 550);
        MinimumSize = new Size(700, 590);
        StartPosition = FormStartPosition.CenterParent;

        Ui.StyleGrid(_grid);
        _grid.RowTemplate.Height = 40;

        _stars.Items.AddRange(new object[] { 1, 2, 3, 4, 5 });
        _stars.SelectedIndex = 4;

        var saveRating = new Button { Left = 185, Top = 2, Width = 80, Height = 26, Text = "Save" };
        var removeRating = new Button { Left = 272, Top = 2, Width = 80, Height = 26, Text = "Remove" };
        saveRating.Click += (_, _) => SaveRating();
        removeRating.Click += (_, _) => RemoveRating();
        _post.Click += (_, _) => PostReview();
        _cancelEdit.Click += (_, _) => ResetReviewBox();
        _editMine.Click += (_, _) => StartEdit();
        _deleteMine.Click += (_, _) => DeleteMine();

        _readerPanel.Controls.AddRange(new Control[]
        {
            new Label { Text = "Your rating (1-5):", Left = 0, Top = 7, Width = 105 }, _stars, saveRating, removeRating, _myRating,
            new Label { Text = $"Your review (max {ReviewController.MaxReviewLength} characters):", Left = 0, Top = 40, Width = 400 },
            _reviewText, _post, _cancelEdit, _editMine, _deleteMine
        });

        Controls.AddRange(new Control[]
        {
            _title, _meta, _average, _description,
            new Label { Text = "Reviews (newest first)", Left = 20, Top = 162, Width = 300, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) },
            _grid, _readerPanel, _loginNote
        });

        _readerPanel.Visible = Session.IsReader;
        _loginNote.Visible = !Session.IsReader;
        _loginNote.Text = Session.IsAdmin
            ? "Administrators manage books and reviews from the main window. Log in as a reader to rate or review."
            : "Log in as a reader to rate this book or write a review.";

        LoadBook();
    }

    private void LoadBook()
    {
        var book = _books.GetById(_bookId);
        if (book == null)
        {
            Ui.Error("This book no longer exists.");
            Close();
            return;
        }

        _title.Text = book.Title;
        _meta.Text = $"by {book.Author}   |   {book.Category}   |   ISBN {book.Isbn}   |   {book.PublicationYear}";
        _average.Text = "Average rating: " + Ui.Stars(book.AverageRating, book.RatingCount);
        _description.Text = string.IsNullOrWhiteSpace(book.Description) ? "No description." : book.Description;

        var list = _reviews.GetForBook(_bookId);
        _grid.DataSource = null;
        _grid.DataSource = list;
        if (_grid.Columns.Count > 0)
        {
            foreach (var hidden in new[] { "Id", "UserId", "BookId", "BookTitle" })
                _grid.Columns[hidden].Visible = false;
            _grid.Columns["UserName"].Visible = true;
            _grid.Columns["UserName"].HeaderText = "Reader";
            _grid.Columns["UserName"].FillWeight = 22;
            _grid.Columns["CreatedAt"].Visible = true;
            _grid.Columns["CreatedAt"].HeaderText = "Date";
            _grid.Columns["CreatedAt"].FillWeight = 22;
            _grid.Columns["Content"].Visible = true;
            _grid.Columns["Content"].HeaderText = "Review";
            _grid.Columns["Content"].FillWeight = 100;
            _grid.Columns["Content"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        }

        if (Session.IsReader)
        {
            int? mine = _reviews.GetMyRating(_bookId);
            _myRating.Text = mine.HasValue ? $"You rated this book {mine} / 5" : "You have not rated this book yet";
            if (mine.HasValue) _stars.SelectedItem = mine.Value;
        }
    }

    private void SaveRating()
    {
        var result = _reviews.SetRating(_bookId, (int)_stars.SelectedItem);
        if (!result.Success) { Ui.Error(result.Message); return; }
        LoadBook();
    }

    private void RemoveRating()
    {
        var result = _reviews.RemoveRating(_bookId);
        if (!result.Success) { Ui.Error(result.Message); return; }
        LoadBook();
    }

    private void PostReview()
    {
        var result = _editingReviewId == 0
            ? _reviews.Add(_bookId, _reviewText.Text)
            : _reviews.Update(_editingReviewId, _reviewText.Text);

        if (!result.Success) { Ui.Error(result.Message); return; }
        ResetReviewBox();
        LoadBook();
    }

    private Review SelectedReview() => _grid.CurrentRow?.DataBoundItem as Review;

    private Review SelectedOwnReview(string action)
    {
        var review = SelectedReview();
        if (review == null) { Ui.Info($"Please select one of your reviews to {action}."); return null; }
        if (review.UserId != Session.CurrentUser.Id)
        {
            Ui.Error($"You can only {action} your own reviews.");
            return null;
        }
        return review;
    }

    private void StartEdit()
    {
        var review = SelectedOwnReview("edit");
        if (review == null) return;
        _editingReviewId = review.Id;
        _reviewText.Text = review.Content;
        _post.Text = "Update review";
        _cancelEdit.Visible = true;
        _reviewText.Focus();
    }

    private void DeleteMine()
    {
        var review = SelectedOwnReview("delete");
        if (review == null) return;
        if (!Ui.Confirm("Delete your review?")) return;

        var result = _reviews.Delete(review.Id);
        if (!result.Success) { Ui.Error(result.Message); return; }
        ResetReviewBox();
        LoadBook();
    }

    private void ResetReviewBox()
    {
        _editingReviewId = 0;
        _reviewText.Clear();
        _post.Text = "Post review";
        _cancelEdit.Visible = false;
    }
}
