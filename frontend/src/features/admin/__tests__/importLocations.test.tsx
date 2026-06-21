import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { LocationsImportTab } from '../components/LocationsImportTab'

const successResponse = {
  starSystems: { added: 3, updated: 1, reactivated: 0, softDeleted: 2, total: 4 },
  spaceStations: { added: 10, updated: 5, reactivated: 1, softDeleted: 0, skipped: 2, total: 15 },
}

function renderTab() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return {
    user: userEvent.setup(),
    ...render(
      <QueryClientProvider client={client}>
        <LocationsImportTab />
      </QueryClientProvider>
    ),
  }
}

describe('LocationsImportTab', () => {
  it('renders import button', () => {
    renderTab()
    expect(screen.getByRole('button', { name: 'Import Locations' })).toBeDefined()
  })

  it('shows loading state while importing', async () => {
    let resolve: () => void = () => {}
    server.use(
      http.post('/api/admin/locations/import', () =>
        new Promise<Response>((res) => {
          resolve = () => res(HttpResponse.json(successResponse))
        })
      )
    )
    const { user } = renderTab()
    await user.click(screen.getByRole('button', { name: 'Import Locations' }))
    expect(screen.getByRole('button', { name: 'Importing…' })).toBeDefined()
    expect(screen.getByRole('button')).toBeDisabled()
    resolve()
  })

  it('renders star systems and space stations summary on success', async () => {
    server.use(http.post('/api/admin/locations/import', () => HttpResponse.json(successResponse)))
    const { user } = renderTab()
    await user.click(screen.getByRole('button', { name: 'Import Locations' }))

    await waitFor(() => {
      expect(screen.getByText('Star Systems')).toBeDefined()
      expect(screen.getByText('Space Stations')).toBeDefined()
    })
    // skipped count appears for space stations
    expect(screen.getByText('Skipped:')).toBeDefined()
    // success message
    expect(screen.getByText(/import completed successfully/i)).toBeDefined()
  })

  it('renders error message on generic API failure', async () => {
    server.use(
      http.post('/api/admin/locations/import', () => new HttpResponse(null, { status: 500 }))
    )
    const { user } = renderTab()
    await user.click(screen.getByRole('button', { name: 'Import Locations' }))

    await waitFor(() => {
      expect(screen.getByText(/import failed/i)).toBeDefined()
    })
  })

  it('renders error detail on 502 empty-source error', async () => {
    server.use(
      http.post('/api/admin/locations/import', () =>
        HttpResponse.json(
          { title: 'Source returned empty data', detail: 'The UEX source returned empty data for star systems.' },
          { status: 502 }
        )
      )
    )
    const { user } = renderTab()
    await user.click(screen.getByRole('button', { name: 'Import Locations' }))

    await waitFor(() => {
      expect(screen.getByText(/import failed/i)).toBeDefined()
    })
  })
})
