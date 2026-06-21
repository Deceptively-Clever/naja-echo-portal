import { useState } from 'react'
import { ChevronsUpDown } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { useStationSearch } from '../hooks/useStationSearch'
import type { StationOption } from '../schemas/stationSchemas'

interface StationComboboxProps {
  value?: string
  onValueChange: (id: string, name: string) => void
  placeholder?: string
  disabled?: boolean
  allowClear?: boolean
}

export function StationCombobox({
  value,
  onValueChange,
  placeholder = 'Select a station…',
  disabled = false,
  allowClear = false,
}: StationComboboxProps) {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const { data: stations = [], isLoading } = useStationSearch(search || undefined, 25)

  const selectedStation = stations.find((s) => s.id === value)

  const handleSelect = (station: StationOption) => {
    onValueChange(station.id, station.name)
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
          disabled={disabled}
          className="w-full justify-between"
        >
          <span className="truncate">
            {selectedStation?.name || placeholder}
          </span>
          <ChevronsUpDown className="ml-2 size-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-full p-0" align="start" onWheelCapture={(e) => e.stopPropagation()}>
        <Command shouldFilter={false}>
          <CommandInput
            placeholder="Search stations…"
            value={search}
            onValueChange={setSearch}
          />
          <CommandList>
            {isLoading && <CommandEmpty>Loading stations…</CommandEmpty>}
            {!isLoading && stations.length === 0 && <CommandEmpty>No stations found.</CommandEmpty>}
            {(allowClear || stations.length > 0) && (
              <CommandGroup>
                {allowClear && (
                  <CommandItem
                    value="__clear__"
                    onSelect={() => { onValueChange('', ''); setOpen(false); setSearch('') }}
                  >
                    All stations
                  </CommandItem>
                )}
                {stations.map((station) => (
                  <CommandItem
                    key={station.id}
                    value={station.id}
                    onSelect={() => handleSelect(station)}
                  >
                    {station.name}
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
