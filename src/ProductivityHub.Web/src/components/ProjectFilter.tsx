import { useProjects } from '../api/hooks'

// A "filter by project" dropdown (open projects only). Empty value = all.
export default function ProjectFilter({ value, onChange }: {
  value: string
  onChange: (projectId: string) => void
}) {
  const { data: projects = [] } = useProjects('open')
  if (projects.length === 0) return null
  return (
    <select className="form-select form-select-sm" style={{ width: 'auto' }}
      value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">All projects</option>
      {projects.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
    </select>
  )
}
