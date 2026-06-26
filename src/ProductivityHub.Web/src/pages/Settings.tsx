import { SECTIONS, useSettings } from '../settings'

export default function Settings() {
  const { isVisible, toggle } = useSettings()

  return (
    <div>
      <h2 className="mb-1">⚙️ Settings</h2>
      <p className="text-muted">Choose which sections appear in the sidebar and on the dashboard.</p>

      <div className="card card-body" style={{ maxWidth: 480 }}>
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
    </div>
  )
}
