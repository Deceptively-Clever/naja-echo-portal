import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateInventoryItem } from '../api/warehouseApi'
import { warehouseKeys } from './warehouseQueryKeys'
import type { LocationType } from '../schemas/locationSchemas'

export function useUpdateInventoryItem() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      id,
      ownerUserId,
      locationId,
      locationType,
      quantity,
    }: {
      id: string
      ownerUserId: string
      locationId: string
      locationType: LocationType
      quantity: number
    }) => updateInventoryItem(id, { ownerUserId, locationId, locationType, quantity }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.inventory() })
    },
  })
}
