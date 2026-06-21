import { useMutation, useQueryClient } from '@tanstack/react-query'
import { transferMaterialStation } from '../api/stationsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useTransferMaterialStation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, stationId }: { id: string; stationId: string }) =>
      transferMaterialStation(id, stationId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.materials() })
    },
  })
}
