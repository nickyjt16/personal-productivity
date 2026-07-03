// Injected into the current tab to work out the *best* URL + title to save.
// On LinkedIn it prefers the specific post (activity) most in view; elsewhere it
// prefers the page's canonical / og:url so you get the article, not just the site.
function resolveBestLink() {
  const canonical = document.querySelector('link[rel="canonical"]')?.href
  const ogUrl = document.querySelector('meta[property="og:url"]')?.content
  const ogTitle = document.querySelector('meta[property="og:title"]')?.content
  let url = location.href
  let title = ogTitle || document.title

  if (location.host.includes('linkedin.com')) {
    // Find the activity post whose element is most visible in the viewport.
    const nodes = document.querySelectorAll('[data-urn],[data-id],[data-activity-urn]')
    let best = null
    let bestArea = 0
    const vh = window.innerHeight
    const vw = window.innerWidth
    for (const el of nodes) {
      const raw = el.getAttribute('data-urn') || el.getAttribute('data-id') || el.getAttribute('data-activity-urn') || ''
      const m = raw.match(/urn:li:activity:\d+/)
      if (!m) continue
      const r = el.getBoundingClientRect()
      const visH = Math.max(0, Math.min(r.bottom, vh) - Math.max(r.top, 0))
      const visW = Math.max(0, Math.min(r.right, vw) - Math.max(r.left, 0))
      const area = visH * visW
      if (area > bestArea) { bestArea = area; best = m[0] }
    }
    if (best) url = `https://www.linkedin.com/feed/update/${best}/`
    else url = canonical || ogUrl || location.href
  } else {
    url = canonical || ogUrl || location.href
  }
  return { url, title }
}
