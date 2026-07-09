import { useState } from 'react'
import {
  useAddEnvConfig,
  useCreateEnvironment,
  useDeleteEnvConfig,
  useDeleteEnvironment,
  useEnvironments,
  useToggleEnvConfig,
  useUpdateEnvironment,
} from '../api/hooks'
import type { EnvConfig, EnvConfigKind, Environment, EnvironmentType } from '../api/types'

const TYPES: EnvironmentType[] = ['Dev', 'Test', 'UAT', 'Prod', 'Default', 'Sandbox', 'Other']

const typeVariant: Record<EnvironmentType, string> = {
  Dev: 'secondary', Test: 'info', UAT: 'warning', Prod: 'danger',
  Default: 'primary', Sandbox: 'success', Other: 'light',
}

// Quick-launch links derived from the stored IDs/URL.
function makerUrl(e: Environment) {
  return e.ppEnvironmentId ? `https://make.powerapps.com/environments/${e.ppEnvironmentId}/home` : null
}
function adminUrl(e: Environment) {
  return e.ppEnvironmentId ? `https://admin.powerplatform.microsoft.com/environments/${e.ppEnvironmentId}/hub` : null
}
function dataverseApi(e: Environment) {
  return e.url ? `${e.url.replace(/\/+$/, '')}/api/data/v9.2/` : null
}

type EnvForm = {
  name: string; type: EnvironmentType; ppEnvironmentId: string
  url: string; tenantId: string; region: string; notes: string
}
const emptyForm: EnvForm = { name: '', type: 'Dev', ppEnvironmentId: '', url: '', tenantId: '', region: '', notes: '' }

export default function Environments() {
  const { data: envs = [], isLoading } = useEnvironments()
  const create = useCreateEnvironment()
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState<EnvForm>(emptyForm)

  function addEnv(e: React.FormEvent) {
    e.preventDefault()
    if (!form.name.trim()) return
    create.mutate(
      {
        name: form.name.trim(), type: form.type,
        ppEnvironmentId: form.ppEnvironmentId.trim() || undefined,
        url: form.url.trim() || undefined, tenantId: form.tenantId.trim() || undefined,
        region: form.region.trim() || undefined, notes: form.notes.trim() || undefined,
      },
      { onSuccess: () => { setForm(emptyForm); setShowAdd(false) } },
    )
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-start">
        <div>
          <h2 className="mb-1">🌐 Environments</h2>
          <p className="text-muted">Power Platform / Dataverse environments — IDs, URLs, and per-environment setup.</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowAdd((s) => !s)}>
          {showAdd ? 'Close' : '＋ Add environment'}
        </button>
      </div>

      {showAdd && (
        <form className="card card-body mb-4" onSubmit={addEnv}>
          <EnvFields form={form} setForm={setForm} />
          <div><button className="btn btn-primary" disabled={create.isPending}>Add environment</button></div>
        </form>
      )}

      {isLoading ? <p>Loading…</p> : envs.length === 0 ? (
        <p className="text-muted">No environments yet. Add one above.</p>
      ) : (
        envs.map((e) => <EnvCard key={e.id} env={e} />)
      )}
    </div>
  )
}

