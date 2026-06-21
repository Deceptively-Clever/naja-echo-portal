import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { StationCombobox } from './StationCombobox'
import { useTransferItemStation } from '../hooks/useTransferItemStation'
import { useTransferMaterialStation } from '../hooks/useTransferMaterialStation'
import { useLastTransferStation } from '../hooks/useLastTransferStation'
import type { StationOption } from '../schemas/stationSchemas'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  rowId: string
  entityType: 'item' | 'material'
  onSuccess?: () => void
}

export function TransferStationDialog({ open, onOpenChange, rowId, entityType, onSuccess }: Props) {
  const { lastStation, setLastStation } = useLastTransferStation()
  const [selectedStation, setSelectedStation] = useState<StationOption | undefined>(undefined)

  const transferItem = useTransferItemStation()
  const transferMaterial = useTransferMaterialStation()

  const mutation = entityType === 'item' ? transferItem : transferMaterial
  const isPending = mutation.isPending
  const mutationError = mutation.error

  useEffect(() => {
    if (open) {
      setSelectedStation(lastStation)
      mutation.reset()
    }
  }, [open]) // eslint-disable-line react-hooks/exhaustive-deps

  const handleConfirm = async () => {
    if (!selectedStation) return

    await mutation.mutateAsync({ id: rowId, stationId: selectedStation.id })
    setLastStation(selectedStation)
    onSuccess?.()
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Transfer to Station</DialogTitle>
        </DialogHeader>

        <div className="px-6 py-2 flex flex-col gap-3">
          <p className="text-sm text-muted-foreground">
            Select a station to transfer this {entityType === 'item' ? 'item' : 'material'} to.
          </p>
          <StationCombobox
            value={selectedStation?.id}
            onValueChange={(id, name) => setSelectedStation({ id, name })}
            disabled={isPending}
          />
          {mutationError && (
            <p className="text-sm text-destructive">
              Transfer failed: {mutationError instanceof Error ? mutationError.message : 'Unknown error'}
            </p>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button onClick={() => void handleConfirm()} disabled={!selectedStation || isPending}>
            {isPending ? 'Transferring…' : 'Confirm Transfer'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
