import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ShipDetailSheet } from '../components/ShipDetailSheet'
import type { ShipListItem } from '../schemas/shipSchemas'

const activeShip: ShipListItem = {
  id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  name: '100i',
  companyName: 'Origin',
  status: 'active',
}

const softDeletedShip: ShipListItem = {
  id: 'b1ffcd00-0d1c-4ef8-bb6d-6bb9bd380a22',
  name: 'Gladius',
  companyName: 'Aegis',
  status: 'softDeleted',
}

const detailResponse = {
  id: activeShip.id,
  status: 'active' as const,
  fields: {
    name: '100i',
    company_name: 'Origin',
    crew: '1',
    cargo: null,
    description: '',
  },
}

function renderSheet(ship: ShipListItem | null, onClose = () => {}) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  server.use(
    http.get(`/api/admin/ships/${ship?.id}`, () => HttpResponse.json(detailResponse))
  )
  return {
    user: userEvent.setup(),
    ...render(
      <QueryClientProvider client={client}>
        <ShipDetailSheet ship={ship} onClose={onClose} />
      </QueryClientProvider>
    ),
  }
}

describe('ShipDetailSheet', () => {
  it('does not render when ship is null', () => {
    renderSheet(null)
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('shows ship name in title', async () => {
    renderSheet(activeShip)
    await waitFor(() => {
      expect(screen.getByText('100i')).toBeDefined()
    })
  })

  it('shows all fields including empty ones', async () => {
    renderSheet(activeShip)
    await waitFor(() => {
      expect(screen.getByText('crew')).toBeDefined()
      expect(screen.getByText('cargo')).toBeDefined()
      expect(screen.getByText('description')).toBeDefined()
    })
    // Empty field is shown as "empty" text
    const emptyEls = screen.getAllByText('empty')
    expect(emptyEls.length).toBeGreaterThan(0)
  })

  it('shows soft-deleted indicator for removed ships', async () => {
    server.use(
      http.get(`/api/admin/ships/${softDeletedShip.id}`, () =>
        HttpResponse.json({ ...detailResponse, id: softDeletedShip.id, status: 'softDeleted' })
      )
    )
    renderSheet(softDeletedShip)
    await waitFor(() => {
      expect(screen.getByText('No longer in source feed')).toBeDefined()
    })
  })
})
