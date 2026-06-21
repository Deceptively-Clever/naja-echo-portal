import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { ShipComponentsFilters } from '../components/ShipComponentsFilters'
import type { ShipComponentFilterValues } from '../components/ShipComponentsFilters'

const emptyValues: ShipComponentFilterValues = {
  name: '',
  type: '',
  class: '',
  size: '',
  grade: '',
  station: '',
  stationId: '',
}

describe('ShipComponentsFilters', () => {
  it('renders Name text input', () => {
    render(
      <ShipComponentsFilters values={emptyValues} onFilterChange={() => {}} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByPlaceholderText(/filter by name/i)).toBeDefined()
  })

  it('renders Type combobox', () => {
    render(
      <ShipComponentsFilters values={emptyValues} onFilterChange={() => {}} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('combobox', { name: /type/i })).toBeDefined()
  })

  it('renders Class combobox', () => {
    render(
      <ShipComponentsFilters values={emptyValues} onFilterChange={() => {}} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('combobox', { name: /class/i })).toBeDefined()
  })

  it('renders Size combobox', () => {
    render(
      <ShipComponentsFilters values={emptyValues} onFilterChange={() => {}} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('combobox', { name: /size/i })).toBeDefined()
  })

  it('renders Grade combobox', () => {
    render(
      <ShipComponentsFilters values={emptyValues} onFilterChange={() => {}} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('combobox', { name: /grade/i })).toBeDefined()
  })

  it('calls onFilterChange when type is selected', async () => {
    const user = userEvent.setup()
    const onFilterChange = vi.fn()
    render(
      <ShipComponentsFilters values={emptyValues} onFilterChange={onFilterChange} />,
      { wrapper: createWrapper() }
    )
    await user.click(screen.getByRole('combobox', { name: /type/i }))
    await user.click(await screen.findByRole('option', { name: 'Coolers' }))
    expect(onFilterChange).toHaveBeenCalledWith(expect.objectContaining({ type: 'Coolers' }))
  })
})
