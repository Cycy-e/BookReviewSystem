using BookReviewSystem.Controllers;
using BookReviewSystem.Models;

namespace BookReviewSystem.Views;

/// <summary>Catalogue window: search, filter, sort, and (for admins) manage books.</summary>
public class MainForm : BaseForm
{
    private const string AllCategories = "All categories";

    private readonly BookController _books = new();
    private readonly AuthController _auth = new();

    private readonly TextBox _search = new() { Width = 220 };
    private readonly ComboBox _category = new() { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _minRating = new() { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _sort = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { AutoSize = true, Padding = new Padding(0, 8, 0, 0) };

    private readonly Button _details = new() { Text = "View details", Width = 110, Height = 30 };
    private readonly Button _add = new() { Text = "Add book", Width = 90, Height = 30 };
    private readonly Button _edit = new() { Text = "Edit book", Width = 90, Height = 30 };
    private readonly Button _delete = new() { Text = "Delete book", Width = 90, Height = 30 };
    private readonly Button _users = new() { Text = "Manage users", Width = 105, Height = 30 };
    private readonly Button _reviews = new() { Text = "Manage reviews", Width = 115, Height = 30 };
    private readonly Button _authButton = new() { Width = 90, Height = 30 };

    /// <summary>True when the user pressed Log out / Log in, so Program shows the login window again.</summary>
    public bool LoggedOut { get; private set; }

    public MainForm()
    {
        Text = "Book Review System";
        ClientSize = new Size(900, 560);
        MinimumSize = new Size(820, 420);
        StartPosition = FormStartPosition.CenterScreen;

        Ui.StyleGrid(_grid);
        _grid.DoubleClick += (_, _) => OpenDetails();

        // --- filter bar (top) ---
        var filters = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 78, Padding = new Padding(8, 8, 8, 0), WrapContents = true
        };
        var searchButton = new Button { Text = "Search", Width = 75, Height = 26 };
        var clear = new Button { Text = "Clear", Width = 60, Height = 26 };
        _minRating.Items.AddRange(new object[] { "Any", "1+", "2+", "3+", "4+", "5" });
        _minRating.SelectedIndex = 0;
        _sort.Items.AddRange(new object[] { BookController.SortTitle, BookController.SortYear, BookController.SortRating });
        _sort.SelectedIndex = 0;

        filters.Controls.Add(new Label { Text = "Search (title, author, category, ISBN):", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        filters.Controls.Add(_search);
        filters.Controls.Add(searchButton);
        filters.Controls.Add(clear);
        filters.SetFlowBreak(clear, true);
        filters.Controls.Add(new Label { Text = "Category:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        filters.Controls.Add(_category);
        filters.Controls.Add(new Label { Text = "Min rating:", AutoSize = true, Padding = new Padding(12, 6, 0, 0) });
        filters.Controls.Add(_minRating);
        filters.Controls.Add(new Label { Text = "Sort by:", AutoSize = true, Padding = new Padding(12, 6, 0, 0) });
        filters.Controls.Add(_sort);

        // --- button bar (bottom) ---
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(8, 8, 8, 0) };
        bar.Controls.AddRange(new Control[] { _details, _add, _edit, _delete, _users, _reviews, _authButton, _status });

        // Fill control first, then the docked bars
        Controls.Add(_grid);
        Controls.Add(filters);
        Controls.Add(bar);

        searchButton.Click += (_, _) => LoadBooks();
        clear.Click += (_, _) => ClearFilters();
        _search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { LoadBooks(); e.SuppressKeyPress = true; } };
        _category.SelectionChangeCommitted += (_, _) => LoadBooks();
        _minRating.SelectionChangeCommitted += (_, _) => LoadBooks();
        _sort.SelectionChangeCommitted += (_, _) => LoadBooks();

        _details.Click += (_, _) => OpenDetails();
        _add.Click += (_, _) => AddBook();
        _edit.Click += (_, _) => EditBook();
        _delete.Click += (_, _) => DeleteBook();
        _users.Click += (_, _) => { using var f = new AdminUsersForm(); f.ShowDialog(this); };
        _reviews.Click += (_, _) => { using var f = new AdminReviewsForm(); f.ShowDialog(this); LoadBooks(); };
        _authButton.Click += (_, _) => AuthClick();

        // Role-based visibility (FR-4)
        bool admin = Session.IsAdmin;
        _add.Visible = _edit.Visible = _delete.Visible = _users.Visible = _reviews.Visible = admin;
        _authButton.Text = Session.IsLoggedIn ? "Log out" : "Log in";
        _status.Text = Session.IsLoggedIn
            ? $"Signed in as {Session.CurrentUser.FullName} ({Session.CurrentUser.Role})"
            : "Browsing as a visitor";

        LoadCategories();
        LoadBooks();
    }

    private void LoadCategories()
    {
        string selected = _category.SelectedItem as string;
        _category.Items.Clear();
        _category.Items.Add(AllCategories);
        foreach (var c in _books.GetCategories()) _category.Items.Add(c);
        int index = selected == null ? 0 : _category.Items.IndexOf(selected);
        _category.SelectedIndex = index < 0 ? 0 : index;
    }

    private void LoadBooks()
    {
        string category = _category.SelectedIndex <= 0 ? "" : (string)_category.SelectedItem;
        int minRating = _minRating.SelectedIndex; // "Any" = 0, "1+" = 1 ... "5" = 5
        var results = _books.Search(_search.Text, category, minRating, (string)_sort.SelectedItem);

        _grid.DataSource = null;
        _grid.DataSource = results;
        ConfigureColumns();

        if (results.Count == 0)
            _status.Text = "No results were found.";
        else
            _status.Text = (Session.IsLoggedIn
                ? $"Signed in as {Session.CurrentUser.FullName} ({Session.CurrentUser.Role})"
                : "Browsing as a visitor") + $"  |  {results.Count} book(s)";
    }

    private void ConfigureColumns()
    {
        if (_grid.Columns.Count == 0) return;
        _grid.Columns["Id"].Visible = false;
        _grid.Columns["Description"].Visible = false;
        _grid.Columns["RatingCount"].Visible = false;
        _grid.Columns["Title"].FillWeight = 160;
        _grid.Columns["Author"].FillWeight = 120;
        _grid.Columns["Category"].FillWeight = 70;
        _grid.Columns["Isbn"].HeaderText = "ISBN";
        _grid.Columns["Isbn"].FillWeight = 85;
        _grid.Columns["PublicationYear"].HeaderText = "Year";
        _grid.Columns["PublicationYear"].FillWeight = 40;
        _grid.Columns["AverageRating"].HeaderText = "Avg. rating";
        _grid.Columns["AverageRating"].DefaultCellStyle.Format = "0.0";
        _grid.Columns["AverageRating"].FillWeight = 50;
        _grid.Columns["ReviewCount"].HeaderText = "Reviews";
        _grid.Columns["ReviewCount"].FillWeight = 45;
    }

    private void ClearFilters()
    {
        _search.Clear();
        _category.SelectedIndex = 0;
        _minRating.SelectedIndex = 0;
        _sort.SelectedIndex = 0;
        LoadBooks();
    }

    private Book SelectedBook() => _grid.CurrentRow?.DataBoundItem as Book;

    private void OpenDetails()
    {
        var book = SelectedBook();
        if (book == null) { Ui.Info("Please select a book first."); return; }
        using var form = new BookDetailsForm(book.Id);
        form.ShowDialog(this);
        LoadBooks();
    }

    private void AddBook()
    {
        using var form = new BookEditForm(null);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadCategories();
            LoadBooks();
        }
    }

    private void EditBook()
    {
        var book = SelectedBook();
        if (book == null) { Ui.Info("Please select a book to edit."); return; }
        using var form = new BookEditForm(book);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadCategories();
            LoadBooks();
        }
    }

    private void DeleteBook()
    {
        var book = SelectedBook();
        if (book == null) { Ui.Info("Please select a book to delete."); return; }
        if (!Ui.Confirm($"Delete \"{book.Title}\"?\n\nAll its reviews and ratings will be deleted too.")) return;

        var result = _books.Delete(book.Id);
        if (!result.Success) { Ui.Error(result.Message); return; }
        LoadCategories();
        LoadBooks();
    }

    private void AuthClick()
    {
        _auth.Logout();
        LoggedOut = true;
        Close();
    }
}
