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
- **Secrets** are stored locally in that same file, in plain text. It never leaves your machine and
  isn't part of what's shared on GitHub, but treat the file itself as sensitive.

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
