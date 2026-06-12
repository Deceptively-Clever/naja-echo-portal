import { useQuery } from '@tanstack/react-query'
import { getSessionState } from '../api/authApi'
import { authKeys } from './authKeys'

export function useCurrentUser() {
  return useQuery({
    queryKey: authKeys.me(),
    queryFn: getSessionState,
  })
}
