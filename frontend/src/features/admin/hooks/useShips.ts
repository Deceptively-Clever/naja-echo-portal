import { useQuery } from '@tanstack/react-query'
import { getShips } from '../api/shipsApi'
import { shipKeys } from './shipKeys'

export function useShips(page = 1, pageSize = 25) {
  return useQuery({
    queryKey: shipKeys.list(page, pageSize),
    queryFn: () => getShips(page, pageSize),
  })
}
