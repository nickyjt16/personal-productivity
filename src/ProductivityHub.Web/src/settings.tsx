import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'

export interface SectionDef {
  key: string
  label: string
  icon: string
  // Nav route path; empty for sections that only appear on the dashboard.
  path: string
}

// Single source of truth for the app's sections — used by the nav, the
// dashboard, and the settings page.
export const SECTIONS: SectionDef[] = [
  { key: 'todos', label: 'Todos', icon: '✅', path: '/todos' },
  { key: 'inbox', label: 'Inbox', icon: '📥', path: '/inbox' },
  { key: 'bookmarks', label: 'Bookmarks', icon: '🔖', path: '/bookmarks' },
  { key: 'notes', label: 'Notes', icon: '📝', path: '/notes' },
  { key: 'journal', label: 'Journal', icon: '📔', path: '/journal' },
  { key: 'projects', label: 'Projects', icon: '📁', path: '/projects' },
  { key: 'pomodoro', label: 'Pomodoro timer', icon: '🍅', path: '' },
]

const STORAGE_KEY = 'ph.sectionVisibility'

type Visibility = Record<string, boolean>

function load(): Visibility {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as Visibility) : {}
  } catch {
    return {}
  }
}

interface SettingsContextValue {
  isVisible: (key: string) => boolean
  toggle: (key: string) => void
}

const SettingsContext = createContext<SettingsContextValue | null>(null)

export function SettingsProvider({ children }: { children: React.ReactNode }) {
  const [visibility, setVisibility] = useState<Visibility>(load)

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(visibility))
  }, [visibility])

  // Default to visible unless explicitly turned off.
  const isVisible = useCallback((key: string) => visibility[key] !== false, [visibility])

  const toggle = useCallback(
    (key: string) => setVisibility((v) => ({ ...v, [key]: v[key] === false })),
    [],
  )

  const value = useMemo(() => ({ isVisible, toggle }), [isVisible, toggle])
  return <SettingsContext.Provider value={value}>{children}</SettingsContext.Provider>
}

export function useSettings() {
  const ctx = useContext(SettingsContext)
  if (!ctx) throw new Error('useSettings must be used within a SettingsProvider')
  return ctx
}
