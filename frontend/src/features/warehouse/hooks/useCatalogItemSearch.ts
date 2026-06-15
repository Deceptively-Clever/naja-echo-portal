import { useQuery } from '@tanstack/react-query'
import { searchCatalogItems } from '../api/warehouseApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useCatalogItemSearch(search?: string, enabled = true, limit = 25) {
  return useQuery({
    queryKey: warehouseKeys.catalogSearch(search),
    queryFn: () => searchCatalogItems(search, limit),
    enabled,
  })
}
