import { apiFetch } from '@/lib/apiClient'
import { locationListResponseSchema, type LocationOption, type LocationType } from '../schemas/locationSchemas'

export async function getLocations(search?: string, limit = 25): Promise<LocationOption[]> {
  const params = new URLSearchParams()
  if (search) {
    params.append('search', search)
  }
  params.append('limit', String(limit))

  const data = await apiFetch<unknown>(`/api/warehouse/locations?${params.toString()}`)
  return locationListResponseSchema.parse(data).locations
}

export async function transferItemLocation(id: string, locationId: string, locationType: LocationType): Promise<void> {
  await apiFetch(`/api/warehouse/items/${id}/location`, {
    method: 'PUT',
    body: JSON.stringify({ locationId, locationType }),
  })
}

export async function transferMaterialLocation(id: string, locationId: string, locationType: LocationType): Promise<void> {
  await apiFetch(`/api/warehouse/materials/${id}/location`, {
    method: 'PUT',
    body: JSON.stringify({ locationId, locationType }),
  })
}
