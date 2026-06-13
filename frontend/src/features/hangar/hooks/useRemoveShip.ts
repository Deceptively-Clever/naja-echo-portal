import { useMutation, useQueryClient } from '@tanstack/react-query'
import { removeShip } from '../api/hangarApi'
import { hangarKeys } from './hangarQueryKeys'

export function useRemoveShip() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (shipId: string) => removeShip(shipId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hangarKeys.mine() })
      queryClient.invalidateQueries({ queryKey: hangarKeys.org() })
    },
  })
}
