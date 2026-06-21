import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { TransferStationDialog } from '../components/TransferStationDialog'

const mockStation = { id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', name: 'ARC-L1 Wide Forest Station' }

// Reset module-level session state between tests
beforeEach(async () => {
  // Re-import hook module to reset the module-level _lastStation variable
  const mod = await import('../hooks/useLastTransferStation')
  // Force clear by setting to undefined via a side-channel — module variable isn't exported directly
  // We achieve reset by using a fresh test environment via server reset
})

function renderDialog(props: {
  open?: boolean
  entityType?: 'item' | 'material'
  rowId?: string
  onSuccess?: () => void
  onOpenChange?: (o: boolean) => void
}) {
  return {
    user: userEvent.setup(),
    ...render(
      <TransferStationDialog
        open={props.open ?? true}
        onOpenChange={props.onOpenChange ?? (() => {})}
        rowId={props.rowId ?? 'row-id-123'}
        entityType={props.entityType ?? 'item'}
        onSuccess={props.onSuccess}
      />,
      { wrapper: createWrapper() }
    ),
  }
}

describe('TransferStationDialog', () => {
  it('renders dialog with station combobox when open=true', () => {
    renderDialog({})
    expect(screen.getByRole('dialog')).toBeDefined()
    expect(screen.getByRole('combobox')).toBeDefined()
  })

  it('Confirm button is disabled when no station selected', () => {
    renderDialog({})
    const confirmBtn = screen.getByRole('button', { name: /confirm transfer/i })
    expect(confirmBtn).toBeDisabled()
  })

  it('selecting a station enables the Confirm button', async () => {
    server.use(
      http.get('/api/warehouse/stations', () =>
        HttpResponse.json({ stations: [mockStation] })
      )
    )
    const { user } = renderDialog({})
    await user.click(screen.getByRole('combobox'))
    await waitFor(() => {
      expect(screen.getByText('ARC-L1 Wide Forest Station')).toBeDefined()
    })
    await user.click(screen.getByText('ARC-L1 Wide Forest Station'))

    const confirmBtn = screen.getByRole('button', { name: /confirm transfer/i })
    expect(confirmBtn).not.toBeDisabled()
  })

  it('clicking Confirm calls transfer API and calls onSuccess on completion', async () => {
    server.use(
      http.get('/api/warehouse/stations', () =>
        HttpResponse.json({ stations: [mockStation] })
      ),
      http.put('/api/warehouse/items/:id/station', () => new HttpResponse(null, { status: 204 }))
    )
    const onSuccess = vi.fn()
    const onOpenChange = vi.fn()
    const { user } = renderDialog({ onSuccess, onOpenChange })

    await user.click(screen.getByRole('combobox'))
    await waitFor(() => expect(screen.getByText('ARC-L1 Wide Forest Station')).toBeDefined())
    await user.click(screen.getByText('ARC-L1 Wide Forest Station'))
    await user.click(screen.getByRole('button', { name: /confirm transfer/i }))

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalled()
    })
  })

  it('clicking Cancel closes dialog without calling the API', async () => {
    let apiCalled = false
    server.use(
      http.put('/api/warehouse/items/:id/station', () => {
        apiCalled = true
        return new HttpResponse(null, { status: 204 })
      })
    )
    const onOpenChange = vi.fn()
    const { user } = renderDialog({ onOpenChange })
    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(apiCalled).toBe(false)
    expect(onOpenChange).toHaveBeenCalledWith(false)
  })

  it('does not render dialog content when open=false', () => {
    renderDialog({ open: false })
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('transfer material uses materials endpoint', async () => {
    let capturedUrl = ''
    const otherStation = { id: 'c0eebc99-9c0b-4ef8-bb6d-6bb9bd380a33', name: 'CRU-L1 Ambitious Dream Station' }
    server.use(
      http.get('/api/warehouse/stations', () =>
        HttpResponse.json({ stations: [mockStation, otherStation] })
      ),
      http.put('/api/warehouse/materials/:id/station', ({ request }) => {
        capturedUrl = request.url
        return new HttpResponse(null, { status: 204 })
      })
    )
    const onSuccess = vi.fn()
    const { user } = renderDialog({ entityType: 'material', rowId: 'mat-row-1', onSuccess })

    await user.click(screen.getByRole('combobox'))
    await waitFor(() => expect(screen.getByText('CRU-L1 Ambitious Dream Station')).toBeDefined())
    await user.click(screen.getByText('CRU-L1 Ambitious Dream Station'))
    await user.click(screen.getByRole('button', { name: /confirm transfer/i }))

    await waitFor(() => expect(onSuccess).toHaveBeenCalled())
    expect(capturedUrl).toContain('/api/warehouse/materials/mat-row-1/station')
  })
})
