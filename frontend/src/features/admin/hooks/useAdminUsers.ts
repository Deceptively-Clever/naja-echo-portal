import { useQuery } from '@tanstack/react-query'
import { getAdminUsers } from '../api/usersApi'
import { userKeys } from './userKeys'

export function useAdminUsers() {
  return useQuery({
    queryKey: userKeys.adminUsers.list(),
    queryFn: getAdminUsers,
  })
}
