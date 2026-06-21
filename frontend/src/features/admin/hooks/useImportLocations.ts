import { useMutation, useQueryClient } from '@tanstack/react-query'
import { importLocations } from '../api/locationsApi'
import { importLocationsResponseSchema } from '../schemas/locationSchemas'
import type { ImportLocationsResponse } from '../schemas/locationSchemas'

export function useImportLocations() {
  const queryClient = useQueryClient()

  return useMutation<ImportLocationsResponse, Error, void>({
    mutationFn: () => importLocations(),
    onSuccess: (data) => {
      importLocationsResponseSchema.parse(data)
      queryClient.invalidateQueries({ queryKey: ['locations', 'import'] })
    },
  })
}
