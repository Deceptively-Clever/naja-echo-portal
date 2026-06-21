import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { StationCombobox } from '../components/StationCombobox'

const mockStations = [
  { id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', name: 'ARC-L1 Wide Forest Station' },
  { id: 'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22', name: 'CRU-L1 Ambitious Dream Station' },
]

function renderCombobox(props: {
  value?: string
  onValueChange?: (id: string, name: string) => void
  placeholder?: string
}) {
  return {
    user: userEvent.setup(),
    ...render(
      <StationCombobox
        value={props.value}
        onValueChange={props.onValueChange ?? (() => {})}
        placeholder={props.placeholder}
      />,
      { wrapper: createWrapper() }
    ),
  }
}

describe('StationCombobox', () => {
  it('renders with placeholder when no value selected', () => {
    renderCombobox({ placeholder: 'Pick a station…' })
    expect(screen.getByText('Pick a station…')).toBeDefined()
  })

  it('opens and shows station list from API', async () => {
    server.use(
      http.get('/api/warehouse/stations', () =>
        HttpResponse.json({ stations: mockStations })
      )
    )
    const { user } = renderCombobox({})
    await user.click(screen.getByRole('combobox'))

    await waitFor(() => {
      expect(screen.getByText('ARC-L1 Wide Forest Station')).toBeDefined()
      expect(screen.getByText('CRU-L1 Ambitious Dream Station')).toBeDefined()
    })
  })

  it('passes search term to API and renders filtered results', async () => {
    server.use(
      http.get('/api/warehouse/stations', ({ request }) => {
        const url = new URL(request.url)
        const search = url.searchParams.get('search')
        const stations = search
          ? [{ id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', name: 'ARC-L1 Wide Forest Station' }]
          : []
        return HttpResponse.json({ stations })
      })
    )
    const { user } = renderCombobox({})
    await user.click(screen.getByRole('combobox'))
    // Initial empty query → no results
    await waitFor(() => expect(screen.getByText('No stations found.')).toBeDefined())

    await user.type(screen.getByPlaceholderText('Search stations…'), 'ARC')

    await waitFor(() => {
      expect(screen.getByText('ARC-L1 Wide Forest Station')).toBeDefined()
    })
  })

  it('calls onValueChange with station id and name when option clicked', async () => {
    server.use(
      http.get('/api/warehouse/stations', () => HttpResponse.json({ stations: mockStations }))
    )
    const onValueChange = vi.fn()
    const { user } = renderCombobox({ onValueChange })
    await user.click(screen.getByRole('combobox'))

    await waitFor(() => {
      expect(screen.getByText('ARC-L1 Wide Forest Station')).toBeDefined()
    })
    await user.click(screen.getByText('ARC-L1 Wide Forest Station'))

    expect(onValueChange).toHaveBeenCalledWith(
      'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
      'ARC-L1 Wide Forest Station'
    )
  })

  it('shows "No stations found" when API returns empty list', async () => {
    server.use(
      http.get('/api/warehouse/stations', () => HttpResponse.json({ stations: [] }))
    )
    const { user } = renderCombobox({})
    await user.click(screen.getByRole('combobox'))

    await waitFor(() => {
      expect(screen.getByText('No stations found.')).toBeDefined()
    })
  })
})
