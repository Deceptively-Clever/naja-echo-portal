import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addShip } from '../api/hangarApi'
import { hangarKeys } from './hangarQueryKeys'

export function useAddShip() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (shipId: string) => addShip({ shipId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hangarKeys.mine() })
      queryClient.invalidateQueries({ queryKey: hangarKeys.org() })
      queryClient.invalidateQueries({ queryKey: hangarKeys.catalog() })
    },
  })
}
