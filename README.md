# Productivity Hub

A personal, local-only productivity app — a small dashboard tying together six lightweight tools:

- **Todos** — tasks with priority, due date, done state, and full **edit** (title, notes, priority, due date)
- **Quick-capture inbox** — dump a thought instantly, triage it into a todo later
- **Bookmarks / read-later** — save links, mark as read
- **Notes** — free-text scratchpad
- **Pomodoro timer** — 25/5 focus sessions, optionally tied to a task, with a floating always-on-top window
- **Daily journal** — one dated entry per day
- **Settings** — show/hide any section to keep the app focused

Single-user and local — no authentication, all data in a local SQLite file on your machine.

## Stack

- **API:** ASP.NET Core 9 (attribute controllers), EF Core 9 + SQLite
- **Web:** React 19 + Vite + TypeScript + React Router 7 + TanStack Query + Bootstrap 5
- One Kestrel process serves both the API and the built SPA on `http://localhost:5180`.

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
