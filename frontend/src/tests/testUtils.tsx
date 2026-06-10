import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import type { ReactNode } from 'react'

export function createWrapper(initialEntries = ['/']) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: 0,
        gcTime: 0,
      },
    },
  })

  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={initialEntries}>{children}</MemoryRouter>
      </QueryClientProvider>
    )
  }
}
