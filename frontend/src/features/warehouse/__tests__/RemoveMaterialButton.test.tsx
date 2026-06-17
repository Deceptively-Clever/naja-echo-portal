import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { RemoveMaterialButton } from '../components/RemoveMaterialButton'

const mockRow = {
  id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  commodityId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22',
  materialName: 'Titanium',
  materialCode: 'TTAM',
  quantity: 12.5,
  quality: 750,
  ownerUserId: 'c0eebc99-9c0b-4ef8-bb6d-6bb9bd380a33',
  ownerDisplayName: 'Alice',
  location: 'Bay 1',
}

describe('RemoveMaterialButton', () => {
  it('renders a remove button', () => {
    render(
      <RemoveMaterialButton row={mockRow} onRemove={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('button', { name: /remove/i })).toBeDefined()
  })

  it('calls onRemove with the row id when confirmed', async () => {
    const user = userEvent.setup()
    const onRemove = vi.fn().mockResolvedValue(undefined)
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    render(
      <RemoveMaterialButton row={mockRow} onRemove={onRemove} />,
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
      <RemoveMaterialButton row={mockRow} onRemove={onRemove} />,
      { wrapper: createWrapper() }
    )
    await user.click(screen.getByRole('button', { name: /remove/i }))
    expect(onRemove).not.toHaveBeenCalled()
    confirmSpy.mockRestore()
  })
})
