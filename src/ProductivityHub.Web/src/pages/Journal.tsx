import { useEffect, useState } from 'react'
import { useJournalEntry, useJournalList, useSaveJournal } from '../api/hooks'

function today() {
  return new Date().toISOString().slice(0, 10)
}

export default function Journal() {
  const [date, setDate] = useState(today())
  const { data: entry, isLoading } = useJournalEntry(date)
  const { data: recent = [] } = useJournalList()
  const save = useSaveJournal()

  const [body, setBody] = useState('')
  const [mood, setMood] = useState('')

  useEffect(() => {
    setBody(entry?.body ?? '')
    setMood(entry?.mood ?? '')
  }, [date, entry?.body, entry?.mood])

  function submit() {
    save.mutate({ entryDate: date, body, mood: mood || undefined })
  }

  return (
    <div>
      <h2 className="mb-4">📔 Daily journal</h2>
      <div className="row g-3">
        <div className="col-md-8">
          <div className="card card-body">
            <div className="row g-2 mb-3">
              <div className="col-auto">
                <label className="form-label">Date</label>
                <input type="date" className="form-control" value={date} max={today()}
                  onChange={(e) => setDate(e.target.value)} />
              </div>
              <div className="col">
                <label className="form-label">Mood (optional)</label>
                <input className="form-control" value={mood} placeholder="e.g. focused, tired"
                  onChange={(e) => setMood(e.target.value)} />
              </div>
            </div>
            {isLoading ? <p>Loading…</p> : (
              <textarea className="form-control" rows={12} placeholder="How was your day?"
                value={body} onChange={(e) => setBody(e.target.value)} />
            )}
            <button className="btn btn-primary mt-2 align-self-start" onClick={submit}
              disabled={save.isPending}>Save entry</button>
          </div>
        </div>

        <div className="col-md-4">
          <h6 className="text-muted">Recent entries</h6>
          <div className="list-group">
            {recent.map((e) => (
              <button key={e.id}
                className={`list-group-item list-group-item-action ${e.entryDate === date ? 'active' : ''}`}
                onClick={() => setDate(e.entryDate)}>
                <div className="fw-semibold">{new Date(e.entryDate).toLocaleDateString()}</div>
                <small className={e.entryDate === date ? '' : 'text-muted'}>
                  {e.body.slice(0, 40) || '(empty)'}
                </small>
              </button>
            ))}
            {recent.length === 0 && <p className="text-muted small">No entries yet.</p>}
          </div>
        </div>
      </div>
    </div>
  )
}
