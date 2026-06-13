import { useQuery } from '@tanstack/react-query'
import { getShipById } from '../api/shipsApi'
import { shipKeys } from './shipKeys'

export function useShipDetail(id: string | null) {
  return useQuery({
    queryKey: shipKeys.detail(id ?? ''),
    queryFn: () => getShipById(id!),
    enabled: id !== null,
  })
}
