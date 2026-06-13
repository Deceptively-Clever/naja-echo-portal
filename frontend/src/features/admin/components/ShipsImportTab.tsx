import { useState } from 'react'
import { ImportShipsButton } from './ImportShipsButton'
import { ShipsTable } from './ShipsTable'
import { ShipsPagination } from './ShipsPagination'
import { ShipDetailSheet } from './ShipDetailSheet'
import { useShips } from '../hooks/useShips'
import type { ShipListItem } from '../schemas/shipSchemas'

export function ShipsImportTab() {
  const [page, setPage] = useState(1)
  const [selectedShip, setSelectedShip] = useState<ShipListItem | null>(null)
  const { data, isLoading } = useShips(page)

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Ships</h2>
        <ImportShipsButton />
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : data ? (
        <>
          <ShipsTable ships={data.items} onViewDetails={setSelectedShip} />
          <ShipsPagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onPageChange={setPage}
          />
        </>
      ) : null}

      <ShipDetailSheet ship={selectedShip} onClose={() => setSelectedShip(null)} />
    </div>
  )
}
