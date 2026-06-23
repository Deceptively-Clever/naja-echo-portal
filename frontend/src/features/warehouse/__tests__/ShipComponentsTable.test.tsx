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
    quality: 900,
    ownerUserId: 'c0eebc99-9c0b-4ef8-bb6d-6bb9bd380a33',
    ownerDisplayName: 'Alice',
    location: 'Bay 1',
  },
  {
    id: 'd0eebc99-9c0b-4ef8-bb6d-6bb9bd380a44',
    itemId: 'e0eebc99-9c0b-4ef8-bb6d-6bb9bd380a55',
    name: 'Gun Type-R',
    type: 'Gun',
    class: null,
    size: null,
    grade: null,
    quantity: 3,
    quality: 500,
    ownerUserId: 'f0eebc99-9c0b-4ef8-bb6d-6bb9bd380a66',
    ownerDisplayName: 'Bob',
    location: 'Dock 2',
  },
]

describe('ShipComponentsTable', () => {
  it('renders all column headers including quality', () => {
    render(<ShipComponentsTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('Name')).toBeDefined()
    expect(screen.getByText('Type')).toBeDefined()
    expect(screen.getByText('Class')).toBeDefined()
    expect(screen.getByText('Size')).toBeDefined()
    expect(screen.getByText('Grade')).toBeDefined()
    expect(screen.getByText('Qty')).toBeDefined()
    expect(screen.getByText('Quality')).toBeDefined()
    expect(screen.getByText('Owner')).toBeDefined()
    expect(screen.getByText('Location')).toBeDefined()
  })

  it('does not render a Section column header', () => {
    render(<ShipComponentsTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.queryByText('Section')).toBeNull()
  })

  it('renders populated class, size, grade values', () => {
    render(<ShipComponentsTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('Military')).toBeDefined()
    expect(screen.getByText('2')).toBeDefined()
    expect(screen.getByText('A')).toBeDefined()
  })

  it('renders Unknown for null class, size, grade', () => {
    render(<ShipComponentsTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    const unknownCells = screen.getAllByText('Unknown')
    expect(unknownCells.length).toBeGreaterThanOrEqual(3)
  })

  it('renders item names and owner display names', () => {
    render(<ShipComponentsTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('Shield Mk1')).toBeDefined()
    expect(screen.getByText('Gun Type-R')).toBeDefined()
    expect(screen.getByText('Alice')).toBeDefined()
    expect(screen.getByText('Bob')).toBeDefined()
  })

  it('shows empty-inventory state when rows is empty', () => {
    render(<ShipComponentsTable rows={[]} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText(/no ship component/i)).toBeDefined()
  })

  it('shows edit controls when isQuartermaster=true', () => {
    render(<ShipComponentsTable rows={mockRows} isQuartermaster={true} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getAllByLabelText(/edit item/i).length).toBeGreaterThan(0)
  })

  it('hides edit controls when isQuartermaster=false', () => {
    render(<ShipComponentsTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.queryAllByLabelText(/edit item/i)).toHaveLength(0)
  })
})
