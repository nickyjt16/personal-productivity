import { useEffect, useState } from 'react'
import { useCreateNote, useDeleteNote, useNotes, useUpdateNote } from '../api/hooks'

export default function Notes() {
  const { data: notes = [], isLoading } = useNotes()
  const create = useCreateNote()
  const update = useUpdateNote()
  const remove = useDeleteNote()

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')

  const selected = notes.find((n) => n.id === selectedId) ?? null

  useEffect(() => {
    setTitle(selected?.title ?? '')
    setBody(selected?.body ?? '')
  }, [selectedId, selected?.title, selected?.body])

  function newNote() {
    setSelectedId(null)
    setTitle('')
    setBody('')
  }

  function save() {
    if (!body.trim() && !title.trim()) return
    if (selectedId) {
      update.mutate({ id: selectedId, title: title || undefined, body })
    } else {
      create.mutate({ title: title || undefined, body }, {
        onSuccess: (note) => setSelectedId(note.id),
      })
    }
  }

  return (
    <div>
      <h2 className="mb-4">📝 Notes</h2>
      <div className="row g-3">
        <div className="col-md-4">
          <button className="btn btn-outline-primary w-100 mb-2" onClick={newNote}>+ New note</button>
          {isLoading ? <p>Loading…</p> : (
            <div className="list-group">
              {notes.map((n) => (
                <button key={n.id}
                  className={`list-group-item list-group-item-action ${n.id === selectedId ? 'active' : ''}`}
                  onClick={() => setSelectedId(n.id)}>
                  <div className="fw-semibold text-truncate">{n.title || 'Untitled'}</div>
                  <small className={n.id === selectedId ? '' : 'text-muted'}>
                    {n.body.slice(0, 50) || 'No content'}
                  </small>
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="col-md-8">
          <div className="card card-body">
            <input className="form-control form-control-lg mb-2 border-0 px-0" placeholder="Title"
              value={title} onChange={(e) => setTitle(e.target.value)} />
            <textarea className="form-control border-0 px-0" rows={14} placeholder="Start writing…"
              value={body} onChange={(e) => setBody(e.target.value)} />
            <div className="d-flex gap-2 mt-2">
              <button className="btn btn-primary" onClick={save}
                disabled={create.isPending || update.isPending}>Save</button>
              {selectedId && (
                <button className="btn btn-outline-danger"
                  onClick={() => { remove.mutate(selectedId); newNote() }}>Delete</button>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
