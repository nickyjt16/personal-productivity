import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type {
  Bookmark,
  InboxItem,
  JournalEntry,
  Note,
  PomodoroKind,
  PomodoroSession,
  Priority,
  Project,
  ProjectStatus,
  RecurUnit,
  Secret,
  Todo,
  VaultStatus,
  Environment,
  EnvConfig,
  EnvConfigKind,
  EnvironmentType,
} from './types'

// Build a query string from defined params only.
function qs(params: Record<string, string | boolean | undefined>): string {
  const pairs = Object.entries(params)
    .filter(([, v]) => v !== undefined)
    .map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`)
  return pairs.length ? `?${pairs.join('&')}` : ''
}

// ---------- Todos ----------
export function useTodos(done?: boolean, projectId?: string) {
  return useQuery({
    queryKey: ['todos', done, projectId],
    queryFn: () => api.get<Todo[]>(`/api/todos${qs({ done, projectId })}`),
  })
}

export function useCreateTodo() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { title: string; notes?: string; priority?: Priority; dueDate?: string; recurUnit?: RecurUnit; recurInterval?: number }) =>
      api.post<Todo>('/api/todos', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['todos'] }),
  })
}

export function useUpdateTodo() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: {
      id: string; title: string; notes?: string | null; priority: Priority;
      isDone: boolean; dueDate?: string | null; recurUnit?: RecurUnit; recurInterval?: number
    }) => api.put<Todo>(`/api/todos/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['todos'] }),
  })
}

export function useToggleTodo() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post<Todo>(`/api/todos/${id}/toggle`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['todos'] }),
  })
}

export function useDeleteTodo() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.del(`/api/todos/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['todos'] }),
  })
}

// ---------- Inbox ----------
export function useInbox(processed?: boolean) {
  const qs = processed === undefined ? '' : `?processed=${processed}`
  return useQuery({ queryKey: ['inbox', processed], queryFn: () => api.get<InboxItem[]>(`/api/inbox${qs}`) })
}

export function useCapture() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (text: string) => api.post<InboxItem>('/api/inbox', { text }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inbox'] }),
  })
}

export function useProcessInbox() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post(`/api/inbox/${id}/process`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inbox'] }),
  })
}

export function useInboxToTodo() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post(`/api/inbox/${id}/to-todo`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['inbox'] })
      qc.invalidateQueries({ queryKey: ['todos'] })
    },
  })
}

export function useInboxToNote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post(`/api/inbox/${id}/to-note`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['inbox'] })
      qc.invalidateQueries({ queryKey: ['notes'] })
    },
  })
}

export function useDeleteInbox() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.del(`/api/inbox/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inbox'] }),
  })
}

// ---------- Bookmarks ----------
export function useBookmarks(read?: boolean, projectId?: string) {
  return useQuery({
    queryKey: ['bookmarks', read, projectId],
    queryFn: () => api.get<Bookmark[]>(`/api/bookmarks${qs({ read, projectId })}`),
  })
}

export function useCreateBookmark() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { url: string; title?: string; notes?: string }) =>
      api.post<Bookmark>('/api/bookmarks', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['bookmarks'] }),
  })
}

export function useToggleBookmark() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post(`/api/bookmarks/${id}/toggle-read`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['bookmarks'] }),
  })
}

export function useDeleteBookmark() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.del(`/api/bookmarks/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['bookmarks'] }),
  })
}

export interface ImportResult {
  enabled: boolean
  folderPath: string
  folderExists: boolean
  filesProcessed: number
  imported: number
  duplicates: number
  skippedNoUrl: number
  errors: string[]
}

export function useImportLinks() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.post<ImportResult>('/api/bookmarks/import'),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['bookmarks'] }),
  })
}

// ---------- Notes ----------
export function useNotes(projectId?: string, archived?: boolean) {
  return useQuery({
    queryKey: ['notes', projectId, archived ?? false],
    queryFn: () => api.get<Note[]>(`/api/notes${qs({ projectId, archived })}`),
  })
}

export function useToggleNoteArchive() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post<Note>(`/api/notes/${id}/archive`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notes'] }),
  })
}

export function useCreateNote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { title?: string; body: string }) => api.post<Note>('/api/notes', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notes'] }),
  })
}

export function useUpdateNote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string; title?: string; body: string }) =>
      api.put<Note>(`/api/notes/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notes'] }),
  })
}

