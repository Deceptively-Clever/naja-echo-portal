import { useState } from 'react'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog'
import { EditShipComponentDialog } from './EditShipComponentDialog'
import { RowActions } from './RowActions'
import type { ShipComponentRow } from '../schemas/shipComponentSchemas'

interface Props {
  rows: ShipComponentRow[]
  isQuartermaster: boolean
  onRemove: (id: string) => Promise<void>
}

export function ShipComponentsTable({ rows, isQuartermaster, onRemove }: Props) {
  const [editRow, setEditRow] = useState<ShipComponentRow | null>(null)
  const [removeRow, setRemoveRow] = useState<ShipComponentRow | null>(null)
  const [removePending, setRemovePending] = useState(false)

  const handleRemoveConfirm = async () => {
    if (!removeRow) return
    setRemovePending(true)
    try {
      await onRemove(removeRow.id)
      setRemoveRow(null)
    } finally {
      setRemovePending(false)
    }
  }

  if (rows.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        No ship components in inventory.
      </div>
    )
  }

  return (
    <>
      {editRow && (
        <EditShipComponentDialog
          key={editRow.id}
          open={!!editRow}
          onOpenChange={(o) => { if (!o) setEditRow(null) }}
          row={editRow}
          onSuccess={() => setEditRow(null)}
        />
      )}
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Type</TableHead>
          <TableHead>Class</TableHead>
          <TableHead>Size</TableHead>
          <TableHead>Grade</TableHead>
          <TableHead>Qty</TableHead>
          <TableHead>Quality</TableHead>
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
            <TableCell>{row.quantity}</TableCell>
            <TableCell>{row.quality}</TableCell>
            <TableCell>{row.ownerDisplayName}</TableCell>
            <TableCell>{row.location}</TableCell>
            {isQuartermaster && (
              <TableCell>
                <RowActions
                  onEdit={() => setEditRow(row)}
                  onRemove={() => setRemoveRow(row)}
                  removePending={removePending && removeRow?.id === row.id}
                />
              </TableCell>
            )}
          </TableRow>
        ))}
      </TableBody>
    </Table>
    {removeRow && (
      <ConfirmDeleteDialog
        open={!!removeRow}
        onOpenChange={(o) => {
          if (!o) setRemoveRow(null)
        }}
        title="Delete Ship Component"
        description={`Are you sure you want to remove "${removeRow.name}" from inventory?`}
        isPending={removePending}
        onConfirm={handleRemoveConfirm}
      />
    )}
    </>
  )
}
