import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { signOut } from '../api/authApi'

export function useSignOut() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: signOut,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
      navigate('/')
    },
  })
}
