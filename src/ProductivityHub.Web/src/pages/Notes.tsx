import { useEffect, useState } from 'react'
import { useCreateNote, useDeleteNote, useNotes, useSetItemProjects, useUpdateNote } from '../api/hooks'
import ProjectBadges from '../components/ProjectBadges'
import ProjectFilter from '../components/ProjectFilter'
import ProjectPicker from '../components/ProjectPicker'

export default function Notes() {
  const [projectFilter, setProjectFilter] = useState('')
  const { data: notes = [], isLoading } = useNotes(projectFilter || undefined)
  const create = useCreateNote()
  const update = useUpdateNote()
  const remove = useDeleteNote()
  const setProjects = useSetItemProjects('notes')

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [projectIds, setProjectIds] = useState<string[]>([])

  const selected = notes.find((n) => n.id === selectedId) ?? null

  useEffect(() => {
    setTitle(selected?.title ?? '')
    setBody(selected?.body ?? '')
    setProjectIds(selected?.projects.map((p) => p.id) ?? [])
  }, [selectedId, selected?.title, selected?.body, selected])

  function newNote() {
    setSelectedId(null)
    setTitle('')
    setBody('')
    setProjectIds([])
  }

  function save() {
    if (!body.trim() && !title.trim()) return
    if (selectedId) {
      update.mutate({ id: selectedId, title: title || undefined, body }, {
        onSuccess: () => setProjects.mutate({ id: selectedId, projectIds }),
      })
    } else {
      create.mutate({ title: title || undefined, body }, {
        onSuccess: (note) => {
          setSelectedId(note.id)
          setProjects.mutate({ id: note.id, projectIds })
        },
      })
    }
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">📝 Notes</h2>
        <ProjectFilter value={projectFilter} onChange={setProjectFilter} />
      </div>
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
                  {n.projects.length > 0 && <div className="mt-1"><ProjectBadges projects={n.projects} /></div>}
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="col-md-8">
          <div className="card card-body">
            <input className="form-control form-control-lg mb-2 border-0 px-0" placeholder="Title"
              value={title} onChange={(e) => setTitle(e.target.value)} />
            <textarea className="form-control border-0 px-0" rows={12} placeholder="Start writing…"
              value={body} onChange={(e) => setBody(e.target.value)} />
            <div className="mt-2">
              <ProjectPicker value={projectIds} onChange={setProjectIds} current={selected?.projects ?? []} />
            </div>
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
