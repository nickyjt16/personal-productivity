export type Priority = 'Low' | 'Medium' | 'High'
export type RecurUnit = 'None' | 'Day' | 'Week' | 'Month'
export type PomodoroKind = 'Focus' | 'ShortBreak' | 'LongBreak'
export type ProjectStatus = 'New' | 'Active' | 'Complete' | 'Archived'

export interface ProjectRef {
  id: string
  name: string
  color: string
}

export interface Project {
  id: string
  name: string
  description: string | null
  color: string
  status: ProjectStatus
  createdAt: string
  updatedAt: string
  todosTotal: number
  todosDone: number
  noteCount: number
  bookmarkCount: number
  secretCount: number
}

export interface Todo {
  id: string
  title: string
  notes: string | null
  priority: Priority
  isDone: boolean
  dueDate: string | null
  createdAt: string
  completedAt: string | null
  recurUnit: RecurUnit
  recurInterval: number
  projects: ProjectRef[]
}

export interface InboxItem {
  id: string
  text: string
  isProcessed: boolean
  createdAt: string
  processedAt: string | null
}

export interface Bookmark {
  id: string
  url: string
  title: string | null
  notes: string | null
  isRead: boolean
  createdAt: string
  readAt: string | null
  projects: ProjectRef[]
}

export interface Note {
  id: string
  title: string | null
  body: string
  createdAt: string
  updatedAt: string
  projects: ProjectRef[]
}

export interface JournalEntry {
  id: string
  entryDate: string
  body: string
  mood: string | null
  createdAt: string
  updatedAt: string
}

export interface Secret {
  id: string
  name: string
  clientId: string | null
  value: string | null
  expiresOn: string
  notes: string | null
  notify: string[]
  link: string | null
  projects: ProjectRef[]
  environments: SecretEnvRef[]
  daysLeft: number
  // A value is stored (hasValue) but may be hidden until the vault is unlocked (locked).
  hasValue: boolean
  locked: boolean
}

export interface SecretEnvRef {
  id: string
  name: string
  type: EnvironmentType
}

export interface VaultStatus {
  configured: boolean
  unlocked: boolean
  hint: string | null
}

export type EnvironmentType = 'Dev' | 'Test' | 'UAT' | 'Prod' | 'Default' | 'Sandbox' | 'Other'
export type EnvConfigKind = 'ConnectionReference' | 'EnvironmentVariable'

export interface EnvConfig {
  id: string
  kind: EnvConfigKind
  name: string
  value: string | null
  solution: string | null
  isSet: boolean
  notes: string | null
}

export interface Environment {
  id: string
  name: string
  type: EnvironmentType
  ppEnvironmentId: string | null
  url: string | null
  tenantId: string | null
  region: string | null
  notes: string | null
  configs: EnvConfig[]
  secrets: { id: string; name: string }[]
}

export interface PomodoroSession {
  id: string
  todoItemId: string | null
  todoTitle: string | null
  startedAt: string
  durationMinutes: number
  completedAt: string | null
  kind: PomodoroKind
}
