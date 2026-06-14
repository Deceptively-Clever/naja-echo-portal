import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { CategorySelector } from '../components/CategorySelector'
import type { CategoryListItem } from '../schemas/itemSchemas'

const categories: CategoryListItem[] = [
  { uexId: 1, name: 'Armor', section: 'Combat', type: 'item', isGameRelated: true, isMining: false, sourceDateModified: null, localItemCount: 5, lastImportedAt: null },
  { uexId: 2, name: 'Mining Lasers', section: 'Mining', type: 'item', isGameRelated: true, isMining: true, sourceDateModified: null, localItemCount: 3, lastImportedAt: null },
  { uexId: 3, name: 'Ore', section: 'Mining', type: 'item', isGameRelated: false, isMining: true, sourceDateModified: null, localItemCount: 0, lastImportedAt: null },
]

describe('CategorySelector', () => {
  it('renders all categories', () => {
    render(<CategorySelector categories={categories} selectedId={undefined} onSelect={() => {}} />)
    expect(screen.getByText('Armor')).toBeDefined()
    expect(screen.getByText('Mining Lasers')).toBeDefined()
    expect(screen.getByText('Ore')).toBeDefined()
  })

  it('filters by name search', async () => {
    const user = userEvent.setup()
    render(<CategorySelector categories={categories} selectedId={undefined} onSelect={() => {}} />)

    await user.type(screen.getByPlaceholderText(/search/i), 'armor')

    expect(screen.queryByText('Armor')).toBeDefined()
    expect(screen.queryByText('Mining Lasers')).toBeNull()
  })

  it('filters by section', async () => {
    const user = userEvent.setup()
    render(<CategorySelector categories={categories} selectedId={undefined} onSelect={() => {}} />)

    await user.selectOptions(screen.getByRole('combobox', { name: /section/i }), 'Mining')

    await waitFor(() => {
      expect(screen.queryByText('Armor')).toBeNull()
      expect(screen.getByText('Mining Lasers')).toBeDefined()
    })
  })

  it('filters by mining toggle', async () => {
    const user = userEvent.setup()
    render(<CategorySelector categories={categories} selectedId={undefined} onSelect={() => {}} />)

    await user.click(screen.getByRole('checkbox', { name: /mining/i }))

    await waitFor(() => {
      expect(screen.queryByText('Armor')).toBeNull()
      expect(screen.getByText('Mining Lasers')).toBeDefined()
      expect(screen.getByText('Ore')).toBeDefined()
    })
  })

  it('calls onSelect with uexId when row clicked', async () => {
    const user = userEvent.setup()
    const onSelect = vi.fn()
    render(<CategorySelector categories={categories} selectedId={undefined} onSelect={onSelect} />)

    await user.click(screen.getByText('Armor'))

    expect(onSelect).toHaveBeenCalledWith(1)
  })
})
