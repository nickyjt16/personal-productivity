import { useState } from 'react'
import { useEnvironments } from '../api/hooks'

const typeVariant: Record<string, string> = {
  Dev: 'secondary', Test: 'info', UAT: 'warning', Prod: 'danger',
  Default: 'primary', Sandbox: 'success', Other: 'light',
}

// A dropdown of environments with checkboxes. `value` is the selected env ids;
// `onChange` receives the new full set. Environments are created on the
// Environments page, so there's no inline "new" here.
export default function EnvironmentPicker({
  value,
  onChange,
}: {
  value: string[]
  onChange: (ids: string[]) => void
}) {
  const { data: envs = [] } = useEnvironments()
  const [open, setOpen] = useState(false)

  function toggle(id: string) {
    onChange(value.includes(id) ? value.filter((x) => x !== id) : [...value, id])
  }

  return (
    <div className="position-relative d-inline-block">
      <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => setOpen((o) => !o)}>
        🌐 Environments{value.length ? ` (${value.length})` : ''}
      </button>

      {open && (
        <>
          <div className="position-fixed top-0 start-0 w-100 h-100" style={{ zIndex: 1040 }}
            onClick={() => setOpen(false)} />
          <div className="card shadow-sm position-absolute mt-1 p-2" style={{ zIndex: 1050, minWidth: 240 }}>
            {envs.length === 0 && <div className="text-muted small px-1">No environments yet. Add them on the Environments page.</div>}
            <div style={{ maxHeight: 240, overflowY: 'auto' }}>
              {envs.map((e) => (
                <label key={e.id} className="d-flex align-items-center gap-2 px-1 py-1" style={{ cursor: 'pointer' }}>
                  <input type="checkbox" className="form-check-input mt-0"
                    checked={value.includes(e.id)} onChange={() => toggle(e.id)} />
                  <span>{e.name}</span>
                  <span className={`badge text-bg-${typeVariant[e.type] ?? 'light'} ms-auto`}>{e.type}</span>
                </label>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
