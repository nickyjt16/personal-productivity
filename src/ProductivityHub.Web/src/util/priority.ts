import type { Priority } from '../api/types'

// Bootstrap badge variant per priority — shared by the Todos list and dashboard.
export const priorityVariant: Record<Priority, string> = {
  Low: 'secondary',
  Medium: 'info',
  High: 'danger',
}
