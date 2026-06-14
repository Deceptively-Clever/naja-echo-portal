import { useState } from 'react'
import { ImportCommoditiesButton } from './ImportCommoditiesButton'
import { CommoditiesTable } from './CommoditiesTable'
import { ShipsPagination } from './ShipsPagination'
import { useCommodities } from '../hooks/useCommodities'

export function CommoditiesImportTab() {
  const [page, setPage] = useState(1)
  const { data, isLoading } = useCommodities(page)

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">Commodities</h2>
          <p className="text-sm text-muted-foreground">
            Import commodity catalog data from the UEX Corp feed.
          </p>
        </div>
        <ImportCommoditiesButton />
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : data ? (
        <>
          <CommoditiesTable commodities={data.items} />
          <ShipsPagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onPageChange={setPage}
          />
        </>
      ) : null}
    </div>
  )
}
