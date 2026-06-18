import { useState } from 'react'
import { Button } from '@/components/ui/button'

interface Props {
  currentQuantity: number
  onConfirm: (quantity: number) => Promise<void>
  onCancel?: () => void
  isPending?: boolean
}

export function EditMaterialQuantityControl({ currentQuantity, onConfirm, onCancel, isPending }: Props) {
  const [value, setValue] = useState(currentQuantity)
  const [error, setError] = useState('')

  async function handleSave() {
    if (value <= 0) {
      setError('Quantity must be greater than 0.')
      return
    }
    setError('')
    await onConfirm(value)
  }

  return (
    <div className="flex flex-col gap-1">
      <div className="flex items-center gap-2">
        <label htmlFor="edit-material-qty-input" className="sr-only">Quantity</label>
        <input
          id="edit-material-qty-input"
          aria-label="Quantity"
          type="number"
          inputMode="decimal"
          step="0.001"
          min={0.001}
          className="h-8 w-24 rounded-md border border-input bg-background px-2 text-sm text-foreground"
          value={value}
          onChange={(e) => setValue(Number(e.target.value))}
        />
        <Button size="sm" onClick={() => void handleSave()} disabled={isPending}>
          {isPending ? 'Saving…' : 'Save'}
        </Button>
        {onCancel && (
          <Button size="sm" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
        )}
      </div>
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  )
}
