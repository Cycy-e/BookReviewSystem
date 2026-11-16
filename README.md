# Book Review and Management System (Lab 3)

A Windows Forms desktop application (C#, .NET 8) with a SQLite database, built with the **MVC** pattern
from the Software Requirements Specification (FR-1 to FR-16, NFR-1 to NFR-6).

**Author:** Uwera Cynthia Carmella (Student ID 27404), AUCA

## Requirements
- Windows with the .NET 8 SDK (or Visual Studio 2022 with the ".NET desktop development" workload)

## Run
```
dotnet restore
dotnet run
```
Or open `BookReviewSystem.csproj` in Visual Studio and press F5.

The database file `bookreview.db` is created automatically next to the executable on first run, with a demo
administrator and six sample books.

| Role | Email | Password |
|------|-------|----------|
| Administrator | admin@bookreview.com | Admin@123 |
| Reader | register a new account from the login window | - |

## MVC structure
| Layer | Folder | Responsibility |
|-------|--------|----------------|
| Model | `Models/` | `User`, `Book`, `Review`, `OperationResult` |
| View | `Views/` | WinForms windows: login, register, catalogue, book details, book edit, admin users, admin reviews |
| Controller | `Controllers/` | `AuthController`, `BookController`, `ReviewController`, `AdminController`, `Session` - all validation, permissions and SQL |
| Data | `Data/` | SQLite connection, schema, seed data, PBKDF2 password hashing |

Views never talk to the database; they call controllers and show the returned `OperationResult` messages.

## Requirement coverage
| Requirement | Where |
|-------------|-------|
| FR-1 Register (validation, duplicate email, weak password) | `AuthController.Register`, `RegisterForm` |
| FR-2 / FR-3 Login with clear errors, logout | `AuthController`, `LoginForm`, `MainForm` |
| FR-4 Role permissions | `Session`, checks in every controller, admin buttons hidden for others |
| FR-5 to FR-7 Add / edit / delete book (confirm, cascade delete) | `BookController`, `BookEditForm`, `MainForm` |
| FR-8 Book details, average rating, reviews | `BookDetailsForm` |
| FR-9 to FR-12 Reviews and ratings (1-5, one per reader per book, own edit/delete, newest first) | `ReviewController`, `BookDetailsForm` |
| FR-13 / FR-14 Search, filter by category and minimum rating, sort | `BookController.Search`, `MainForm` |
| FR-15 / FR-16 Admin reviews and users | `AdminReviewsForm`, `AdminUsersForm` |
| NFR-3 Hashed passwords | `Data/PasswordHasher.cs` |
| NFR-5 Data kept after restart | SQLite file database |
| NFR-6 Layered code | MVC folders above |

## Push to GitHub
```
git add .
git commit -m "Lab 3: Book Review and Management System (WinForms, MVC, SQLite)"
git branch -M main
git remote add origin https://github.com/<your-username>/BookReviewSystem.git
git push -u origin main
```
