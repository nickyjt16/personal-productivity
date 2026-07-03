import { useState } from 'react'
import {
  useCreateTodo,
  useDeleteTodo,
  useSetItemProjects,
  useTodos,
  useToggleTodo,
  useUpdateTodo,
} from '../api/hooks'
import type { Priority, RecurUnit, Todo } from '../api/types'
import ProjectBadges from '../components/ProjectBadges'
import ProjectFilter from '../components/ProjectFilter'
import ProjectPicker from '../components/ProjectPicker'
import { dueBadge } from '../util/due'
import { priorityVariant } from '../util/priority'

const priorities: Priority[] = ['Low', 'Medium', 'High']
const recurUnits: RecurUnit[] = ['None', 'Day', 'Week', 'Month']

function recurLabel(unit: RecurUnit, interval: number): string | null {
  if (unit === 'None' || interval < 1) return null
  const u = unit.toLowerCase()
  return interval === 1 ? `🔁 every ${u}` : `🔁 every ${interval} ${u}s`
}

// Convert an ISO timestamp to the yyyy-MM-dd value a <input type="date"> expects.
function toDateInput(iso: string | null): string {
  return iso ? iso.slice(0, 10) : ''
}

export default function Todos() {
  const [projectFilter, setProjectFilter] = useState('')
  const { data: todos = [], isLoading } = useTodos(undefined, projectFilter || undefined)
  const create = useCreateTodo()
  const toggle = useToggleTodo()
  const remove = useDeleteTodo()

  const [title, setTitle] = useState('')
  const [priority, setPriority] = useState<Priority>('Medium')
  const [dueDate, setDueDate] = useState('')
  const [repeat, setRepeat] = useState<RecurUnit>('None')
  const [editingId, setEditingId] = useState<string | null>(null)

  function add(e: React.FormEvent) {
    e.preventDefault()
    if (!title.trim()) return
    create.mutate(
      {
        title: title.trim(), priority, dueDate: dueDate || undefined,
        recurUnit: repeat, recurInterval: repeat === 'None' ? 0 : 1,
      },
      { onSuccess: () => { setTitle(''); setDueDate(''); setRepeat('None') } },
    )
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">✅ Todos</h2>
        <ProjectFilter value={projectFilter} onChange={setProjectFilter} />
      </div>

      <form className="card card-body mb-4" onSubmit={add}>
        <div className="row g-2 align-items-end">
          <div className="col">
            <label className="form-label">Task</label>
            <input className="form-control" value={title} placeholder="What needs doing?"
              onChange={(e) => setTitle(e.target.value)} />
          </div>
          <div className="col-auto">
            <label className="form-label">Priority</label>
            <select className="form-select" value={priority}
              onChange={(e) => setPriority(e.target.value as Priority)}>
              {priorities.map((p) => <option key={p} value={p}>{p}</option>)}
            </select>
          </div>
          <div className="col-auto">
            <label className="form-label">Due</label>
            <input type="date" className="form-control" value={dueDate}
              onChange={(e) => setDueDate(e.target.value)} />
          </div>
          <div className="col-auto">
            <label className="form-label">Repeat</label>
            <select className="form-select" value={repeat}
              onChange={(e) => setRepeat(e.target.value as RecurUnit)}>
              <option value="None">No repeat</option>
              <option value="Day">Daily</option>
              <option value="Week">Weekly</option>
              <option value="Month">Monthly</option>
            </select>
          </div>
          <div className="col-auto">
            <button className="btn btn-primary" disabled={create.isPending}>Add</button>
          </div>
        </div>
      </form>

      {isLoading ? <p>Loading…</p> : todos.length === 0 ? (
        <p className="text-muted">No tasks yet. Add one above.</p>
      ) : (
        <ul className="list-group">
          {todos.map((t) => (
            <li key={t.id} className="list-group-item">
              {editingId === t.id ? (
                <EditRow todo={t} onClose={() => setEditingId(null)} />
              ) : (
                <div className="d-flex align-items-center gap-2">
                  <input type="checkbox" className="form-check-input mt-0" checked={t.isDone}
                    onChange={() => toggle.mutate(t.id)} />
                  <div className="flex-grow-1">
                    <span className={t.isDone ? 'text-decoration-line-through text-muted' : ''}>
                      {t.title}
                    </span>
                    {t.notes && <div className="small text-muted">{t.notes}</div>}
                    {t.projects.length > 0 && <div className="mt-1"><ProjectBadges projects={t.projects} /></div>}
                  </div>
                  <span className={`badge text-bg-${priorityVariant[t.priority]}`}>{t.priority}</span>
                  {(() => {
                    const due = dueBadge(t.dueDate, t.isDone)
                    return due ? <span className={`badge text-bg-${due.variant}`}>{due.label}</span> : null
                  })()}
                  {recurLabel(t.recurUnit, t.recurInterval) && (
                    <span className="badge text-bg-light text-muted">{recurLabel(t.recurUnit, t.recurInterval)}</span>
                  )}
                  <button className="btn btn-sm btn-outline-secondary" title="Edit"
                    onClick={() => setEditingId(t.id)}>✎</button>
                  <button className="btn btn-sm btn-outline-danger" title="Delete"
                    onClick={() => remove.mutate(t.id)}>✕</button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function EditRow({ todo, onClose }: { todo: Todo; onClose: () => void }) {
  const update = useUpdateTodo()
  const setProjects = useSetItemProjects('todos')
  const [title, setTitle] = useState(todo.title)
  const [notes, setNotes] = useState(todo.notes ?? '')
  const [priority, setPriority] = useState<Priority>(todo.priority)
  const [dueDate, setDueDate] = useState(toDateInput(todo.dueDate))
  const [recurUnit, setRecurUnit] = useState<RecurUnit>(todo.recurUnit)
  const [recurInterval, setRecurInterval] = useState(todo.recurInterval || 1)
  const [projectIds, setProjectIds] = useState<string[]>(todo.projects.map((p) => p.id))

  function save(e: React.FormEvent) {
    e.preventDefault()
    if (!title.trim()) return
    update.mutate(
      {
        id: todo.id,
        title: title.trim(),
        notes: notes.trim() || null,
        priority,
        isDone: todo.isDone,
        dueDate: dueDate || null,
        recurUnit,
        recurInterval: recurUnit === 'None' ? 0 : Math.max(1, recurInterval),
      },
      { onSuccess: () => setProjects.mutate({ id: todo.id, projectIds }, { onSuccess: onClose }) },
    )
  }

  return (
    <form onSubmit={save}>
      <div className="row g-2 align-items-end">
        <div className="col-12 col-md">
          <label className="form-label small">Task</label>
          <input className="form-control" value={title} autoFocus placeholder="What needs doing?"
            onChange={(e) => setTitle(e.target.value)} />
        </div>
        <div className="col-auto">
          <label className="form-label small">Priority</label>
          <select className="form-select" value={priority}
            onChange={(e) => setPriority(e.target.value as Priority)}>
            {priorities.map((p) => <option key={p} value={p}>{p}</option>)}
          </select>
        </div>
        <div className="col-auto">
          <label className="form-label small">Due</label>
          <input type="date" className="form-control" value={dueDate}
            onChange={(e) => setDueDate(e.target.value)} />
        </div>
        <div className="col-auto">
          <label className="form-label small">Repeat every</label>
          <div className="d-flex gap-1">
            <input type="number" min={1} className="form-control" style={{ width: 70 }} value={recurInterval}
              disabled={recurUnit === 'None'} onChange={(e) => setRecurInterval(Number(e.target.value))} />
            <select className="form-select" value={recurUnit}
              onChange={(e) => setRecurUnit(e.target.value as RecurUnit)}>
              <option value="None">— no repeat —</option>
              {recurUnits.filter((u) => u !== 'None').map((u) => (
                <option key={u} value={u}>{u.toLowerCase()}(s)</option>
              ))}
            </select>
          </div>
        </div>
      </div>
      <div className="mt-2">
        <label className="form-label small">Notes</label>
        <textarea className="form-control" rows={2} value={notes} placeholder="Optional notes"
          onChange={(e) => setNotes(e.target.value)} />
      </div>
      <div className="d-flex align-items-center gap-2 mt-2">
        <ProjectPicker value={projectIds} onChange={setProjectIds} current={todo.projects} />
        <ProjectBadges projects={todo.projects.filter((p) => projectIds.includes(p.id))} />
      </div>
      <div className="d-flex gap-2 mt-2">
        <button className="btn btn-sm btn-primary" disabled={update.isPending || setProjects.isPending}>Save</button>
        <button type="button" className="btn btn-sm btn-outline-secondary" onClick={onClose}>
          Cancel
        </button>
      </div>
    </form>
  )
}
