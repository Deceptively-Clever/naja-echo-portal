import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { ImportHangarDialog } from '../components/ImportHangarDialog'
import { MyHangarView } from '../pages/MyHangarView'

const emptyHangarResponse = {
  items: [],
  page: 1,
  pageSize: 25,
  totalCount: 0,
  totalPages: 0,
}

const successResult = {
  totalRecords: 2,
  importedShips: 2,
  unmatchedRecords: 0,
  unmatchedShipNames: [],
}

const partialResult = {
  totalRecords: 3,
  importedShips: 1,
  unmatchedRecords: 2,
  unmatchedShipNames: ['A.T.L.S.', 'Unknown Ship'],
}

function makeJsonFile(content: unknown, name = 'hangar.json'): File {
  const blob = new Blob([JSON.stringify(content)], { type: 'application/json' })
  return new File([blob], name, { type: 'application/json' })
}

// ── MyHangarView Integration ──────────────────────────────────────────────

describe('MyHangarView', () => {
  it('shows Import button', async () => {
    server.use(
      http.get('/api/hangar/mine', () => HttpResponse.json(emptyHangarResponse))
    )
    render(<MyHangarView />, { wrapper: createWrapper(['/hangar']) })
    expect(screen.getByRole('button', { name: /import/i })).toBeDefined()
  })

  it('opens ImportHangarDialog when Import button clicked', async () => {
    server.use(
      http.get('/api/hangar/mine', () => HttpResponse.json(emptyHangarResponse))
    )
    const user = userEvent.setup()
    render(<MyHangarView />, { wrapper: createWrapper(['/hangar']) })
    await user.click(screen.getByRole('button', { name: /import/i }))
    expect(screen.getByRole('dialog', { name: /import hangar/i })).toBeDefined()
  })
})

// ── ImportHangarDialog ────────────────────────────────────────────────────

describe('ImportHangarDialog', () => {
  it('does not render when open=false', () => {
    render(<ImportHangarDialog open={false} onClose={() => {}} />, { wrapper: createWrapper() })
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('shows warning step when first opened', () => {
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper() })
    expect(screen.getByRole('dialog')).toBeDefined()
    expect(screen.getByText(/replace your entire hangar/i)).toBeDefined()
  })

  it('does not show file input on warning step', () => {
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper() })
    expect(screen.queryByLabelText(/select hangarxplor/i)).toBeNull()
  })

  it('shows file input after confirming warning', async () => {
    const user = userEvent.setup()
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper() })
    await user.click(screen.getByRole('button', { name: /i understand/i }))
    expect(screen.getByLabelText(/select hangarxplor/i)).toBeDefined()
  })

  it('calls onClose when Cancel is clicked on warning step', async () => {
    const user = userEvent.setup()
    let closed = false
    render(<ImportHangarDialog open={true} onClose={() => { closed = true }} />, { wrapper: createWrapper() })
    await user.click(screen.getByRole('button', { name: /cancel/i }))
    expect(closed).toBe(true)
  })

  it('calls onClose when X button clicked', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(<ImportHangarDialog open={true} onClose={onClose} />, { wrapper: createWrapper() })
    await user.click(screen.getByLabelText(/close dialog/i))
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls POST import API with parsed items and shows summary on success', async () => {
    let capturedBody: unknown = null
    server.use(
      http.post('/api/hangar/mine/import', async ({ request }) => {
        capturedBody = await request.json()
        return HttpResponse.json(successResult)
      })
    )
    const user = userEvent.setup()
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper(['/hangar']) })

    await user.click(screen.getByRole('button', { name: /i understand/i }))

    const file = makeJsonFile([
      { name: 'Gladius', ship_name: 'Gladius' },
      { name: 'Avenger Titan' },
    ])
    const fileInput = screen.getByLabelText(/select hangarxplor/i)
    await userEvent.upload(fileInput, file)

    await waitFor(() => {
      expect(screen.getByText(/ships imported/i)).toBeDefined()
    })
    expect(capturedBody).toBeTruthy()
    // Both totalRecords and importedShips are "2" — verify count items appear in summary
    expect(screen.getAllByText('2').length).toBeGreaterThanOrEqual(2)
  })

  it('shows summary with unmatched ship names when API returns them', async () => {
    server.use(
      http.post('/api/hangar/mine/import', () => HttpResponse.json(partialResult))
    )
    const user = userEvent.setup()
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper(['/hangar']) })
    await user.click(screen.getByRole('button', { name: /i understand/i }))

    const file = makeJsonFile([{ name: 'Gladius' }, { name: 'A.T.L.S.' }, { name: 'Unknown Ship' }])
    await userEvent.upload(screen.getByLabelText(/select hangarxplor/i), file)

    await waitFor(() => {
      expect(screen.getByText('A.T.L.S.')).toBeDefined()
      expect(screen.getByText('Unknown Ship')).toBeDefined()
    })
  })

  it('does not show unmatched section when unmatchedShipNames is empty', async () => {
    server.use(
      http.post('/api/hangar/mine/import', () => HttpResponse.json(successResult))
    )
    const user = userEvent.setup()
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper(['/hangar']) })
    await user.click(screen.getByRole('button', { name: /i understand/i }))

    const file = makeJsonFile([{ name: 'Gladius' }])
    await userEvent.upload(screen.getByLabelText(/select hangarxplor/i), file)

    await waitFor(() => screen.getByText(/ships imported/i))
    expect(screen.queryByText(/unmatched ship names/i)).toBeNull()
  })

  it('shows error for invalid JSON content without calling API', async () => {
    let apiCalled = false
    server.use(
      http.post('/api/hangar/mine/import', () => {
        apiCalled = true
        return HttpResponse.json(successResult)
      })
    )
    const user = userEvent.setup()
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper() })
    await user.click(screen.getByRole('button', { name: /i understand/i }))

    // File has .json extension (passes accept filter) but contains invalid JSON
    const badJsonFile = new File(['this is not { valid } json!!'], 'ships.json', { type: 'application/json' })
    await userEvent.upload(screen.getByLabelText(/select hangarxplor/i), badJsonFile)

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeDefined()
    })
    expect(apiCalled).toBe(false)
  })

  it('shows error for JSON that is not an array without calling API', async () => {
    let apiCalled = false
    server.use(
      http.post('/api/hangar/mine/import', () => {
        apiCalled = true
        return HttpResponse.json(successResult)
      })
    )
    const user = userEvent.setup()
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper() })
    await user.click(screen.getByRole('button', { name: /i understand/i }))

    const badFile = makeJsonFile({ not: 'an array' }, 'bad.json')
    await userEvent.upload(screen.getByLabelText(/select hangarxplor/i), badFile)

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeDefined()
    })
    expect(apiCalled).toBe(false)
  })

  it('shows size error for files over 5 MB without calling API', async () => {
    let apiCalled = false
    server.use(
      http.post('/api/hangar/mine/import', () => {
        apiCalled = true
        return HttpResponse.json(successResult)
      })
    )
    const user = userEvent.setup()
    render(<ImportHangarDialog open={true} onClose={() => {}} />, { wrapper: createWrapper() })
    await user.click(screen.getByRole('button', { name: /i understand/i }))

    const bigContent = 'x'.repeat(6 * 1024 * 1024)
    const bigFile = new File([bigContent], 'big.json', { type: 'application/json' })
    await userEvent.upload(screen.getByLabelText(/select hangarxplor/i), bigFile)

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeDefined()
    })
    expect(screen.getByText(/too large/i)).toBeDefined()
    expect(apiCalled).toBe(false)
  })
})
