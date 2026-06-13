import { render, screen, act, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import userEvent from '@testing-library/user-event'
import { createWrapper } from '@/tests/testUtils'
import { ShipCardGallery } from '../components/ShipCardGallery'

const makeShip = (n: number) => ({
  shipId: `a0eebc99-0000-4ef8-bb6d-${String(n).padStart(12, '0')}`,
  name: `Ship ${n}`,
  companyName: null,
  urlPhoto: null,
  scu: null,
  crew: null,
})

// ── IntersectionObserver mock ──────────────────────────────────────────────

type IOCallback = (entries: IntersectionObserverEntry[]) => void
let triggerIntersection: ((isIntersecting: boolean) => void) | null = null

// ── ShipCardGallery infinite scroll tests ─────────────────────────────────

describe('ShipCardGallery infinite scroll', () => {
  let originalIO: typeof IntersectionObserver | undefined

  beforeEach(() => {
    originalIO = window.IntersectionObserver

    class MockIntersectionObserver {
      private cb: IOCallback
      constructor(cb: IOCallback) {
        this.cb = cb
        triggerIntersection = (isIntersecting) =>
          this.cb([{ isIntersecting } as IntersectionObserverEntry])
      }
      observe = vi.fn()
      disconnect = vi.fn()
      unobserve = vi.fn()
    }
    window.IntersectionObserver = MockIntersectionObserver as unknown as typeof IntersectionObserver
  })

  afterEach(() => {
    triggerIntersection = null
    window.IntersectionObserver = originalIO
  })

  it('calls onLoadMore when sentinel enters viewport and hasMore is true', () => {
    const onLoadMore = vi.fn()
    render(
      <ShipCardGallery
        ships={[makeShip(1)]}
        search=""
        onSearchChange={() => {}}
        emptyStateMessage="Empty"
        onLoadMore={onLoadMore}
        hasMore={true}
        isLoading={false}
      />,
      { wrapper: createWrapper() }
    )

    act(() => { triggerIntersection?.(true) })
    expect(onLoadMore).toHaveBeenCalledTimes(1)
  })

  it('does not call onLoadMore when hasMore is false', () => {
    const onLoadMore = vi.fn()
    render(
      <ShipCardGallery
        ships={[makeShip(1)]}
        search=""
        onSearchChange={() => {}}
        emptyStateMessage="Empty"
        onLoadMore={onLoadMore}
        hasMore={false}
        isLoading={false}
      />,
      { wrapper: createWrapper() }
    )

    act(() => { triggerIntersection?.(true) })
    expect(onLoadMore).not.toHaveBeenCalled()
  })

  it('does not call onLoadMore while isLoading is true', () => {
    const onLoadMore = vi.fn()
    render(
      <ShipCardGallery
        ships={[makeShip(1)]}
        search=""
        onSearchChange={() => {}}
        emptyStateMessage="Empty"
        onLoadMore={onLoadMore}
        hasMore={true}
        isLoading={true}
      />,
      { wrapper: createWrapper() }
    )

    act(() => { triggerIntersection?.(true) })
    expect(onLoadMore).not.toHaveBeenCalled()
  })

  it('does not call onLoadMore when sentinel is not intersecting', () => {
    const onLoadMore = vi.fn()
    render(
      <ShipCardGallery
        ships={[makeShip(1)]}
        search=""
        onSearchChange={() => {}}
        emptyStateMessage="Empty"
        onLoadMore={onLoadMore}
        hasMore={true}
        isLoading={false}
      />,
      { wrapper: createWrapper() }
    )

    act(() => { triggerIntersection?.(false) })
    expect(onLoadMore).not.toHaveBeenCalled()
  })

  it('does not render pagination controls', () => {
    render(
      <ShipCardGallery
        ships={Array.from({ length: 30 }, (_, i) => makeShip(i + 1))}
        search=""
        onSearchChange={() => {}}
        emptyStateMessage="Empty"
        hasMore={true}
        isLoading={false}
      />,
      { wrapper: createWrapper() }
    )

    expect(screen.queryByRole('button', { name: /next/i })).toBeNull()
    expect(screen.queryByRole('button', { name: /previous/i })).toBeNull()
    expect(screen.queryByRole('button', { name: /page/i })).toBeNull()
  })

  it('search change triggers onSearchChange after debounce and accepts parent search update', async () => {
    const user = userEvent.setup()
    let capturedSearch = ''
    const { rerender } = render(
      <ShipCardGallery
        ships={[makeShip(1)]}
        search=""
        onSearchChange={(v) => { capturedSearch = v }}
        emptyStateMessage="Empty"
      />,
      { wrapper: createWrapper() }
    )

    await user.type(screen.getByRole('searchbox', { name: /search ships/i }), 'glad')

    // Wait for 300ms debounce to fire
    await waitFor(() => {
      expect(capturedSearch).toBe('glad')
    }, { timeout: 1000 })

    // Simulate parent updating search prop (as a controlled component would)
    rerender(
      <ShipCardGallery
        ships={[makeShip(1)]}
        search="glad"
        onSearchChange={(v) => { capturedSearch = v }}
        emptyStateMessage="Empty"
      />
    )

    const input = screen.getByRole('searchbox', { name: /search ships/i }) as HTMLInputElement
    expect(input.value).toBe('glad')
  })
})
