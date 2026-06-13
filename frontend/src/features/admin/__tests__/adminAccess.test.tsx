import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AdminRoute } from '@/features/auth/AdminRoute'
import { navItems } from '@/features/dashboard/navigation/navItems'
import { DashboardNav } from '@/features/dashboard/components/DashboardNav'

const adminSession = {
  authenticated: true as const,
  user: {
    id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
    displayName: 'Admin User',
    discordUsername: 'adminuser',
    roles: ['Admin'],
  },
}

const regularSession = {
  authenticated: true as const,
  user: {
    id: 'b1ffcd00-0d1c-4ef8-bb6d-6bb9bd380a22',
    displayName: 'Regular User',
    discordUsername: 'regularuser',
    roles: [],
  },
}

function renderAdminRoute(session: typeof adminSession | typeof regularSession) {
  server.use(http.get('/api/auth/me', () => HttpResponse.json(session)))
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/dashboard" element={<div>Dashboard</div>} />
          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<div>Admin Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('AdminRoute', () => {
  it('allows admin user to access protected route', async () => {
    renderAdminRoute(adminSession)
    await waitFor(() => {
      expect(screen.getByText('Admin Content')).toBeDefined()
    })
  })

  it('redirects non-admin user to dashboard', async () => {
    renderAdminRoute(regularSession)
    await waitFor(() => {
      expect(screen.getByText('Dashboard')).toBeDefined()
      expect(screen.queryByText('Admin Content')).toBeNull()
    })
  })
})

describe('DashboardNav role gating', () => {
  it('hides admin items for non-admin users', () => {
    render(
      <MemoryRouter>
        <DashboardNav items={navItems} roles={[]} />
      </MemoryRouter>
    )
    expect(screen.queryByText('Data Import')).toBeNull()
  })

  it('shows admin items for admin users', () => {
    render(
      <MemoryRouter>
        <DashboardNav items={navItems} roles={['Admin']} />
      </MemoryRouter>
    )
    expect(screen.getByText('Data Import')).toBeDefined()
  })

  it('shows Admin group heading for admin users', () => {
    render(
      <MemoryRouter>
        <DashboardNav items={navItems} roles={['Admin']} />
      </MemoryRouter>
    )
    expect(screen.getByText('Admin')).toBeDefined()
  })
})
