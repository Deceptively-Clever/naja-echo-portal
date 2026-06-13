import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { ShipsTable } from '../components/ShipsTable'
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

describe('ShipsTable', () => {
  it('renders ship name and company', () => {
    render(
      <MemoryRouter>
        <ShipsTable ships={[activeShip]} onViewDetails={vi.fn()} />
      </MemoryRouter>
    )
    expect(screen.getByText('100i')).toBeDefined()
    expect(screen.getByText('Origin')).toBeDefined()
  })

  it('shows soft-deleted badge for removed ships', () => {
    render(
      <MemoryRouter>
        <ShipsTable ships={[softDeletedShip]} onViewDetails={vi.fn()} />
      </MemoryRouter>
    )
    expect(screen.getByText('Removed from feed')).toBeDefined()
  })

  it('shows empty state when no ships', () => {
    render(
      <MemoryRouter>
        <ShipsTable ships={[]} onViewDetails={vi.fn()} />
      </MemoryRouter>
    )
    expect(screen.getByText('No ships imported yet.')).toBeDefined()
  })

  it('calls onViewDetails when View Details is clicked', async () => {
    const onViewDetails = vi.fn()
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <ShipsTable ships={[activeShip]} onViewDetails={onViewDetails} />
      </MemoryRouter>
    )
    await user.click(screen.getByRole('button', { name: 'View Details' }))
    expect(onViewDetails).toHaveBeenCalledWith(activeShip)
  })
})
