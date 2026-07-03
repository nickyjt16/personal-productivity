// Popup: shows the resolved target (post/article, not just the site) and saves it.
const titleEl = document.getElementById('title')
const urlEl = document.getElementById('url')
const saveBtn = document.getElementById('save')
const statusEl = document.getElementById('status')

let current = { url: '', title: '' }

async function resolveTarget() {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true })
  try {
    const [res] = await chrome.scripting.executeScript({ target: { tabId: tab.id }, func: resolveBestLink })
    if (res?.result?.url) return res.result
  } catch {
    // no scripting access (e.g. chrome:// page) — fall back to the tab URL
  }
  return { url: tab?.url ?? '', title: tab?.title ?? '' }
}

async function init() {
  current = await resolveTarget()
  titleEl.textContent = current.title || '(untitled)'
  urlEl.textContent = current.url
}

saveBtn.addEventListener('click', async () => {
  saveBtn.disabled = true
  statusEl.textContent = 'Saving…'
  statusEl.className = 'status'
  try {
    const res = await fetch(`${HUB_BASE}/api/bookmarks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ url: current.url, title: current.title || undefined }),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    statusEl.textContent = 'Saved ✓'
    statusEl.className = 'status ok'
    setTimeout(() => window.close(), 800)
  } catch {
    statusEl.textContent = 'Failed — is Productivity Hub running?'
    statusEl.className = 'status err'
    saveBtn.disabled = false
  }
})

init()
