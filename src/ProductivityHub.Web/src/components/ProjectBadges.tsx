import type { ProjectRef } from '../api/types'

// Small coloured pills showing which projects an item belongs to.
export default function ProjectBadges({ projects }: { projects: ProjectRef[] }) {
  if (!projects?.length) return null
  return (
    <span className="d-inline-flex flex-wrap gap-1 align-items-center">
      {projects.map((p) => (
        <span key={p.id} className="badge rounded-pill"
          style={{ backgroundColor: p.color, color: '#fff', fontWeight: 500 }}>
          {p.name}
        </span>
      ))}
    </span>
  )
}
