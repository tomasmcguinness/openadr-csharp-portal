import * as signalR from '@microsoft/signalr'
import { useEffect, useRef } from 'react'
import type { ReportDto } from './types'

interface Handlers {
  onReportReceived: (report: ReportDto) => void
  onVenConnectionChanged: (venName: string, mqttConnected: boolean) => void
}

/** Connects to DashboardHub once and keeps the latest handlers wired up, so the chart and
 * status panel update live as soon as a report arrives or a VEN connects/disconnects. */
export function useDashboardHub(handlers: Handlers) {
  const handlersRef = useRef(handlers)
  handlersRef.current = handlers

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/dashboard')
      .withAutomaticReconnect()
      .build()

    connection.on('ReportReceived', (report: ReportDto) => handlersRef.current.onReportReceived(report))
    connection.on('VenConnectionChanged', (venName: string, mqttConnected: boolean) =>
      handlersRef.current.onVenConnectionChanged(venName, mqttConnected),
    )

    connection.start().catch((error) => console.error('SignalR connection failed:', error))

    return () => {
      connection.stop()
    }
  }, [])
}
