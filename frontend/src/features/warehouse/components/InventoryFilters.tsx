import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
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
        <Select value={values.type} onValueChange={(v) => update({ type: v === '__all__' ? '' : v })}>
          <SelectTrigger aria-label="Type" className="w-40">
            <SelectValue placeholder="All types" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">All types</SelectItem>
            {filters.types.map((t) => (
              <SelectItem key={t} value={t}>{t}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Subtype</label>
        <Select value={values.subtype} onValueChange={(v) => update({ subtype: v === '__all__' ? '' : v })}>
          <SelectTrigger aria-label="Subtype" className="w-40">
            <SelectValue placeholder="All subtypes" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">All subtypes</SelectItem>
            {filters.subtypes.map((s) => (
              <SelectItem key={s} value={s}>{s}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Owner</label>
        <Select value={values.ownerUserId} onValueChange={(v) => update({ ownerUserId: v === '__all__' ? '' : v })}>
          <SelectTrigger aria-label="Owner" className="w-40">
            <SelectValue placeholder="All owners" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">All owners</SelectItem>
            {filters.owners.map((o) => (
              <SelectItem key={o.userId} value={o.userId}>{o.displayName}</SelectItem>
            ))}
          </SelectContent>
        </Select>
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
