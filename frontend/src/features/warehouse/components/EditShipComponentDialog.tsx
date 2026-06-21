import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { StationCombobox } from './StationCombobox'
import { useInventoryFilters } from '../hooks/useInventoryFilters'
import { useUpdateInventoryItem } from '../hooks/useUpdateInventoryItem'
import type { ShipComponentRow } from '../schemas/shipComponentSchemas'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  row: ShipComponentRow | null
  onSuccess?: () => void
}

export function EditShipComponentDialog({ open, onOpenChange, row, onSuccess }: Props) {
  const [ownerUserId, setOwnerUserId] = useState<string>('')
  const [stationId, setStationId] = useState<string>('')
  const [quantity, setQuantity] = useState<string>('')

  const filters = useInventoryFilters()
  const mutation = useUpdateInventoryItem()

  const [prevOpen, setPrevOpen] = useState(false)
  if (open !== prevOpen) {
    setPrevOpen(open)
    if (open && row) {
      setOwnerUserId(row.ownerUserId)
      setStationId(row.stationId ?? '')
      setQuantity(String(row.quantity))
    }
  }

  useEffect(() => {
    if (open) mutation.reset()
  }, [open]) // eslint-disable-line react-hooks/exhaustive-deps

  const handleConfirm = async () => {
    if (!row || !ownerUserId || !stationId || !quantity) return

    await mutation.mutateAsync({
      id: row.id,
      ownerUserId,
      stationId,
      quantity: Number(quantity),
    })
    onSuccess?.()
    onOpenChange(false)
  }

  const quantityNum = Number(quantity)
  const isValid = ownerUserId && stationId && quantityNum > 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Ship Component</DialogTitle>
        </DialogHeader>

        <div className="space-y-4 px-6 py-2">
          <div className="space-y-2">
            <label htmlFor="owner-select" className="text-sm font-medium">
              Owner
            </label>
            <Select value={ownerUserId} onValueChange={setOwnerUserId} disabled={mutation.isPending}>
              <SelectTrigger id="owner-select">
                <SelectValue placeholder="Select owner" />
              </SelectTrigger>
              <SelectContent>
                {filters.data?.owners.map((owner) => (
                  <SelectItem key={owner.userId} value={owner.userId}>
                    {owner.displayName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">
              Station
            </label>
            <StationCombobox
              value={stationId}
              onValueChange={(id) => setStationId(id)}
              disabled={mutation.isPending}
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="quantity-input" className="text-sm font-medium">
              Quantity
            </label>
            <input
              id="quantity-input"
              type="number"
              min="1"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              disabled={mutation.isPending}
              placeholder="1"
              className="w-full rounded border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
            />
          </div>

          {mutation.error && (
            <p className="text-sm text-destructive">
              Update failed: {mutation.error instanceof Error ? mutation.error.message : 'Unknown error'}
            </p>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            Cancel
          </Button>
          <Button onClick={() => void handleConfirm()} disabled={!isValid || mutation.isPending}>
            {mutation.isPending ? 'Saving…' : 'Confirm'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
