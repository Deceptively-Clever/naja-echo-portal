import { useMutation, useQueryClient } from '@tanstack/react-query'
import { refreshCategories } from '../api/itemsApi'
import { itemKeys } from './itemKeys'

export function useRefreshCategories() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: refreshCategories,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: itemKeys.categories() })
    },
  })
}
