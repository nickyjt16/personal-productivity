# CLAUDE.md — guide for AI agents working on Productivity Hub

This file orients an AI coding agent (Claude Code or similar) that has just cloned this repo.
It explains what the app is, how the pieces fit, the conventions to follow, and the traps to avoid.
Read it before making changes.

## What this app is

A **personal, local-only productivity app** for a single user. There is **no authentication, no
multi-tenancy, and no cloud** — all data lives in one SQLite file on the user's machine. Do not add
sign-in, user accounts, telemetry, or calls to external services unless explicitly asked. Privacy and
"it just runs on my PC" are core product values.

There are **two front-ends that share the same database**:
1. **Desktop app** (WPF) — runs in-process, no server.
2. **Web app** (React SPA) — served by the ASP.NET Core API.

Feature areas: Todos (with priority, due dates, and daily/weekly/monthly recurrence), Inbox, Bookmarks,
Notes, Journal, Projects, Secrets (expiry tracking), Pomodoro, plus Search, dark mode, and backup/restore.

## Solution layout

```
src/
  ProductivityHub.Core/      Shared data layer — EF Core 9 + SQLite. Referenced by API and Desktop.
  ProductivityHub.Api/       ASP.NET Core 9. JSON API under /api + serves the built React SPA from wwwroot.
  ProductivityHub.Web/       React 19 + Vite + TypeScript + React Router 7 + TanStack Query + Bootstrap 5.
  ProductivityHub.Desktop/   .NET 9 WPF (net9.0-windows). Uses Core directly, no HTTP.
tests/
  ProductivityHub.Api.Tests/ xUnit tests against the API/Core.
extension/                   Chrome/Edge bookmark-capture extension (plain JS).
docs/                        Extra docs (e.g. Teams link import).
```

### Core (the important one)
- `AppDbContext` — one `DbSet` per entity (`Todos`, `InboxItems`, `Bookmarks`, `Notes`,
  `JournalEntries`, `PomodoroSessions`, `Projects`, `Secrets`) plus join tables
  (`TodoProjects`, `NoteProjects`, `BookmarkProjects`, `SecretProjects`).
- `Entities.cs` — all entity classes and enums (`Priority`, `ProjectStatus`, `RecurUnit`).
- **`SchemaUpdater.cs` — schema evolution is done by hand, NOT EF migrations.** On startup the API
  calls `db.Database.EnsureCreated()` then `SchemaUpdater.ApplyAsync(db)`, which runs idempotent
  `CREATE TABLE IF NOT EXISTS` statements and an `AddColumnIfMissingAsync(db, table, column, sqlType)`
  helper for new columns. There is **no `Migrations/` folder — do not add one.** To add a field, add
  the property to the entity AND add an `AddColumnIfMissingAsync(...)` call in `SchemaUpdater`.
- `AppPaths.cs` — resolves the DB location. Default is `%APPDATA%\ProductivityHub\productivityhub.db`.
  A pointer file `db-location.txt` in that folder can redirect the DB elsewhere (this is the optional
  OneDrive-sync feature). Use `AppPaths.ConnectionString` / `AppPaths.DatabasePath`.
- **`DateTimeOffsetToBinaryConverter`** is applied to `DateTimeOffset` columns so SQLite `ORDER BY`
  sorts chronologically. Keep using it for any new `DateTimeOffset` fields, or ordering breaks.
- Dates that are date-only use `DateOnly`.

### API
- Controllers live in `src/ProductivityHub.Api/Controllers/` — one per feature. They expose small
  DTOs (defined in the controller file) and use `AppDbContext` directly. Follow the existing DTO +
  request-record pattern when adding endpoints.
- Dev URL: **http://localhost:5180** (see `Properties/launchSettings.json`).
- The API serves the SPA from `wwwroot/`, which is the **build output of the web app** (committed).

### Web
- `src/api/types.ts` — TS types mirroring the API DTOs.
- `src/api/hooks.ts` — TanStack Query hooks (`useTodos`, `useCreateTodo`, `useSetItemProjects`, …).
- `src/pages/` — one page per feature. `src/components/` — shared (`ProjectPicker`, `ProjectBadges`,
  `ProjectFilter`).
