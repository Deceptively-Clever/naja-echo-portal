import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'
import { useShipComponents } from '../hooks/useShipComponents'
import { useIsQuartermaster } from '../hooks/useIsQuartermaster'
import { useRemoveInventoryItem } from '../hooks/useRemoveInventoryItem'
import { ShipComponentsTable } from '../components/ShipComponentsTable'
import { ShipComponentsFilters, type ShipComponentFilterValues } from '../components/ShipComponentsFilters'
import { AddInventoryDialog } from '../components/AddInventoryDialog'
import { Skeleton } from '@/components/ui/skeleton'

const emptyFilters: ShipComponentFilterValues = {
  name: '',
  type: '',
  class: '',
  size: '',
  grade: '',
}

export function ShipComponentsView() {
  const isQuartermaster = useIsQuartermaster()
  const { data: session } = useCurrentUser()
  const [filterValues, setFilterValues] = useState<ShipComponentFilterValues>(emptyFilters)
  const [addOpen, setAddOpen] = useState(false)
  const currentUserId = session?.authenticated === true ? session.user.id : ''

  const activeFilters = {
    name: filterValues.name || undefined,
    type: filterValues.type ? [filterValues.type] : undefined,
    class: filterValues.class ? [filterValues.class] : undefined,
    size: filterValues.size ? [parseInt(filterValues.size, 10)] : undefined,
    grade: filterValues.grade ? [filterValues.grade] : undefined,
  }

  const { data, isLoading } = useShipComponents(activeFilters)
  const removeItem = useRemoveInventoryItem()

  const rows = data?.items ?? []

  async function handleRemove(id: string) {
    await removeItem.mutateAsync(id)
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Ship Components</h1>
        {isQuartermaster && (
          <Button onClick={() => setAddOpen(true)}>+ Add Component</Button>
        )}
      </div>

      <ShipComponentsFilters
        values={filterValues}
        onFilterChange={setFilterValues}
      />

      {isLoading ? (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </div>
      ) : rows.length === 0 ? (
        <div className="py-12 text-center text-muted-foreground">
          No ship components match the current filters.
        </div>
      ) : (
        <ShipComponentsTable
          rows={rows}
          isQuartermaster={isQuartermaster}
          onRemove={handleRemove}
        />
      )}
      {isQuartermaster && (
        <AddInventoryDialog
          scope="ship-components"
          open={addOpen}
          onClose={() => setAddOpen(false)}
          currentUserId={currentUserId}
        />
      )}
    </div>
  )
}
