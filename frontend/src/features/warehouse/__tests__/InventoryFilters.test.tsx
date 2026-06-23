import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { InventoryFilters } from '../components/InventoryFilters'

const mockFilters = {
  types: ['Armor', 'Weapons'],
  subtypes: ['Ballistic', 'Laser'],
  owners: [
    { userId: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', displayName: 'Alice' },
    { userId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22', displayName: 'Bob' },
  ],
}

const emptyValues = { name: '', type: '', subtype: '', ownerUserId: '', location: '', locationId: '' }

describe('InventoryFilters', () => {
  it('renders name filter input', () => {
    render(<InventoryFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByLabelText(/name/i)).toBeDefined()
  })

  it('renders type dropdown with options', async () => {
    const user = userEvent.setup()
    render(<InventoryFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    await user.click(screen.getByLabelText(/^type$/i))
    expect(await screen.findByRole('option', { name: 'Armor' })).toBeDefined()
    expect(screen.getByRole('option', { name: 'Weapons' })).toBeDefined()
  })

  it('renders subtype dropdown with options', async () => {
    const user = userEvent.setup()
    render(<InventoryFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    await user.click(screen.getByLabelText(/subtype/i))
    expect(await screen.findByRole('option', { name: 'Ballistic' })).toBeDefined()
    expect(screen.getByRole('option', { name: 'Laser' })).toBeDefined()
  })

  it('renders owner dropdown with options', async () => {
    const user = userEvent.setup()
    render(<InventoryFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    await user.click(screen.getByLabelText(/owner/i))
    expect(await screen.findByRole('option', { name: 'Alice' })).toBeDefined()
    expect(screen.getByRole('option', { name: 'Bob' })).toBeDefined()
  })

  it('renders location filter combobox', () => {
    render(<InventoryFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('All locations')).toBeDefined()
  })

  it('calls onFilterChange when name is typed', async () => {
    const user = userEvent.setup()
    const onFilterChange = vi.fn()
    render(<InventoryFilters filters={mockFilters} values={emptyValues} onFilterChange={onFilterChange} />, {
      wrapper: createWrapper(),
    })
    await user.type(screen.getByLabelText(/name/i), 'laser')
    expect(onFilterChange).toHaveBeenCalled()
  })

  it('calls onFilterChange when type is selected', async () => {
    const user = userEvent.setup()
    const onFilterChange = vi.fn()
    render(<InventoryFilters filters={mockFilters} values={emptyValues} onFilterChange={onFilterChange} />, {
      wrapper: createWrapper(),
    })
    await user.click(screen.getByLabelText(/^type$/i))
    await user.click(await screen.findByRole('option', { name: 'Weapons' }))
    expect(onFilterChange).toHaveBeenCalledWith(expect.objectContaining({ type: 'Weapons' }))
  })

  it('reflects current filter values', () => {
    const values = { ...emptyValues, name: 'laser', type: 'Weapons' }
    render(<InventoryFilters filters={mockFilters} values={values} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    expect((screen.getByLabelText(/name/i) as HTMLInputElement).value).toBe('laser')
  })
})
