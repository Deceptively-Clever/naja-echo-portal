import { Combobox } from '@/components/ui/combobox'
import { StationCombobox } from './StationCombobox'

export interface ShipComponentFilterValues {
  name: string
  type: string
  class: string
  size: string
  grade: string
  station: string
  stationId: string
}

interface Props {
  values: ShipComponentFilterValues
  onFilterChange: (values: ShipComponentFilterValues) => void
}

const TYPE_OPTIONS = ['Coolers', 'Power Plants', 'Shield Generators', 'Quantum Drives'].map((v) => ({ value: v, label: v }))
const CLASS_OPTIONS = ['Military', 'Civilian', 'Industrial', 'Racing', 'Stealth'].map((v) => ({ value: v, label: v }))
const SIZE_OPTIONS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'].map((v) => ({ value: v, label: v }))
const GRADE_OPTIONS = ['A', 'B', 'C'].map((v) => ({ value: v, label: v }))

export function ShipComponentsFilters({ values, onFilterChange }: Props) {
  const update = (patch: Partial<ShipComponentFilterValues>) =>
    onFilterChange({ ...values, ...patch })

  return (
    <div className="flex flex-wrap gap-3">
      <div className="flex flex-col gap-1">
        <label htmlFor="sc-filter-name" className="text-xs text-muted-foreground">Name</label>
        <input
          id="sc-filter-name"
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
          options={TYPE_OPTIONS}
          value={values.type}
          onValueChange={(v) => update({ type: v })}
          placeholder="All types"
          searchPlaceholder="Search types…"
          className="w-44"
          aria-label="Type"
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Class</label>
        <Combobox
          options={CLASS_OPTIONS}
          value={values.class}
          onValueChange={(v) => update({ class: v })}
          placeholder="All classes"
          searchPlaceholder="Search classes…"
          className="w-36"
          aria-label="Class"
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Size</label>
        <Combobox
          options={SIZE_OPTIONS}
          value={values.size}
          onValueChange={(v) => update({ size: v })}
          placeholder="All sizes"
          searchPlaceholder="Search sizes…"
          className="w-28"
          aria-label="Size"
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Grade</label>
        <Combobox
          options={GRADE_OPTIONS}
          value={values.grade}
          onValueChange={(v) => update({ grade: v })}
          placeholder="All grades"
          searchPlaceholder="Search grades…"
          className="w-28"
          aria-label="Grade"
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground">Station</label>
        <StationCombobox
          value={values.stationId || undefined}
          onValueChange={(id, name) => update({ stationId: id, station: name })}
          placeholder="All stations"
          allowClear
        />
      </div>
    </div>
  )
}
