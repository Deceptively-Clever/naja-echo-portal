import { apiFetch } from '@/lib/apiClient'
import type { StationOption, StationListResponse } from '../schemas/stationSchemas'

export async function searchStations(search?: string, limit = 25): Promise<StationOption[]> {
  const params = new URLSearchParams()
  if (search) {
    params.append('search', search)
  }
  params.append('limit', String(limit))

  const data = await apiFetch<StationListResponse>(
    `/api/warehouse/stations?${params.toString()}`,
  )
  return data.stations
}

export async function transferItemStation(id: string, stationId: string): Promise<void> {
  await apiFetch(`/api/warehouse/items/${id}/station`, {
    method: 'PUT',
    body: JSON.stringify({ stationId }),
  })
}

export async function transferMaterialStation(id: string, stationId: string): Promise<void> {
  await apiFetch(`/api/warehouse/materials/${id}/station`, {
    method: 'PUT',
    body: JSON.stringify({ stationId }),
  })
}
