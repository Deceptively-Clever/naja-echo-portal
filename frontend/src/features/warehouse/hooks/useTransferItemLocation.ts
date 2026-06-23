import { useMutation, useQueryClient } from '@tanstack/react-query'
import { transferItemLocation } from '../api/locationsApi'
import { warehouseKeys } from './warehouseQueryKeys'
import type { LocationType } from '../schemas/locationSchemas'

export function useTransferItemLocation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, locationId, locationType }: { id: string; locationId: string; locationType: LocationType }) =>
      transferItemLocation(id, locationId, locationType),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.inventory() })
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.shipComponents() })
    },
  })
}
