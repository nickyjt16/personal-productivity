import { useState } from 'react'
import { useCreateProject, useProjects } from '../api/hooks'
import type { ProjectRef } from '../api/types'

// A dropdown of open projects with checkboxes + inline "new project".
// `value` is the selected project ids; `onChange` receives the new full set.
// `current` provides names/colours for already-selected projects that may be
// closed (so they still render with a label).
export default function ProjectPicker({
  value,
  onChange,
  current = [],
  size = 'sm',
}: {
  value: string[]
  onChange: (ids: string[]) => void
  current?: ProjectRef[]
  size?: 'sm' | 'md'
}) {
  const { data: openProjects = [] } = useProjects('open')
  const create = useCreateProject()
  const [open, setOpen] = useState(false)
  const [newName, setNewName] = useState('')

  // Union of open projects and any already-selected (possibly closed) ones.
  const known = new Map<string, { id: string; name: string; color: string }>()
  for (const p of openProjects) known.set(p.id, { id: p.id, name: p.name, color: p.color })
  for (const p of current) if (!known.has(p.id)) known.set(p.id, p)
  const options = [...known.values()].sort((a, b) => a.name.localeCompare(b.name))

  function toggle(id: string) {
    onChange(value.includes(id) ? value.filter((x) => x !== id) : [...value, id])
  }

  function addNew() {
    const name = newName.trim()
    if (!name) return
    create.mutate({ name }, {
      onSuccess: (p) => { onChange([...value, p.id]); setNewName('') },
    })
  }

  return (
    <div className="position-relative d-inline-block">
      <button type="button"
        className={`btn btn-${size === 'sm' ? 'sm ' : ''}btn-outline-secondary`}
        onClick={() => setOpen((o) => !o)}>
        🏷 Projects{value.length ? ` (${value.length})` : ''}
      </button>

      {open && (
        <>
          {/* click-away backdrop */}
          <div className="position-fixed top-0 start-0 w-100 h-100" style={{ zIndex: 1040 }}
            onClick={() => setOpen(false)} />
          <div className="card shadow-sm position-absolute mt-1 p-2" style={{ zIndex: 1050, minWidth: 240 }}>
            {options.length === 0 && <div className="text-muted small px-1 mb-1">No projects yet.</div>}
            <div style={{ maxHeight: 220, overflowY: 'auto' }}>
              {options.map((p) => (
                <label key={p.id} className="d-flex align-items-center gap-2 px-1 py-1" style={{ cursor: 'pointer' }}>
                  <input type="checkbox" className="form-check-input mt-0"
                    checked={value.includes(p.id)} onChange={() => toggle(p.id)} />
                  <span className="badge rounded-pill" style={{ backgroundColor: p.color, color: '#fff' }}>
                    {p.name}
                  </span>
                </label>
              ))}
            </div>
            {/* Not a <form>: this picker is rendered inside other forms, and nested
                forms are invalid HTML. Enter and the button both call addNew. */}
            <div className="input-group input-group-sm mt-2">
              <input className="form-control" placeholder="New project…" value={newName}
                onChange={(e) => setNewName(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addNew() } }} />
              <button type="button" className="btn btn-outline-primary" disabled={create.isPending}
                onClick={addNew}>Add</button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
