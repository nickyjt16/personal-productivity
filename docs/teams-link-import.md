# Forwarding links from Teams (phone) → Bookmarks

When you're on your phone and want to read something later, send yourself the link in a Teams
chat. A Power Automate flow drops that link into a OneDrive folder, OneDrive syncs the file to your
PC, and Productivity Hub imports it into **Bookmarks** — automatically while it's running, or on
demand with the **↻ Check for new links** button.

This route needs **no authentication in the app**: the OneDrive desktop client (already signed in
to Windows) does the syncing, and the Hub only ever reads a local folder.

```
Phone → Teams self-chat → Power Automate → file in OneDrive → (OneDrive sync) → local folder → Hub → Bookmarks
```

## 1. Pick a OneDrive folder

Create a folder in your OneDrive, e.g. **`Apps/ProductivityHub/incoming`**.

Find its **local** path (where OneDrive syncs it). For work OneDrive it's usually:

```
C:\Users\<you>\OneDrive - <Org>\Apps\ProductivityHub\incoming
```

In File Explorer, right-click the folder → **Always keep on this device** (so files download
locally instead of staying cloud-only).

## 2. Point the Hub at that folder

In `src/ProductivityHub.Api/appsettings.json`, set:

```json
"LinkImport": {
  "Enabled": true,
  "FolderPath": "C:\\Users\\<you>\\OneDrive - <Org>\\Apps\\ProductivityHub\\incoming",
  "IntervalSeconds": 60
}
```

`%OneDriveCommercial%` / `%OneDrive%` environment variables are expanded, so you can also write
`"%OneDriveCommercial%\\Apps\\ProductivityHub\\incoming"`. While running, the Hub scans this folder
every `IntervalSeconds` and on startup.

## 3. Build the Power Automate flow

Create an **automated cloud flow**:

1. **Trigger:** Microsoft Teams → **When a new message is added to a chat**. Set the chat to your
   own **self-chat** (the chat with just you).
2. **Action:** OneDrive for Business → **Create file**.
   - **Folder Path:** `/Apps/ProductivityHub/incoming`
   - **File Name:** `link-@{utcNow()}.txt` (any unique name)
   - **File Content:** the message body (the trigger's *Message* / *Content* field).

That's it — you don't need to parse the URL in the flow. The Hub extracts the first `http(s)` link
(or several) from whatever text the message contained, so you can send a bare URL or a URL with a
note around it.

> If your tenant only exposes a channel trigger, you can instead post links to a private Teams
> channel you own and trigger on that — the file-creation action is identical.

## 4. Test it

1. Make sure the Hub is running and `LinkImport.Enabled` is `true`.
2. Send yourself a Teams message containing a link.
3. Within ~a minute (OneDrive sync + the import interval) it appears in **Bookmarks** — or click
   **↻ Check for new links** to pull immediately.

## How the import behaves

- Extracts **all** `http(s)` URLs found in each file (one message can yield several bookmarks).
- **De-duplicates** against links you already have.
- Files with a link are **deleted** after import; files with **no** link are moved to a `_skipped`
  subfolder (never lost, never re-processed).
- If a file is still being written/synced, it's skipped and retried on the next pass.
