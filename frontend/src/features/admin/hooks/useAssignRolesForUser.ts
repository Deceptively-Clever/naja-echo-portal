import { useMutation, useQueryClient } from '@tanstack/react-query'
import { assignRolesForUser } from '../api/usersApi'
import { userKeys } from './userKeys'

export function useAssignRolesForUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, roles }: { userId: string; roles: string[] }) =>
      assignRolesForUser(userId, roles),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.adminUsers.list() })
    },
  })
}
