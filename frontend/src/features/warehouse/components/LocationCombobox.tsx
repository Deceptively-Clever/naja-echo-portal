import { useState } from 'react'
import { ChevronsUpDown } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { useLocationSearch } from '../hooks/useLocationSearch'
import { useDebounce } from '../hooks/useDebounce'
import type { LocationOption } from '../schemas/locationSchemas'

interface LocationComboboxProps {
  value?: string
  onValueChange: (location: LocationOption | null) => void
  placeholder?: string
  disabled?: boolean
  allowClear?: boolean
  'aria-label'?: string
}

export function LocationCombobox({
  value,
  onValueChange,
  placeholder = 'Select a location…',
  disabled = false,
  allowClear = false,
  'aria-label': ariaLabel,
}: LocationComboboxProps) {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounce(search, 300)
  const { data: locations = [], isLoading } = useLocationSearch(debouncedSearch || undefined, 25)

  // Keep the last known LocationOption so the button always shows a name even when
  // the current search results don't include the selected id (e.g. not in first 25).
  const [lastSelected, setLastSelected] = useState<LocationOption | undefined>(undefined)
  const selectedInResults = locations.find((l) => l.id === value)
  const displayName = selectedInResults?.name ?? (value ? lastSelected?.name : undefined)

  const handleSelect = (location: LocationOption) => {
    setLastSelected(location)
    onValueChange(location)
    setOpen(false)
    setSearch('')
  }

  const handleClear = () => {
    setLastSelected(undefined)
    onValueChange(null)
    setOpen(false)
    setSearch('')
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          aria-label={ariaLabel}
          disabled={disabled}
          className="w-full justify-between"
        >
          <span className="truncate">
            {displayName ?? placeholder}
          </span>
          <ChevronsUpDown className="ml-2 size-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-full p-0" align="start" onWheelCapture={(e) => e.stopPropagation()}>
        <Command shouldFilter={false}>
          <CommandInput
            placeholder="Search locations…"
            value={search}
            onValueChange={setSearch}
          />
          <CommandList>
            {isLoading && <CommandEmpty>Loading locations…</CommandEmpty>}
            {!isLoading && locations.length === 0 && (
              <CommandEmpty>No locations found — import stations and cities first</CommandEmpty>
            )}
            {(allowClear || locations.length > 0) && (
              <CommandGroup>
                {allowClear && (
                  <CommandItem
                    value="__clear__"
                    onSelect={handleClear}
                  >
                    All locations
                  </CommandItem>
                )}
                {locations.map((location) => (
                  <CommandItem
                    key={location.id}
                    value={location.id}
                    onSelect={() => handleSelect(location)}
                  >
                    {location.name}
                  </CommandItem>
                ))}
              </CommandGroup>
            )}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