function EnvFields({ form, setForm }: { form: EnvForm; setForm: (f: EnvForm) => void }) {
  const set = (patch: Partial<EnvForm>) => setForm({ ...form, ...patch })
  return (
    <div className="row g-2 mb-2">
      <div className="col-md-6">
        <label className="form-label">Name</label>
        <input className="form-control" value={form.name} placeholder="e.g. Contoso — Dev"
          onChange={(e) => set({ name: e.target.value })} />
      </div>
      <div className="col-md-3">
        <label className="form-label">Type</label>
        <select className="form-select" value={form.type} onChange={(e) => set({ type: e.target.value as EnvironmentType })}>
          {TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
        </select>
      </div>
      <div className="col-md-3">
        <label className="form-label">Region</label>
        <input className="form-control" value={form.region} placeholder="e.g. UK"
          onChange={(e) => set({ region: e.target.value })} />
      </div>
      <div className="col-md-6">
        <label className="form-label">Environment ID</label>
        <input className="form-control font-monospace" value={form.ppEnvironmentId} placeholder="GUID from the admin centre"
          onChange={(e) => set({ ppEnvironmentId: e.target.value })} />
      </div>
      <div className="col-md-6">
        <label className="form-label">Org / environment URL</label>
        <input className="form-control" value={form.url} placeholder="https://contoso.crm11.dynamics.com"
          onChange={(e) => set({ url: e.target.value })} />
      </div>
      <div className="col-md-6">
        <label className="form-label">Tenant ID</label>
        <input className="form-control font-monospace" value={form.tenantId} placeholder="Directory (tenant) ID"
          onChange={(e) => set({ tenantId: e.target.value })} />
      </div>
      <div className="col-md-6">
        <label className="form-label">Notes</label>
        <input className="form-control" value={form.notes} placeholder="Optional"
          onChange={(e) => set({ notes: e.target.value })} />
      </div>
    </div>
  )
}

function EnvCard({ env }: { env: Environment }) {
  const update = useUpdateEnvironment()
  const remove = useDeleteEnvironment()
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState<EnvForm>({
    name: env.name, type: env.type, ppEnvironmentId: env.ppEnvironmentId ?? '',
    url: env.url ?? '', tenantId: env.tenantId ?? '', region: env.region ?? '', notes: env.notes ?? '',
  })

  function save(e: React.FormEvent) {
    e.preventDefault()
    if (!form.name.trim()) return
    update.mutate(
      {
        id: env.id, name: form.name.trim(), type: form.type,
        ppEnvironmentId: form.ppEnvironmentId.trim() || undefined,
        url: form.url.trim() || undefined, tenantId: form.tenantId.trim() || undefined,
        region: form.region.trim() || undefined, notes: form.notes.trim() || undefined,
      },
      { onSuccess: () => setEditing(false) },
    )
  }

  const links: [string, string | null][] = [
    ['Maker portal', makerUrl(env)],
    ['Admin centre', adminUrl(env)],
    ['Open org', env.url],
    ['Dataverse API', dataverseApi(env)],
  ]

  return (
    <div className="card mb-3">
      <div className="card-body">
        {editing ? (
          <form onSubmit={save}>
            <EnvFields form={form} setForm={setForm} />
            <div className="d-flex gap-2">
              <button className="btn btn-sm btn-primary" disabled={update.isPending}>Save</button>
              <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => setEditing(false)}>Cancel</button>
            </div>
          </form>
        ) : (
          <>
            <div className="d-flex align-items-center gap-2">
              <h5 className="mb-0">{env.name}</h5>
              <span className={`badge text-bg-${typeVariant[env.type]}`}>{env.type}</span>
              {env.region && <span className="text-muted small">{env.region}</span>}
              <div className="ms-auto d-flex gap-1">
                <button className="btn btn-sm btn-outline-secondary" onClick={() => setEditing(true)}>✎</button>
                <button className="btn btn-sm btn-outline-danger"
                  onClick={() => { if (confirm(`Delete environment "${env.name}"?`)) remove.mutate(env.id) }}>✕</button>
              </div>
            </div>

            <dl className="row small mt-2 mb-2">
              {env.ppEnvironmentId && (<><dt className="col-sm-3 text-muted">Environment ID</dt><dd className="col-sm-9 font-monospace">{env.ppEnvironmentId}</dd></>)}
              {env.url && (<><dt className="col-sm-3 text-muted">URL</dt><dd className="col-sm-9">{env.url}</dd></>)}
              {env.tenantId && (<><dt className="col-sm-3 text-muted">Tenant ID</dt><dd className="col-sm-9 font-monospace">{env.tenantId}</dd></>)}
              {env.notes && (<><dt className="col-sm-3 text-muted">Notes</dt><dd className="col-sm-9">{env.notes}</dd></>)}
            </dl>

            <div className="d-flex flex-wrap gap-2 mb-1">
              {links.filter(([, href]) => href).map(([label, href]) => (
                <a key={label} className="btn btn-sm btn-outline-primary" href={href!} target="_blank" rel="noreferrer">
                  {label} ↗
                </a>
              ))}
            </div>

            {env.secrets.length > 0 && (
              <div className="small mt-2">
                <span className="text-muted">🔑 Secrets: </span>
                {env.secrets.map((s, i) => (
                  <span key={s.id}>{i > 0 && ', '}{s.name}</span>
                ))}
              </div>
            )}
          </>
        )}

        <hr />
        <ConfigChecklist env={env} />
      </div>
    </div>
  )
}

