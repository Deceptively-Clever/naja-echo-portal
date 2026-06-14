import { useQuery } from '@tanstack/react-query'
import { getCategories } from '../api/itemsApi'
import { itemKeys } from './itemKeys'

export function useCategories() {
  return useQuery({
    queryKey: itemKeys.categories(),
    queryFn: getCategories,
  })
}
