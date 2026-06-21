import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { MaterialsFilters } from '../components/MaterialsFilters'

const mockFilters = {
  owners: [
    { userId: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', displayName: 'Alice' },
    { userId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22', displayName: 'Bob' },
  ],
  locations: ['Bay 1', 'Dock 3'],
}

const emptyValues = { material: '', ownerUserId: '', station: '', stationId: '', qualityMin: 1, qualityMax: 1000 }

describe('MaterialsFilters', () => {
  it('renders material filter input', () => {
    render(<MaterialsFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByLabelText(/material/i)).toBeDefined()
  })

  it('calls onFilterChange when material text is typed', async () => {
    const user = userEvent.setup()
    const onFilterChange = vi.fn()
    render(<MaterialsFilters filters={mockFilters} values={emptyValues} onFilterChange={onFilterChange} />, {
      wrapper: createWrapper(),
    })
    await user.type(screen.getByLabelText(/material/i), 'titan')
    expect(onFilterChange).toHaveBeenCalled()
  })

  it('renders owner dropdown with options', async () => {
    const user = userEvent.setup()
    render(<MaterialsFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    await user.click(screen.getByLabelText(/owner/i))
    expect(await screen.findByRole('option', { name: 'Alice' })).toBeDefined()
    expect(screen.getByRole('option', { name: 'Bob' })).toBeDefined()
  })

  it('selecting an Owner replaces rather than accumulates a prior selection', async () => {
    const user = userEvent.setup()
    const onFilterChange = vi.fn()
    const values = { ...emptyValues, ownerUserId: mockFilters.owners[0].userId }
    render(<MaterialsFilters filters={mockFilters} values={values} onFilterChange={onFilterChange} />, {
      wrapper: createWrapper(),
    })
    await user.click(screen.getByLabelText(/owner/i))
    await user.click(await screen.findByRole('option', { name: 'Bob' }))
    expect(onFilterChange).toHaveBeenCalledWith(
      expect.objectContaining({ ownerUserId: mockFilters.owners[1].userId })
    )
    expect(onFilterChange).toHaveBeenCalledTimes(1)
  })

  it('renders station filter combobox', () => {
    render(<MaterialsFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    expect(screen.getByText('All stations')).toBeDefined()
  })

  it('defaults the Quality range to 1–1000', () => {
    render(<MaterialsFilters filters={mockFilters} values={emptyValues} onFilterChange={() => {}} />, {
      wrapper: createWrapper(),
    })
    const minInput = screen.getByLabelText(/minimum quality/i) as HTMLInputElement
    const maxInput = screen.getByLabelText(/maximum quality/i) as HTMLInputElement
    expect(minInput.value).toBe('1')
    expect(maxInput.value).toBe('1000')
  })

  it('produces [min,max] filter values when the quality min/max inputs change', () => {
    const onFilterChange = vi.fn()
    render(<MaterialsFilters filters={mockFilters} values={emptyValues} onFilterChange={onFilterChange} />, {
      wrapper: createWrapper(),
    })
    const minInput = screen.getByLabelText(/minimum quality/i)
    fireEvent.change(minInput, { target: { value: '200' } })
    expect(onFilterChange).toHaveBeenCalledWith(expect.objectContaining({ qualityMin: 200 }))
  })

  it('Clear resets all filters to defaults', async () => {
    const user = userEvent.setup()
    const onFilterChange = vi.fn()
    const values = {
      material: 'titan',
      ownerUserId: mockFilters.owners[0].userId,
      station: 'Bay 1',
      stationId: 'station-1',
      qualityMin: 200,
      qualityMax: 800,
    }
    render(<MaterialsFilters filters={mockFilters} values={values} onFilterChange={onFilterChange} />, {
      wrapper: createWrapper(),
    })
    await user.click(screen.getByRole('button', { name: /clear/i }))
    expect(onFilterChange).toHaveBeenCalledWith(emptyValues)
  })
})