- `vite.config.ts` builds **straight into `../ProductivityHub.Api/wwwroot`** (`emptyOutDir: true`) so
  one Kestrel process serves everything in production. `npm run dev` proxies `/api` → `:5180`.

### Desktop (WPF)
- `App.xaml.cs` — startup: loads settings, applies theme, inits DB, shows `MainWindow`, sets up the
  tray, and fires launch reminders (`NotifyDueTodosAsync`, `NotifyExpiringSecretsAsync`).
- `MainWindow.xaml(.cs)` — sidebar nav + content host; `Views/*View.xaml(.cs)` are the pages.
- `Db.cs` — `Db.Context()` returns an `AppDbContext`; `Db.InitAsync()` creates/updates schema.
- `Rows.cs` — display-model wrappers (e.g. `TodoRow`, `SecretRow`) with computed badge text/brushes.
- **Theming:** `Themes/Light.xaml` + `Dark.xaml` are swappable brush dictionaries; `Themes/Controls.xaml`
  holds implicit control styles/templates. `App.ApplyTheme("dark"|"light")` swaps the first merged
  dictionary at runtime. Use `DynamicResource` for theme brushes (keys: `BgBrush`, `SurfaceBrush`,
  `TextBrush`, `MutedBrush`, `AccentBrush`, `SidebarBrush`, etc.).
- **Tray + hotkey:** `TrayManager.cs` owns a WinForms `NotifyIcon`, minimise-to-tray, and a global
  **Ctrl+Alt+N** hotkey (Win32 `RegisterHotKey` on the main window's HWND) that opens
  `QuickAddWindow` to drop a note into the Inbox. `ShutdownMode="OnExplicitShutdown"` (in `App.xaml`)
  means closing the window hides it; only the tray "Quit" ends the process.

## Conventions & gotchas

- **A change usually spans all three layers.** Adding a todo field, for example, touches:
  `Entities.cs` → `SchemaUpdater.cs` (column) → `TodosController` DTO/requests → web
  `types.ts` + `hooks.ts` + page → desktop `Rows.cs` + `Views/TodosView.xaml(.cs)`. The recurrence
  feature is a good worked example to copy.
- **Desktop project has `UseWindowsForms` + `UseWPF` both true** (needed for the tray). The `.csproj`
  **removes the `System.Windows.Forms` and `System.Drawing` implicit global usings** because they
  collide with WPF's `Brush`/`Button`/`UserControl`. In `TrayManager.cs` those namespaces are imported
  explicitly (WinForms via a `WinForms` alias). Keep that arrangement — don't re-add the global usings.
- **Custom WPF ComboBox template** renders `SelectionBoxItem.ToString()`, not `DisplayMemberPath`. Give
  combo item models a `ToString()` override (see `LinkRow`/`ComboItem` in `Rows.cs`) and set
  `SelectedValuePath` rather than `DisplayMemberPath`.
- **Secrets are stored in plain text** in the DB by design (local-only). Encryption is intentionally
  not implemented — if asked, discuss DPAPI (local-only) vs a master password (survives sync) first,
  because DPAPI conflicts with the OneDrive-sync feature.
- Don't commit real secrets. The `.db` file is git-ignored.

## Build, run, test

```powershell
# Desktop (recommended for quick manual checks)
dotnet run --project src\ProductivityHub.Desktop

# Web + API
cd src\ProductivityHub.Web; npm install; npm run build; cd ..\..
dotnet run --project src\ProductivityHub.Api      # then open http://localhost:5180

# Web dev server with hot reload (run the API separately for /api)
cd src\ProductivityHub.Web; npm run dev

# Tests
dotnet test

# Ship a double-clickable exe (no .NET install needed)
dotnet publish src\ProductivityHub.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**CI** (`.github/workflows/ci.yml`) builds Core + API + web and runs tests on every push. The WPF
desktop app is Windows-only and is **built/verified locally**, not in CI — so if you change desktop
code, build it yourself before claiming it works.

## When you finish a change

- Build the affected project(s) and run `dotnet test`.
- If you touched the web app, run `npm run build` so `wwwroot` (committed) stays in sync.
- If you touched desktop code, build the Desktop project — CI won't catch WPF breakage.
