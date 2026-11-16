using BookReviewSystem.Data;
using BookReviewSystem.Views;

namespace BookReviewSystem;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            Database.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show("The database could not be opened:\n" + ex.Message,
                "Book Review System", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Login -> main window -> (logout returns to login)
        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK) break;

            using var main = new MainForm();
            main.ShowDialog();
            if (!main.LoggedOut) break;
        }
    }
}
