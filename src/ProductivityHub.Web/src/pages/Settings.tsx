import { useState } from 'react'
import { useClearAllData } from '../api/hooks'
import { SECTIONS, useSettings } from '../settings'

export default function Settings() {
  const { isVisible, toggle, theme, setTheme } = useSettings()

  return (
    <div>
      <h2 className="mb-1">⚙️ Settings</h2>
      <p className="text-muted">Choose which sections appear in the sidebar and on the dashboard.</p>

      <div className="card card-body mb-4" style={{ maxWidth: 480 }}>
        <h6 className="text-muted mb-3">Appearance</h6>
        <div className="form-check form-switch d-flex align-items-center gap-2">
          <input className="form-check-input" type="checkbox" role="switch" id="toggle-dark"
            checked={theme === 'dark'} onChange={(e) => setTheme(e.target.checked ? 'dark' : 'light')} />
          <label className="form-check-label" htmlFor="toggle-dark">
            🌙 Dark mode
          </label>
        </div>
      </div>

      <div className="card card-body mb-4" style={{ maxWidth: 480 }}>
        <h6 className="text-muted mb-3">Sections</h6>
        {SECTIONS.map((s) => (
          <div key={s.key} className="form-check form-switch d-flex align-items-center gap-2 mb-3">
            <input
              className="form-check-input"
              type="checkbox"
              role="switch"
              id={`toggle-${s.key}`}
              checked={isVisible(s.key)}
              onChange={() => toggle(s.key)}
            />
            <label className="form-check-label" htmlFor={`toggle-${s.key}`}>
              <span className="me-2">{s.icon}</span>{s.label}
            </label>
          </div>
        ))}
        <p className="text-muted small mb-0">
          Hidden sections are just tucked away — turn them back on here any time. Your choices are
          saved on this device.
        </p>
      </div>

      <DangerZone />
    </div>
  )
}

function DangerZone() {
  const clear = useClearAllData()
  const [confirming, setConfirming] = useState(false)
  const [done, setDone] = useState(false)

  function doClear() {
    clear.mutate(undefined, {
      onSuccess: () => { setConfirming(false); setDone(true) },
    })
  }

  return (
    <div className="card border-danger" style={{ maxWidth: 480 }}>
      <div className="card-body">
        <h6 className="text-danger mb-2">Danger zone</h6>
        <p className="text-muted small">
          Permanently delete <strong>all</strong> data — every todo, inbox item, bookmark, note,
          journal entry, Pomodoro session, and project. This cannot be undone.
        </p>

        {done && (
          <div className="alert alert-success py-2" role="alert">All data cleared.</div>
        )}
        {clear.isError && (
          <div className="alert alert-danger py-2" role="alert">
            Couldn’t clear data — is the app running?
          </div>
        )}

        {!confirming ? (
          <button className="btn btn-outline-danger" onClick={() => { setConfirming(true); setDone(false) }}>
            Clear all data
          </button>
        ) : (
          <div className="d-flex align-items-center gap-2">
            <span className="fw-semibold">Delete everything?</span>
            <button className="btn btn-danger" onClick={doClear} disabled={clear.isPending}>
              {clear.isPending ? 'Clearing…' : 'Yes, delete it all'}
            </button>
            <button className="btn btn-outline-secondary" onClick={() => setConfirming(false)}
              disabled={clear.isPending}>
              Cancel
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
