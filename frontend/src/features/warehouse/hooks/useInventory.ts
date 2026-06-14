import { useQuery } from '@tanstack/react-query'
import { getInventory, type InventoryFilters } from '../api/warehouseApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useInventory(filters: InventoryFilters = {}) {
  return useQuery({
    queryKey: warehouseKeys.inventoryList(filters),
    queryFn: () => getInventory(filters),
  })
}
