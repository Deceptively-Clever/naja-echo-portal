import { useMutation, useQueryClient } from '@tanstack/react-query'
import { transferItemStation } from '../api/stationsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useTransferItemStation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, stationId }: { id: string; stationId: string }) =>
      transferItemStation(id, stationId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.inventory() })
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.shipComponents() })
    },
  })
}
