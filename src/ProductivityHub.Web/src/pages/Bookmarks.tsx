import { useState } from 'react'
import {
  useBookmarks,
  useCreateBookmark,
  useDeleteBookmark,
  useImportLinks,
  useSetItemProjects,
  useToggleBookmark,
  type ImportResult,
} from '../api/hooks'
import type { Bookmark } from '../api/types'
import ProjectBadges from '../components/ProjectBadges'
import ProjectFilter from '../components/ProjectFilter'
import ProjectPicker from '../components/ProjectPicker'

export default function Bookmarks() {
  const [projectFilter, setProjectFilter] = useState('')
  const { data: items = [], isLoading } = useBookmarks(undefined, projectFilter || undefined)
  const create = useCreateBookmark()
  const importLinks = useImportLinks()

  const [url, setUrl] = useState('')
  const [title, setTitle] = useState('')
  const [importMsg, setImportMsg] = useState<string | null>(null)

  function add(e: React.FormEvent) {
    e.preventDefault()
    if (!url.trim()) return
    create.mutate(
      { url: url.trim(), title: title.trim() || undefined },
      { onSuccess: () => { setUrl(''); setTitle('') } },
    )
  }

  function checkForLinks() {
    importLinks.mutate(undefined, {
      onSuccess: (r: ImportResult) => setImportMsg(describeImport(r)),
      onError: (e) => setImportMsg(`Import failed: ${e instanceof Error ? e.message : 'unknown error'}`),
    })
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-start">
        <div>
          <h2 className="mb-1">🔖 Bookmarks / read later</h2>
          <p className="text-muted">Save links now, read them when you have time.</p>
        </div>
        <div className="d-flex align-items-center gap-2">
          <ProjectFilter value={projectFilter} onChange={setProjectFilter} />
          <button className="btn btn-outline-primary" onClick={checkForLinks} disabled={importLinks.isPending}>
            {importLinks.isPending ? 'Checking…' : '↻ Check for new links'}
          </button>
        </div>
      </div>

      {importMsg && (
        <div className="alert alert-info py-2 d-flex justify-content-between align-items-center" role="alert">
          <span>{importMsg}</span>
          <button type="button" className="btn-close" aria-label="Dismiss"
            onClick={() => setImportMsg(null)} />
        </div>
      )}

      <form className="card card-body mb-4" onSubmit={add}>
        <div className="row g-2 align-items-end">
          <div className="col-md-5">
            <label className="form-label">URL</label>
            <input className="form-control" value={url} placeholder="https://…"
              onChange={(e) => setUrl(e.target.value)} />
          </div>
          <div className="col">
            <label className="form-label">Title (optional)</label>
            <input className="form-control" value={title}
              onChange={(e) => setTitle(e.target.value)} />
          </div>
          <div className="col-auto">
            <button className="btn btn-primary" disabled={create.isPending}>Save</button>
          </div>
        </div>
      </form>

      {isLoading ? <p>Loading…</p> : items.length === 0 ? (
        <p className="text-muted">No bookmarks saved yet.</p>
      ) : (
        <ul className="list-group">
          {items.map((b) => <BookmarkRow key={b.id} bookmark={b} />)}
        </ul>
      )}
    </div>
  )
}

function BookmarkRow({ bookmark: b }: { bookmark: Bookmark }) {
  const toggle = useToggleBookmark()
  const remove = useDeleteBookmark()
  const setProjects = useSetItemProjects('bookmarks')

  return (
    <li className="list-group-item">
      <div className="d-flex align-items-center gap-2">
        <input type="checkbox" className="form-check-input mt-0" checked={b.isRead}
          title="Mark read" onChange={() => toggle.mutate(b.id)} />
        <a href={b.url} target="_blank" rel="noreferrer"
          className={`flex-grow-1 text-truncate ${b.isRead ? 'text-muted' : ''}`}>
          {b.title || b.url}
        </a>
        {b.isRead && <span className="badge text-bg-light text-muted">read</span>}
        <ProjectPicker
          value={b.projects.map((p) => p.id)}
          current={b.projects}
          onChange={(ids) => setProjects.mutate({ id: b.id, projectIds: ids })}
        />
        <button className="btn btn-sm btn-outline-danger" onClick={() => remove.mutate(b.id)}>✕</button>
      </div>
      {b.projects.length > 0 && <div className="mt-1"><ProjectBadges projects={b.projects} /></div>}
    </li>
  )
}

function describeImport(r: ImportResult): string {
  if (!r.enabled) return 'Teams link import is turned off. Set LinkImport in appsettings.json to enable it.'
  if (!r.folderExists) return `Import folder not found: ${r.folderPath}. Check the path and that OneDrive has synced it.`
  if (r.imported === 0 && r.duplicates === 0 && r.skippedNoUrl === 0) return 'No new links found.'
  const parts = [`Imported ${r.imported}`]
  if (r.duplicates) parts.push(`${r.duplicates} already saved`)
  if (r.skippedNoUrl) parts.push(`${r.skippedNoUrl} had no link`)
  return parts.join(', ') + '.'
}
