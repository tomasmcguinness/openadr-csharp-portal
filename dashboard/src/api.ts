import type { ForecastResponse, VenStatus } from './types'

export async function fetchStatus(): Promise<VenStatus[]> {
  const response = await fetch('/api/dashboard/status')
  if (!response.ok) {
    throw new Error(`GET /api/dashboard/status failed: ${response.status}`)
  }
  return response.json()
}

export async function fetchForecast(venName: string, date: string): Promise<ForecastResponse> {
  const params = new URLSearchParams({ venName, date })
  const response = await fetch(`/api/dashboard/forecast?${params}`)
  if (!response.ok) {
    throw new Error(`GET /api/dashboard/forecast failed: ${response.status}`)
  }
  return response.json()
}
