import { useState } from 'react'
import {
  useBookmarks,
  useCreateProject,
  useDeleteProject,
  useNotes,
  useProjects,
  useSetItemProjects,
  useTodos,
  useUpdateProject,
} from '../api/hooks'
import type { Bookmark, Note, Project, ProjectStatus, Todo } from '../api/types'
import { PROJECT_COLORS } from '../projectColors'

const STATUS_FILTERS: { key: string; label: string }[] = [
  { key: 'open', label: 'Open' },
  { key: 'complete', label: 'Complete' },
  { key: 'archived', label: 'Archived' },
  { key: 'all', label: 'All' },
]

const statusBadge: Record<ProjectStatus, string> = {
  New: 'secondary',
  Active: 'primary',
  Complete: 'success',
  Archived: 'dark',
}

export default function Projects() {
  const [filter, setFilter] = useState('open')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const { data: projects = [], isLoading } = useProjects(filter)
  const create = useCreateProject()

  const [name, setName] = useState('')
  const [color, setColor] = useState(PROJECT_COLORS[0])
  const [description, setDescription] = useState('')

  const selected = projects.find((p) => p.id === selectedId) ?? null

  function addProject(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim()) return
    create.mutate(
      { name: name.trim(), color, description: description.trim() || undefined },
      { onSuccess: () => { setName(''); setDescription(''); setColor(PROJECT_COLORS[0]) } },
    )
  }

  return (
    <div>
      <h2 className="mb-1">📁 Projects</h2>
      <p className="text-muted">Group todos, notes, and bookmarks under a project.</p>

      <form className="card card-body mb-4" onSubmit={addProject}>
        <div className="row g-2 align-items-end">
          <div className="col-md-4">
            <label className="form-label">New project</label>
            <input className="form-control" value={name} placeholder="Project name"
              onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="col">
            <label className="form-label">Description (optional)</label>
            <input className="form-control" value={description}
              onChange={(e) => setDescription(e.target.value)} />
          </div>
          <div className="col-auto">
            <button className="btn btn-primary" disabled={create.isPending}>Add</button>
          </div>
        </div>
        <div className="mt-2">
          <ColorSwatches value={color} onChange={setColor} />
        </div>
      </form>

      <ul className="nav nav-pills mb-3 gap-1">
        {STATUS_FILTERS.map((f) => (
          <li className="nav-item" key={f.key}>
            <button className={`nav-link ${filter === f.key ? 'active' : ''}`}
              onClick={() => { setFilter(f.key); setSelectedId(null) }}>{f.label}</button>
          </li>
        ))}
      </ul>

      {isLoading ? <p>Loading…</p> : projects.length === 0 ? (
        <p className="text-muted">No projects here. Create one above.</p>
      ) : (
        <div className="row g-3">
          {projects.map((p) => (
            <div className="col-md-6 col-lg-4" key={p.id}>
              <ProjectCard project={p} active={p.id === selectedId}
                onSelect={() => setSelectedId(p.id === selectedId ? null : p.id)} />
            </div>
          ))}
        </div>
      )}

      {selected && <ProjectDetail key={selected.id} project={selected} />}
    </div>
  )
}

function ColorSwatches({ value, onChange }: { value: string; onChange: (c: string) => void }) {
  return (
    <div className="d-flex flex-wrap gap-2">
      {PROJECT_COLORS.map((c) => (
        <button key={c} type="button" title={c}
          onClick={() => onChange(c)}
          className="rounded-circle border-0"
          style={{
            width: 24, height: 24, backgroundColor: c,
            outline: value === c ? '3px solid rgba(0,0,0,.35)' : 'none',
          }} />
      ))}
    </div>
  )
}

