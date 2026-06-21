import { apiFetch } from '@/lib/apiClient'
import type { ImportLocationsResponse } from '../schemas/locationSchemas'

export async function importLocations(): Promise<ImportLocationsResponse> {
  return apiFetch<ImportLocationsResponse>('/api/admin/locations/import', {
    method: 'POST',
  })
}
