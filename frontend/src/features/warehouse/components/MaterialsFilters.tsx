import { Combobox } from '@/components/ui/combobox'
import { Button } from '@/components/ui/button'
import type { MaterialFiltersResponse, MaterialFilterFormValues } from '../schemas/materialSchemas'

const DEFAULT_VALUES: MaterialFilterFormValues = {
  material: '',
  ownerUserId: '',
  location: '',
  qualityMin: 1,
  qualityMax: 1000,
}

interface Props {
  filters: MaterialFiltersResponse
  values: MaterialFilterFormValues
  onFilterChange: (values: MaterialFilterFormValues) => void
}

export function MaterialsFilters({ filters, values, onFilterChange }: Props) {
  const update = (patch: Partial<MaterialFilterFormValues>) => onFilterChange({ ...values, ...patch })

  const ownerOptions = filters.owners.map((o) => ({ value: o.userId, label: o.displayName }))
  const locationOptions = filters.locations.map((l) => ({ value: l, label: l }))

  return (
    <div className="flex flex-wrap items-end gap-3">
      <div className="flex flex-col gap-1">
        <label htmlFor="filter-material" className="text-xs text-muted-foreground">Material</label>
        <input
          id="filter-material"
          aria-label="Material"
          className="h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground"
          value={values.material}
          onChange={(e) => update({ material: e.target.value })}
          placeholder="Filter by name or code…"
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
        <label className="text-xs text-muted-foreground">Location</label>
        <Combobox
          options={locationOptions}
          value={values.location}
          onValueChange={(v) => update({ location: v })}
          placeholder="All locations"
          searchPlaceholder="Search locations…"
          className="w-40"
          aria-label="Location"
        />
      </div>

      <div className="flex flex-col gap-1 w-56">
        <label className="text-xs text-muted-foreground">Quality</label>
        <div className="flex items-center gap-2">
          <label htmlFor="filter-quality-min" className="sr-only">Minimum Quality</label>
          <input
            id="filter-quality-min"
            aria-label="Minimum Quality"
            type="number"
            min={1}
            max={1000}
            className="h-8 w-20 rounded-md border border-input bg-background px-2 text-sm text-foreground"
            value={values.qualityMin}
            onChange={(e) => update({ qualityMin: Number(e.target.value) })}
          />
          <span className="text-xs text-muted-foreground">to</span>
          <label htmlFor="filter-quality-max" className="sr-only">Maximum Quality</label>
          <input
            id="filter-quality-max"
            aria-label="Maximum Quality"
            type="number"
            min={1}
            max={1000}
            className="h-8 w-20 rounded-md border border-input bg-background px-2 text-sm text-foreground"
            value={values.qualityMax}
            onChange={(e) => update({ qualityMax: Number(e.target.value) })}
          />
        </div>
      </div>

      <Button variant="outline" size="sm" onClick={() => onFilterChange(DEFAULT_VALUES)}>
        Clear
      </Button>
    </div>
  )
}
