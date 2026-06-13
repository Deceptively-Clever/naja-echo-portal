import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { AddShipDialog } from '../components/AddShipDialog'
import { RemoveShipButton } from '../components/RemoveShipButton'

const mockShip = {
  shipId: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  name: 'Gladius',
  companyName: 'Aegis',
  urlPhoto: null,
  scu: null,
  crew: null,
}

const mockCatalogItem = {
  ...mockShip,
  alreadyOwned: false,
}

const mockOwnedCatalogItem = {
  ...mockShip,
  shipId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22',
  name: 'Hornet',
  alreadyOwned: true,
}

const mockCatalogResponse = {
  items: [mockCatalogItem, mockOwnedCatalogItem],
  page: 1,
  pageSize: 25,
  totalCount: 2,
  totalPages: 1,
}

// ── AddShipDialog ──────────────────────────────────────────────────────

describe('AddShipDialog', () => {
  it('does not render when open=false', () => {
    render(<AddShipDialog open={false} onClose={() => {}} />, { wrapper: createWrapper() })
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('renders dialog when open=true', () => {
    render(<AddShipDialog open={true} onClose={() => {}} />, { wrapper: createWrapper() })
    expect(screen.getByRole('dialog')).toBeDefined()
  })

  it('renders search input inside dialog', () => {
    render(<AddShipDialog open={true} onClose={() => {}} />, { wrapper: createWrapper() })
    expect(screen.getByLabelText(/search ship catalog/i)).toBeDefined()
  })

  it('calls onClose when close button is clicked', async () => {
    const user = userEvent.setup()
    let closed = false
    render(<AddShipDialog open={true} onClose={() => { closed = true }} />, { wrapper: createWrapper() })
    await user.click(screen.getByLabelText(/close dialog/i))
    expect(closed).toBe(true)
  })

  it('shows catalog results when search has results', async () => {
    server.use(
      http.get('/api/hangar/catalog/search', () => HttpResponse.json(mockCatalogResponse))
    )
    const user = userEvent.setup()
    render(<AddShipDialog open={true} onClose={() => {}} />, { wrapper: createWrapper(['/hangar']) })
    await user.type(screen.getByLabelText(/search ship catalog/i), 'glad')

    await waitFor(() => {
      expect(screen.getByText('Gladius')).toBeDefined()
    })
  })

  it('shows Owned label for already-owned ships', async () => {
    server.use(
      http.get('/api/hangar/catalog/search', () => HttpResponse.json(mockCatalogResponse))
    )
    const user = userEvent.setup()
    render(<AddShipDialog open={true} onClose={() => {}} />, { wrapper: createWrapper(['/hangar']) })
    await user.type(screen.getByLabelText(/search ship catalog/i), 'ship')

    await waitFor(() => {
      expect(screen.getByText('Owned')).toBeDefined()
    })
  })

  it('shows Add button only for unowned ships', async () => {
    server.use(
      http.get('/api/hangar/catalog/search', () => HttpResponse.json(mockCatalogResponse))
    )
    const user = userEvent.setup()
    render(<AddShipDialog open={true} onClose={() => {}} />, { wrapper: createWrapper(['/hangar']) })
    await user.type(screen.getByLabelText(/search ship catalog/i), 'ship')

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /add gladius/i })).toBeDefined()
      expect(screen.queryByRole('button', { name: /add hornet/i })).toBeNull()
    })
  })

  it('shows success message after adding a ship', async () => {
    server.use(
      http.get('/api/hangar/catalog/search', () => HttpResponse.json(mockCatalogResponse)),
      http.post('/api/hangar/mine', () => HttpResponse.json({ shipId: mockCatalogItem.shipId, name: 'Gladius' }, { status: 201 }))
    )
    const user = userEvent.setup()
    render(<AddShipDialog open={true} onClose={() => {}} />, { wrapper: createWrapper(['/hangar']) })
    await user.type(screen.getByLabelText(/search ship catalog/i), 'glad')

    await waitFor(() => screen.getByRole('button', { name: /add gladius/i }))
    await user.click(screen.getByRole('button', { name: /add gladius/i }))

    await waitFor(() => {
      expect(screen.getByText(/added to your hangar/i)).toBeDefined()
    })
  })

  it('shows error message when add fails', async () => {
    server.use(
      http.get('/api/hangar/catalog/search', () => HttpResponse.json(mockCatalogResponse)),
      http.post('/api/hangar/mine', () => HttpResponse.json({ title: 'Error' }, { status: 500 }))
    )
    const user = userEvent.setup()
    render(<AddShipDialog open={true} onClose={() => {}} />, { wrapper: createWrapper(['/hangar']) })
    await user.type(screen.getByLabelText(/search ship catalog/i), 'glad')

    await waitFor(() => screen.getByRole('button', { name: /add gladius/i }))
    await user.click(screen.getByRole('button', { name: /add gladius/i }))

    await waitFor(() => {
      expect(screen.getByText(/failed to add/i)).toBeDefined()
    })
  })
})

// ── RemoveShipButton ───────────────────────────────────────────────────

describe('RemoveShipButton', () => {
  it('renders trash icon button initially', () => {
    render(<RemoveShipButton ship={mockShip} />, { wrapper: createWrapper() })
    expect(screen.getByRole('button', { name: /remove gladius/i })).toBeDefined()
  })

  it('shows confirmation UI after clicking remove', async () => {
    const user = userEvent.setup()
    render(<RemoveShipButton ship={mockShip} />, { wrapper: createWrapper() })
    await user.click(screen.getByRole('button', { name: /remove gladius/i }))
    expect(screen.getByText(/remove gladius\?/i)).toBeDefined()
    expect(screen.getByRole('button', { name: /remove/i })).toBeDefined()
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDefined()
  })

  it('restores initial state when Cancel is clicked', async () => {
    const user = userEvent.setup()
    render(<RemoveShipButton ship={mockShip} />, { wrapper: createWrapper() })
    await user.click(screen.getByRole('button', { name: /remove gladius/i }))
    await user.click(screen.getByRole('button', { name: /cancel/i }))
    expect(screen.getByRole('button', { name: /remove gladius/i })).toBeDefined()
  })

  it('calls DELETE endpoint when Remove confirmed', async () => {
    let deleteCalled = false
    server.use(
      http.delete(`/api/hangar/mine/${mockShip.shipId}`, () => {
        deleteCalled = true
        return new HttpResponse(null, { status: 204 })
      })
    )
    const user = userEvent.setup()
    render(<RemoveShipButton ship={mockShip} />, { wrapper: createWrapper(['/hangar']) })
    await user.click(screen.getByRole('button', { name: /remove gladius/i }))
    await user.click(screen.getByRole('button', { name: /^remove$/i }))

    await waitFor(() => {
      expect(deleteCalled).toBe(true)
    })
  })
})
