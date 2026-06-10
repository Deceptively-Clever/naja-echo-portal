import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ProtectedRoute } from './ProtectedRoute'

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
  it('renders children when user is authenticated', async () => {
    renderWithRouter('/dashboard')
    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeDefined()
    })
  })

  it('redirects to / when user is not authenticated', async () => {
    server.use(
      http.get('/api/auth/me', () => new HttpResponse(null, { status: 401 }))
    )
    renderWithRouter('/dashboard')
    await waitFor(() => {
      expect(screen.getByText('Landing Page')).toBeDefined()
    })
  })
})
