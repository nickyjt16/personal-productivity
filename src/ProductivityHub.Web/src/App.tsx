import { useState } from 'react'
import { NavLink, Route, Routes, useNavigate } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import Todos from './pages/Todos'
import Inbox from './pages/Inbox'
import Bookmarks from './pages/Bookmarks'
import Notes from './pages/Notes'
import Journal from './pages/Journal'
import Projects from './pages/Projects'
import Search from './pages/Search'
import Settings from './pages/Settings'
import { SECTIONS, useSettings } from './settings'

export default function App() {
  const { isVisible } = useSettings()

  // Dashboard and Settings are always present; the rest follow visibility.
  const sectionLinks = SECTIONS.filter((s) => s.path && isVisible(s.key))

  return (
    <div className="d-flex min-vh-100">
      <aside className="bg-dark text-white p-3 d-flex flex-column" style={{ width: 220 }}>
        <h1 className="h5 mb-3">⚡ Productivity Hub</h1>
        <SearchBox />
        <nav className="nav nav-pills flex-column gap-1">
          <NavItem to="/" icon="🏠" label="Dashboard" end />
          {sectionLinks.map((s) => (
            <NavItem key={s.key} to={s.path} icon={s.icon} label={s.label} />
          ))}
          <hr className="text-secondary my-2" />
          <NavItem to="/settings" icon="⚙️" label="Settings" />
        </nav>
      </aside>

      <main className="flex-grow-1 bg-body-tertiary p-4" style={{ overflowY: 'auto' }}>
        <div className="container-fluid" style={{ maxWidth: 960 }}>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/todos" element={<Todos />} />
            <Route path="/inbox" element={<Inbox />} />
            <Route path="/bookmarks" element={<Bookmarks />} />
            <Route path="/notes" element={<Notes />} />
            <Route path="/journal" element={<Journal />} />
            <Route path="/projects" element={<Projects />} />
            <Route path="/search" element={<Search />} />
            <Route path="/settings" element={<Settings />} />
          </Routes>
        </div>
      </main>
    </div>
  )
}

function SearchBox() {
  const [q, setQ] = useState('')
  const navigate = useNavigate()
  return (
    <form className="mb-3" onSubmit={(e) => { e.preventDefault(); if (q.trim()) navigate(`/search?q=${encodeURIComponent(q.trim())}`) }}>
      <input className="form-control form-control-sm" type="search" placeholder="🔎 Search…"
        value={q} onChange={(e) => setQ(e.target.value)} />
    </form>
  )
}

function NavItem({ to, icon, label, end }: { to: string; icon: string; label: string; end?: boolean }) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) => `nav-link text-start ${isActive ? 'active' : 'text-white-50'}`}
    >
      <span className="me-2">{icon}</span>
      {label}
    </NavLink>
  )
}
