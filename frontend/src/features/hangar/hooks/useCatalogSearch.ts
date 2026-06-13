import { useQuery } from '@tanstack/react-query'
import { searchCatalog } from '../api/hangarApi'
import { hangarKeys } from './hangarQueryKeys'

export function useCatalogSearch(search?: string, page = 1, pageSize = 25) {
  return useQuery({
    queryKey: hangarKeys.catalogSearch(search, page),
    queryFn: () => searchCatalog({ search, page, pageSize }),
    enabled: Boolean(search && search.trim().length > 0),
  })
}
