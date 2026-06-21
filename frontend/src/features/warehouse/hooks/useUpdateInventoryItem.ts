import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateInventoryItem } from '../api/warehouseApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useUpdateInventoryItem() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      id,
      ownerUserId,
      stationId,
      quantity,
    }: {
      id: string
      ownerUserId: string
      stationId: string
      quantity: number
    }) => updateInventoryItem(id, { ownerUserId, stationId, quantity }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.inventory() })
    },
  })
}
