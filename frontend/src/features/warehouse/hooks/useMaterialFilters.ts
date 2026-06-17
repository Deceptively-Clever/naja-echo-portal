import { useQuery } from '@tanstack/react-query'
import { getMaterialFilters } from '../api/materialsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useMaterialFilters() {
  return useQuery({
    queryKey: warehouseKeys.materialFilters(),
    queryFn: getMaterialFilters,
  })
}
