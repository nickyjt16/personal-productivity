# Productivity Hub

A simple personal productivity app that runs **on your own computer**. Your data stays on your
machine — there's no account, no sign-in, and nothing is sent to the cloud.

It has:

- ✅ **Todos** — tasks with priority, due dates, and optional **repeat** (daily/weekly/monthly)
- 📥 **Inbox** — jot something down fast, sort it later
- 🔖 **Bookmarks** — save links to read later (with a browser button — see below)
- 📝 **Notes** — a simple notepad
- 📔 **Journal** — one entry per day
- 📁 **Projects** — group todos, notes and bookmarks together
- 🔑 **Secrets** — track passwords/keys and their expiry dates; get a warning a week before one expires
- 🍅 **Pomodoro timer** — a focus timer with a floating always-on-top window
- 🔎 **Search**, 🌙 **dark mode**, and one-click **backup / restore**

The **desktop app** adds a few extras:

- 🔔 **Reminders on launch** — a heads-up for todos due today/overdue and secrets expiring soon
- ⚡ **Quick capture** — press **Ctrl+Alt+N** anywhere to drop a thought into your Inbox without switching windows
- 📌 **Lives in the tray** — closing the window keeps it running in the notification area; right-click the tray icon to open, quick-capture, or quit
- ☁️ **Optional sync** — in Settings you can move the data file into OneDrive (or any folder) to share it between machines; **off by default**, everything stays local until you turn it on

There are **two versions of the app and they share the same data**, so you can use whichever you like:

1. **Desktop app** — a normal Windows app that opens in its own window. **Easiest — start here.**
2. **Web app** — runs in your browser.

---

## Features in detail

- **✅ Todos** — each task has a **priority** (Low/Medium/High) and an optional **due date**. Overdue
  and due-today tasks get a coloured badge. Tasks can **repeat** — set *Daily*, *Weekly*, or *Monthly*
  and, when you tick one off, instead of disappearing it rolls its due date forward to the next
  occurrence. Assign a task to one or more **projects** (see below).
- **📥 Inbox** — a fast scratch space. Type a thought, press **Enter**, and it's saved. Later, triage
  each item: turn it into a todo, mark it done, or delete it.
- **🔖 Bookmarks** — save links to read later, mark them read/unread, and group them by project. Save
  pages straight from your browser with the extension (section 3), including smart LinkedIn-post capture.
- **📝 Notes** — a simple notepad; notes can belong to projects.
- **📔 Journal** — one dated entry per day.
- **📁 Projects** — a project groups related **todos, notes, bookmarks and secrets**. Give it a colour
  and a status (Active/Complete/Archived). Every list has a **project filter** to focus on one project,
  and the project view shows progress across its items.
- **🔑 Secrets** — track API keys, passwords, client secrets and their **expiry dates**. You get a
  warning a week before one expires (and again on the desktop app at launch). Each secret can have a
  link to where it's managed and can be linked to projects. Secret **values can be encrypted** behind
  a master password you set (see "Your data" below).
- **🍅 Pomodoro** — a focus timer with a floating, always-on-top mini window so it stays visible while
  you work in other apps.
- **🔎 Search** — type in the search box and press **Enter** to search across everything.
- **🌙 Dark mode** and one-click **backup / restore** live in **Settings**.

### Desktop-only extras

- **🔔 Launch reminders** — when the desktop app starts it shows todos due today/overdue and any secrets
  expiring soon.
- **📌 Runs in the system tray** — closing the window doesn't quit the app; it tucks into the
  notification area (bottom-right of the taskbar). **Double-click** the tray icon to reopen, or
  **right-click** it for *Open / Quick capture / Quit*.
- **⚡ Global quick capture** — press **Ctrl+Alt+N from anywhere** (even when the app is hidden or you're
  in another program) to pop a small box; jot a thought and it lands in your Inbox to sort later.
- **☁️ Optional sync** — **Settings → Data location** can move the data file into OneDrive (or any
  folder) so two PCs share it. It's **off by default** — everything stays on your machine until you
  turn it on.

### Keyboard shortcuts

| Shortcut | Where | What it does |
|----------|-------|--------------|
| **Ctrl+Alt+N** | Anywhere (desktop app) | Open quick capture — jot a note into the Inbox |
| **Ctrl+Enter** | Quick-capture box | Save the note |
| **Esc** | Quick-capture box | Close without saving |
| **Enter** | Inbox capture field | Add the item |
| **Enter** | Search box | Run the search |

### Where do I add a todo/secret to a project?

- **Desktop app:** each todo or secret row has a small **🏷 tag button** on the right — click it to pick
  its projects.
- **Web app:** on **Secrets**, the project picker is in the add/edit form. On **Todos**, click the
  **✎ edit** button on a task and the project picker appears in the edit row.

---

## 1. Desktop app (recommended)

### Step 1 — install .NET (one-time)

