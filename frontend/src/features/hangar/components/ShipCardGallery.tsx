import { useCallback, useRef, useEffect, useState } from 'react'
import type { HangarShipCard, OrgHangarShipCard } from '../schemas/hangarShipCard'
import { ShipCard } from './ShipCard'

interface ShipCardGalleryProps {
  ships: (HangarShipCard | OrgHangarShipCard)[]
  search: string
  onSearchChange: (value: string) => void
  emptyStateMessage: string
  renderBadge?: (ship: HangarShipCard | OrgHangarShipCard) => React.ReactNode
  renderOverlay?: (ship: HangarShipCard | OrgHangarShipCard) => React.ReactNode
  isLoading?: boolean
  onLoadMore?: () => void
  hasMore?: boolean
  headerSlot?: React.ReactNode
}

export function ShipCardGallery({
  ships,
  search,
  onSearchChange,
  emptyStateMessage,
  renderBadge,
  renderOverlay,
  isLoading,
  onLoadMore,
  hasMore,
  headerSlot,
}: ShipCardGalleryProps) {
  const [inputValue, setInputValue] = useState(search)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const sentinelRef = useRef<HTMLDivElement>(null)

  const handleInput = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const value = e.target.value
      setInputValue(value)
      if (debounceRef.current) clearTimeout(debounceRef.current)
      debounceRef.current = setTimeout(() => {
        onSearchChange(value)
      }, 300)
    },
    [onSearchChange]
  )

  // Keep local input in sync when parent resets search externally
  useEffect(() => {
    setInputValue(search)
  }, [search])

  // IntersectionObserver for infinite scroll sentinel
  useEffect(() => {
    if (!sentinelRef.current || !onLoadMore) return
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting && hasMore && !isLoading) {
          onLoadMore()
        }
      },
      { threshold: 0.1 }
    )
    observer.observe(sentinelRef.current)
    return () => observer.disconnect()
  }, [onLoadMore, hasMore, isLoading])

  return (
    <div className="flex flex-col gap-4">
      {/* Search + optional header controls */}
      <div className="flex flex-col sm:flex-row gap-2 items-start sm:items-center">
        <input
          type="search"
          value={inputValue}
          onChange={handleInput}
          placeholder="Search ships…"
          className="flex h-9 w-full sm:max-w-xs rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          aria-label="Search ships"
        />
        {headerSlot}
      </div>

      {/* Gallery grid */}
      {ships.length === 0 && !isLoading ? (
        <p className="text-sm text-muted-foreground py-8 text-center">{emptyStateMessage}</p>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-3">
          {ships.map((ship) => (
            <ShipCard
              key={ship.shipId}
              ship={ship}
              badge={renderBadge?.(ship)}
              hoverOverlay={renderOverlay?.(ship)}
            />
          ))}
        </div>
      )}

      {/* Infinite scroll sentinel — invisible, no pagination controls */}
      <div ref={sentinelRef} aria-hidden className="h-1" />

      {isLoading && (
        <p className="text-sm text-muted-foreground text-center py-2">Loading…</p>
      )}
    </div>
  )
}
