import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { RemoveInventoryButton } from '../components/RemoveInventoryButton'

const mockRow = {
  id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  itemId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22',
  name: 'Laser Mk1',
  type: 'Weapons',
  subtype: 'Laser',
  quantity: 3,
  ownerUserId: 'c0eebc99-9c0b-4ef8-bb6d-6bb9bd380a33',
  ownerDisplayName: 'Alice',
  location: 'Bay 1',
}

describe('RemoveInventoryButton', () => {
  it('renders a remove button', () => {
    render(
      <RemoveInventoryButton row={mockRow} onRemove={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('button', { name: /remove/i })).toBeDefined()
  })

  it('button has accessible label', () => {
    render(
      <RemoveInventoryButton row={mockRow} onRemove={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    const button = screen.getByRole('button', { name: /remove/i })
    expect(button).toBeDefined()
  })

  it('calls onRemove when confirmed', async () => {
    const user = userEvent.setup()
    const onRemove = vi.fn().mockResolvedValue(undefined)
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    render(
      <RemoveInventoryButton row={mockRow} onRemove={onRemove} />,
      { wrapper: createWrapper() }
    )
    await user.click(screen.getByRole('button', { name: /remove/i }))
    expect(onRemove).toHaveBeenCalledWith(mockRow.id)
    confirmSpy.mockRestore()
  })

  it('does not call onRemove when cancelled', async () => {
    const user = userEvent.setup()
    const onRemove = vi.fn()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(
      <RemoveInventoryButton row={mockRow} onRemove={onRemove} />,
      { wrapper: createWrapper() }
    )
    await user.click(screen.getByRole('button', { name: /remove/i }))
    expect(onRemove).not.toHaveBeenCalled()
    confirmSpy.mockRestore()
  })
})
