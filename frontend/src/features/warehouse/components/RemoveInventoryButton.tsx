import { useState } from 'react'
import { Button } from '@/components/ui/button'
import type { InventoryRow } from '../schemas/inventorySchemas'

interface Props {
  row: InventoryRow
  onRemove: (id: string) => Promise<void>
}

export function RemoveInventoryButton({ row, onRemove }: Props) {
  const [isPending, setIsPending] = useState(false)

  async function handleClick() {
    if (!confirm(`Remove "${row.name}" from inventory?`)) return
    setIsPending(true)
    try {
      await onRemove(row.id)
    } finally {
      setIsPending(false)
    }
  }

  return (
    <Button
      variant="ghost"
      size="sm"
      aria-label={`Remove ${row.name}`}
      className="h-auto p-0 text-xs text-destructive underline hover:bg-transparent hover:no-underline"
      onClick={() => void handleClick()}
      disabled={isPending}
    >
      {isPending ? 'Removing…' : 'Remove'}
    </Button>
  )
}
