namespace BookReviewSystem.Views;

/// <summary>Small helpers shared by all forms.</summary>
public static class Ui
{
    public static void Info(string message) =>
        MessageBox.Show(message, "Book Review System", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Error(string message) =>
        MessageBox.Show(message, "Book Review System", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static bool Confirm(string message) =>
        MessageBox.Show(message, "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    public static Label Label(string text, int x, int y, int width = 100) =>
        new() { Text = text, Left = x, Top = y + 3, Width = width, AutoSize = false };

    public static string Stars(double average, int count) =>
        count == 0 ? "No ratings yet" : $"{average:0.0} / 5  ({count} rating{(count == 1 ? "" : "s")})";

    /// <summary>Applies the common look used by every grid in the application.</summary>
    public static void StyleGrid(DataGridView grid)
    {
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.FixedSingle;
    }
}
