import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { RegistrationTokenCard } from '../components/RegistrationTokenCard'

const futureExpiry = new Date(Date.now() + 25 * 60 * 1000).toISOString()
const pastExpiry = new Date(Date.now() - 5 * 60 * 1000).toISOString()

const mockRegistration = {
  token: 'naja-abc123testtoken',
  expiresAt: futureExpiry,
}

describe('RegistrationTokenCard', () => {
  beforeEach(() => {
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText: vi.fn().mockResolvedValue(undefined) },
      configurable: true,
    })
  })

  it('renders the token value', () => {
    render(<RegistrationTokenCard registration={mockRegistration} />, { wrapper: createWrapper() })
    expect(screen.getByText('naja-abc123testtoken')).toBeDefined()
  })

  it('renders a copy-to-clipboard button', () => {
    render(<RegistrationTokenCard registration={mockRegistration} />, { wrapper: createWrapper() })
    expect(screen.getByRole('button', { name: /copy token to clipboard/i })).toBeDefined()
  })

  it('displays countdown with remaining time for non-expired token', () => {
    render(<RegistrationTokenCard registration={mockRegistration} />, { wrapper: createWrapper() })
    // Should show a minutes:seconds badge
    const badge = screen.getByText(/\d+:\d{2}/)
    expect(badge).toBeDefined()
  })

  it('shows expired badge when token is expired', () => {
    render(
      <RegistrationTokenCard registration={{ token: 'naja-old', expiresAt: pastExpiry }} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByText('Expired')).toBeDefined()
  })
})
