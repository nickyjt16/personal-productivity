import { useCallback, useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useCompletePomodoro, usePomodoroToday, useStartPomodoro, useTodos } from '../api/hooks'

const FOCUS_MINUTES = 25
const BREAK_MINUTES = 5

function format(seconds: number) {
  const m = Math.floor(seconds / 60).toString().padStart(2, '0')
  const s = (seconds % 60).toString().padStart(2, '0')
  return `${m}:${s}`
}

// Short chime via WebAudio (no asset needed).
function chime() {
  try {
    const ctx = new AudioContext()
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()
    osc.connect(gain)
    gain.connect(ctx.destination)
    osc.type = 'sine'
    osc.frequency.value = 880
    gain.gain.setValueAtTime(0.0001, ctx.currentTime)
    gain.gain.exponentialRampToValueAtTime(0.3, ctx.currentTime + 0.02)
    gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.7)
    osc.start()
    osc.stop(ctx.currentTime + 0.7)
    osc.onended = () => ctx.close()
  } catch {
    // Audio is best-effort.
  }
}

export default function PomodoroTimer() {
  const { data: openTodos = [] } = useTodos(false)
  const { data: sessions = [] } = usePomodoroToday()
  const start = useStartPomodoro()
  const complete = useCompletePomodoro()

  const [isBreak, setIsBreak] = useState(false)
  const totalSeconds = (isBreak ? BREAK_MINUTES : FOCUS_MINUTES) * 60
  const [remaining, setRemaining] = useState(totalSeconds)
  const [running, setRunning] = useState(false)
  const [todoId, setTodoId] = useState('')
  const sessionIdRef = useRef<string | null>(null)

  // Floating "picture-in-picture" clock.
  const pipWindowRef = useRef<Window | null>(null)
  const [pipContainer, setPipContainer] = useState<HTMLElement | null>(null)
  const pipSupported = typeof window !== 'undefined' && 'documentPictureInPicture' in window

  const closePip = useCallback(() => {
    pipWindowRef.current?.close()
    pipWindowRef.current = null
    setPipContainer(null)
  }, [])

  const openPip = useCallback(async () => {
    if (!pipSupported || pipWindowRef.current) return
    try {
      const pip = await window.documentPictureInPicture!.requestWindow({ width: 220, height: 200 })
      const style = pip.document.createElement('style')
      style.textContent = `
        html, body { margin: 0; height: 100%; }
        body {
          display: flex; flex-direction: column; align-items: center; justify-content: center;
          gap: 12px; font-family: system-ui, "Segoe UI", sans-serif;
          background: #1b1b2f; color: #fff;
        }
        .pip-clock { font-size: 56px; font-variant-numeric: tabular-nums; font-weight: 600; }
        .pip-kind { font-size: 13px; opacity: .7; letter-spacing: .05em; text-transform: uppercase; }
        .pip-row { display: flex; gap: 8px; }
        .pip-btn {
          border: 0; border-radius: 6px; padding: 7px 12px; font-size: 13px; cursor: pointer;
          background: #3a3a5a; color: #fff;
        }
        .pip-btn.primary { background: #0d6efd; }
        .pip-btn.reopen { background: #198754; }
      `
      pip.document.head.appendChild(style)
      const container = pip.document.createElement('div')
      container.style.display = 'contents'
      pip.document.body.appendChild(container)
      // If the user closes the floating window manually, clear our refs.
      pip.addEventListener('pagehide', () => {
        pipWindowRef.current = null
        setPipContainer(null)
      })
      pipWindowRef.current = pip
      setPipContainer(container)
    } catch {
      // PiP can be blocked (no user gesture, etc.) — timer still runs in-page.
    }
  }, [pipSupported])

  // Reset the clock when switching focus/break (only while idle).
  useEffect(() => {
    if (!running) setRemaining(totalSeconds)
  }, [totalSeconds, running])

  // Tick + finish handling.
  useEffect(() => {
    if (!running) return
    if (remaining <= 0) {
      setRunning(false)
      if (sessionIdRef.current) {
        complete.mutate(sessionIdRef.current)
        sessionIdRef.current = null
      }
      // Finish: close the floating clock, bring the app forward, notify + chime.
      closePip()
      window.focus()
      chime()
      if ('Notification' in window && Notification.permission === 'granted') {
        new Notification('🍅 Pomodoro complete', {
          body: isBreak ? 'Break over — back to it!' : 'Nice work. Time for a break.',
        })
      }
      return
    }
    const timer = setTimeout(() => setRemaining((r) => r - 1), 1000)
    return () => clearTimeout(timer)
  }, [running, remaining, complete, closePip, isBreak])

  // Clean up the floating window if the component unmounts.
  useEffect(() => closePip, [closePip])

  function handleStart() {
    // Request the floating window synchronously within the click gesture.
    void openPip()
    if ('Notification' in window && Notification.permission === 'default') {
      void Notification.requestPermission()
    }
    start.mutate(
      {
        todoItemId: !isBreak && todoId ? todoId : undefined,
        durationMinutes: isBreak ? BREAK_MINUTES : FOCUS_MINUTES,
        kind: isBreak ? 'ShortBreak' : 'Focus',
      },
      { onSuccess: (s) => { sessionIdRef.current = s.id } },
    )
    setRunning(true)
  }

  function reset() {
    setRunning(false)
    setRemaining(totalSeconds)
    sessionIdRef.current = null
  }

  const completedFocus = sessions.filter((s) => s.kind === 'Focus' && s.completedAt).length

  // The compact UI rendered into the floating window.
  const miniTimer = pipContainer && createPortal(
    <>
      <div className="pip-kind">{isBreak ? 'Break' : 'Focus'}</div>
      <div className="pip-clock">{format(remaining)}</div>
      <div className="pip-row">
        <button className="pip-btn primary" onClick={() => setRunning((r) => !r)}>
          {running ? 'Pause' : 'Resume'}
        </button>
        <button className="pip-btn" onClick={reset}>Restart</button>
      </div>
      <div className="pip-row">
        <button className="pip-btn reopen" onClick={() => { window.focus(); closePip() }}>
          Reopen app
        </button>
      </div>
    </>,
    pipContainer,
  )

  return (
    <div className="card card-body text-center">
      <h5 className="card-title">🍅 Pomodoro</h5>

      <div className="btn-group btn-group-sm mb-3 align-self-center">
        <button className={`btn ${!isBreak ? 'btn-danger' : 'btn-outline-danger'}`}
          disabled={running} onClick={() => setIsBreak(false)}>Focus 25</button>
        <button className={`btn ${isBreak ? 'btn-success' : 'btn-outline-success'}`}
          disabled={running} onClick={() => setIsBreak(true)}>Break 5</button>
      </div>

      <div className="display-3 font-monospace mb-3">{format(remaining)}</div>

      {!isBreak && (
        <select className="form-select form-select-sm mb-3" value={todoId}
          disabled={running} onChange={(e) => setTodoId(e.target.value)}>
          <option value="">(no task linked)</option>
          {openTodos.map((t) => <option key={t.id} value={t.id}>{t.title}</option>)}
        </select>
      )}

      <div className="d-flex gap-2 justify-content-center">
        {!running ? (
          <button className="btn btn-primary" onClick={handleStart}>Start</button>
        ) : (
          <button className="btn btn-warning" onClick={() => setRunning(false)}>Pause</button>
        )}
        <button className="btn btn-outline-secondary" onClick={reset}>Reset</button>
        {running && pipSupported && !pipContainer && (
          <button className="btn btn-outline-primary" onClick={() => void openPip()}>Pop out ⧉</button>
        )}
      </div>

      {!pipSupported && (
        <p className="text-muted small mb-0 mt-2">
          Floating timer needs Chrome/Edge 116+.
        </p>
      )}

      <p className="text-muted small mb-0 mt-3">
        {completedFocus} focus session{completedFocus === 1 ? '' : 's'} completed today
      </p>

      {miniTimer}
    </div>
  )
}
