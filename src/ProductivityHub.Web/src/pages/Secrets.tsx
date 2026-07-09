import { useState } from 'react'
import {
  useCreateSecret,
  useDeleteSecret,
  useLockVault,
  useSecrets,
  useSetItemProjects,
  useSetSecretEnvironments,
  useSetVault,
  useUnlockVault,
  useUpdateSecret,
  useVaultStatus,
} from '../api/hooks'
import type { Secret } from '../api/types'
import ProjectBadges from '../components/ProjectBadges'
import ProjectPicker from '../components/ProjectPicker'
import EnvironmentPicker from '../components/EnvironmentPicker'

const envTypeVariant: Record<string, string> = {
  Dev: 'secondary', Test: 'info', UAT: 'warning', Prod: 'danger',
  Default: 'primary', Sandbox: 'success', Other: 'light',
}

// Master-password bar: set a password on first use, unlock/lock thereafter.
function VaultBar() {
  const { data: status } = useVaultStatus()
  const setVault = useSetVault()
  const unlock = useUnlockVault()
  const lock = useLockVault()

  const [pwd, setPwd] = useState('')
  const [confirm, setConfirm] = useState('')
  const [hint, setHint] = useState('')
  const [err, setErr] = useState('')

  if (!status) return null

  // Already set up and unlocked.
  if (status.configured && status.unlocked) {
    return (
      <div className="alert alert-success d-flex align-items-center justify-content-between py-2 mb-3">
        <span>🔓 Secret values are unlocked and visible.</span>
        <button className="btn btn-sm btn-outline-success" onClick={() => lock.mutate(undefined)}>Lock</button>
      </div>
    )
  }

  // Set up but locked — offer to unlock.
  if (status.configured && !status.unlocked) {
    return (
      <form
        className="alert alert-warning mb-3"
        onSubmit={(e) => {
          e.preventDefault()
          setErr('')
          unlock.mutate({ password: pwd }, {
            onError: () => setErr('Wrong password. Try again.'),
            onSuccess: () => setPwd(''),
          })
        }}
      >
        <div className="fw-semibold mb-1">🔒 Secret values are locked</div>
        {status.hint && <div className="small text-muted mb-2">Hint: {status.hint}</div>}
        <div className="d-flex gap-2 align-items-start">
          <input type="password" className="form-control form-control-sm" style={{ maxWidth: 260 }}
            value={pwd} placeholder="Master password" onChange={(e) => setPwd(e.target.value)} />
          <button className="btn btn-sm btn-primary" disabled={unlock.isPending || !pwd}>Unlock</button>
        </div>
        {err && <div className="text-danger small mt-1">{err}</div>}
      </form>
    )
  }

  // Not configured yet — offer to set a master password.
  return (
    <form
      className="alert alert-secondary mb-3"
      onSubmit={(e) => {
        e.preventDefault()
        setErr('')
        if (pwd.length < 4) { setErr('Password must be at least 4 characters.'); return }
        if (pwd !== confirm) { setErr("The passwords don't match."); return }
        setVault.mutate({ password: pwd, hint: hint.trim() || undefined }, {
          onSuccess: () => { setPwd(''); setConfirm(''); setHint('') },
          onError: (e2) => setErr(e2 instanceof Error ? e2.message : 'Could not set the password.'),
        })
      }}
    >
      <div className="fw-semibold mb-1">🔒 Protect your secret values</div>
      <div className="small text-muted mb-2">
        Set a master password to encrypt secret values. It is never stored and can’t be reset — if you
        forget it you’ll need to re-enter your secrets. The rest of the app is unaffected.
      </div>
      <div className="row g-2">
        <div className="col-auto">
          <input type="password" className="form-control form-control-sm" value={pwd}
            placeholder="Master password" onChange={(e) => setPwd(e.target.value)} />
        </div>
        <div className="col-auto">
          <input type="password" className="form-control form-control-sm" value={confirm}
            placeholder="Confirm password" onChange={(e) => setConfirm(e.target.value)} />
        </div>
        <div className="col-auto">
          <input className="form-control form-control-sm" value={hint}
            placeholder="Hint (optional)" onChange={(e) => setHint(e.target.value)} />
        </div>
        <div className="col-auto">
          <button className="btn btn-sm btn-primary" disabled={setVault.isPending}>Set master password</button>
        </div>
      </div>
      {err && <div className="text-danger small mt-1">{err}</div>}
    </form>
  )
}

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
  const setProjects = useSetItemProjects('secrets')
  const setEnvironments = useSetSecretEnvironments()

  const { data: vault } = useVaultStatus()
  const notConfigured = vault?.configured === false

  const [editingId, setEditingId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [clientId, setClientId] = useState('')
  const [value, setValue] = useState('')
  const [expiresOn, setExpiresOn] = useState('')
  const [notes, setNotes] = useState('')
  const [notify, setNotify] = useState('')
  const [link, setLink] = useState('')
  const [projectIds, setProjectIds] = useState<string[]>([])
  const [envIds, setEnvIds] = useState<string[]>([])
  const [reveal, setReveal] = useState<Record<string, boolean>>({})
  const [saveError, setSaveError] = useState('')
  const [showAdd, setShowAdd] = useState(false)

  function reset() {
    setEditingId(null); setName(''); setClientId(''); setValue(''); setExpiresOn('')
    setNotes(''); setNotify(''); setLink(''); setProjectIds([]); setEnvIds([]); setShowAdd(false)
  }

  function save(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim() || !expiresOn) return
    setSaveError('')
    const body = {
      name: name.trim(), clientId: clientId.trim() || undefined, value: value || undefined,
      expiresOn, notes: notes.trim() || undefined, link: link.trim() || undefined,
      notify: notify.split('\n').map((x) => x.trim()).filter(Boolean),
    }
    const onError = (err: unknown) =>
      setSaveError(err instanceof Error ? err.message : 'Could not save the secret.')
    if (editingId) {
      update.mutate({ id: editingId, ...body }, {
        onSuccess: () => {
          setProjects.mutate({ id: editingId, projectIds })
          setEnvironments.mutate({ id: editingId, environmentIds: envIds })
          reset()
        },
        onError,
      })
    } else {
      create.mutate(body, {
        onSuccess: (s) => {
          setProjects.mutate({ id: s.id, projectIds })
          setEnvironments.mutate({ id: s.id, environmentIds: envIds })
          reset()
        },
        onError,
      })
    }
  }

  function edit(s: Secret) {
    setEditingId(s.id); setName(s.name); setClientId(s.clientId ?? ''); setValue(s.value ?? '')
    setExpiresOn(s.expiresOn.slice(0, 10)); setNotes(s.notes ?? ''); setNotify(s.notify.join('\n'))
    setLink(s.link ?? ''); setProjectIds(s.projects.map((p) => p.id))
    setEnvIds(s.environments.map((e) => e.id)); setShowAdd(true)
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-start">
        <div>
          <h2 className="mb-1">🔑 Secrets</h2>
          <p className="text-muted">
            Track client secrets & keys and their expiry. You’ll get a heads-up a week before one expires.
            Stored locally on this device only.
          </p>
        </div>
        <button className="btn btn-primary" disabled={notConfigured}
          title={notConfigured ? 'Set a master password first' : undefined}
          onClick={() => (showAdd ? reset() : setShowAdd(true))}>
          {showAdd ? 'Close' : '＋ Add secret'}
        </button>
      </div>

      <VaultBar />

      {(showAdd || editingId) && (
      <form className="card card-body mb-4" onSubmit={save}>
        <div className="row g-2">
          <div className="col-md-4">
            <label className="form-label">Name</label>
            <input className="form-control" value={name} placeholder="e.g. My app registration"
              onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="col-md-4">
            <label className="form-label">Client ID (optional)</label>
            <input className="form-control" value={clientId} placeholder="e.g. 00000000-0000-0000-0000-000000000000"
              onChange={(e) => setClientId(e.target.value)} />
          </div>
          <div className="col-md-4">
            <label className="form-label">Expiry date</label>
            <input type="date" className="form-control" value={expiresOn} onChange={(e) => setExpiresOn(e.target.value)} />
          </div>
          <div className="col-md-6">
            <label className="form-label">Secret value (optional)</label>
            <input className="form-control" value={value} type="text" placeholder="Paste the secret / key"
              onChange={(e) => setValue(e.target.value)} />
          </div>
          <div className="col-md-6">
            <label className="form-label">Link to resource (optional)</label>
            <input className="form-control" value={link} placeholder="https://portal.azure.com/…"
              onChange={(e) => setLink(e.target.value)} />
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
          <div className="col-12 d-flex align-items-center gap-2">
            <button className="btn btn-primary" disabled={create.isPending || update.isPending}>
              {editingId ? 'Save' : 'Add'}
            </button>
            {editingId && <button type="button" className="btn btn-outline-secondary" onClick={reset}>Cancel</button>}
            <ProjectPicker value={projectIds} onChange={setProjectIds} />
            <EnvironmentPicker value={envIds} onChange={setEnvIds} />
          </div>
          {saveError && <div className="col-12 text-danger small">{saveError}</div>}
        </div>
      </form>
      )}

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
                    {s.locked ? (
                      <div className="small text-muted">🔒 Value hidden — unlock to view</div>
                    ) : s.value && (
                      <div className="small text-muted font-monospace">
                        {reveal[s.id] ? s.value : '••••••••'}
                        <button className="btn btn-sm btn-link p-0 ms-2"
                          onClick={() => setReveal((r) => ({ ...r, [s.id]: !r[s.id] }))}>
                          {reveal[s.id] ? 'hide' : 'show'}
                        </button>
                      </div>
                    )}
                    {s.link && (
                      <div className="small">
                        <a href={s.link} target="_blank" rel="noreferrer">{s.link}</a>
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
                    {s.projects.length > 0 && <div className="mt-1"><ProjectBadges projects={s.projects} /></div>}
                    {s.environments.length > 0 && (
                      <div className="mt-1 d-flex flex-wrap gap-1">
                        {s.environments.map((e) => (
                          <span key={e.id} className={`badge text-bg-${envTypeVariant[e.type] ?? 'light'}`}>🌐 {e.name}</span>
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
