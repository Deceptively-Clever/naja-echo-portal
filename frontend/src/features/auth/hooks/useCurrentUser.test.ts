import { renderHook, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { useCurrentUser } from './useCurrentUser'
import { createWrapper } from '@/tests/testUtils'

describe('useCurrentUser', () => {
  it('returns user data when /api/auth/me responds 200', async () => {
    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data).toMatchObject({
      displayName: 'Test User',
    })
  })

  it('returns undefined and does not retry on 401', async () => {
    server.use(
      http.get('/api/auth/me', () => new HttpResponse(null, { status: 401 }))
    )

    const { result } = renderHook(() => useCurrentUser(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isError).toBe(true))

    expect(result.current.data).toBeUndefined()
    expect(result.current.failureCount).toBe(1)
  })
})
