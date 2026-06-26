// Popup: shows the current tab and saves it to the Hub on click.
const titleEl = document.getElementById('title')
const urlEl = document.getElementById('url')
const saveBtn = document.getElementById('save')
const statusEl = document.getElementById('status')

let current = { url: '', title: '' }

async function init() {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true })
  current = { url: tab?.url ?? '', title: tab?.title ?? '' }
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
  } catch (err) {
    statusEl.textContent = 'Failed — is Productivity Hub running?'
    statusEl.className = 'status err'
    saveBtn.disabled = false
  }
})

init()
