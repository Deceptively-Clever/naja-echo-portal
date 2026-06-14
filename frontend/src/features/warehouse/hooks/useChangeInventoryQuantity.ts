import { useMutation, useQueryClient } from '@tanstack/react-query'
import { changeInventoryQuantity } from '../api/warehouseApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useChangeInventoryQuantity() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, quantity }: { id: string; quantity: number }) =>
      changeInventoryQuantity(id, quantity),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.inventory() })
    },
  })
}
