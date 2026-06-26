// Service worker: right-click context menu + shared save logic.
importScripts('config.js')

const MENU_ID = 'save-to-hub'

chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: MENU_ID,
    title: 'Save to Productivity Hub',
    contexts: ['page', 'link'],
  })
})

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId !== MENU_ID) return
  // Prefer a right-clicked link; otherwise save the page itself.
  const url = info.linkUrl || info.pageUrl || tab?.url
  const title = info.linkUrl ? undefined : tab?.title
  await saveBookmark(url, title)
})

// Saves a bookmark to the Hub API and shows a desktop notification with the
// result. Returns true on success.
async function saveBookmark(url, title) {
  if (!url) {
    notify('Nothing to save', 'No URL found for this page.')
    return false
  }
  try {
    const res = await fetch(`${HUB_BASE}/api/bookmarks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ url, title: title || undefined }),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    notify('Saved to Productivity Hub ✓', title || url)
    return true
  } catch (err) {
    notify('Could not save', 'Is Productivity Hub running? ' + (err?.message ?? ''))
    return false
  }
}

function notify(title, message) {
  chrome.notifications.create({
    type: 'basic',
    iconUrl: 'icon128.png',
    title,
    message: message ?? '',
  }, () => {
    // Swallow "icon not found" errors so a missing icon never breaks saving.
    void chrome.runtime.lastError
  })
}

// Expose for the popup via messaging.
chrome.runtime.onMessage.addListener((msg, _sender, sendResponse) => {
  if (msg?.type === 'save') {
    saveBookmark(msg.url, msg.title).then(sendResponse)
    return true // async response
  }
})