export function useDeleteNote() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.del(`/api/notes/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notes'] }),
  })
}

// ---------- Journal ----------
export function useJournalList() {
  return useQuery({ queryKey: ['journal'], queryFn: () => api.get<JournalEntry[]>('/api/journal') })
}

export function useJournalEntry(date: string) {
  return useQuery({
    queryKey: ['journal', date],
    // 404 means "no entry yet" — treat as null rather than an error.
    queryFn: () => api.get<JournalEntry>(`/api/journal/${date}`).catch(() => null),
  })
}

export function useSaveJournal() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { entryDate: string; body: string; mood?: string }) =>
      api.put<JournalEntry>('/api/journal', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['journal'] }),
  })
}

// ---------- Pomodoro ----------
export function usePomodoroToday() {
  return useQuery({ queryKey: ['pomodoro'], queryFn: () => api.get<PomodoroSession[]>('/api/pomodoro') })
}

export function useStartPomodoro() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { todoItemId?: string; durationMinutes: number; kind?: PomodoroKind }) =>
      api.post<PomodoroSession>('/api/pomodoro', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pomodoro'] }),
  })
}

export function useCompletePomodoro() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.post(`/api/pomodoro/${id}/complete`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pomodoro'] }),
  })
}

// ---------- Projects ----------
export function useProjects(status: string = 'open') {
  return useQuery({
    queryKey: ['projects', status],
    queryFn: () => api.get<Project[]>(`/api/projects${qs({ status })}`),
  })
}

export function useCreateProject() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { name: string; description?: string; color?: string; status?: ProjectStatus }) =>
      api.post<Project>('/api/projects', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['projects'] }),
  })
}

export function useUpdateProject() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: {
      id: string; name: string; description?: string | null; color?: string; status?: ProjectStatus
    }) => api.put<Project>(`/api/projects/${id}`, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['projects'] })
      // Archiving a project archives its notes server-side.
      qc.invalidateQueries({ queryKey: ['notes'] })
    },
  })
}

export function useDeleteProject() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.del(`/api/projects/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['projects'] })
      qc.invalidateQueries({ queryKey: ['todos'] })
      qc.invalidateQueries({ queryKey: ['notes'] })
      qc.invalidateQueries({ queryKey: ['bookmarks'] })
    },
  })
}

// ---------- Secrets ----------
export function useSecrets(projectId?: string) {
  return useQuery({
    queryKey: ['secrets', projectId],
    queryFn: () => api.get<Secret[]>(`/api/secrets${qs({ projectId })}`),
  })
}

export function useExpiringSecrets() {
  return useQuery({ queryKey: ['secrets', 'expiring'], queryFn: () => api.get<Secret[]>('/api/secrets/expiring') })
}

export function useCreateSecret() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { name: string; clientId?: string; value?: string; expiresOn: string; notes?: string; notify?: string[]; link?: string }) =>
      api.post<Secret>('/api/secrets', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['secrets'] }),
  })
}

export function useUpdateSecret() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string; name: string; clientId?: string; value?: string; expiresOn: string; notes?: string; notify?: string[]; link?: string }) =>
      api.put<Secret>(`/api/secrets/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['secrets'] }),
  })
}

export function useDeleteSecret() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.del(`/api/secrets/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['secrets'] }),
  })
}

export function useSetSecretEnvironments() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, environmentIds }: { id: string; environmentIds: string[] }) =>
      api.put(`/api/secrets/${id}/environments`, { environmentIds }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['secrets'] })
      qc.invalidateQueries({ queryKey: ['environments'] })
    },
  })
}

// ---------- Secret vault (master password) ----------
export function useVaultStatus() {
  return useQuery({ queryKey: ['vault'], queryFn: () => api.get<VaultStatus>('/api/vault') })
}

