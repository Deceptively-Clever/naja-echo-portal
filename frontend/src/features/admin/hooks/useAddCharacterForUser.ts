import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addCharacterForUser } from '../api/usersApi'
import { userKeys } from './userKeys'

export function useAddCharacterForUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, handle }: { userId: string; handle: string }) =>
      addCharacterForUser(userId, handle),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.adminUsers.list() })
    },
  })
}
