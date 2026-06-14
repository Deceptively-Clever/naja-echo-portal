import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addInventoryItem } from '../api/warehouseApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useAddInventoryItem() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: addInventoryItem,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.inventory() })
    },
  })
}
