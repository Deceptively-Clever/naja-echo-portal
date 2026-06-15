import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { useChangeInventoryQuantity } from '../hooks/useChangeInventoryQuantity'
import { EditQuantityControl } from './EditQuantityControl'
import { RemoveInventoryButton } from './RemoveInventoryButton'
import type { ShipComponentRow } from '../schemas/shipComponentSchemas'

interface Props {
  rows: ShipComponentRow[]
  isQuartermaster: boolean
  onRemove: (id: string) => Promise<void>
}

export function ShipComponentsTable({ rows, isQuartermaster, onRemove }: Props) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const changeQty = useChangeInventoryQuantity()

  if (rows.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        No ship components in inventory.
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Type</TableHead>
          <TableHead>Class</TableHead>
          <TableHead>Size</TableHead>
          <TableHead>Grade</TableHead>
          <TableHead>Qty</TableHead>
          <TableHead>Owner</TableHead>
          <TableHead>Location</TableHead>
          {isQuartermaster && <TableHead>Actions</TableHead>}
        </TableRow>
      </TableHeader>
      <TableBody>
        {rows.map((row) => (
          <TableRow key={row.id}>
            <TableCell>{row.name}</TableCell>
            <TableCell>{row.type ?? '—'}</TableCell>
            <TableCell>{row.class ?? 'Unknown'}</TableCell>
            <TableCell>{row.size != null ? String(row.size) : 'Unknown'}</TableCell>
            <TableCell>{row.grade ?? 'Unknown'}</TableCell>
            <TableCell>
              {isQuartermaster && editingId === row.id ? (
                <EditQuantityControl
                  currentQuantity={row.quantity}
                  isPending={changeQty.isPending}
                  onConfirm={async (qty) => {
                    await changeQty.mutateAsync({ id: row.id, quantity: qty })
                    setEditingId(null)
                  }}
                  onCancel={() => setEditingId(null)}
                />
              ) : (
                <span>{row.quantity}</span>
              )}
            </TableCell>
            <TableCell>{row.ownerDisplayName}</TableCell>
            <TableCell>{row.location}</TableCell>
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
                  <RemoveInventoryButton row={row} onRemove={onRemove} />
                </div>
              </TableCell>
            )}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
