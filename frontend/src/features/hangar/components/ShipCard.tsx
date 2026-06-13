import type { HangarShipCard } from '../schemas/hangarShipCard'

interface ShipCardProps {
  ship: HangarShipCard
  badge?: React.ReactNode
  hoverOverlay?: React.ReactNode
}

export function ShipCard({ ship, badge, hoverOverlay }: ShipCardProps) {
  return (
    <div className="relative h-40 rounded-lg overflow-hidden border border-border group bg-muted">
      {/* Readability scrim */}
      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/30 to-transparent" />

      {/* Ship name — top left */}
      <div className="absolute top-2 left-2 right-2">
        <p className="text-sm font-semibold text-white drop-shadow leading-tight line-clamp-2">
          {ship.name}
        </p>
        {ship.companyName && (
          <p className="text-xs text-white/70 drop-shadow">{ship.companyName}</p>
        )}
      </div>

      {/* Optional badge — bottom right */}
      {badge && (
        <div className="absolute bottom-2 right-2">
          {badge}
        </div>
      )}

      {/* Optional hover overlay */}
      {hoverOverlay && (
        <div className="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
          {hoverOverlay}
        </div>
      )}
    </div>
  )
}
