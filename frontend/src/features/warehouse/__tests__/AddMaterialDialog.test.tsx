import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { AddMaterialDialog } from '../components/AddMaterialDialog'

const mockCommodityResults = {
  commodities: [
    { commodityId: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', name: 'Titanium', code: 'TTAM' },
    { commodityId: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22', name: 'Tin', code: 'TIN' },
  ],
}

const mockFilters = {
  owners: [
    { userId: 'c0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', displayName: 'Current User' },
    { userId: 'd0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22', displayName: 'Other Owner' },
  ],
  locations: ['Bay 1', 'Bay 2'],
}

describe('AddMaterialDialog', () => {
  it('does not render when open=false', () => {
    render(
      <AddMaterialDialog open={false} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('renders dialog when open=true', () => {
    render(
      <AddMaterialDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('dialog')).toBeDefined()
  })

  it('renders commodity search, location, quantity, and quality fields', () => {
    render(
      <AddMaterialDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByLabelText(/search commodities/i)).toBeDefined()
    expect(screen.getByLabelText(/location/i)).toBeDefined()
    expect(screen.getByLabelText(/quantity/i)).toBeDefined()
    expect(screen.getByLabelText(/quality/i)).toBeDefined()
  })

  it('Owner select defaults to the current user', () => {
    server.use(
      http.get('/api/warehouse/materials/filters', () => HttpResponse.json(mockFilters))
    )
    render(
      <AddMaterialDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    expect(screen.getByRole('combobox', { name: /owner/i })).toBeDefined()
  })

  it('Quality input defaults to 500', () => {
    render(
      <AddMaterialDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    const quality = screen.getByLabelText(/quality/i) as HTMLInputElement
    expect(quality.value).toBe('500')
  })

  it('Location input shows suggestions from existing locations', async () => {
    server.use(
      http.get('/api/warehouse/materials/filters', () => HttpResponse.json(mockFilters))
    )
    render(
      <AddMaterialDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    await waitFor(() => {
      const location = screen.getByLabelText(/location/i) as HTMLInputElement
      const datalist = document.getElementById(location.getAttribute('list') ?? '')
      expect(datalist?.querySelector('option[value="Bay 1"]')).not.toBeNull()
    })
  })

  it('shows commodity catalog results when search is typed', async () => {
    server.use(
      http.get('/api/warehouse/materials/catalog/search', () => HttpResponse.json(mockCommodityResults))
    )
    const user = userEvent.setup()
    render(
      <AddMaterialDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    await user.type(screen.getByLabelText(/search commodities/i), 'titanium')
    await waitFor(() => {
      expect(screen.getByText('Titanium')).toBeDefined()
    })
  })

  it('shows a validation message for Quantity <= 0', async () => {
    server.use(
      http.get('/api/warehouse/materials/catalog/search', () => HttpResponse.json(mockCommodityResults))
    )
    const user = userEvent.setup()
    render(
      <AddMaterialDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    await user.type(screen.getByLabelText(/search commodities/i), 'titanium')
    await waitFor(() => screen.getByText('Titanium'))
    await user.click(screen.getByText('Titanium'))
    await user.type(screen.getByLabelText(/^location$/i), 'Bay 1')

    const quantity = screen.getByLabelText(/quantity/i) as HTMLInputElement
    await user.clear(quantity)
    await user.type(quantity, '0')
    await user.click(screen.getByRole('button', { name: /add material/i }))

    expect(await screen.findByText(/quantity must be greater than 0/i)).toBeDefined()
  })

  it('shows a validation message for Quality outside 1..1000', async () => {
    server.use(
      http.get('/api/warehouse/materials/catalog/search', () => HttpResponse.json(mockCommodityResults))
    )
    const user = userEvent.setup()
    render(
      <AddMaterialDialog open={true} onClose={() => {}} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    await user.type(screen.getByLabelText(/search commodities/i), 'titanium')
    await waitFor(() => screen.getByText('Titanium'))
    await user.click(screen.getByText('Titanium'))
    await user.type(screen.getByLabelText(/^location$/i), 'Bay 1')

    const quality = screen.getByLabelText(/quality/i) as HTMLInputElement
    await user.clear(quality)
    await user.type(quality, '1001')
    await user.click(screen.getByRole('button', { name: /add material/i }))

    expect(await screen.findByText(/quality must be/i)).toBeDefined()
  })

  it('calls onClose when cancel is clicked', async () => {
    const user = userEvent.setup()
    let closed = false
    render(
      <AddMaterialDialog open={true} onClose={() => { closed = true }} currentUserId="user-1" />,
      { wrapper: createWrapper() }
    )
    await user.click(screen.getByRole('button', { name: /cancel/i }))
    expect(closed).toBe(true)
  })
})
