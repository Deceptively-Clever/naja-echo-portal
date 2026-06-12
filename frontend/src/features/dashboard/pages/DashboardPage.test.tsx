import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { DashboardPage } from './DashboardPage'
import { createWrapper } from '@/tests/testUtils'

describe('DashboardPage', () => {
  it('displays displayName from current user', async () => {
    render(<DashboardPage />, { wrapper: createWrapper(['/dashboard']) })
    await waitFor(() => {
      expect(screen.getByText('Test User')).toBeDefined()
    })
  })

  it('renders user badge with accessible label', async () => {
    render(<DashboardPage />, { wrapper: createWrapper(['/dashboard']) })
    await waitFor(() => {
      const badge = screen.getByLabelText('Signed in as Test User')
      expect(badge).not.toBeNull()
    })
  })

  it('shows display name initial in fallback avatar', async () => {
    server.use(
      http.get('/api/auth/me', () =>
        HttpResponse.json({
          authenticated: true,
          user: {
            id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
            displayName: 'No Avatar User',
            discordUsername: 'noavatar',
          },
        })
      )
    )
    render(<DashboardPage />, { wrapper: createWrapper(['/dashboard']) })
    await waitFor(() => {
      expect(screen.getByText('N')).toBeDefined()
    })
  })

  it('renders sign out button', async () => {
    render(<DashboardPage />, { wrapper: createWrapper(['/dashboard']) })
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /sign out/i })).toBeDefined()
    })
  })
})
