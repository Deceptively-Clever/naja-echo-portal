import { useQuery } from '@tanstack/react-query'
import { searchSystemsCatalog } from '../api/shipComponentsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useSystemsCatalogSearch(search?: string, enabled = true) {
  return useQuery({
    queryKey: [...warehouseKeys.shipComponents(), 'catalog', { search }],
    queryFn: () => searchSystemsCatalog(search),
    enabled,
  })
}
