import { renderHook, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { anonymousSession } from '@/tests/handlers'
import { useCurrentUser } from './useCurrentUser'
import { createWrapper } from '@/tests/testUtils'

describe('useCurrentUser', () => {
  // T027: authenticated session state
  it('returns authenticated session when signed in', async () => {
    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data?.authenticated).toBe(true)
    if (result.current.data?.authenticated) {
      expect(result.current.data.user.displayName).toBe('Test User')
      expect(result.current.data.user.discordUsername).toBe('testuser')
    }
  })

  // T028: unauthenticated session state — /api/auth/me always returns 200
  it('returns anonymous session when not signed in', async () => {
    server.use(
      http.get('/api/auth/me', () => HttpResponse.json(anonymousSession))
    )

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data?.authenticated).toBe(false)
  })
})
