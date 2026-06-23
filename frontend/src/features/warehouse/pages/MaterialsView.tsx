import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'
import { useMaterials } from '../hooks/useMaterials'
import { useMaterialFilters } from '../hooks/useMaterialFilters'
import { useIsQuartermaster } from '../hooks/useIsQuartermaster'
import { useRemoveMaterial } from '../hooks/useRemoveMaterial'
import { MaterialsTable } from '../components/MaterialsTable'
import { MaterialsFilters } from '../components/MaterialsFilters'
import { AddMaterialDialog } from '../components/AddMaterialDialog'
import { Skeleton } from '@/components/ui/skeleton'
import type { MaterialFilterFormValues } from '../schemas/materialSchemas'
import type { LocationOption } from '../schemas/locationSchemas'

const emptyFilters: MaterialFilterFormValues = {
  material: '',
  ownerUserId: '',
  location: '',
  locationId: '',
  qualityMin: 1,
  qualityMax: 1000,
}

export function MaterialsView() {
  const { data: session } = useCurrentUser()
  const isQuartermaster = useIsQuartermaster()

  const [addOpen, setAddOpen] = useState(false)
  const [rememberedLocation, setRememberedLocation] = useState<LocationOption | undefined>(undefined)
  const [rememberedOwnerId, setRememberedOwnerId] = useState('')
  const [filterValues, setFilterValues] = useState<MaterialFilterFormValues>(emptyFilters)

  const activeFilters = {
    material: filterValues.material || undefined,
    ownerUserId: filterValues.ownerUserId || undefined,
    location: filterValues.location || undefined,
    qualityMin: filterValues.qualityMin !== 1 ? filterValues.qualityMin : undefined,
    qualityMax: filterValues.qualityMax !== 1000 ? filterValues.qualityMax : undefined,
  }
  const hasActiveFilters = Object.values(activeFilters).some((v) => v !== undefined)

  const { data: materialsData, isLoading } = useMaterials(activeFilters)
  const { data: filtersData } = useMaterialFilters()
  const rows = materialsData?.rows ?? []
  const filters = filtersData ?? { owners: [], locations: [] }
  const removeMaterial = useRemoveMaterial()

  const currentUserId = session?.authenticated === true ? session.user.id : ''

  function handleAddClose(opts?: { rememberedLocation?: LocationOption; rememberedOwnerId?: string }) {
    setAddOpen(false)
    if (opts?.rememberedLocation) setRememberedLocation(opts.rememberedLocation)
    if (opts?.rememberedOwnerId) setRememberedOwnerId(opts.rememberedOwnerId)
  }

  async function handleRemove(id: string) {
    await removeMaterial.mutateAsync(id)
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Warehouse Materials</h1>
        {isQuartermaster && (
          <Button onClick={() => setAddOpen(true)}>+ Add Material</Button>
        )}
      </div>

      <MaterialsFilters filters={filters} values={filterValues} onFilterChange={setFilterValues} />

      {isLoading ? (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </div>
      ) : (
        <MaterialsTable
          rows={rows}
          isQuartermaster={isQuartermaster}
          onRemove={isQuartermaster ? handleRemove : undefined}
          hasActiveFilters={hasActiveFilters}
        />
      )}

      {isQuartermaster && (
        <AddMaterialDialog
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
