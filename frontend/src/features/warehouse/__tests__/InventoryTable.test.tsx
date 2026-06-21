import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { InventoryTable } from '../components/InventoryTable'

const mockRows = [
  {
    id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
    itemId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22',
    name: 'Laser Mk1',
    type: 'Weapons',
    subtype: 'Laser',
    quantity: 3,
    quality: 750,
    ownerUserId: 'c0eebc99-9c0b-4ef8-bb6d-6bb9bd380a33',
    ownerDisplayName: 'Alice',
    location: 'Bay 1',
  },
  {
    id: 'd0eebc99-9c0b-4ef8-bb6d-6bb9bd380a44',
    itemId: 'e0eebc99-9c0b-4ef8-bb6d-6bb9bd380a55',
    name: 'Ballistic Pistol',
    type: 'Weapons',
    subtype: 'Ballistic',
    quantity: 10,
    quality: 500,
    ownerUserId: 'f0eebc99-9c0b-4ef8-bb6d-6bb9bd380a66',
    ownerDisplayName: 'Bob',
    location: 'Dock 3',
  },
]

describe('InventoryTable', () => {
  it('renders the item name column', () => {
    render(<InventoryTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('Laser Mk1')).toBeDefined()
    expect(screen.getByText('Ballistic Pistol')).toBeDefined()
  })

  it('renders the type and subtype columns', () => {
    render(<InventoryTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getAllByText('Weapons').length).toBeGreaterThan(0)
    expect(screen.getByText('Laser')).toBeDefined()
  })

  it('renders quantity values', () => {
    render(<InventoryTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('3')).toBeDefined()
    expect(screen.getByText('10')).toBeDefined()
  })

  it('renders quality values', () => {
    render(<InventoryTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('750')).toBeDefined()
    expect(screen.getByText('500')).toBeDefined()
  })

  it('renders owner display names', () => {
    render(<InventoryTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('Alice')).toBeDefined()
    expect(screen.getByText('Bob')).toBeDefined()
  })

  it('renders location values', () => {
    render(<InventoryTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('Bay 1')).toBeDefined()
    expect(screen.getByText('Dock 3')).toBeDefined()
  })

  it('shows empty state message when rows is empty', () => {
    render(<InventoryTable rows={[]} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText(/no items/i)).toBeDefined()
  })

  it('shows edit controls when isQuartermaster=true', () => {
    render(<InventoryTable rows={mockRows} isQuartermaster={true} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getAllByLabelText(/edit item/i).length).toBeGreaterThan(0)
  })

  it('hides edit controls when isQuartermaster=false', () => {
    render(<InventoryTable rows={mockRows} isQuartermaster={false} onRemove={() => Promise.resolve()} />, {
      wrapper: createWrapper(),
    })
    expect(screen.queryAllByLabelText(/edit item/i)).toHaveLength(0)
  })
})
