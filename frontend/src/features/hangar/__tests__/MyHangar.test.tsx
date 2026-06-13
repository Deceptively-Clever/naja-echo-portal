import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { ShipCard } from '../components/ShipCard'
import { ShipCardGallery } from '../components/ShipCardGallery'
import { MyHangarView } from '../pages/MyHangarView'

const mockShip = {
  shipId: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  name: 'Gladius',
  companyName: 'Aegis',
  urlPhoto: 'https://example.com/gladius.jpg',
  scu: 0,
  crew: '1',
}

const mockPagedResponse = {
  items: [mockShip],
  page: 1,
  pageSize: 25,
  totalCount: 1,
  totalPages: 1,
}

// ── ShipCard ──────────────────────────────────────────────────────────

describe('ShipCard', () => {
  it('renders ship name', () => {
    render(<ShipCard ship={mockShip} />, { wrapper: createWrapper() })
    expect(screen.getByText('Gladius')).toBeDefined()
  })

  it('renders company name', () => {
    render(<ShipCard ship={mockShip} />, { wrapper: createWrapper() })
    expect(screen.getByText('Aegis')).toBeDefined()
  })

  it('renders with default muted background (no background-image)', () => {
    const { container } = render(<ShipCard ship={mockShip} />, { wrapper: createWrapper() })
    const card = container.firstChild as HTMLElement
    expect(card.style.backgroundImage).toBe('')
    expect(card.className).toContain('bg-muted')
  })

  it('renders badge slot when provided', () => {
    render(
      <ShipCard ship={mockShip} badge={<span>4 owners</span>} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByText('4 owners')).toBeDefined()
  })
})

// ── ShipCardGallery ───────────────────────────────────────────────────

describe('ShipCardGallery', () => {
  it('renders ship cards', () => {
    render(
      <ShipCardGallery
        ships={[mockShip]}
        search=""
        onSearchChange={() => {}}
        emptyStateMessage="No ships"
      />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByText('Gladius')).toBeDefined()
  })

  it('shows empty state when ships array is empty', () => {
    render(
      <ShipCardGallery
        ships={[]}
        search=""
        onSearchChange={() => {}}
        emptyStateMessage="No ships found"
      />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByText('No ships found')).toBeDefined()
  })

  it('calls onSearchChange after debounce when user types in the search box', async () => {
    const user = userEvent.setup()
    let captured = ''
    render(
      <ShipCardGallery
        ships={[]}
        search=""
        onSearchChange={(v) => { captured = v }}
        emptyStateMessage="Empty"
      />,
      { wrapper: createWrapper() }
    )
    await user.type(screen.getByRole('searchbox'), 'glad')
    // Debounce fires after 300ms — wait for it
    await waitFor(() => {
      expect(captured.length).toBeGreaterThan(0)
    }, { timeout: 1000 })
    expect(captured).toBe('glad')
  })

  it('does not show pagination controls', () => {
    render(
      <ShipCardGallery
        ships={[mockShip]}
        search=""
        onSearchChange={() => {}}
        emptyStateMessage="Empty"
        hasMore={true}
      />,
      { wrapper: createWrapper() }
    )
    expect(screen.queryByRole('button', { name: /next/i })).toBeNull()
    expect(screen.queryByRole('button', { name: /previous/i })).toBeNull()
  })
})

// ── MyHangarView with MSW ─────────────────────────────────────────────

describe('MyHangarView', () => {
  it('renders ships from API', async () => {
    server.use(
      http.get('/api/hangar/mine', () => HttpResponse.json(mockPagedResponse))
    )
    render(<MyHangarView />, { wrapper: createWrapper(['/hangar']) })
    await waitFor(() => {
      expect(screen.getByText('Gladius')).toBeDefined()
    })
  })

  it('shows empty state when hangar is empty', async () => {
    server.use(
      http.get('/api/hangar/mine', () =>
        HttpResponse.json({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 })
      )
    )
    render(<MyHangarView />, { wrapper: createWrapper(['/hangar']) })
    await waitFor(() => {
      expect(screen.getByText(/add your first ship/i)).toBeDefined()
    })
  })

  it('shows Add Ship button', async () => {
    server.use(
      http.get('/api/hangar/mine', () => HttpResponse.json(mockPagedResponse))
    )
    render(<MyHangarView />, { wrapper: createWrapper(['/hangar']) })
    expect(screen.getByRole('button', { name: /add ship/i })).toBeDefined()
  })
})
