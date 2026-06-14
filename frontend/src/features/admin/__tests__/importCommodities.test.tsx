import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ImportCommoditiesButton } from '../components/ImportCommoditiesButton'

const now = new Date().toISOString()
const importResult = {
  fetched: 10, skipped: 0, inserted: 8, updated: 2, restored: 0, softDeleted: 1,
  startedAt: now, completedAt: now, durationMs: 250, warning: null,
}

const emptyFeedResult = {
  fetched: 0, skipped: 0, inserted: 0, updated: 0, restored: 0, softDeleted: 0,
  startedAt: now, completedAt: now, durationMs: 5,
  warning: 'Feed returned zero records; no changes applied.',
}

function renderButton() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return {
    user: userEvent.setup(),
    ...render(
      <QueryClientProvider client={client}>
        <ImportCommoditiesButton />
      </QueryClientProvider>
    ),
  }
}

describe('ImportCommoditiesButton', () => {
  it('renders Import Commodities button', () => {
    renderButton()
    expect(screen.getByRole('button', { name: 'Import Commodities' })).toBeDefined()
  })

  it('shows loading state during import', async () => {
    let resolve: () => void = () => {}
    server.use(
      http.post('/api/admin/commodities/import', () =>
        new Promise<Response>((res) => { resolve = () => res(HttpResponse.json(importResult)) })
      )
    )

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Commodities' }))
    expect(screen.getByRole('button', { name: 'Importing…' })).toBeDefined()
    expect(screen.getByRole('button')).toBeDisabled()
    resolve()
  })

  it('shows success message with counts after import', async () => {
    server.use(http.post('/api/admin/commodities/import', () => HttpResponse.json(importResult)))

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Commodities' }))

    await waitFor(() => {
      const status = screen.getByRole('status').textContent ?? ''
      expect(status).toContain('8 added')
      expect(status).toContain('2 updated')
    })
  })

  it('shows warning message when feed returns zero records', async () => {
    server.use(http.post('/api/admin/commodities/import', () => HttpResponse.json(emptyFeedResult, { status: 202 })))

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Commodities' }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('zero records')
    })
  })

  it('shows error message on import failure', async () => {
    server.use(http.post('/api/admin/commodities/import', () => new HttpResponse(null, { status: 500 })))

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Commodities' }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('Import failed')
    })
  })

  it('shows in-progress message on 409', async () => {
    server.use(http.post('/api/admin/commodities/import', () => new HttpResponse(null, { status: 409 })))

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Commodities' }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('already in progress')
    })
  })
})
