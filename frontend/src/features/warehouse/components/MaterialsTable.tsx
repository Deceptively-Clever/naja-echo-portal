import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { useChangeMaterialQuantity } from '../hooks/useChangeMaterialQuantity'
import { EditMaterialQuantityControl } from './EditMaterialQuantityControl'
import { RemoveMaterialButton } from './RemoveMaterialButton'
import type { MaterialRow } from '../schemas/materialSchemas'

interface Props {
  rows: MaterialRow[]
  isQuartermaster?: boolean
  onRemove?: (id: string) => Promise<void>
  hasActiveFilters?: boolean
}

export function MaterialsTable({ rows, isQuartermaster = false, onRemove, hasActiveFilters = false }: Props) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const changeQty = useChangeMaterialQuantity()

  if (rows.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        {hasActiveFilters ? 'No results match the current filters.' : 'No material inventory.'}
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Material</TableHead>
          <TableHead>Owner</TableHead>
          <TableHead>Location</TableHead>
          <TableHead>Quantity</TableHead>
          <TableHead>Quality</TableHead>
          {isQuartermaster && <TableHead>Actions</TableHead>}
        </TableRow>
      </TableHeader>
      <TableBody>
        {rows.map((row) => (
          <TableRow key={row.id}>
            <TableCell>{row.materialName}</TableCell>
            <TableCell>{row.ownerDisplayName}</TableCell>
            <TableCell>{row.location}</TableCell>
            <TableCell>
              {isQuartermaster && editingId === row.id ? (
                <EditMaterialQuantityControl
                  currentQuantity={row.quantity}
                  isPending={changeQty.isPending}
                  onConfirm={async (qty) => {
                    await changeQty.mutateAsync({ id: row.id, quantity: qty })
                    setEditingId(null)
                  }}
                  onCancel={() => setEditingId(null)}
                />
              ) : (
                <span>{row.quantity.toFixed(3)}</span>
              )}
            </TableCell>
            <TableCell>{row.quality}</TableCell>
            {isQuartermaster && (
              <TableCell>
                <div className="flex gap-3">
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label="Change quantity"
                    className="h-auto p-0 text-xs underline hover:bg-transparent hover:no-underline"
                    onClick={() => setEditingId(editingId === row.id ? null : row.id)}
                  >
                    Edit Qty
                  </Button>
                  {onRemove && <RemoveMaterialButton row={row} onRemove={onRemove} />}
                </div>
              </TableCell>
            )}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
