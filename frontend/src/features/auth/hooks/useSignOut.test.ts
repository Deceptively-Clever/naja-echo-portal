import { renderHook, act, waitFor } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { useSignOut } from './useSignOut'
import { createWrapper } from '@/tests/testUtils'

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => mockNavigate }
})

describe('useSignOut', () => {
  it('calls sign-out endpoint and navigates to / on success', async () => {
    const { result } = renderHook(() => useSignOut(), { wrapper: createWrapper() })

    act(() => { result.current.mutate() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockNavigate).toHaveBeenCalledWith('/')
  })
})
