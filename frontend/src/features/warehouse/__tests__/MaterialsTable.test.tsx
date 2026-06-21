import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { MaterialsTable } from '../components/MaterialsTable'

const mockRows = [
  {
    id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
    commodityId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22',
    materialName: 'Titanium',
    materialCode: 'TTAM',
    quantity: 12.5,
    quality: 750,
    ownerUserId: 'c0eebc99-9c0b-4ef8-bb6d-6bb9bd380a33',
    ownerDisplayName: 'Alice',
    location: 'Bay 1',
  },
  {
    id: 'd0eebc99-9c0b-4ef8-bb6d-6bb9bd380a44',
    commodityId: 'e0eebc99-9c0b-4ef8-bb6d-6bb9bd380a55',
    materialName: 'Quantanium',
    materialCode: 'QTM',
    quantity: 3,
    quality: 500,
    ownerUserId: 'f0eebc99-9c0b-4ef8-bb6d-6bb9bd380a66',
    ownerDisplayName: 'Bob',
    location: 'Dock 3',
  },
]

describe('MaterialsTable', () => {
  it('renders the Material, Owner, Station, Quantity, and Quality columns', () => {
    render(<MaterialsTable rows={mockRows} />, { wrapper: createWrapper() })
    expect(screen.getByText('Material')).toBeDefined()
    expect(screen.getByText('Owner')).toBeDefined()
    expect(screen.getByText('Station')).toBeDefined()
    expect(screen.getByText('Quantity')).toBeDefined()
    expect(screen.getByText('Quality')).toBeDefined()
  })

  it('renders material, owner, location, and quality values', () => {
    render(<MaterialsTable rows={mockRows} />, { wrapper: createWrapper() })
    expect(screen.getByText('Titanium')).toBeDefined()
    expect(screen.getByText('Quantanium')).toBeDefined()
    expect(screen.getByText('Alice')).toBeDefined()
    expect(screen.getByText('Bob')).toBeDefined()
    expect(screen.getByText('Bay 1')).toBeDefined()
    expect(screen.getByText('Dock 3')).toBeDefined()
    expect(screen.getByText('750')).toBeDefined()
    expect(screen.getByText('500')).toBeDefined()
  })

  it('formats quantity with exactly 3 decimal places', () => {
    render(<MaterialsTable rows={mockRows} />, { wrapper: createWrapper() })
    expect(screen.getByText('12.500')).toBeDefined()
    expect(screen.getByText('3.000')).toBeDefined()
  })

  it('renders the "no material inventory" empty state when rows is empty', () => {
    render(<MaterialsTable rows={[]} />, { wrapper: createWrapper() })
    expect(screen.getByText(/no material inventory/i)).toBeDefined()
  })

  it('renders a distinct "no results match the current filters" empty state when filters are active and rows is empty', () => {
    render(<MaterialsTable rows={[]} hasActiveFilters={true} />, { wrapper: createWrapper() })
    expect(screen.getByText(/no results match the current filters/i)).toBeDefined()
    expect(screen.queryByText(/no material inventory/i)).toBeNull()
  })

  it('shows edit controls when isQuartermaster=true', () => {
    render(<MaterialsTable rows={mockRows} isQuartermaster={true} />, { wrapper: createWrapper() })
    expect(screen.getAllByLabelText(/edit item/i).length).toBeGreaterThan(0)
  })

  it('hides edit controls when isQuartermaster=false', () => {
    render(<MaterialsTable rows={mockRows} isQuartermaster={false} />, { wrapper: createWrapper() })
    expect(screen.queryAllByLabelText(/edit item/i)).toHaveLength(0)
  })

  it('hides edit controls when isQuartermaster is omitted', () => {
    render(<MaterialsTable rows={mockRows} />, { wrapper: createWrapper() })
    expect(screen.queryAllByLabelText(/edit item/i)).toHaveLength(0)
  })

  it('shows remove controls when isQuartermaster=true', () => {
    render(
      <MaterialsTable rows={mockRows} isQuartermaster={true} onRemove={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getAllByRole('button', { name: /remove/i }).length).toBeGreaterThan(0)
  })

  it('hides remove controls when isQuartermaster=false', () => {
    render(<MaterialsTable rows={mockRows} isQuartermaster={false} />, { wrapper: createWrapper() })
    expect(screen.queryAllByRole('button', { name: /remove/i })).toHaveLength(0)
  })

  it('renders Quality as plain read-only text, even for Quartermasters', () => {
    render(
      <MaterialsTable rows={mockRows} isQuartermaster={true} onRemove={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByText('750')).toBeDefined()
    expect(screen.getByText('500')).toBeDefined()
    expect(screen.queryByLabelText(/quality/i)).toBeNull()
    expect(screen.queryAllByRole('spinbutton').length).toBe(0)
  })
})
