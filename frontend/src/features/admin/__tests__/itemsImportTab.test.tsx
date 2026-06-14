import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RefreshCategoriesButton } from '../components/RefreshCategoriesButton'

const refreshResponse = {
  fetched: 10, inserted: 8, updated: 2, unchanged: 0, failed: 0,
  startedAt: '2026-01-01T00:00:00Z', completedAt: '2026-01-01T00:00:01Z', durationMs: 1000,
}

function renderRefreshButton() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return {
    user: userEvent.setup(),
    ...render(
      <QueryClientProvider client={client}>
        <RefreshCategoriesButton />
      </QueryClientProvider>
    ),
  }
}

describe('RefreshCategoriesButton', () => {
  it('renders Refresh Categories button', () => {
    renderRefreshButton()
    expect(screen.getByRole('button', { name: /refresh categories/i })).toBeDefined()
  })

  it('shows loading state while refreshing', async () => {
    let resolve: () => void = () => {}
    server.use(
      http.post('/api/admin/items/categories/refresh', () =>
        new Promise<Response>((res) => { resolve = () => res(HttpResponse.json(refreshResponse)) })
      )
    )

    const { user } = renderRefreshButton()
    await user.click(screen.getByRole('button', { name: /refresh categories/i }))
    expect(screen.getByRole('button')).toBeDisabled()
    resolve()
  })

  it('shows success summary after refresh', async () => {
    server.use(
      http.post('/api/admin/items/categories/refresh', () => HttpResponse.json(refreshResponse))
    )

    const { user } = renderRefreshButton()
    await user.click(screen.getByRole('button', { name: /refresh categories/i }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('8')
    })
  })

  it('shows error message on failure', async () => {
    server.use(
      http.post('/api/admin/items/categories/refresh', () => new HttpResponse(null, { status: 500 }))
    )

    const { user } = renderRefreshButton()
    await user.click(screen.getByRole('button', { name: /refresh categories/i }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('failed')
    })
  })

  it('shows in-progress message on 409', async () => {
    server.use(
      http.post('/api/admin/items/categories/refresh', () => new HttpResponse(null, { status: 409 }))
    )

    const { user } = renderRefreshButton()
    await user.click(screen.getByRole('button', { name: /refresh categories/i }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('already in progress')
    })
  })
})
