import { useQuery } from '@tanstack/react-query'
import { getMaterials, type MaterialFilters } from '../api/materialsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useMaterials(filters: MaterialFilters = {}) {
  return useQuery({
    queryKey: warehouseKeys.materialsList(filters),
    queryFn: () => getMaterials(filters),
  })
}
