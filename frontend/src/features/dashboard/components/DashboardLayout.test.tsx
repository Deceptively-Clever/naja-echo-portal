import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from '@/features/theme/ThemeProvider'
import { DashboardLayout } from './DashboardLayout'

function renderWithShell(initialEntry = '/dashboard', childContent = 'Child Page') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <ThemeProvider>
        <MemoryRouter initialEntries={[initialEntry]}>
          <Routes>
            <Route element={<DashboardLayout />}>
              <Route path="/dashboard" element={<div>{childContent}</div>} />
              <Route path="/dashboard/profile" element={<div>Profile Page</div>} />
              <Route path="/dashboard/settings" element={<div>Settings Page</div>} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ThemeProvider>
    </QueryClientProvider>
  )
}

describe('DashboardLayout', () => {
  it('renders header landmark', async () => {
    renderWithShell()
    await waitFor(() => {
      expect(screen.getByRole('banner')).toBeDefined()
    })
  })

  it('renders navigation landmark', async () => {
    renderWithShell()
    await waitFor(() => {
      expect(screen.getByRole('navigation', { name: /primary navigation/i })).toBeDefined()
    })
  })

  it('renders main content landmark', async () => {
    renderWithShell()
    await waitFor(() => {
      expect(screen.getByRole('main')).toBeDefined()
    })
  })

  it('renders child route content inside the shell', async () => {
    renderWithShell('/dashboard', 'Dashboard Home Content')
    await waitFor(() => {
      expect(screen.getByText('Dashboard Home Content')).toBeDefined()
    })
  })

  it('renders the dashboard navigation item', async () => {
    renderWithShell()
    await waitFor(() => {
      expect(screen.getByRole('link', { name: /dashboard/i })).toBeDefined()
    })
  })
})
