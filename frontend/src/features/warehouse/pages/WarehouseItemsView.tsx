import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'
import { useInventory } from '../hooks/useInventory'
import { useInventoryFilters } from '../hooks/useInventoryFilters'
import { useIsQuartermaster } from '../hooks/useIsQuartermaster'
import { useRemoveInventoryItem } from '../hooks/useRemoveInventoryItem'
import { InventoryTable } from '../components/InventoryTable'
import { InventoryFilters, type FilterValues } from '../components/InventoryFilters'
import { AddInventoryDialog } from '../components/AddInventoryDialog'

const emptyFilters: FilterValues = {
  name: '',
  type: '',
  subtype: '',
  ownerUserId: '',
  location: '',
}

export function WarehouseItemsView() {
  const { data: session } = useCurrentUser()
  const isQuartermaster = useIsQuartermaster()

  const [filterValues, setFilterValues] = useState<FilterValues>(emptyFilters)
  const [addOpen, setAddOpen] = useState(false)
  const [rememberedLocation, setRememberedLocation] = useState('')
  const [rememberedOwnerId, setRememberedOwnerId] = useState('')

  const activeFilters = {
    name: filterValues.name || undefined,
    type: filterValues.type || undefined,
    subtype: filterValues.subtype || undefined,
    ownerUserId: filterValues.ownerUserId || undefined,
    location: filterValues.location || undefined,
  }

  const { data: inventoryData, isLoading } = useInventory(activeFilters)
  const { data: filtersData } = useInventoryFilters()
  const removeItem = useRemoveInventoryItem()

  const rows = inventoryData?.items ?? []
  const filters = filtersData ?? { types: [], subtypes: [], owners: [] }

  const currentUserId =
    session?.authenticated === true ? session.user.id : ''

  function handleAddClose(opts?: { rememberedLocation?: string; rememberedOwnerId?: string }) {
    setAddOpen(false)
    if (opts?.rememberedLocation) setRememberedLocation(opts.rememberedLocation)
    if (opts?.rememberedOwnerId) setRememberedOwnerId(opts.rememberedOwnerId)
  }

  async function handleRemove(id: string) {
    await removeItem.mutateAsync(id)
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Warehouse Items</h1>
        {isQuartermaster && (
          <Button onClick={() => setAddOpen(true)}>+ Add Item</Button>
        )}
      </div>

      <InventoryFilters
        filters={filters}
        values={filterValues}
        onFilterChange={setFilterValues}
      />

      {isLoading ? (
        <div className="py-8 text-center text-muted-foreground">Loading…</div>
      ) : (
        <InventoryTable
          rows={rows}
          isQuartermaster={isQuartermaster}
          onRemove={handleRemove}
        />
      )}

      {isQuartermaster && (
        <AddInventoryDialog
          open={addOpen}
          onClose={handleAddClose}
          currentUserId={currentUserId}
          rememberedLocation={rememberedLocation}
          rememberedOwnerId={rememberedOwnerId}
        />
      )}
    </div>
  )
}
