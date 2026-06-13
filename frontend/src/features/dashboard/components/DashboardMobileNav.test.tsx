import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { DashboardMobileNav } from './DashboardMobileNav'
import { navItems } from '../navigation/navItems'

function renderMobileNav(open: boolean, onOpenChange = vi.fn()) {
  return render(
    <MemoryRouter initialEntries={['/dashboard']}>
      <DashboardMobileNav items={navItems} open={open} onOpenChange={onOpenChange} />
    </MemoryRouter>
  )
}

describe('DashboardMobileNav', () => {
  it('does not show nav items when closed', () => {
    renderMobileNav(false)
    expect(screen.queryByRole('link', { name: /dashboard/i })).toBeNull()
  })

  it('shows nav items when open', async () => {
    renderMobileNav(true)
    await waitFor(() => {
      expect(screen.getByRole('link', { name: /dashboard/i })).toBeDefined()
    })
  })

  it('calls onOpenChange(false) when a nav item is clicked', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()
    renderMobileNav(true, onOpenChange)

    await waitFor(() => screen.getByRole('link', { name: /dashboard/i }))
    await user.click(screen.getByRole('link', { name: /dashboard/i }))
    expect(onOpenChange).toHaveBeenCalledWith(false)
  })

  it('shows "Navigation" as the sheet title', async () => {
    renderMobileNav(true)
    await waitFor(() => {
      expect(screen.getByText('Navigation')).toBeDefined()
    })
  })
})