function ProjectCard({ project, active, onSelect }: {
  project: Project; active: boolean; onSelect: () => void
}) {
  const pct = project.todosTotal ? Math.round((project.todosDone / project.todosTotal) * 100) : 0
  return (
    <div className={`card h-100 ${active ? 'border-primary' : ''}`} role="button" onClick={onSelect}>
      <div style={{ height: 6, backgroundColor: project.color }} className="rounded-top" />
      <div className="card-body">
        <div className="d-flex justify-content-between align-items-start">
          <h5 className="card-title mb-1">{project.name}</h5>
          <span className={`badge text-bg-${statusBadge[project.status]}`}>{project.status}</span>
        </div>
        {project.description && <p className="text-muted small mb-2">{project.description}</p>}
        <div className="text-muted small mb-2">
          {project.todosTotal} todo{project.todosTotal === 1 ? '' : 's'} ·
          {' '}{project.noteCount} note{project.noteCount === 1 ? '' : 's'} ·
          {' '}{project.bookmarkCount} link{project.bookmarkCount === 1 ? '' : 's'}
        </div>
        {project.todosTotal > 0 && (
          <div className="progress" style={{ height: 6 }} title={`${project.todosDone}/${project.todosTotal} done`}>
            <div className="progress-bar bg-success" style={{ width: `${pct}%` }} />
          </div>
        )}
      </div>
    </div>
  )
}

function ProjectDetail({ project }: { project: Project }) {
  const update = useUpdateProject()
  const remove = useDeleteProject()
  const [editing, setEditing] = useState(false)

  function setStatus(status: ProjectStatus) {
    update.mutate({ id: project.id, name: project.name, status })
  }

  return (
    <div className="card card-body mt-4 border-primary">
      <div className="d-flex justify-content-between align-items-start">
        <h4 className="mb-0">
          <span className="badge rounded-pill me-2" style={{ backgroundColor: project.color }}>&nbsp;</span>
          {project.name}
        </h4>
        <div className="d-flex gap-2 flex-wrap">
          {(project.status === 'New') && (
            <button className="btn btn-sm btn-primary" onClick={() => setStatus('Active')}>Start</button>
          )}
          {(project.status === 'New' || project.status === 'Active') && (
            <button className="btn btn-sm btn-success" onClick={() => setStatus('Complete')}>Mark complete</button>
          )}
          {project.status !== 'Archived' && (
            <button className="btn btn-sm btn-outline-secondary" onClick={() => setStatus('Archived')}>Archive</button>
          )}
          {(project.status === 'Complete' || project.status === 'Archived') && (
            <button className="btn btn-sm btn-outline-primary" onClick={() => setStatus('Active')}>Reactivate</button>
          )}
          <button className="btn btn-sm btn-outline-secondary" onClick={() => setEditing((e) => !e)}>Edit</button>
          <button className="btn btn-sm btn-outline-danger"
            onClick={() => { if (confirm(`Delete project "${project.name}"? Items stay, links are removed.`)) remove.mutate(project.id) }}>
            Delete
          </button>
        </div>
      </div>

      {editing && <EditProjectForm project={project} onDone={() => setEditing(false)} />}
      {project.description && !editing && <p className="text-muted mt-2 mb-0">{project.description}</p>}

      <hr />
      <LinkedItems project={project} />
    </div>
  )
}

function EditProjectForm({ project, onDone }: { project: Project; onDone: () => void }) {
  const update = useUpdateProject()
  const [name, setName] = useState(project.name)
  const [description, setDescription] = useState(project.description ?? '')
  const [color, setColor] = useState(project.color)

  function save(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim()) return
    update.mutate(
      { id: project.id, name: name.trim(), description: description.trim() || null, color },
      { onSuccess: onDone },
    )
  }

  return (
    <form className="mt-3" onSubmit={save}>
      <div className="row g-2">
        <div className="col-md-4">
          <input className="form-control" value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <div className="col">
          <input className="form-control" value={description} placeholder="Description"
            onChange={(e) => setDescription(e.target.value)} />
        </div>
      </div>
      <div className="mt-2"><ColorSwatches value={color} onChange={setColor} /></div>
      <div className="d-flex gap-2 mt-2">
        <button className="btn btn-sm btn-primary" disabled={update.isPending}>Save</button>
        <button type="button" className="btn btn-sm btn-outline-secondary" onClick={onDone}>Cancel</button>
      </div>
    </form>
  )
}

