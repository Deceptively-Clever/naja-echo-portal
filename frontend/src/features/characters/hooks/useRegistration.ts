import { useQuery } from '@tanstack/react-query'
import { getRegistration } from '../api/charactersApi'
import { characterKeys } from './characterQueryKeys'

export function useRegistration() {
  return useQuery({
    queryKey: characterKeys.registration(),
    queryFn: getRegistration,
  })
}