function useVaultMutation<TArgs>(fn: (args: TArgs) => Promise<unknown>) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['vault'] })
      qc.invalidateQueries({ queryKey: ['secrets'] })
    },
  })
}

export function useSetVault() {
  return useVaultMutation((body: { password: string; hint?: string }) =>
    api.post<VaultStatus>('/api/vault/set', body))
}

export function useUnlockVault() {
  return useVaultMutation((body: { password: string }) =>
    api.post<VaultStatus>('/api/vault/unlock', body))
}

export function useLockVault() {
  return useVaultMutation(() => api.post('/api/vault/lock'))
}

// ---------- Environments ----------
type EnvBody = {
  name: string; type: EnvironmentType; ppEnvironmentId?: string
  url?: string; tenantId?: string; region?: string; notes?: string
}
type ConfigBody = { kind: EnvConfigKind; name: string; value?: string; solution?: string; notes?: string }

export function useEnvironments() {
  return useQuery({ queryKey: ['environments'], queryFn: () => api.get<Environment[]>('/api/environments') })
}

function useEnvMutation<TArgs>(fn: (args: TArgs) => Promise<unknown>) {
  const qc = useQueryClient()
  return useMutation({ mutationFn: fn, onSuccess: () => qc.invalidateQueries({ queryKey: ['environments'] }) })
}

export function useCreateEnvironment() {
  return useEnvMutation((body: EnvBody) => api.post<Environment>('/api/environments', body))
}
export function useUpdateEnvironment() {
  return useEnvMutation(({ id, ...body }: EnvBody & { id: string }) => api.put<Environment>(`/api/environments/${id}`, body))
}
export function useDeleteEnvironment() {
  return useEnvMutation((id: string) => api.del(`/api/environments/${id}`))
}
export function useAddEnvConfig() {
  return useEnvMutation(({ envId, ...body }: ConfigBody & { envId: string }) =>
    api.post<EnvConfig>(`/api/environments/${envId}/configs`, body))
}
export function useUpdateEnvConfig() {
  return useEnvMutation(({ envId, configId, ...body }: ConfigBody & { envId: string; configId: string }) =>
    api.put<EnvConfig>(`/api/environments/${envId}/configs/${configId}`, body))
}
export function useToggleEnvConfig() {
  return useEnvMutation(({ envId, configId }: { envId: string; configId: string }) =>
    api.post<EnvConfig>(`/api/environments/${envId}/configs/${configId}/toggle`))
}
export function useDeleteEnvConfig() {
  return useEnvMutation(({ envId, configId }: { envId: string; configId: string }) =>
    api.del(`/api/environments/${envId}/configs/${configId}`))
}

// ---------- Search ----------
export interface SearchHit {
  type: 'todo' | 'note' | 'bookmark'
  id: string
  title: string
  subtitle: string | null
  url: string | null
}
export interface SearchResults {
  query: string
  todos: SearchHit[]
  notes: SearchHit[]
  bookmarks: SearchHit[]
}

export function useSearch(query: string) {
  const q = query.trim()
  return useQuery({
    queryKey: ['search', q],
    queryFn: () => api.get<SearchResults>(`/api/search${qs({ q })}`),
    enabled: q.length > 0,
  })
}

// ---------- Data ----------
export function useClearAllData() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.post('/api/data/clear'),
    onSuccess: () => qc.invalidateQueries(),
  })
}

export function useImportData() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (backup: unknown) => api.post('/api/data/import', backup),
    onSuccess: () => qc.invalidateQueries(),
  })
}

// Set the full set of projects an item belongs to. `kind` is the API route segment.
export function useSetItemProjects(kind: 'todos' | 'notes' | 'bookmarks' | 'secrets') {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, projectIds }: { id: string; projectIds: string[] }) =>
      api.put(`/api/${kind}/${id}/projects`, { projectIds }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [kind] })
      qc.invalidateQueries({ queryKey: ['projects'] })
    },
  })
}
