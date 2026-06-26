import { useState } from 'react'
import { useBookmarks, useCreateBookmark, useDeleteBookmark, useToggleBookmark } from '../api/hooks'

export default function Bookmarks() {
  const { data: items = [], isLoading } = useBookmarks()
  const create = useCreateBookmark()
  const toggle = useToggleBookmark()
  const remove = useDeleteBookmark()

  const [url, setUrl] = useState('')
  const [title, setTitle] = useState('')

  function add(e: React.FormEvent) {
    e.preventDefault()
    if (!url.trim()) return
    create.mutate(
      { url: url.trim(), title: title.trim() || undefined },
      { onSuccess: () => { setUrl(''); setTitle('') } },
    )
  }

  return (
    <div>
      <h2 className="mb-1">🔖 Bookmarks / read later</h2>
      <p className="text-muted">Save links now, read them when you have time.</p>

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
          {items.map((b) => (
            <li key={b.id} className="list-group-item d-flex align-items-center gap-2">
              <input type="checkbox" className="form-check-input mt-0" checked={b.isRead}
                title="Mark read" onChange={() => toggle.mutate(b.id)} />
              <a href={b.url} target="_blank" rel="noreferrer"
                className={`flex-grow-1 text-truncate ${b.isRead ? 'text-muted' : ''}`}>
                {b.title || b.url}
              </a>
              {b.isRead && <span className="badge text-bg-light text-muted">read</span>}
              <button className="btn btn-sm btn-outline-danger" onClick={() => remove.mutate(b.id)}>✕</button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