function LinkedItems({ project }: { project: Project }) {
  const pid = project.id
  const { data: todos = [] } = useTodos(undefined, pid)
  const { data: notes = [] } = useNotes(pid)
  const { data: bookmarks = [] } = useBookmarks(undefined, pid)
  const { data: allTodos = [] } = useTodos()
  const { data: allNotes = [] } = useNotes()
  const { data: allBookmarks = [] } = useBookmarks()

  const setTodo = useSetItemProjects('todos')
  const setNote = useSetItemProjects('notes')
  const setBookmark = useSetItemProjects('bookmarks')

  const projectIdsOf = (item: { projects: { id: string }[] }) => item.projects.map((p) => p.id)
  const linkedIds = new Set([...todos, ...notes, ...bookmarks].map((i) => i.id))

  return (
    <div className="row g-4">
      <div className="col-md-4">
        <ItemColumn<Todo>
          title="✅ Todos"
          linked={todos}
          unlinked={allTodos.filter((t) => !linkedIds.has(t.id))}
          label={(t) => t.title}
          onAdd={(t) => setTodo.mutate({ id: t.id, projectIds: [...projectIdsOf(t), pid] })}
          onRemove={(t) => setTodo.mutate({ id: t.id, projectIds: projectIdsOf(t).filter((x) => x !== pid) })}
        />
      </div>
      <div className="col-md-4">
        <ItemColumn<Note>
          title="📝 Notes"
          linked={notes}
          unlinked={allNotes.filter((n) => !linkedIds.has(n.id))}
          label={(n) => n.title || 'Untitled'}
          onAdd={(n) => setNote.mutate({ id: n.id, projectIds: [...projectIdsOf(n), pid] })}
          onRemove={(n) => setNote.mutate({ id: n.id, projectIds: projectIdsOf(n).filter((x) => x !== pid) })}
        />
      </div>
      <div className="col-md-4">
        <ItemColumn<Bookmark>
          title="🔖 Bookmarks"
          linked={bookmarks}
          unlinked={allBookmarks.filter((b) => !linkedIds.has(b.id))}
          label={(b) => b.title || b.url}
          onAdd={(b) => setBookmark.mutate({ id: b.id, projectIds: [...projectIdsOf(b), pid] })}
          onRemove={(b) => setBookmark.mutate({ id: b.id, projectIds: projectIdsOf(b).filter((x) => x !== pid) })}
        />
      </div>
    </div>
  )
}

function ItemColumn<T extends { id: string }>({ title, linked, unlinked, label, onAdd, onRemove }: {
  title: string
  linked: T[]
  unlinked: T[]
  label: (item: T) => string
  onAdd: (item: T) => void
  onRemove: (item: T) => void
}) {
  return (
    <div>
      <h6 className="text-muted">{title} <span className="badge text-bg-light">{linked.length}</span></h6>
      <ul className="list-group list-group-flush mb-2">
        {linked.map((item) => (
          <li key={item.id} className="list-group-item d-flex justify-content-between align-items-center px-0 py-1">
            <span className="text-truncate">{label(item)}</span>
            <button className="btn btn-sm btn-link text-danger p-0 ms-2" title="Remove from project"
              onClick={() => onRemove(item)}>✕</button>
          </li>
        ))}
        {linked.length === 0 && <li className="list-group-item px-0 py-1 text-muted small">None yet.</li>}
      </ul>
      <select className="form-select form-select-sm" value=""
        onChange={(e) => {
          const item = unlinked.find((i) => i.id === e.target.value)
          if (item) onAdd(item)
        }}>
        <option value="">+ Add existing…</option>
        {unlinked.map((item) => <option key={item.id} value={item.id}>{label(item)}</option>)}
      </select>
    </div>
  )
}
