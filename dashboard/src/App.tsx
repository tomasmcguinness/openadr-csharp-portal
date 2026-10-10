import { useCallback, useEffect, useState } from 'react'
import './App.css'
import { fetchForecast, fetchStatus } from './api'
import { ForecastChart } from './ForecastChart'
import { StatusPanel } from './StatusPanel'
import type { ForecastResponse, ReportDto, VenStatus } from './types'
import { useDashboardHub } from './useDashboardHub'

function todayIso(): string {
  return new Date().toISOString().slice(0, 10)
}

function App() {
  const [statuses, setStatuses] = useState<VenStatus[]>([])
  const [date, setDate] = useState(todayIso())
  const [forecast, setForecast] = useState<ForecastResponse | null>(null)
  const [showGhosts, setShowGhosts] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const venName = statuses[0]?.venName ?? null

  const reloadStatus = useCallback(() => {
    fetchStatus().then(setStatuses).catch((e) => setError(String(e)))
  }, [])

  const reloadForecast = useCallback(() => {
    if (!venName) return
    fetchForecast(venName, date).then(setForecast).catch((e) => setError(String(e)))
  }, [venName, date])

  useEffect(() => {
    reloadStatus()
  }, [reloadStatus])

  useEffect(() => {
    reloadForecast()
  }, [reloadForecast])

  useDashboardHub({
    onReportReceived: useCallback(
      (report: ReportDto) => {
        if (report.clientName === venName) {
          reloadForecast()
          reloadStatus()
        }
      },
      [venName, reloadForecast, reloadStatus],
    ),
    onVenConnectionChanged: useCallback((changedVenName: string, mqttConnected: boolean) => {
      setStatuses((prev) => prev.map((s) => (s.venName === changedVenName ? { ...s, mqttConnected } : s)))
    }, []),
  })

  return (
    <div className="page">
      <header className="page-header">
        <h1>My Home</h1>
        <p className="page-subtitle">Whole-home demand forecast, via OpenADR 3.1</p>
      </header>

      {error && <p className="error-banner">{error}</p>}

      <div className="layout">
        <aside className="sidebar">
          <StatusPanel status={statuses[0] ?? null} />

          <div className="controls">
            <label className="control-row">
              <span>Date</span>
              <input type="date" value={date} onChange={(e) => setDate(e.target.value)} max={todayIso()} />
            </label>
            <label className="control-row control-checkbox">
              <input type="checkbox" checked={showGhosts} onChange={(e) => setShowGhosts(e.target.checked)} />
              <span>Ghost earlier forecasts</span>
            </label>
          </div>
        </aside>

        <main className="main">
          {forecast ? (
            <ForecastChart forecast={forecast} showGhosts={showGhosts} />
          ) : (
            <p className="chart-empty">
              {venName ? 'Loading forecast…' : 'Waiting for a VEN to report in…'}
            </p>
          )}
        </main>
      </div>
    </div>
  )
}

export default App
