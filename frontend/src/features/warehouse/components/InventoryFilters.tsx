import { Combobox } from '@/components/ui/combobox'
import type { InventoryFiltersResponse } from '../schemas/inventorySchemas'

export interface FilterValues {
  name: string
  type: string
  subtype: string
  ownerUserId: string
  location: string
}

interface Props {
  filters: InventoryFiltersResponse
  values: FilterValues
  onFilterChange: (values: FilterValues) => void
}

export function InventoryFilters({ filters, values, onFilterChange }: Props) {
  const update = (patch: Partial<FilterValues>) => onFilterChange({ ...values, ...patch })

  const typeOptions = filters.types.map((t) => ({ value: t, label: t }))
  const subtypeOptions = filters.subtypes.map((s) => ({ value: s, label: s }))
  const ownerOptions = filters.owners.map((o) => ({ value: o.userId, label: o.displayName }))

  return (
    <div className="flex flex-wrap gap-3">
      <div className="flex flex-col gap-1">
        <label htmlFor="filter-name" className="text-xs text-muted-foreground">Name</label>
        <input
          id="filter-name"
          aria-label="Name"
          className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
          value={values.name}
          onChange={(e) => update({ name: e.target.value })}
          placeholder="Filter by name…"
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Type</label>
        <Combobox
          options={typeOptions}
          value={values.type}
          onValueChange={(v) => update({ type: v })}
          placeholder="All types"
          searchPlaceholder="Search types…"
          className="w-40"
          aria-label="Type"
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Subtype</label>
        <Combobox
          options={subtypeOptions}
          value={values.subtype}
          onValueChange={(v) => update({ subtype: v })}
          placeholder="All subtypes"
          searchPlaceholder="Search subtypes…"
          className="w-40"
          aria-label="Subtype"
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Owner</label>
        <Combobox
          options={ownerOptions}
          value={values.ownerUserId}
          onValueChange={(v) => update({ ownerUserId: v })}
          placeholder="All owners"
          searchPlaceholder="Search owners…"
          className="w-40"
          aria-label="Owner"
        />
      </div>

      <div className="flex flex-col gap-1">
        <label htmlFor="filter-location" className="text-xs text-muted-foreground">Location</label>
        <input
          id="filter-location"
          aria-label="Location"
          className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
          value={values.location}
          onChange={(e) => update({ location: e.target.value })}
          placeholder="Filter by location…"
        />
      </div>
    </div>
  )
}
