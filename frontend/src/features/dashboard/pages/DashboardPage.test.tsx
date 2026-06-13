import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { DashboardPage } from './DashboardPage'
import { createWrapper } from '@/tests/testUtils'

describe('DashboardPage', () => {
  it('renders a welcome heading', async () => {
    render(<DashboardPage />, { wrapper: createWrapper(['/dashboard']) })
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /welcome/i })).toBeDefined()
    })
  })

  it('renders placeholder summary cards', async () => {
    render(<DashboardPage />, { wrapper: createWrapper(['/dashboard']) })
    await waitFor(() => {
      expect(screen.getByText('Org Overview')).toBeDefined()
      expect(screen.getByText('Upcoming Operations')).toBeDefined()
      expect(screen.getByText('Member Activity')).toBeDefined()
      expect(screen.getByText('Getting Started')).toBeDefined()
    })
  })

  it('marks placeholder cards as coming soon', async () => {
    render(<DashboardPage />, { wrapper: createWrapper(['/dashboard']) })
    await waitFor(() => {
      const badges = screen.getAllByText('Coming soon')
      expect(badges.length).toBeGreaterThan(0)
    })
  })
})
