import { useQuery } from '@tanstack/react-query'
import { getCharacters } from '../api/charactersApi'
import { characterKeys } from './characterQueryKeys'

export function useCharacters() {
  return useQuery({
    queryKey: characterKeys.list(),
    queryFn: getCharacters,
  })
}
