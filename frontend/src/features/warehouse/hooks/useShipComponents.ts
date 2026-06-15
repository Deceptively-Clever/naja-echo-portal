import { useQuery } from '@tanstack/react-query'
import { getShipComponents, type ShipComponentFilters } from '../api/shipComponentsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useShipComponents(filters: ShipComponentFilters = {}) {
  return useQuery({
    queryKey: warehouseKeys.shipComponentsList(filters),
    queryFn: () => getShipComponents(filters),
  })
}
