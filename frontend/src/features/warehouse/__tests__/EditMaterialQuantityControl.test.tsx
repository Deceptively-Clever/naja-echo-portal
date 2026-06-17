import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { EditMaterialQuantityControl } from '../components/EditMaterialQuantityControl'

describe('EditMaterialQuantityControl', () => {
  it('renders current quantity', () => {
    render(
      <EditMaterialQuantityControl currentQuantity={5.5} onConfirm={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    const input = screen.getByLabelText(/quantity/i) as HTMLInputElement
    expect(input.value).toBe('5.5')
  })

  it('does not render a Quality field', () => {
    render(
      <EditMaterialQuantityControl currentQuantity={5} onConfirm={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    expect(screen.queryByLabelText(/quality/i)).toBeNull()
  })

  it('calls onConfirm with the absolute new quantity on submit', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn().mockResolvedValue(undefined)
    render(
      <EditMaterialQuantityControl currentQuantity={5} onConfirm={onConfirm} />,
      { wrapper: createWrapper() }
    )
    const input = screen.getByLabelText(/quantity/i)
    await user.clear(input)
    await user.type(input, '10.25')
    await user.click(screen.getByRole('button', { name: /save/i }))
    expect(onConfirm).toHaveBeenCalledWith(10.25)
  })

  it('shows error when quantity is 0 and does not call onConfirm', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(
      <EditMaterialQuantityControl currentQuantity={5} onConfirm={onConfirm} />,
      { wrapper: createWrapper() }
    )
    const input = screen.getByLabelText(/quantity/i)
    await user.clear(input)
    await user.type(input, '0')
    await user.click(screen.getByRole('button', { name: /save/i }))
    expect(screen.getByText(/must be greater than 0/i)).toBeDefined()
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('shows error when quantity is negative and does not call onConfirm', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(
      <EditMaterialQuantityControl currentQuantity={5} onConfirm={onConfirm} />,
      { wrapper: createWrapper() }
    )
    const input = screen.getByLabelText(/quantity/i)
    fireEvent.change(input, { target: { value: '-1' } })
    await user.click(screen.getByRole('button', { name: /save/i }))
    expect(screen.getByText(/must be greater than 0/i)).toBeDefined()
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('renders cancel button', () => {
    render(
      <EditMaterialQuantityControl currentQuantity={5} onConfirm={() => Promise.resolve()} onCancel={() => {}} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDefined()
  })
})
