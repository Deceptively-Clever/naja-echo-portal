import { useMutation, useQueryClient } from '@tanstack/react-query'
import { verifyCharacter } from '../api/charactersApi'
import { ApiError } from '@/lib/apiClient'
import { characterKeys } from './characterQueryKeys'

function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.status) {
      case 422: return 'Token not found on your RSI profile'
      case 409: return error.message.includes('expired')
        ? 'Token expired — please start a new registration'
        : 'This handle is already claimed'
      case 404: return 'RSI citizen profile not found for that handle'
      case 502: return 'Could not reach RSI — please try again shortly'
      default: return 'An unexpected error occurred'
    }
  }
  return 'An unexpected error occurred'
}

export function useVerifyCharacter() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (handle: string) => verifyCharacter(handle),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: characterKeys.list() })
      void queryClient.invalidateQueries({ queryKey: characterKeys.registration() })
    },
    meta: { getErrorMessage },
  })
}

export { getErrorMessage }
