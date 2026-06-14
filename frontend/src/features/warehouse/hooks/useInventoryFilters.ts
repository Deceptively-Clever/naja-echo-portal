import { useQuery } from '@tanstack/react-query'
import { getInventoryFilters } from '../api/warehouseApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useInventoryFilters() {
  return useQuery({
    queryKey: warehouseKeys.filters(),
    queryFn: getInventoryFilters,
  })
}
