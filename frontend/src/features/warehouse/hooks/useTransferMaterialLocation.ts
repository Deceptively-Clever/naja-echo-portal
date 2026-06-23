import { useMutation, useQueryClient } from '@tanstack/react-query'
import { transferMaterialLocation } from '../api/locationsApi'
import { warehouseKeys } from './warehouseQueryKeys'
import type { LocationType } from '../schemas/locationSchemas'

export function useTransferMaterialLocation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, locationId, locationType }: { id: string; locationId: string; locationType: LocationType }) =>
      transferMaterialLocation(id, locationId, locationType),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.materials() })
    },
  })
}
