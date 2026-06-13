import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { LandingPage } from './LandingPage'
import { createWrapper } from '@/tests/testUtils'

describe('LandingPage', () => {
  it('renders application heading', () => {
    server.use(http.get('/api/auth/me', () => new HttpResponse(null, { status: 401 })))
    render(<LandingPage />, { wrapper: createWrapper() })
    expect(screen.getByText('Welcome to Naja Echó!')).toBeDefined()
  })

  it('renders Sign in with Discord button linking to /api/auth/discord/login', () => {
    server.use(http.get('/api/auth/me', () => new HttpResponse(null, { status: 401 })))
    render(<LandingPage />, { wrapper: createWrapper() })
    const link = screen.getByRole('link', { name: /sign in with discord/i })
    expect(link).toBeDefined()
    expect(link.getAttribute('href')).toBe('/api/auth/discord/login')
  })
})
