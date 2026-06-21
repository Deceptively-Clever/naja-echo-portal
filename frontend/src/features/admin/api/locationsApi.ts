import { apiFetch } from '@/lib/apiClient'
import { importLocationsResponseSchema, type ImportLocationsResponse } from '../schemas/locationSchemas'

export async function importLocations(): Promise<ImportLocationsResponse> {
  const data = await apiFetch<unknown>('/api/admin/locations/import', { method: 'POST' })
  return importLocationsResponseSchema.parse(data)
}
