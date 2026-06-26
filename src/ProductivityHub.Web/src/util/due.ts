export type DueStatus = 'overdue' | 'today' | 'upcoming'

// Classify a todo's due date relative to today (local time). Done todos and
// todos with no due date return null.
export function dueStatus(dueDate: string | null, isDone: boolean): DueStatus | null {
  if (!dueDate || isDone) return null
  const due = new Date(dueDate)
  const now = new Date()
  const d = new Date(due.getFullYear(), due.getMonth(), due.getDate()).getTime()
  const t = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime()
  if (d < t) return 'overdue'
  if (d === t) return 'today'
  return 'upcoming'
}

// Bootstrap badge variant + label for a due date, or null if nothing to show.
export function dueBadge(dueDate: string | null, isDone: boolean): { variant: string; label: string } | null {
  const status = dueStatus(dueDate, isDone)
  if (!status || !dueDate) return null
  if (status === 'overdue') return { variant: 'danger', label: 'Overdue' }
  if (status === 'today') return { variant: 'warning', label: 'Due today' }
  return { variant: 'light', label: new Date(dueDate).toLocaleDateString() }
}
