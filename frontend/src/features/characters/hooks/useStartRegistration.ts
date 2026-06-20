import { useMutation, useQueryClient } from '@tanstack/react-query'
import { startRegistration } from '../api/charactersApi'
import { characterKeys } from './characterQueryKeys'

export function useStartRegistration() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: startRegistration,
    onSuccess: (data) => {
      queryClient.setQueryData(characterKeys.registration(), data)
    },
  })
}
