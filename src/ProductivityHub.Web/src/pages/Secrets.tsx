import { useState } from 'react'
import { useCreateSecret, useDeleteSecret, useSecrets, useUpdateSecret } from '../api/hooks'
import type { Secret } from '../api/types'

function expiryBadge(daysLeft: number): { variant: string; label: string } {
  if (daysLeft < 0) return { variant: 'danger', label: `Expired ${-daysLeft}d ago` }
  if (daysLeft === 0) return { variant: 'danger', label: 'Expires today' }
  if (daysLeft <= 7) return { variant: 'warning', label: `Expires in ${daysLeft}d` }
  return { variant: 'light', label: `${daysLeft}d left` }
}

export default function Secrets() {
  const { data: secrets = [], isLoading } = useSecrets()
  const create = useCreateSecret()
  const update = useUpdateSecret()
  const remove = useDeleteSecret()

  const [editingId, setEditingId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [clientId, setClientId] = useState('')
  const [value, setValue] = useState('')
  const [expiresOn, setExpiresOn] = useState('')
  const [notes, setNotes] = useState('')
  const [notify, setNotify] = useState('')
  const [reveal, setReveal] = useState<Record<string, boolean>>({})

  function reset() {
    setEditingId(null); setName(''); setClientId(''); setValue(''); setExpiresOn(''); setNotes(''); setNotify('')
  }

  function save(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim() || !expiresOn) return
    const body = {
      name: name.trim(), clientId: clientId.trim() || undefined, value: value || undefined,
      expiresOn, notes: notes.trim() || undefined,
      notify: notify.split('\n').map((x) => x.trim()).filter(Boolean),
    }
    if (editingId) update.mutate({ id: editingId, ...body }, { onSuccess: reset })
    else create.mutate(body, { onSuccess: reset })
  }

  function edit(s: Secret) {
    setEditingId(s.id); setName(s.name); setClientId(s.clientId ?? ''); setValue(s.value ?? '')
    setExpiresOn(s.expiresOn.slice(0, 10)); setNotes(s.notes ?? ''); setNotify(s.notify.join('\n'))
  }

  return (
    <div>
      <h2 className="mb-1">🔑 Secrets</h2>
      <p className="text-muted">
        Track client secrets & keys and their expiry. You’ll get a heads-up a week before one expires.
        Stored locally on this device only.
      </p>

      <form className="card card-body mb-4" onSubmit={save}>
        <div className="row g-2">
          <div className="col-md-4">
            <label className="form-label">Name</label>
            <input className="form-control" value={name} placeholder="e.g. My app registration"
              onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="col-md-4">
            <label className="form-label">Client ID (optional)</label>
            <input className="form-control" value={clientId} onChange={(e) => setClientId(e.target.value)} />
          </div>
          <div className="col-md-4">
            <label className="form-label">Expires on</label>
            <input type="date" className="form-control" value={expiresOn} onChange={(e) => setExpiresOn(e.target.value)} />
          </div>
          <div className="col-md-8">
            <label className="form-label">Secret value (optional)</label>
            <input className="form-control" value={value} type="text" onChange={(e) => setValue(e.target.value)} />
          </div>
          <div className="col-md-4 d-flex align-items-end gap-2">
            <button className="btn btn-primary" disabled={create.isPending || update.isPending}>
              {editingId ? 'Save' : 'Add'}
            </button>
            {editingId && <button type="button" className="btn btn-outline-secondary" onClick={reset}>Cancel</button>}
          </div>
          <div className="col-12">
            <input className="form-control" value={notes} placeholder="Notes (optional)"
              onChange={(e) => setNotes(e.target.value)} />
          </div>
          <div className="col-12">
            <label className="form-label">Who to inform when this changes (one per line)</label>
            <textarea className="form-control" rows={2} value={notify}
              placeholder="e.g. jane@example.com&#10;Platform Team&#10;#secrets-channel"
              onChange={(e) => setNotify(e.target.value)} />
          </div>
        </div>
      </form>

      {isLoading ? <p>Loading…</p> : secrets.length === 0 ? (
        <p className="text-muted">No secrets tracked yet.</p>
      ) : (
        <ul className="list-group">
          {secrets.map((s) => {
            const badge = expiryBadge(s.daysLeft)
            return (
              <li key={s.id} className="list-group-item">
                <div className="d-flex align-items-center gap-2">
                  <div className="flex-grow-1">
                    <span className="fw-semibold">{s.name}</span>
                    {s.clientId && <span className="text-muted small ms-2">{s.clientId}</span>}
                    {s.value && (
                      <div className="small text-muted font-monospace">
                        {reveal[s.id] ? s.value : '••••••••'}
                        <button className="btn btn-sm btn-link p-0 ms-2"
                          onClick={() => setReveal((r) => ({ ...r, [s.id]: !r[s.id] }))}>
                          {reveal[s.id] ? 'hide' : 'show'}
                        </button>
                      </div>
                    )}
                    {s.notes && <div className="small text-muted">{s.notes}</div>}
                    {s.notify.length > 0 && (
                      <div className="mt-1 d-flex flex-wrap gap-1 align-items-center">
                        <span className="small text-muted">Notify:</span>
                        {s.notify.map((n, i) => (
                          <span key={i} className="badge rounded-pill text-bg-secondary">{n}</span>
                        ))}
                      </div>
                    )}
                  </div>
                  <span className="badge text-bg-light text-muted">{new Date(s.expiresOn).toLocaleDateString()}</span>
                  <span className={`badge text-bg-${badge.variant}`}>{badge.label}</span>
                  <button className="btn btn-sm btn-outline-secondary" onClick={() => edit(s)}>✎</button>
                  <button className="btn btn-sm btn-outline-danger" onClick={() => remove.mutate(s.id)}>✕</button>
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
