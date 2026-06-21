import { useMutation, useQueryClient } from '@tanstack/react-query'
import { importLocations } from '../api/locationsApi'
import { stationKeys } from '@/features/warehouse/hooks/stationKeys'
import type { ImportLocationsResponse } from '../schemas/locationSchemas'

export function useImportLocations() {
  const queryClient = useQueryClient()

  return useMutation<ImportLocationsResponse, Error, void>({
    mutationFn: () => importLocations(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: stationKeys.all })
    },
  })
}
