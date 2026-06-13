import { useQuery } from '@tanstack/react-query'
import { getOwningMembers } from '../api/hangarApi'
import { hangarKeys } from './hangarQueryKeys'

export function useOwningMembers() {
  return useQuery({
    queryKey: hangarKeys.orgMembers(),
    queryFn: getOwningMembers,
  })
}
