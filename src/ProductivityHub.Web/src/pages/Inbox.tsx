import { useState } from 'react'
import { useCapture, useDeleteInbox, useInbox, useInboxToTodo, useProcessInbox } from '../api/hooks'

export default function Inbox() {
  const { data: items = [], isLoading } = useInbox()
  const capture = useCapture()
  const process = useProcessInbox()
  const toTodo = useInboxToTodo()
  const remove = useDeleteInbox()

  const [text, setText] = useState('')

  function add(e: React.FormEvent) {
    e.preventDefault()
    if (!text.trim()) return
    capture.mutate(text.trim(), { onSuccess: () => setText('') })
  }

  return (
    <div>
      <h2 className="mb-1">📥 Quick-capture inbox</h2>
      <p className="text-muted">Dump anything fast, triage it later into a todo.</p>

      <form className="input-group mb-4" onSubmit={add}>
        <input className="form-control" value={text} placeholder="Capture a thought…"
          onChange={(e) => setText(e.target.value)} autoFocus />
        <button className="btn btn-primary" disabled={capture.isPending}>Capture</button>
      </form>

      {isLoading ? <p>Loading…</p> : items.length === 0 ? (
        <p className="text-muted">Inbox is empty. 🎉</p>
      ) : (
        <ul className="list-group">
          {items.map((i) => (
            <li key={i.id}
              className={`list-group-item d-flex align-items-center gap-2 ${i.isProcessed ? 'opacity-50' : ''}`}>
              <span className="flex-grow-1">{i.text}</span>
              {!i.isProcessed && (
                <>
                  <button className="btn btn-sm btn-outline-success"
                    title="Convert to todo" onClick={() => toTodo.mutate(i.id)}>→ Todo</button>
                  <button className="btn btn-sm btn-outline-secondary"
                    title="Mark processed" onClick={() => process.mutate(i.id)}>Done</button>
                </>
              )}
              <button className="btn btn-sm btn-outline-danger" onClick={() => remove.mutate(i.id)}>✕</button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
