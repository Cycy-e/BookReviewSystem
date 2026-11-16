using BookReviewSystem.Controllers;
using BookReviewSystem.Models;

namespace BookReviewSystem.Views;

/// <summary>Add a new book, or edit an existing one (administrators only).</summary>
public class BookEditForm : BaseForm
{
    private readonly BookController _books = new();
    private readonly Book _existing;

    private readonly TextBox _title = new() { Left = 120, Top = 20, Width = 300 };
    private readonly TextBox _author = new() { Left = 120, Top = 55, Width = 300 };
    private readonly TextBox _category = new() { Left = 120, Top = 90, Width = 300 };
    private readonly TextBox _isbn = new() { Left = 120, Top = 125, Width = 300 };
    private readonly NumericUpDown _year = new() { Left = 120, Top = 160, Width = 100, Minimum = 1000, Maximum = 3000, Value = DateTime.Now.Year };
    private readonly TextBox _description = new() { Left = 120, Top = 195, Width = 300, Height = 90, Multiline = true, ScrollBars = ScrollBars.Vertical };

    public BookEditForm(Book existing)
    {
        _existing = existing;
        Text = existing == null ? "Add a book" : "Edit book";
        ClientSize = new Size(450, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var save = new Button { Text = "Save", Left = 220, Top = 300, Width = 95, Height = 30 };
        var cancel = new Button { Text = "Cancel", Left = 325, Top = 300, Width = 95, Height = 30, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => Save();
        AcceptButton = null; // Enter must stay available in the multi-line description
        CancelButton = cancel;

        Controls.AddRange(new Control[]
        {
            Ui.Label("Title *", 20, 20), _title,
            Ui.Label("Author *", 20, 55), _author,
            Ui.Label("Category *", 20, 90), _category,
            Ui.Label("ISBN *", 20, 125), _isbn,
            Ui.Label("Year *", 20, 160), _year,
            Ui.Label("Description", 20, 195), _description,
            save, cancel
        });

        if (existing != null)
        {
            _title.Text = existing.Title;
            _author.Text = existing.Author;
            _category.Text = existing.Category;
            _isbn.Text = existing.Isbn;
            _year.Value = Math.Clamp(existing.PublicationYear, (int)_year.Minimum, (int)_year.Maximum);
            _description.Text = existing.Description;
        }
    }

    private void Save()
    {
        var book = new Book
        {
            Id = _existing?.Id ?? 0,
            Title = _title.Text,
            Author = _author.Text,
            Category = _category.Text,
            Isbn = _isbn.Text,
            PublicationYear = (int)_year.Value,
            Description = _description.Text
        };

        var result = _existing == null ? _books.Add(book) : _books.Update(book);
        if (!result.Success)
        {
            Ui.Error(result.Message);
            return;
        }
        DialogResult = DialogResult.OK;
    }
}
