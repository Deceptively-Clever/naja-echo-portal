import { Badge } from '@/components/ui/badge'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { CommodityListItem } from '../schemas/commoditySchemas'

interface CommoditiesTableProps {
  commodities: CommodityListItem[]
}

export function CommoditiesTable({ commodities }: CommoditiesTableProps) {
  if (commodities.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-md border border-dashed py-12 text-center">
        <p className="text-muted-foreground">No commodities imported yet.</p>
        <p className="text-sm text-muted-foreground">Click &ldquo;Import Commodities&rdquo; to fetch data from the UEX feed.</p>
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Code</TableHead>
          <TableHead>Kind</TableHead>
          <TableHead>Status</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {commodities.map((commodity) => (
          <TableRow key={commodity.id}>
            <TableCell className="font-medium">{commodity.name}</TableCell>
            <TableCell className="text-muted-foreground">{commodity.code ?? '—'}</TableCell>
            <TableCell className="text-muted-foreground">{commodity.kind ?? '—'}</TableCell>
            <TableCell>
              {commodity.status === 'softDeleted' && (
                <Badge variant="secondary">Removed from feed</Badge>
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
