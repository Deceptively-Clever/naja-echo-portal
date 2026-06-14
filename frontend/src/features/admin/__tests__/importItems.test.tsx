import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ItemsImportTab } from '../components/ItemsImportTab'

const categoriesResponse = {
  categories: [
    { uexId: 1, name: 'Armor', section: 'Combat', type: 'item', isGameRelated: true, isMining: false, sourceDateModified: null, localItemCount: 5, lastImportedAt: null },
    { uexId: 2, name: 'Weapons', section: 'Combat', type: 'item', isGameRelated: true, isMining: false, sourceDateModified: null, localItemCount: 10, lastImportedAt: null },
  ],
  lastRefreshedAt: '2026-01-01T00:00:00Z',
}

const importResult = {
  status: 'Success',
  categoriesProcessed: 1,
  categoriesSucceeded: 1,
  categoriesFailed: 0,
  itemsFetched: 5,
  itemsInserted: 3,
  itemsUpdated: 2,
  itemsUnchanged: 0,
  itemsSkippedNoUuid: 0,
  itemsSoftDeleted: 0,
  itemsFailed: 0,
  startedAt: '2026-01-01T00:00:00Z',
  completedAt: '2026-01-01T00:00:01Z',
  durationMs: 1000,
  errors: [],
}

function renderTab() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  server.use(
    http.get('/api/admin/items/categories', () => HttpResponse.json(categoriesResponse))
  )
  return {
    user: userEvent.setup(),
    ...render(
      <QueryClientProvider client={client}>
        <ItemsImportTab />
      </QueryClientProvider>
    ),
  }
}

function renderEmptyTab() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  server.use(
    http.get('/api/admin/items/categories', () => HttpResponse.json({ categories: [], lastRefreshedAt: null }))
  )
  return {
    user: userEvent.setup(),
    ...render(
      <QueryClientProvider client={client}>
        <ItemsImportTab />
      </QueryClientProvider>
    ),
  }
}

describe('ItemsImportTab', () => {
  it('shows empty state when no categories available', async () => {
    renderEmptyTab()
    await waitFor(() => {
      expect(screen.getByText(/no eligible categories/i)).toBeDefined()
    })
  })

  it('shows Import All button when categories exist', async () => {
    renderTab()
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /import all/i })).toBeDefined()
    })
  })

  it('sends import request when button clicked', async () => {
    server.use(http.post('/api/admin/items/import', () => HttpResponse.json(importResult)))
    const { user } = renderTab()
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /import all/i })).toBeDefined()
    })

    await user.click(screen.getByRole('button', { name: /import all/i }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('3 added')
    })
  })

  it('shows concurrency message on 409', async () => {
    server.use(http.post('/api/admin/items/import', () => new HttpResponse(null, { status: 409 })))
    const { user } = renderTab()
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /import all/i })).toBeDefined()
    })

    await user.click(screen.getByRole('button', { name: /import all/i }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('already in progress')
    })
  })

  it('shows error on failure', async () => {
    server.use(http.post('/api/admin/items/import', () => new HttpResponse(null, { status: 500 })))
    const { user } = renderTab()
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /import all/i })).toBeDefined()
    })

    await user.click(screen.getByRole('button', { name: /import all/i }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('failed')
    })
  })
})
