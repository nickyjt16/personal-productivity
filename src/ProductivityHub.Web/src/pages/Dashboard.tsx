import { Link } from 'react-router-dom'
import { useBookmarks, useExpiringSecrets, useInbox, useJournalEntry, useTodos, useToggleTodo } from '../api/hooks'
import PomodoroTimer from '../components/PomodoroTimer'
import { useSettings } from '../settings'
import { dueBadge, dueStatus } from '../util/due'
import { priorityVariant } from '../util/priority'

function today() {
  return new Date().toISOString().slice(0, 10)
}

export default function Dashboard() {
  const { isVisible } = useSettings()
  const { data: openTodos = [] } = useTodos(false)
  const { data: unprocessed = [] } = useInbox(false)
  const { data: unread = [] } = useBookmarks(false)
  const { data: expiringSecrets = [] } = useExpiringSecrets()
  const { data: journal } = useJournalEntry(today())

  const greeting = new Date().getHours() < 12 ? 'Good morning' :
    new Date().getHours() < 18 ? 'Good afternoon' : 'Good evening'

  const toggle = useToggleTodo()
  const overdueCount = openTodos.filter((t) => dueStatus(t.dueDate, t.isDone) === 'overdue').length

  return (
    <div>
      <h2 className="mb-4">{greeting} 👋</h2>

      <div className="row g-3 mb-4">
        {isVisible('todos') && <StatCard to="/todos" label="Open tasks" value={openTodos.length} icon="✅" />}
        {isVisible('inbox') && <StatCard to="/inbox" label="To triage" value={unprocessed.length} icon="📥" />}
        {isVisible('bookmarks') && <StatCard to="/bookmarks" label="Unread links" value={unread.length} icon="🔖" />}
        {isVisible('secrets') && <StatCard to="/secrets" label="Secrets expiring (7d)" value={expiringSecrets.length} icon="🔑" />}
        {isVisible('journal') && <StatCard to="/journal" label="Journal today" value={journal ? '✓' : '—'} icon="📔" />}
      </div>

      <div className="row g-3">
        {isVisible('todos') && (
          <div className="col-md-7">
            <div className="card card-body">
              <h5 className="card-title d-flex justify-content-between align-items-center">
                <span>Today's tasks</span>
                {overdueCount > 0 && (
                  <span className="badge text-bg-danger">{overdueCount} overdue</span>
                )}
              </h5>
              {openTodos.length === 0 ? (
                <p className="text-muted mb-0">Nothing open. <Link to="/todos">Add a task →</Link></p>
              ) : (
                <ul className="list-group list-group-flush">
                  {openTodos.slice(0, 6).map((t) => {
                    const due = dueBadge(t.dueDate, t.isDone)
                    return (
                      <li key={t.id} className="list-group-item d-flex align-items-center gap-2 px-0">
                        <input type="checkbox" className="form-check-input mt-0" checked={t.isDone}
                          title="Mark done" onChange={() => toggle.mutate(t.id)} />
                        <span className="flex-grow-1 text-truncate">{t.title}</span>
                        {due && <span className={`badge text-bg-${due.variant}`}>{due.label}</span>}
                        <span className={`badge text-bg-${priorityVariant[t.priority]}`}>{t.priority}</span>
                      </li>
                    )
                  })}
                </ul>
              )}
            </div>
          </div>
        )}
        {isVisible('pomodoro') && (
          <div className="col-md-5">
            <PomodoroTimer />
          </div>
        )}
      </div>
    </div>
  )
}

function StatCard({ to, label, value, icon }: {
  to: string; label: string; value: number | string; icon: string
}) {
  return (
    <div className="col-6 col-lg-3">
      <Link to={to} className="text-decoration-none">
        <div className="card card-body text-center h-100">
          <div className="fs-3">{icon}</div>
          <div className="fs-2 fw-bold">{value}</div>
          <div className="text-muted small">{label}</div>
        </div>
      </Link>
    </div>
  )
}
