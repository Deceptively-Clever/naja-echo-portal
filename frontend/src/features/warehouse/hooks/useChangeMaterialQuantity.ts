import { useMutation, useQueryClient } from '@tanstack/react-query'
import { changeMaterialQuantity } from '../api/materialsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useChangeMaterialQuantity() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, quantity }: { id: string; quantity: number }) =>
      changeMaterialQuantity(id, quantity),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.materials() })
    },
  })
}
