import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { createWrapper } from '@/tests/testUtils'
import { EditQuantityControl } from '../components/EditQuantityControl'

describe('EditQuantityControl', () => {
  it('renders current quantity', () => {
    render(
      <EditQuantityControl currentQuantity={5} onConfirm={() => Promise.resolve()} />,
      { wrapper: createWrapper() }
    )
    const input = screen.getByLabelText(/quantity/i) as HTMLInputElement
    expect(input.value).toBe('5')
  })

  it('calls onConfirm with new quantity on submit', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn().mockResolvedValue(undefined)
    render(
      <EditQuantityControl currentQuantity={5} onConfirm={onConfirm} />,
      { wrapper: createWrapper() }
    )
    const input = screen.getByLabelText(/quantity/i)
    await user.clear(input)
    await user.type(input, '10')
    await user.click(screen.getByRole('button', { name: /save/i }))
    expect(onConfirm).toHaveBeenCalledWith(10)
  })

  it('shows error when quantity is 0', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(
      <EditQuantityControl currentQuantity={5} onConfirm={onConfirm} />,
      { wrapper: createWrapper() }
    )
    const input = screen.getByLabelText(/quantity/i)
    await user.clear(input)
    await user.type(input, '0')
    await user.click(screen.getByRole('button', { name: /save/i }))
    expect(screen.getByText(/must be at least 1/i)).toBeDefined()
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('shows error when quantity is negative', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(
      <EditQuantityControl currentQuantity={5} onConfirm={onConfirm} />,
      { wrapper: createWrapper() }
    )
    const input = screen.getByLabelText(/quantity/i)
    fireEvent.change(input, { target: { value: '-1' } })
    await user.click(screen.getByRole('button', { name: /save/i }))
    expect(screen.getByText(/must be at least 1/i)).toBeDefined()
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('renders cancel button', () => {
    render(
      <EditQuantityControl currentQuantity={5} onConfirm={() => Promise.resolve()} onCancel={() => {}} />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDefined()
  })
})
