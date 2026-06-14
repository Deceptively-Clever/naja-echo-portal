import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { AddInventoryDialog } from '../components/AddInventoryDialog'

const mockCatalogResults = {
  items: [
    { itemId: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', name: 'Laser Mk1', type: 'Weapons', subtype: 'Laser' },
    { itemId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22', name: 'Ballistic Pistol', type: 'Weapons', subtype: 'Ballistic' },
  ],
}

describe('AddInventoryDialog', () => {
  it('does not render when open=false', () => {
    render(
      <AddInventoryDialog open={false} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('renders dialog when open=true', () => {
    render(
      <AddInventoryDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('dialog')).toBeDefined()
  })

  it('renders catalog search input', () => {
    render(
      <AddInventoryDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByLabelText(/search catalog/i)).toBeDefined()
  })

  it('renders location and quantity fields', () => {
    render(
      <AddInventoryDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByLabelText(/location/i)).toBeDefined()
    expect(screen.getByLabelText(/quantity/i)).toBeDefined()
  })

  it('quantity defaults to 1', () => {
    render(
      <AddInventoryDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    const qty = screen.getByLabelText(/quantity/i) as HTMLInputElement
    expect(qty.value).toBe('1')
  })

  it('shows catalog results when search is typed', async () => {
    server.use(
      http.get('/api/warehouse/catalog/search', () => HttpResponse.json(mockCatalogResults))
    )
    const user = userEvent.setup()
    render(
      <AddInventoryDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper(['/warehouse/items']) }
    )
    await user.type(screen.getByLabelText(/search catalog/i), 'laser')
    await waitFor(() => {
      expect(screen.getByText('Laser Mk1')).toBeDefined()
    })
  })

  it('calls onClose when cancel is clicked', async () => {
    const user = userEvent.setup()
    let closed = false
    render(
      <AddInventoryDialog open={true} onClose={() => { closed = true }} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    await user.click(screen.getByRole('button', { name: /cancel/i }))
    expect(closed).toBe(true)
  })

  it('pre-fills location from rememberedLocation prop', () => {
    render(
      <AddInventoryDialog
        open={true}
        onClose={() => {}}
        currentUserId="user-1"
        rememberedLocation="Bay 3"
      />,
      { wrapper: createWrapper() }
    )
    const location = screen.getByLabelText(/location/i) as HTMLInputElement
    expect(location.value).toBe('Bay 3')
  })
})
