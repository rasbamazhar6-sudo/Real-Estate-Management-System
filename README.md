# Real Estate Management System

A desktop application for managing real estate projects, plots, customers, sales, installments, and financial reporting. Built as a university project for a **Visual Programming** course.

## Team

| Name | Role |
|------|------|
| Rasba Mazhar | Developer |
| Eisha Asim | Developer |
| Furqan Arshad | Developer |

## Features

- **Project & plot management** — create and manage housing schemes, plots, and availability
- **Customer (party) management** — store buyer/seller contact and profile details
- **Sales & ownership mapping** — link plots to customers and track ownership
- **Installment & payment tracking** — payment plans, installments, and transaction ledger
- **Authentication & security** — user login/registration with **SHA-256** password hashing
- **Dashboard analytics** — visual plot dashboard and receivables overview
- **Search & filtering** — find records across projects, plots, and customers
- **Report generation** — PDF/Excel exports (QuestPDF, ClosedXML)
- **Modern UI** — WPF interface with Material Design styling

## Tech Stack

| Layer | Technology |
|-------|------------|
| Language | C# |
| Framework | .NET 8 (Windows) |
| UI | WPF (Windows Presentation Foundation) |
| Database | SQL Server (LocalDB attach) |
| Data access | ADO.NET (`Microsoft.Data.SqlClient`) |
| Reporting | QuestPDF, ClosedXML |
| Security | SHA-256 password hashing, parameterized queries |

## Architecture

```
┌─────────────────────────────────┐
│   Presentation (WPF Pages)      │
├─────────────────────────────────┤
│   Data Access (Data/*.cs)       │
├─────────────────────────────────┤
│   SQL Server (RealEstateDB)     │
└─────────────────────────────────┘
```

See [BACKEND_ARCHITECTURE_SUMMARY.md](BACKEND_ARCHITECTURE_SUMMARY.md) for a detailed backend overview.

## Prerequisites

- **Windows 10/11**
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with:
  - .NET desktop development workload
  - SQL Server Data Tools (optional, for database scripts)
- **SQL Server LocalDB** (included with Visual Studio) — instance: `(LocalDB)\MSSQLLocalDB`
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Getting Started

### 1. Clone the repository

```powershell
git clone https://github.com/rasbamazhar6-sudo/Real-Estate-Management-System
cd VP
```

### 2. Restore dependencies

```powershell
dotnet restore Project.sln
```

Or open `Project.sln` in Visual Studio and let NuGet restore packages automatically.

### 3. Set up the database

The application connects to a LocalDB database file at:

`Database/RealEstateDB.mdf`

**Option A — Use existing database files (if you have them locally)**

1. Create the `Database` folder in the project root if it does not exist.
2. Place `RealEstateDB.mdf` and `RealEstateDB_log.ldf` inside `Database/`.
3. These files are **not** committed to Git (see `.gitignore`). Each developer maintains a local copy.

**Option B — Create a new database from scripts**

1. Open **SQL Server Management Studio (SSMS)** and connect to `(LocalDB)\MSSQLLocalDB`.
2. Create the database files under your project’s `Database` folder, for example:

   ```sql
   CREATE DATABASE RealEstateDB
   ON (NAME = N'RealEstateDB',
       FILENAME = N'C:\path\to\VP\Database\RealEstateDB.mdf',
       SIZE = 8MB, FILEGROWTH = 64MB)
   LOG ON (NAME = N'RealEstateDB_log',
           FILENAME = N'C:\path\to\VP\Database\RealEstateDB_log.ldf',
           SIZE = 8MB, FILEGROWTH = 64MB);
   ```

   Replace `C:\path\to\VP` with your actual clone path.

3. Review `project.sql` — it may contain machine-specific paths from the original export. Run the **table and constraint** sections against your `RealEstateDB`, or adjust file paths before executing the full script.
4. If upgrading an older schema, also run:
   - `Complete_Database_Migration.sql`
   - `Add_Cascade_Delete.sql`

### 4. Build and run

**Visual Studio**

1. Open `Project.sln`.
2. Set configuration to **Release** or **Debug**.
3. Press **F5** to run.

**Command line**

```powershell
dotnet build Project.sln -c Release
dotnet run --project Project.csproj -c Release
```

On successful build, the database files in `Database/` are copied to the output folder automatically (see `Project.csproj`).

### 5. First login

Register a new account from the **Create Account** screen, or use credentials provided by your team if a seed user exists in your local database.

## Project Structure

```
VP/
├── Data/                 # ADO.NET data access layer
├── Pages/                # WPF views (XAML + code-behind)
├── Services/             # Export and shared services
├── Properties/           # App resources and publish profiles
├── resources/            # Icons and UI assets
├── Database/             # LocalDB files (local only, not in Git)
├── project.sql           # Database schema script
├── Complete_Database_Migration.sql
├── Add_Cascade_Delete.sql
├── installer.iss         # Inno Setup installer script
└── Project.sln
```

## Building an Installer (optional)

An [Inno Setup](https://jrsoftware.org/isinfo.php) script is included as `installer.iss`.

1. Publish or build the app in **Release** mode.
2. Update the `Source` path in `installer.iss` to point to your `bin\Release\net8.0-windows\` output folder.
3. Compile the script with Inno Setup Compiler.

Installer binaries are excluded from Git; attach them to a **GitHub Release** if you want to distribute builds.

## NuGet Packages

| Package | Purpose |
|---------|---------|
| Microsoft.Data.SqlClient | SQL Server connectivity |
| MaterialDesignInXamlToolkitAddOns | UI components and styling |
| QuestPDF | PDF report generation |
| ClosedXML | Excel export |

## Troubleshooting

| Issue | Suggested fix |
|-------|----------------|
| Database connection failed | Confirm `(LocalDB)\MSSQLLocalDB` is installed and `Database/RealEstateDB.mdf` exists |
| MDF file locked | Close SSMS connections and stop any running instance of the app |
| Build errors after clone | Run `dotnet restore` and rebuild; ensure .NET 8 SDK is installed |
| Missing tables | Re-run `project.sql` and migration scripts against your database |

## Documentation

- [BACKEND_ARCHITECTURE_SUMMARY.md](BACKEND_ARCHITECTURE_SUMMARY.md) — data access layer and security overview
- [BACKEND_PRESENTATION_GUIDE.md](BACKEND_PRESENTATION_GUIDE.md) — presentation and demo notes

## License

This project was developed for academic purposes. Contact the team before commercial use or redistribution.
