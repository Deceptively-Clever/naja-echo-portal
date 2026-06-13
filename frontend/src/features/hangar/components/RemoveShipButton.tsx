import { useState } from 'react'
import { Trash2 } from 'lucide-react'
import { useRemoveShip } from '../hooks/useRemoveShip'
import type { HangarShipCard } from '../schemas/hangarShipCard'

interface RemoveShipButtonProps {
  ship: HangarShipCard
}

export function RemoveShipButton({ ship }: RemoveShipButtonProps) {
  const [confirming, setConfirming] = useState(false)
  const { mutate, isPending } = useRemoveShip()

  if (confirming) {
    return (
      <div
        className="flex flex-col items-center gap-1 p-2 bg-black/70 rounded"
        onClick={(e) => e.stopPropagation()}
      >
        <p className="text-xs text-white text-center">Remove {ship.name}?</p>
        <div className="flex gap-1">
          <button
            onClick={() => {
              mutate(ship.shipId, { onSettled: () => setConfirming(false) })
            }}
            disabled={isPending}
            className="px-2 py-0.5 text-xs bg-destructive text-destructive-foreground rounded hover:bg-destructive/90 disabled:opacity-50"
          >
            Remove
          </button>
          <button
            onClick={() => setConfirming(false)}
            className="px-2 py-0.5 text-xs bg-secondary text-secondary-foreground rounded hover:bg-secondary/90"
          >
            Cancel
          </button>
        </div>
      </div>
    )
  }

  return (
    <button
      onClick={(e) => { e.stopPropagation(); setConfirming(true) }}
      aria-label={`Remove ${ship.name}`}
      className="p-1.5 rounded-full bg-black/60 text-white hover:bg-destructive transition-colors"
    >
      <Trash2 className="h-4 w-4" aria-hidden />
    </button>
  )
}
