import { useMemo, useState } from 'react'
import type { ForecastResponse } from './types'

const WIDTH = 900
const HEIGHT = 320
const MARGIN = { top: 16, right: 16, bottom: 32, left: 44 }
const MAX_GHOSTS = 8

function niceMax(value: number): number {
  if (value <= 0) return 1
  const magnitude = Math.pow(10, Math.floor(Math.log10(value)))
  const normalized = value / magnitude
  const step = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10
  return step * magnitude
}

export function ForecastChart({ forecast, showGhosts }: { forecast: ForecastResponse; showGhosts: boolean }) {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null)

  const { hours, actualKw, forecastVersions } = forecast
  const latest = forecastVersions.find((v) => v.isLatest) ?? forecastVersions[forecastVersions.length - 1]
  const ghosts = showGhosts
    ? forecastVersions.filter((v) => !v.isLatest).slice(-MAX_GHOSTS)
    : []

  const plotWidth = WIDTH - MARGIN.left - MARGIN.right
  const plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom
  const bandWidth = plotWidth / hours.length
  const barWidth = Math.min(24, bandWidth - 2)

  const maxValue = useMemo(() => {
    const all = [...(latest?.valuesKw ?? []), ...actualKw, ...ghosts.flatMap((g) => g.valuesKw)]
    const max = Math.max(0, ...all.filter((v): v is number => v !== null))
    return niceMax(max * 1.15)
  }, [latest, actualKw, ghosts])

  const yScale = (kw: number) => plotHeight - (kw / maxValue) * plotHeight
  // Local to the already-translated <g transform="translate(MARGIN.left, MARGIN.top)"> below —
  // these are plot-area coordinates, not absolute SVG coordinates.
  const xCenter = (index: number) => index * bandWidth + bandWidth / 2

  const actualPoints = actualKw
    .map((v, i) => (v === null ? null : { x: xCenter(i), y: MARGIN.top + yScale(v) }))
    .filter((p): p is { x: number; y: number } => p !== null)

  const linePath = actualPoints.length > 0
    ? actualPoints.reduce((path, p, i) => path + (i === 0 ? `M ${p.x} ${p.y}` : ` L ${p.x} ${p.y}`), '')
    : ''

  const yTicks = [0, maxValue / 2, maxValue]
  const hovered = hoverIndex !== null ? hours[hoverIndex] : null

  return (
    <div className="chart-root">
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} className="chart-svg" role="img" aria-label="Forecast and actual demand by hour">
        <g transform={`translate(${MARGIN.left}, ${MARGIN.top})`}>
          {yTicks.map((tick) => (
            <g key={tick}>
              <line x1={0} y1={yScale(tick)} x2={plotWidth} y2={yScale(tick)} stroke="var(--gridline)" strokeWidth={1} />
              <text x={-8} y={yScale(tick)} textAnchor="end" dominantBaseline="middle" className="axis-label">
                {tick.toFixed(1)}
              </text>
            </g>
          ))}
          <line x1={0} y1={plotHeight} x2={plotWidth} y2={plotHeight} stroke="var(--baseline)" strokeWidth={1} />

          {ghosts.map((ghost) => (
            <g key={ghost.createdAt} opacity={0.12}>
              {ghost.valuesKw.map((v, i) =>
                v === null ? null : (
                  <rect
                    key={i}
                    x={xCenter(i) - barWidth / 2}
                    y={yScale(v)}
                    width={barWidth}
                    height={plotHeight - yScale(v)}
                    rx={4}
                    fill="var(--series-forecast)"
                  />
                ),
              )}
            </g>
          ))}

          {latest?.valuesKw.map((v, i) =>
            v === null ? null : (
              <rect
                key={i}
                x={xCenter(i) - barWidth / 2}
                y={yScale(v)}
                width={barWidth}
                height={plotHeight - yScale(v)}
                rx={4}
                fill="var(--series-forecast)"
              />
            ),
          )}

          {linePath && <path d={linePath} fill="none" stroke="var(--series-actual)" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />}
          {actualPoints.map((p, i) => (
            <circle key={i} cx={p.x} cy={p.y} r={4} fill="var(--series-actual)" stroke="var(--surface-1)" strokeWidth={2} />
          ))}

          {hours.map((h, i) => (
            <g key={h.index}>
              {i % Math.ceil(hours.length / 12) === 0 && (
                <text x={xCenter(i)} y={plotHeight + 18} textAnchor="middle" className="axis-label">
                  {h.label}
                </text>
              )}
              <rect
                x={i * bandWidth}
                y={0}
                width={bandWidth}
                height={plotHeight}
                fill="transparent"
                onMouseEnter={() => setHoverIndex(i)}
                onMouseLeave={() => setHoverIndex(null)}
              />
            </g>
          ))}

          {hoverIndex !== null && (
            <line x1={xCenter(hoverIndex)} y1={0} x2={xCenter(hoverIndex)} y2={plotHeight} stroke="var(--text-muted)" strokeWidth={1} strokeDasharray="2,2" />
          )}
        </g>
      </svg>

      <div className="chart-legend">
        <span className="legend-item">
          <span className="legend-swatch legend-swatch-rect" style={{ background: 'var(--series-forecast)' }} />
          Forecast
        </span>
        <span className="legend-item">
          <span className="legend-swatch legend-swatch-line" style={{ background: 'var(--series-actual)' }} />
          Actual
        </span>
      </div>

      {hovered && hoverIndex !== null && (
        <div
          className="chart-tooltip"
          style={{ left: `${((hoverIndex + 0.5) / hours.length) * 100}%` }}
        >
          <div className="chart-tooltip-label">{hovered.label}</div>
          <div className="chart-tooltip-row">
            <strong>{latest?.valuesKw[hoverIndex] ?? '—'}</strong> kW forecast
          </div>
          <div className="chart-tooltip-row">
            <strong>{actualKw[hoverIndex] ?? '—'}</strong> kW actual
          </div>
        </div>
      )}
    </div>
  )
}
