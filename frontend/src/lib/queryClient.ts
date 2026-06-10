import { QueryClient } from '@tanstack/react-query'
import { ApiError } from './apiClient'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 0,
      retry: (count, error) => {
        if (error instanceof ApiError && error.status === 401) return false
        return count < 1
      },
    },
  },
})
