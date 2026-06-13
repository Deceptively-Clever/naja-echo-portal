import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ImportShipsButton } from '../components/ImportShipsButton'

const importResult = { added: 3, updated: 2, reactivated: 1, softDeleted: 0, total: 6 }

function renderButton() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return {
    user: userEvent.setup(),
    ...render(
      <QueryClientProvider client={client}>
        <ImportShipsButton />
      </QueryClientProvider>
    ),
  }
}

describe('ImportShipsButton', () => {
  it('renders Import Ships button', () => {
    renderButton()
    expect(screen.getByRole('button', { name: 'Import Ships' })).toBeDefined()
  })

  it('shows loading state during import', async () => {
    let resolve: () => void = () => {}
    server.use(
      http.post('/api/admin/ships/import', () =>
        new Promise<Response>((res) => { resolve = () => res(HttpResponse.json(importResult)) })
      )
    )

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Ships' }))
    expect(screen.getByRole('button', { name: 'Importing…' })).toBeDefined()
    expect(screen.getByRole('button')).toBeDisabled()
    resolve()
  })

  it('shows success message with counts after import', async () => {
    server.use(http.post('/api/admin/ships/import', () => HttpResponse.json(importResult)))

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Ships' }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('3 added')
    })
  })

  it('shows error message on import failure', async () => {
    server.use(http.post('/api/admin/ships/import', () => new HttpResponse(null, { status: 500 })))

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Ships' }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('Import failed')
    })
  })

  it('shows in-progress message on 409', async () => {
    server.use(http.post('/api/admin/ships/import', () => new HttpResponse(null, { status: 409 })))

    const { user } = renderButton()
    await user.click(screen.getByRole('button', { name: 'Import Ships' }))

    await waitFor(() => {
      expect(screen.getByRole('status').textContent).toContain('already in progress')
    })
  })
})
