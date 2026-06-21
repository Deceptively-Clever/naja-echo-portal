import { useState } from 'react'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog'
import { EditMaterialDialog } from './EditMaterialDialog'
import { RowActions } from './RowActions'
import type { MaterialRow } from '../schemas/materialSchemas'

interface Props {
  rows: MaterialRow[]
  isQuartermaster?: boolean
  onRemove?: (id: string) => Promise<void>
  hasActiveFilters?: boolean
}

export function MaterialsTable({ rows, isQuartermaster = false, onRemove, hasActiveFilters = false }: Props) {
  const [editRow, setEditRow] = useState<MaterialRow | null>(null)
  const [removeRow, setRemoveRow] = useState<MaterialRow | null>(null)
  const [removePending, setRemovePending] = useState(false)

  const handleRemoveConfirm = async () => {
    if (!removeRow || !onRemove) return
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
        {hasActiveFilters ? 'No results match the current filters.' : 'No material inventory.'}
      </div>
    )
  }

  return (
    <>
      <EditMaterialDialog
        open={!!editRow}
        onOpenChange={(o) => { if (!o) setEditRow(null) }}
        row={editRow}
        onSuccess={() => setEditRow(null)}
      />
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Material</TableHead>
          <TableHead>Owner</TableHead>
          <TableHead>Station</TableHead>
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
            <TableCell>{row.quantity.toFixed(3)}</TableCell>
            <TableCell>{row.quality}</TableCell>
            {isQuartermaster && (
              <TableCell>
                <RowActions
                  onEdit={() => setEditRow(row)}
                  onRemove={onRemove ? () => setRemoveRow(row) : () => {}}
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
        title="Delete Material"
        description={`Are you sure you want to remove "${removeRow.materialName}" from inventory?`}
        isPending={removePending}
        onConfirm={handleRemoveConfirm}
      />
    )}
    </>
  )
}
