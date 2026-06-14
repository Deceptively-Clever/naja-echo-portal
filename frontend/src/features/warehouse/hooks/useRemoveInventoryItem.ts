import { useMutation, useQueryClient } from '@tanstack/react-query'
import { removeInventoryItem } from '../api/warehouseApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useRemoveInventoryItem() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: removeInventoryItem,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.inventory() })
    },
  })
}
