import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { AuthErrorPage } from './AuthErrorPage'
import { createWrapper } from '@/tests/testUtils'

describe('AuthErrorPage', () => {
  it('renders user-friendly error heading', () => {
    render(<AuthErrorPage />, { wrapper: createWrapper() })
    expect(screen.getByText(/sign-in could not be completed/i)).toBeDefined()
  })

  it('renders Try again link pointing to /', () => {
    render(<AuthErrorPage />, { wrapper: createWrapper() })
    const link = screen.getByRole('link', { name: /try again/i })
    expect(link).toBeDefined()
    expect(link.getAttribute('href')).toBe('/')
  })

  it('does not expose internal error details', () => {
    render(<AuthErrorPage />, { wrapper: createWrapper() })
    expect(screen.queryByText(/stack trace/i)).toBeNull()
    expect(screen.queryByText(/exception/i)).toBeNull()
    expect(screen.queryByText(/access_token/i)).toBeNull()
  })
})
