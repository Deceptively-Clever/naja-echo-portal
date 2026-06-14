import { useQuery } from '@tanstack/react-query'
import { getCommodities } from '../api/commoditiesApi'
import { commodityKeys } from './commodityKeys'

export function useCommodities(page = 1, pageSize = 25) {
  return useQuery({
    queryKey: commodityKeys.list(page, pageSize),
    queryFn: () => getCommodities(page, pageSize),
  })
}
