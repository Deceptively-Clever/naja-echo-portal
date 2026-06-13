import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { ShipListItem } from '../schemas/shipSchemas'

interface ShipsTableProps {
  ships: ShipListItem[]
  onViewDetails: (ship: ShipListItem) => void
}

export function ShipsTable({ ships, onViewDetails }: ShipsTableProps) {
  if (ships.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-md border border-dashed py-12 text-center">
        <p className="text-muted-foreground">No ships imported yet.</p>
        <p className="text-sm text-muted-foreground">Click &ldquo;Import Ships&rdquo; to fetch data from the UEX feed.</p>
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Company</TableHead>
          <TableHead>Status</TableHead>
          <TableHead className="w-32"></TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {ships.map((ship) => (
          <TableRow key={ship.id}>
            <TableCell className="font-medium">{ship.name}</TableCell>
            <TableCell className="text-muted-foreground">{ship.companyName ?? '—'}</TableCell>
            <TableCell>
              {ship.status === 'softDeleted' && (
                <Badge variant="secondary">Removed from feed</Badge>
              )}
            </TableCell>
            <TableCell>
              <Button variant="outline" size="sm" onClick={() => onViewDetails(ship)}>
                View Details
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
