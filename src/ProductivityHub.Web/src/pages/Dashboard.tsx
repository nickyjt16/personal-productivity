import { Link } from 'react-router-dom'
import { useBookmarks, useInbox, useJournalEntry, useTodos } from '../api/hooks'
import PomodoroTimer from '../components/PomodoroTimer'
import { useSettings } from '../settings'

function today() {
  return new Date().toISOString().slice(0, 10)
}

export default function Dashboard() {
  const { isVisible } = useSettings()
  const { data: openTodos = [] } = useTodos(false)
  const { data: unprocessed = [] } = useInbox(false)
  const { data: unread = [] } = useBookmarks(false)
  const { data: journal } = useJournalEntry(today())

  const greeting = new Date().getHours() < 12 ? 'Good morning' :
    new Date().getHours() < 18 ? 'Good afternoon' : 'Good evening'

  return (
    <div>
      <h2 className="mb-4">{greeting} 👋</h2>

      <div className="row g-3 mb-4">
        {isVisible('todos') && <StatCard to="/todos" label="Open tasks" value={openTodos.length} icon="✅" />}
        {isVisible('inbox') && <StatCard to="/inbox" label="To triage" value={unprocessed.length} icon="📥" />}
        {isVisible('bookmarks') && <StatCard to="/bookmarks" label="Unread links" value={unread.length} icon="🔖" />}
        {isVisible('journal') && <StatCard to="/journal" label="Journal today" value={journal ? '✓' : '—'} icon="📔" />}
      </div>

      <div className="row g-3">
        {isVisible('todos') && (
          <div className="col-md-7">
            <div className="card card-body">
              <h5 className="card-title">Today's tasks</h5>
              {openTodos.length === 0 ? (
                <p className="text-muted mb-0">Nothing open. <Link to="/todos">Add a task →</Link></p>
              ) : (
                <ul className="list-group list-group-flush">
                  {openTodos.slice(0, 6).map((t) => (
                    <li key={t.id} className="list-group-item d-flex justify-content-between px-0">
                      <span>{t.title}</span>
                      <span className="badge text-bg-light text-muted">{t.priority}</span>
                    </li>
                  ))}
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
