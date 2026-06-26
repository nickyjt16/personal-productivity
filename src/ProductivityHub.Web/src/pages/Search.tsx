import { Link, useSearchParams } from 'react-router-dom'
import { useSearch, type SearchHit } from '../api/hooks'

export default function Search() {
  const [params] = useSearchParams()
  const query = params.get('q') ?? ''
  const { data, isLoading } = useSearch(query)

  const total = data ? data.todos.length + data.notes.length + data.bookmarks.length : 0

  return (
    <div>
      <h2 className="mb-1">🔎 Search</h2>
      {query
        ? <p className="text-muted">Results for “{query}”</p>
        : <p className="text-muted">Type in the search box to find todos, notes, and bookmarks.</p>}

      {query && isLoading && <p>Searching…</p>}
      {query && !isLoading && total === 0 && <p className="text-muted">No matches found.</p>}

      {data && total > 0 && (
        <>
          <ResultGroup title="✅ Todos" to="/todos" hits={data.todos} />
          <ResultGroup title="📝 Notes" to="/notes" hits={data.notes} />
          <ResultGroup title="🔖 Bookmarks" to="/bookmarks" hits={data.bookmarks} />
        </>
      )}
    </div>
  )
}

function ResultGroup({ title, to, hits }: { title: string; to: string; hits: SearchHit[] }) {
  if (hits.length === 0) return null
  return (
    <div className="mb-4">
      <h6 className="text-muted">{title} <span className="badge text-bg-light">{hits.length}</span></h6>
      <ul className="list-group">
        {hits.map((h) => (
          <li key={h.id} className="list-group-item">
            {h.url ? (
              <a href={h.url} target="_blank" rel="noreferrer" className="fw-semibold">{h.title}</a>
            ) : (
              <Link to={to} className="fw-semibold text-decoration-none">{h.title}</Link>
            )}
            {h.subtitle && <div className="small text-muted text-truncate">{h.subtitle}</div>}
          </li>
        ))}
      </ul>
    </div>
  )
}
