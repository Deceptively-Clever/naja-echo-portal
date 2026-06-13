import { useMutation, useQueryClient } from '@tanstack/react-query'
import { importShips } from '../api/shipsApi'
import { shipKeys } from './shipKeys'

export function useImportShips() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: importShips,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: shipKeys.lists() })
    },
  })
}
