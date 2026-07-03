# Productivity Hub

A personal, local-only productivity app — a small dashboard tying together six lightweight tools:

- **Todos** — tasks with priority, due date, done state, and full **edit** (title, notes, priority, due date)
- **Quick-capture inbox** — dump a thought instantly, triage it into a todo later
- **Bookmarks / read-later** — save links, mark as read; capture from the browser extension or by [forwarding links from Teams on your phone](docs/teams-link-import.md)
- **Notes** — free-text scratchpad
- **Pomodoro timer** — 25/5 focus sessions, optionally tied to a task, with a floating always-on-top window
- **Daily journal** — one dated entry per day
- **Projects** — group todos, notes, and bookmarks under colour-coded projects (New/Active/Complete/Archived); assign from each item or from the project, with per-list project filters
- **Settings** — show/hide any section to keep the app focused

Single-user and local — no authentication, all data in a local SQLite file on your machine.

## Two ways to run it

There are **two front-ends over one shared database** (`%APPDATA%\ProductivityHub\productivityhub.db`),
so you can use either and the data stays in sync:

1. **Desktop app (WPF)** — a native Windows app that opens instantly with **no server**. Recommended
   for everyday use. See [Desktop app](#desktop-app-wpf).
2. **Web app** — the ASP.NET Core + React version served on `http://localhost:5180`.

## Stack

- **Core:** `ProductivityHub.Core` — shared EF Core 9 entities, `AppDbContext`, schema patch, backup
  (used by both front-ends).
- **Desktop:** .NET 9 **WPF** (`ProductivityHub.Desktop`) — talks to SQLite in-process, no server.
- **API:** ASP.NET Core 9 (attribute controllers) — `ProductivityHub.Api`.
- **Web:** React 19 + Vite + TypeScript + React Router 7 + TanStack Query + Bootstrap 5.
- One Kestrel process serves both the API and the built SPA on `http://localhost:5180`.

## Desktop app (WPF)

Native window, opens with a double-click, **no server or localhost**. Same features as the web app —
todos, inbox, bookmarks, notes, Pomodoro (with an always-on-top mini timer), journal, projects,
search, dark mode, backup/restore, and Teams link-import — over the shared database.

```powershell
# Publish a single-file executable
dotnet publish src\ProductivityHub.Desktop -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# (optional) put a shortcut on your Desktop
powershell -ExecutionPolicy Bypass -File .\create-desktop-app-shortcut.ps1
```

The exe is at `src\ProductivityHub.Desktop\bin\Release\net9.0-windows\win-x64\publish\ProductivityHub.Desktop.exe`
— double-click it (or the Desktop shortcut). Requires the .NET 9 runtime (add `--self-contained true`
to the publish command to bundle it and drop that requirement). Set the Teams link-import folder under
**Settings** in the app.

## Layout

```
ProductivityHub.sln
launch.cmd              # starts the app + opens the browser
create-shortcut.ps1     # one-time: makes a Desktop shortcut to launch.cmd
extension/              # Chrome/Edge browser extension (save bookmarks)
src/
  ProductivityHub.Api/  # API + serves the built SPA from wwwroot
  ProductivityHub.Web/  # React app (builds into ../ProductivityHub.Api/wwwroot)
```

---

## Installation

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/) (includes npm)

Check they're installed:

```bash
dotnet --version   # 9.x
node --version     # v18+ (v22 tested)
```

### 1. Get the code

```bash
git clone https://github.com/nickyjt16/personal-productivity.git
cd personal-productivity
```

### 2. Build the frontend

The React app builds into the API's `wwwroot`, so a single process serves everything:

```bash
cd src/ProductivityHub.Web
npm install
npm run build
cd ../..
```

### 3. Run the app

```bash
dotnet run --project src/ProductivityHub.Api
```

Then open <http://localhost:5180>. A `productivityhub.db` SQLite file is created automatically on
first run.

### 4. (Optional) One-click desktop launch — Windows

After the first frontend build, create a Desktop shortcut:

```powershell
powershell -ExecutionPolicy Bypass -File .\create-shortcut.ps1
```

Double-click **Productivity Hub** on your Desktop — it starts the app and opens the dashboard.
Close the launcher window to stop the app. (The shortcut just runs `launch.cmd`, which you can also
double-click directly.)

---

## Browser extension — save bookmarks

The extension in `extension/` adds a toolbar button and a right-click **Save to Productivity Hub**
menu that send the current page to your read-later list. It's a single Manifest V3 extension that
works in **both Chrome and Edge** — load the same `extension/` folder into each browser you use.

> The app must be **running** for saves to land. If it isn't, the extension shows a "could not save"
> notification.

### Install in Google Chrome

1. Go to `chrome://extensions`.
2. Toggle **Developer mode** on (top-right).
3. Click **Load unpacked**.
4. Select the `extension/` folder inside this repo.
5. (Optional) Pin it: click the puzzle-piece icon in the toolbar → pin **Productivity Hub — Save Bookmark**.

### Install in Microsoft Edge

1. Go to `edge://extensions`.
2. Toggle **Developer mode** on (left sidebar).
3. Click **Load unpacked**.
4. Select the `extension/` folder inside this repo.
5. (Optional) Pin it via the toolbar's extensions (puzzle-piece) menu.

### Using it

- **Toolbar button** — shows the current page; click **Save to read-later**.
- **Right-click** any page or link → **Save to Productivity Hub**.

A desktop notification confirms the save. If you run the app on a different port, change `HUB_BASE`
in `extension/config.js` and reload the extension.

---

## Forwarding links from your phone (via Teams)

On mobile, send yourself a link in a Teams chat and have it appear in **Bookmarks** automatically.
A Power Automate flow writes the link to a OneDrive folder that syncs to your PC, and the Hub
imports it — no authentication needed in the app. Off by default; see
**[docs/teams-link-import.md](docs/teams-link-import.md)** for the full setup (folder, `LinkImport`
config, and the flow). Once configured, links arrive on a timer or via the **↻ Check for new links**
button on the Bookmarks page.

---

## Floating Pomodoro timer

Press **Start** and the timer pops out into a small always-on-top floating window (Chrome/Edge 116+,
via the Document Picture-in-Picture API) showing the countdown plus **Pause/Resume**, **Restart**, and
**Reopen app** buttons. When the session finishes, the floating window closes, the app tab is
refocused, and you get a desktop notification + a chime. On browsers without Document PiP, the timer
stays in-page. (Allow notifications when first prompted.)

---

## Development

Run the API and the Vite dev server separately for hot-reload (Vite proxies `/api` to the API):

```bash
# Terminal 1 — API on http://localhost:5180
dotnet run --project src/ProductivityHub.Api

# Terminal 2 — Vite dev server on http://localhost:5173
cd src/ProductivityHub.Web && npm run dev
```

Rebuild the production bundle (`npm run build`) only when you want the single-process app on `:5180`
to reflect frontend changes — the API serves whatever is in `wwwroot`.
