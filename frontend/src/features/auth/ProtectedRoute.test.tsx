import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ProtectedRoute } from './ProtectedRoute'
import { anonymousSession } from '@/tests/handlers'

function renderWithRouter(initialEntry = '/dashboard') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/" element={<div>Landing Page</div>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/dashboard" element={<div>Protected Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('ProtectedRoute', () => {
  // T041: renders protected content when session is authenticated
  it('renders children when session is authenticated', async () => {
    renderWithRouter('/dashboard')
    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeDefined()
    })
  })

  // T042: redirects when session is anonymous (always-200 response)
  it('redirects to / when session is anonymous', async () => {
    server.use(
      http.get('/api/auth/me', () => HttpResponse.json(anonymousSession))
    )
    renderWithRouter('/dashboard')
    await waitFor(() => {
      expect(screen.getByText('Landing Page')).toBeDefined()
    })
  })
})
