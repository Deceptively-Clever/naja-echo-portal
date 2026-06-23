import { useQuery } from '@tanstack/react-query'
import { getLocations } from '../api/locationsApi'
import { locationKeys } from './locationKeys'

export function useLocationSearch(search?: string, limit = 25) {
  return useQuery({
    queryKey: locationKeys.search(search, limit),
    queryFn: () => getLocations(search, limit),
    staleTime: 5 * 60 * 1000,
  })
}
