import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addMaterial } from '../api/materialsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useAddMaterial() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: addMaterial,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.materials() })
    },
  })
}
