import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateMaterial } from '../api/materialsApi'
import { warehouseKeys } from './warehouseQueryKeys'
import type { LocationType } from '../schemas/locationSchemas'

export function useUpdateMaterial() {
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
    }) => updateMaterial(id, { ownerUserId, locationId, locationType, quantity }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.materials() })
    },
  })
}
