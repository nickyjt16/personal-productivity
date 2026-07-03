// Service worker: right-click context menu + shared save logic.
importScripts('config.js', 'resolve.js')

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

  // If a specific link was right-clicked, save exactly that.
  if (info.linkUrl) { await saveBookmark(info.linkUrl, undefined); return }

  // Otherwise resolve the best target (the in-view LinkedIn post / the article).
  let url = info.pageUrl || tab?.url
  let title = tab?.title
  try {
    const [res] = await chrome.scripting.executeScript({ target: { tabId: tab.id }, func: resolveBestLink })
    if (res?.result?.url) { url = res.result.url; title = res.result.title }
  } catch { /* fall back to the page URL */ }
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
