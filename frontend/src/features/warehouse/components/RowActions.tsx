import { Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface RowActionsProps {
  onEdit: () => void
  onRemove: () => void
  removePending?: boolean
}

export function RowActions({ onEdit, onRemove, removePending }: RowActionsProps) {
  return (
    <div className="flex gap-3">
      <Button
        variant="ghost"
        size="icon"
        aria-label="Edit item"
        className="h-auto w-auto p-0"
        onClick={onEdit}
      >
        <Pencil size={18} />
      </Button>
      <Button
        variant="ghost"
        size="icon"
        aria-label="Remove item"
        className="h-auto w-auto p-0"
        onClick={onRemove}
        disabled={removePending}
      >
        <Trash2 size={18} className="text-destructive" />
      </Button>
    </div>
  )
}
