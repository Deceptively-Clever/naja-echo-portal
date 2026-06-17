import { useMutation, useQueryClient } from '@tanstack/react-query'
import { removeMaterial } from '../api/materialsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useRemoveMaterial() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: removeMaterial,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.materials() })
    },
  })
}
