import { useState } from 'react'
import { Button } from '@/components/ui/button'
import type { MaterialRow } from '../schemas/materialSchemas'

interface Props {
  row: MaterialRow
  onRemove: (id: string) => Promise<void>
}

export function RemoveMaterialButton({ row, onRemove }: Props) {
  const [isPending, setIsPending] = useState(false)

  async function handleClick() {
    if (!confirm(`Remove "${row.materialName}" from inventory?`)) return
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
      aria-label={`Remove ${row.materialName}`}
      className="h-auto p-0 text-xs text-destructive underline hover:bg-transparent hover:no-underline"
      onClick={() => void handleClick()}
      disabled={isPending}
    >
      {isPending ? 'Removing…' : 'Remove'}
    </Button>
  )
}
