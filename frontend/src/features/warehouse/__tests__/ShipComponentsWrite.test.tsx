import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { ShipComponentsTable } from '../components/ShipComponentsTable'

const mockRows = [
  {
    id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
    itemId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22',
    name: 'Shield Mk1',
    type: 'Shield',
    class: 'Military',
    size: 2,
    grade: 'A',
    quantity: 5,
    quality: 500,
    ownerUserId: 'c0eebc99-9c0b-4ef8-bb6d-6bb9bd380a33',
    ownerDisplayName: 'Alice',
    location: 'Bay 1',
  },
]

describe('ShipComponentsWrite', () => {
  it('hides add/edit/delete controls for non-Quartermaster', () => {
    render(
      <ShipComponentsTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    expect(screen.queryAllByLabelText(/change quantity/i)).toHaveLength(0)
    expect(screen.queryByText(/edit qty/i)).toBeNull()
  })

  it('shows edit controls for Quartermaster', () => {
    render(
      <ShipComponentsTable rows={mockRows} isQuartermaster={true} onRemove={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getAllByLabelText(/change quantity/i).length).toBeGreaterThan(0)
  })

  it('Class/Size/Grade cells are read-only (non-editable text)', () => {
    render(
      <ShipComponentsTable rows={mockRows} isQuartermaster={true} onRemove={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByText('Military')).toBeDefined()
    expect(screen.getByText('2')).toBeDefined()
    expect(screen.getByText('A')).toBeDefined()
    expect(screen.queryByRole('textbox', { name: /class/i })).toBeNull()
    expect(screen.queryByRole('textbox', { name: /grade/i })).toBeNull()
  })
})
