import { useQuery } from '@tanstack/react-query'
import { searchCommodities } from '../api/materialsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useCommoditySearch(search?: string, enabled = true, limit = 25) {
  return useQuery({
    queryKey: warehouseKeys.materialCatalogSearch(search),
    queryFn: () => searchCommodities(search, limit),
    enabled,
  })
}
