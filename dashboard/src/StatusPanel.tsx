import type { VenStatus } from './types'

function formatRelative(iso: string | null): string {
  if (!iso) return 'never'
  const seconds = Math.round((Date.now() - new Date(iso).getTime()) / 1000)
  if (seconds < 5) return 'just now'
  if (seconds < 60) return `${seconds}s ago`
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.round(minutes / 60)
  return `${hours}h ago`
}

export function StatusPanel({ status }: { status: VenStatus | null }) {
  if (!status) {
    return (
      <div className="status-panel">
        <p className="status-empty">No VEN has connected yet.</p>
      </div>
    )
  }

  return (
    <div className="status-panel">
      <div className="status-row">
        <span className="status-label">VEN</span>
        <span className="status-value">{status.venName}</span>
      </div>
      {status.mqttConnected !== null && (
        <div className="status-row">
          <span className="status-label">MQTT</span>
          <span className="status-value">
            <span
              className="status-dot"
              style={{ background: status.mqttConnected ? 'var(--status-good)' : 'var(--status-critical)' }}
            />
            {status.mqttConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
      )}
      <div className="status-row">
        <span className="status-label">Last report</span>
        <span className="status-value">{formatRelative(status.lastReportTime)}</span>
      </div>
    </div>
  )
}
