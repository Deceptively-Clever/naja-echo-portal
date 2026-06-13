import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { DashboardNav } from './DashboardNav'
import { navItems } from '../navigation/navItems'

function renderNav(initialEntry = '/dashboard') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <DashboardNav items={navItems} />
    </MemoryRouter>
  )
}

describe('DashboardNav', () => {
  it('renders the dashboard navigation item', () => {
    renderNav()
    expect(screen.getByRole('link', { name: /dashboard/i })).toBeDefined()
  })

  it('marks the active route with aria-current="page"', async () => {
    renderNav('/dashboard')
    await waitFor(() => {
      const dashboardLink = screen.getByRole('link', { name: /dashboard/i })
      expect(dashboardLink.getAttribute('aria-current')).toBe('page')
    })
  })

  it('applies the active indicator class to the active link', async () => {
    renderNav('/dashboard')
    await waitFor(() => {
      const dashboardLink = screen.getByRole('link', { name: /dashboard/i })
      expect(dashboardLink.className).toContain('border-primary')
    })
  })

  it('calls onNavigate when a link is clicked', async () => {
    const onNavigate = vi.fn()
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <DashboardNav items={navItems} onNavigate={onNavigate} />
      </MemoryRouter>
    )
    screen.getByRole('link', { name: /dashboard/i }).click()
    expect(onNavigate).toHaveBeenCalledOnce()
  })
})
