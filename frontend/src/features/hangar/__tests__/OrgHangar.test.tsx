import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { OwnerCountBadge } from '../components/OwnerCountBadge'
import { OrgHangarView } from '../pages/OrgHangarView'

const mockOwner1 = { userId: 'a0eebc99-0000-4ef8-bb6d-6bb9bd380a01', displayName: 'Alice' }
const mockOwner2 = { userId: 'a0eebc99-0000-4ef8-bb6d-6bb9bd380a02', displayName: 'Bob' }

const mockOrgShip = {
  shipId: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  name: 'Gladius',
  companyName: 'Aegis',
  urlPhoto: null,
  scu: null,
  crew: null,
  ownerCount: 2,
  owners: [mockOwner1, mockOwner2],
}

const mockPagedOrgResponse = {
  items: [mockOrgShip],
  page: 1,
  pageSize: 25,
  totalCount: 1,
  totalPages: 1,
}

const mockMembersResponse = [
  { userId: mockOwner1.userId, displayName: 'Alice' },
  { userId: mockOwner2.userId, displayName: 'Bob' },
]

// ── OwnerCountBadge ───────────────────────────────────────────────────

describe('OwnerCountBadge', () => {
  it('renders owner count', () => {
    render(<OwnerCountBadge ownerCount={3} owners={[mockOwner1, mockOwner2]} />, { wrapper: createWrapper() })
    expect(screen.getByText('3')).toBeDefined()
  })

  it('does not show owner names list initially', () => {
    render(<OwnerCountBadge ownerCount={2} owners={[mockOwner1, mockOwner2]} />, { wrapper: createWrapper() })
    expect(screen.queryByText('Alice')).toBeNull()
  })

  it('shows owner names list on hover', async () => {
    const user = userEvent.setup()
    const { container } = render(
      <OwnerCountBadge ownerCount={2} owners={[mockOwner1, mockOwner2]} />,
      { wrapper: createWrapper() }
    )
    await user.hover(container.firstChild as Element)
    await waitFor(() => {
      expect(screen.getByText('Alice')).toBeDefined()
      expect(screen.getByText('Bob')).toBeDefined()
    })
  })

  it('hides owner names list after mouseout', async () => {
    const user = userEvent.setup()
    const { container } = render(
      <OwnerCountBadge ownerCount={2} owners={[mockOwner1, mockOwner2]} />,
      { wrapper: createWrapper() }
    )
    await user.hover(container.firstChild as Element)
    await waitFor(() => expect(screen.getByText('Alice')).toBeDefined())
    await user.unhover(container.firstChild as Element)
    await waitFor(() => expect(screen.queryByText('Alice')).toBeNull())
  })
})

// ── OrgHangarView ─────────────────────────────────────────────────────

describe('OrgHangarView', () => {
  beforeEach(() => {
    server.use(
      http.get('/api/hangar/org', () => HttpResponse.json(mockPagedOrgResponse)),
      http.get('/api/hangar/org/members', () => HttpResponse.json(mockMembersResponse))
    )
  })

  it('renders org ships from API', async () => {
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    await waitFor(() => {
      expect(screen.getByText('Gladius')).toBeDefined()
    })
  })

  it('renders OwnerCountBadge for each org ship', async () => {
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    await waitFor(() => {
      expect(screen.getByText('2')).toBeDefined()
    })
  })

  it('does not show Add Ship button', async () => {
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    await waitFor(() => expect(screen.getByText('Gladius')).toBeDefined())
    expect(screen.queryByRole('button', { name: /add ship/i })).toBeNull()
  })

  it('shows My Ships toggle button', () => {
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    expect(screen.getByRole('button', { name: /my ships/i })).toBeDefined()
  })

  it('shows member filter dropdown with All Members default', async () => {
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    await waitFor(() => {
      expect(screen.getByRole('combobox', { name: /filter by member/i })).toBeDefined()
    })
    expect(screen.getByRole('combobox', { name: /filter by member/i })).toHaveTextContent('All Members')
  })

  it('populates member filter from API', async () => {
    const user = userEvent.setup()
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    await waitFor(() => screen.getByText('Gladius'))
    await user.click(screen.getByRole('combobox', { name: /filter by member/i }))
    expect(await screen.findByRole('option', { name: 'Alice' })).toBeDefined()
    expect(screen.getByRole('option', { name: 'Bob' })).toBeDefined()
  })

  it('My Ships toggle marks button as pressed when active', async () => {
    const user = userEvent.setup()
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    const toggle = screen.getByRole('button', { name: /my ships/i })
    expect(toggle.getAttribute('aria-pressed')).toBe('false')
    await user.click(toggle)
    expect(toggle.getAttribute('aria-pressed')).toBe('true')
  })

  it('selecting a member clears My Ships toggle', async () => {
    const user = userEvent.setup()
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })

    // Activate My Ships
    await user.click(screen.getByRole('button', { name: /my ships/i }))
    expect(screen.getByRole('button', { name: /my ships/i }).getAttribute('aria-pressed')).toBe('true')

    // Wait for members to load, then select one
    await waitFor(() => screen.getByText('Gladius'))
    await user.click(screen.getByRole('combobox', { name: /filter by member/i }))
    await user.click(await screen.findByRole('option', { name: 'Alice' }))

    // My Ships should be cleared
    expect(screen.getByRole('button', { name: /my ships/i }).getAttribute('aria-pressed')).toBe('false')
  })

  it('shows sort dropdown defaulting to Most Owners', () => {
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    expect(screen.getByRole('combobox', { name: /sort by/i })).toHaveTextContent('Most Owners')
  })

  it('sort dropdown contains Most Owners and Ship Name options', async () => {
    const user = userEvent.setup()
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    await user.click(screen.getByRole('combobox', { name: /sort by/i }))
    expect(await screen.findByRole('option', { name: 'Most Owners' })).toBeDefined()
    expect(screen.getByRole('option', { name: 'Ship Name' })).toBeDefined()
  })

  it('changing sort updates the dropdown value', async () => {
    const user = userEvent.setup()
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    await user.click(screen.getByRole('combobox', { name: /sort by/i }))
    await user.click(await screen.findByRole('option', { name: 'Ship Name' }))
    expect(screen.getByRole('combobox', { name: /sort by/i })).toHaveTextContent('Ship Name')
  })

  it('shows empty state when org has no ships', async () => {
    server.use(
      http.get('/api/hangar/org', () =>
        HttpResponse.json({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 })
      )
    )
    render(<OrgHangarView />, { wrapper: createWrapper(['/hangar']) })
    await waitFor(() => {
      expect(screen.getByText(/no members have added any ships yet/i)).toBeDefined()
    })
  })
})
