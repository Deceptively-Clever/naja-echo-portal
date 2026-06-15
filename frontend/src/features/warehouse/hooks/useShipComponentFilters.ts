import { useQuery } from '@tanstack/react-query'
import { getShipComponentFilters } from '../api/shipComponentsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useShipComponentFilters() {
  return useQuery({
    queryKey: [...warehouseKeys.shipComponents(), 'filters'],
    queryFn: getShipComponentFilters,
  })
}
