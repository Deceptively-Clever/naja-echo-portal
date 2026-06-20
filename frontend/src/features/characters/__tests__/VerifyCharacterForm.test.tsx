import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { VerifyCharacterForm } from '../components/VerifyCharacterForm'

describe('VerifyCharacterForm', () => {
  it('submits handle and shows success message on 201', async () => {
    server.use(
      http.post('/api/characters/verify', () =>
        HttpResponse.json(
          { id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', name: 'G8trdone', handle: 'g8r', createdAt: new Date().toISOString() },
          { status: 201 }
        )
      )
    )

    render(<VerifyCharacterForm />, { wrapper: createWrapper() })
    await userEvent.type(screen.getByLabelText(/rsi handle/i), 'g8r')
    await userEvent.click(screen.getByRole('button', { name: /verify/i }))

    await waitFor(() => {
      expect(screen.getByRole('status')).toBeDefined()
    })
  })

  it('shows token-not-found error on 422', async () => {
    server.use(
      http.post('/api/characters/verify', () =>
        HttpResponse.json({ title: 'Token not found on your RSI profile' }, { status: 422 })
      )
    )

    render(<VerifyCharacterForm />, { wrapper: createWrapper() })
    await userEvent.type(screen.getByLabelText(/rsi handle/i), 'g8r')
    await userEvent.click(screen.getByRole('button', { name: /verify/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert').textContent).toContain('Token not found')
    })
  })

  it('shows token-expired error on 409 expired', async () => {
    server.use(
      http.post('/api/characters/verify', () =>
        HttpResponse.json({ title: 'Token expired — please start a new registration' }, { status: 409 })
      )
    )

    render(<VerifyCharacterForm />, { wrapper: createWrapper() })
    await userEvent.type(screen.getByLabelText(/rsi handle/i), 'g8r')
    await userEvent.click(screen.getByRole('button', { name: /verify/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert').textContent).toContain('Token expired')
    })
  })

  it('shows handle-claimed error on 409 claimed', async () => {
    server.use(
      http.post('/api/characters/verify', () =>
        HttpResponse.json({ title: 'This handle is already claimed' }, { status: 409 })
      )
    )

    render(<VerifyCharacterForm />, { wrapper: createWrapper() })
    await userEvent.type(screen.getByLabelText(/rsi handle/i), 'handle123')
    await userEvent.click(screen.getByRole('button', { name: /verify/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert').textContent).toContain('already claimed')
    })
  })

  it('shows rsi profile not found error on 404', async () => {
    server.use(
      http.post('/api/characters/verify', () =>
        HttpResponse.json({ title: 'RSI citizen profile not found for that handle' }, { status: 404 })
      )
    )

    render(<VerifyCharacterForm />, { wrapper: createWrapper() })
    await userEvent.type(screen.getByLabelText(/rsi handle/i), 'unknown')
    await userEvent.click(screen.getByRole('button', { name: /verify/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert').textContent).toContain('not found')
    })
  })

  it('shows rsi unreachable error on 502', async () => {
    server.use(
      http.post('/api/characters/verify', () =>
        HttpResponse.json({ title: 'Could not reach RSI' }, { status: 502 })
      )
    )

    render(<VerifyCharacterForm />, { wrapper: createWrapper() })
    await userEvent.type(screen.getByLabelText(/rsi handle/i), 'g8r')
    await userEvent.click(screen.getByRole('button', { name: /verify/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert').textContent).toContain('Could not reach RSI')
    })
  })
})
