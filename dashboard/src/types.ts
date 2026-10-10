export interface VenStatus {
  venName: string
  venId: string
  mqttConnected: boolean
  lastReportTime: string | null
}

export interface HourBucket {
  index: number
  utcStart: string
  label: string
}

export interface ForecastVersion {
  createdAt: string
  isLatest: boolean
  valuesKw: (number | null)[]
}

export interface ForecastResponse {
  date: string
  timeZone: string
  hours: HourBucket[]
  actualKw: (number | null)[]
  forecastVersions: ForecastVersion[]
}

export interface ReportDto {
  id: string
  createdDateTime: string
  clientName: string
  eventId: string
  payloadDescriptors?: { payloadType: string }[] | null
}
