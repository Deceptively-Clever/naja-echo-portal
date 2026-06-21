import { useQuery } from '@tanstack/react-query'
import { searchStations } from '../api/stationsApi'
import { stationKeys } from './stationKeys'

export function useStationSearch(search?: string, limit = 25) {
  return useQuery({
    queryKey: stationKeys.search(search, limit),
    queryFn: () => searchStations(search, limit),
    staleTime: 5 * 60 * 1000,
  })
}
