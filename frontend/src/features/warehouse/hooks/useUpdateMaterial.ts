import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateMaterial } from '../api/materialsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useUpdateMaterial() {
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
    }) => updateMaterial(id, { ownerUserId, stationId, quantity }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.materials() })
    },
  })
}
