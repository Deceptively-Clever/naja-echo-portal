import { useState } from 'react'
import { Users } from 'lucide-react'
import type { HangarOwner } from '../schemas/hangarShipCard'

interface OwnerCountBadgeProps {
  ownerCount: number
  owners: HangarOwner[]
}

export function OwnerCountBadge({ ownerCount, owners }: OwnerCountBadgeProps) {
  const [hovered, setHovered] = useState(false)

  return (
    <div
      className="relative"
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      <div className="flex items-center gap-1 bg-black/60 text-white text-xs px-1.5 py-0.5 rounded-full">
        <Users className="h-3 w-3" aria-hidden />
        <span>{ownerCount}</span>
      </div>

      {hovered && owners.length > 0 && (
        <div className="absolute bottom-full right-0 mb-1 z-10 bg-popover border border-border rounded shadow-md p-2 min-w-[120px] max-w-[200px]">
          <p className="text-xs font-medium text-popover-foreground mb-1">Owners</p>
          <ul className="space-y-0.5">
            {owners.map((o) => (
              <li key={o.userId} className="text-xs text-muted-foreground truncate">
                {o.displayName}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