function ConfigChecklist({ env }: { env: Environment }) {
  const add = useAddEnvConfig()
  const toggle = useToggleEnvConfig()
  const remove = useDeleteEnvConfig()
  const [kind, setKind] = useState<EnvConfigKind>('ConnectionReference')
  const [name, setName] = useState('')
  const [value, setValue] = useState('')
  const [solution, setSolution] = useState('')

  function addRow(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim()) return
    add.mutate(
      { envId: env.id, kind, name: name.trim(), value: value.trim() || undefined, solution: solution.trim() || undefined },
      { onSuccess: () => { setName(''); setValue(''); setSolution('') } },
    )
  }

  return (
    <div>
      <div className="fw-semibold small mb-2">Setup checklist (connection references &amp; environment variables)</div>
      {env.configs.length > 0 && (
        <ul className="list-group list-group-flush mb-2">
          {env.configs.map((c) => <ConfigRow key={c.id} envId={env.id} config={c} onToggle={toggle} onRemove={remove} />)}
        </ul>
      )}
      <form className="row g-1 align-items-end" onSubmit={addRow}>
        <div className="col-auto">
          <select className="form-select form-select-sm" value={kind} onChange={(e) => setKind(e.target.value as EnvConfigKind)}>
            <option value="ConnectionReference">Connection ref</option>
            <option value="EnvironmentVariable">Env variable</option>
          </select>
        </div>
        <div className="col">
          <input className="form-control form-control-sm" value={name} placeholder="Logical / display name"
            onChange={(e) => setName(e.target.value)} />
        </div>
        <div className="col">
          <input className="form-control form-control-sm" value={value} placeholder="Value to set"
            onChange={(e) => setValue(e.target.value)} />
        </div>
        <div className="col-auto">
          <input className="form-control form-control-sm" style={{ width: 130 }} value={solution} placeholder="Solution (opt.)"
            onChange={(e) => setSolution(e.target.value)} />
        </div>
        <div className="col-auto">
          <button className="btn btn-sm btn-outline-primary" disabled={add.isPending}>Add</button>
        </div>
      </form>
    </div>
  )
}

function ConfigRow({ envId, config: c, onToggle, onRemove }: {
  envId: string
  config: EnvConfig
  onToggle: ReturnType<typeof useToggleEnvConfig>
  onRemove: ReturnType<typeof useDeleteEnvConfig>
}) {
  return (
    <li className="list-group-item px-0 d-flex align-items-center gap-2">
      <input type="checkbox" className="form-check-input mt-0" checked={c.isSet} title="Set in this environment"
        onChange={() => onToggle.mutate({ envId, configId: c.id })} />
      <span className={`badge text-bg-${c.kind === 'ConnectionReference' ? 'info' : 'secondary'}`}>
        {c.kind === 'ConnectionReference' ? 'CR' : 'EV'}
      </span>
      <div className="flex-grow-1">
        <span className={`font-monospace ${c.isSet ? 'text-decoration-line-through text-muted' : ''}`}>{c.name}</span>
        {c.value && <span className="text-muted small ms-2">→ {c.value}</span>}
        {c.solution && <span className="badge rounded-pill text-bg-light text-muted ms-2">{c.solution}</span>}
      </div>
      <button className="btn btn-sm btn-outline-danger" onClick={() => onRemove.mutate({ envId, configId: c.id })}>✕</button>
    </li>
  )
}