Download and install the **.NET 9** "Runtime" (or SDK) from Microsoft:
https://dotnet.microsoft.com/download/dotnet/9.0 — pick **.NET Desktop Runtime 9.0, Windows x64**.

### Step 2 — get this project

Click the green **Code** button on the GitHub page → **Download ZIP**, then unzip it somewhere
(e.g. your Documents folder). *(If you know Git, you can instead run
`git clone https://github.com/nickyjt16/personal-productivity.git`.)*

### Step 3 — build it (one-time)

Open **PowerShell**, go to the project folder, and run this one line:

```powershell
dotnet publish src\ProductivityHub.Desktop -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

When it finishes, it prints where it put the app. The program is here:

```
src\ProductivityHub.Desktop\bin\Release\net9.0-windows\win-x64\publish\ProductivityHub.Desktop.exe
```

### Step 4 — run it

**Double-click `ProductivityHub.Desktop.exe`.** That's it — the app opens in a window, no server, no
browser. (Optional: run `powershell -ExecutionPolicy Bypass -File .\create-desktop-app-shortcut.ps1`
to put a shortcut on your Desktop. If Windows shows a blue "Windows protected your PC" box the first
time, click **More info → Run anyway** — that appears because the app isn't code-signed.)

> **Tip for sharing with someone who isn't technical:** add `--self-contained true` to the Step 3
> command. That makes a bigger file that includes everything, so they can just double-click it
> **without installing .NET first**.

---

## 2. Web app

Use this if you prefer working in a browser. It needs two free tools installed once:
[.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) and
[Node.js](https://nodejs.org/) (the "LTS" version).

```powershell
# from the project folder:

# build the web page (one-time, and whenever the web files change)
cd src\ProductivityHub.Web
npm install
npm run build

# start the app
cd ..\..
dotnet run --project src\ProductivityHub.Api
```

Then open **http://localhost:5180** in your browser. To stop it, close the PowerShell window.

---

## 3. Browser extension (save links with one click)

A small add-on for **Chrome or Edge** that saves the page you're on to your Bookmarks. Works with both
browsers — load the same `extension` folder into each.

**Install:**
1. In your browser go to `chrome://extensions` (Chrome) or `edge://extensions` (Edge).
2. Turn on **Developer mode** (a switch on the page).
3. Click **Load unpacked** and choose the **`extension`** folder inside this project.
4. Pin the new **Productivity Hub** button to your toolbar.

**Use it:** click the toolbar button to save the current page, or **right-click a link → Save to
Productivity Hub**. The desktop or web app must be running for the save to land.

> **LinkedIn tip:** the extension is smart about LinkedIn — clicking the button on your feed saves the
> **specific post** you're looking at (not just "linkedin.com"). If you want a particular post, scroll
> so it's the main thing on screen, then click the button. Or right-click the post's timestamp/link →
> **Save to Productivity Hub**.

---

## Your data, backups & secrets

- Everything is stored in a single file on your PC: `%APPDATA%\ProductivityHub\productivityhub.db`
  (both the desktop and web app use it, which is why they stay in sync).
- The desktop app makes an automatic copy of that file each time it starts (in a `backups` folder next
  to it), so you're protected against accidents.
- **Settings → Backup & restore** lets you export everything to a file, or restore from one — handy for
  moving to a new PC.
- **Secret values** can be **encrypted with a master password**. On the Secrets page, set a master
  password (the desktop app also offers this on first launch). Values are then encrypted (AES-GCM,
  key derived from your password) and you unlock them per session to view or edit them. Everything
  else — names, expiry dates, notes — stays readable so expiry reminders keep working.
  - The password is **never stored and can't be reset**. If you forget it, you'll need to re-enter the
    secrets (an optional hint can jog your memory). This is what keeps them safe even in a backup or a
    synced copy. If you'd rather not use it, just skip it — values are then stored in plain text.
  - The database file never leaves your machine and isn't shared on GitHub, but treat it as sensitive.

## Automatic link forwarding from Teams (optional, advanced)

If you use Microsoft Teams, you can have links you send yourself appear in Bookmarks automatically —
see [docs/teams-link-import.md](docs/teams-link-import.md).

## For developers

- **Core** (`ProductivityHub.Core`): shared data layer (EF Core 9 + SQLite) used by both apps.
- **Desktop** (`ProductivityHub.Desktop`): .NET 9 WPF, in-process, no server.
- **API** (`ProductivityHub.Api`): ASP.NET Core 9, serves the built React SPA + JSON API.
- **Web** (`ProductivityHub.Web`): React 19 + Vite + TypeScript + Bootstrap 5.
- Tests: `dotnet test`. CI (GitHub Actions) builds Core/API/web + runs tests on every push (the
  Windows-only WPF app is built locally).
- **Using an AI coding agent?** See [CLAUDE.md](CLAUDE.md) — it explains the architecture, the
  by-hand schema-evolution convention (no EF migrations), and the gotchas to avoid before you edit.
