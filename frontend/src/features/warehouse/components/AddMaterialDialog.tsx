import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogFooter, DialogTitle } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useCommoditySearch } from '../hooks/useCommoditySearch'
import { useAddMaterial } from '../hooks/useAddMaterial'
import { useMaterialFilters } from '../hooks/useMaterialFilters'
import { StationCombobox } from './StationCombobox'
import { useDebounce } from '../hooks/useDebounce'
import type { CommodityCatalogItem } from '../schemas/materialSchemas'
import type { StationOption } from '../schemas/stationSchemas'

interface Props {
  open: boolean
  onClose: (opts?: { rememberedStation?: StationOption; rememberedOwnerId?: string }) => void
  currentUserId: string
  rememberedStation?: StationOption
  rememberedOwnerId?: string
}

export function AddMaterialDialog({
  open,
  onClose,
  currentUserId,
  rememberedStation,
  rememberedOwnerId = '',
}: Props) {
  const [search, setSearch] = useState('')
  const [selectedCommodity, setSelectedCommodity] = useState<CommodityCatalogItem | null>(null)
  const [ownerUserId, setOwnerUserId] = useState(rememberedOwnerId || currentUserId)
  const [stationId, setStationId] = useState<string | undefined>(rememberedStation?.id)
  const [stationName, setStationName] = useState(rememberedStation?.name ?? '')
  const [quantity, setQuantity] = useState(1)
  const [quality, setQuality] = useState(500)
  const [error, setError] = useState('')
  const [prevOpen, setPrevOpen] = useState(open)

  if (prevOpen !== open) {
    setPrevOpen(open)
    if (open) {
      setSearch('')
      setSelectedCommodity(null)
      setOwnerUserId(rememberedOwnerId || currentUserId)
      setStationId(rememberedStation?.id)
      setStationName(rememberedStation?.name ?? '')
      setQuantity(1)
      setQuality(500)
      setError('')
    }
  }

  const debouncedSearch = useDebounce(search, 300)

  const { data: catalogData } = useCommoditySearch(debouncedSearch || undefined)
  const { data: filtersData } = useMaterialFilters()
  const addMaterial = useAddMaterial()

  const owners = filtersData?.owners ?? []
  const results = catalogData?.commodities ?? []

  async function handleSubmit() {
    if (!selectedCommodity) { setError('Select a commodity from the catalog.'); return }
    if (!stationId) { setError('Select a station.'); return }
    if (quantity <= 0) { setError('Quantity must be greater than 0.'); return }
    if (!Number.isInteger(quality) || quality < 1 || quality > 1000) {
      setError('Quality must be an integer between 1 and 1000.')
      return
    }
    setError('')

    try {
      await addMaterial.mutateAsync({
        commodityId: selectedCommodity.commodityId,
        ownerUserId,
        location: stationName,
        quantity,
        quality,
        stationId,
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
      return
    }

    onClose({ rememberedStation: { id: stationId, name: stationName }, rememberedOwnerId: ownerUserId })
  }

  const isPending = addMaterial.isPending

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose() }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Add Material</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-3 px-6">
          <div className="flex flex-col gap-1">
            <label htmlFor="add-material-search" className="text-sm font-medium">
              Search Commodities
            </label>
            <input
              id="add-material-search"
              aria-label="Search commodities"
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground"
              value={search}
              onChange={(e) => { setSearch(e.target.value); setSelectedCommodity(null) }}
              placeholder="Search materials…"
            />
            <div
              role="listbox"
              aria-label="Search results"
              className="mt-1 h-[180px] overflow-auto rounded border bg-background shadow text-sm"
            >
              {results.length > 0 && !selectedCommodity && (
                <ul>
                  {results.map((c) => (
                    <li
                      key={c.commodityId}
                      role="option"
                      aria-selected={false}
                      tabIndex={0}
                      className="cursor-pointer px-3 py-2 hover:bg-muted focus-visible:bg-muted outline-none"
                      onClick={() => { setSelectedCommodity(c); setSearch(c.name) }}
                      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { setSelectedCommodity(c); setSearch(c.name) } }}
                    >
                      {c.name}
                      {c.code && <span className="ml-1 text-muted-foreground">({c.code})</span>}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>

          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium">Owner</label>
            <Select value={ownerUserId} onValueChange={setOwnerUserId}>
              <SelectTrigger aria-label="Owner">
                <SelectValue placeholder="Select owner" />
              </SelectTrigger>
              <SelectContent>
                {owners.map((o) => (
                  <SelectItem key={o.userId} value={o.userId}>
                    {o.displayName}{o.userId === currentUserId ? ' (you)' : ''}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium">Station</label>
            <StationCombobox
              value={stationId}
              onValueChange={(id, name) => { setStationId(id); setStationName(name) }}
              placeholder="Select a station…"
            />
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="add-material-quantity" className="text-sm font-medium">
              Quantity
            </label>
            <input
              id="add-material-quantity"
              aria-label="Quantity"
              type="number"
              inputMode="decimal"
              step="0.001"
              min={0.001}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground"
              value={quantity}
              onChange={(e) => setQuantity(Number(e.target.value))}
            />
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="add-material-quality" className="text-sm font-medium">
              Quality
            </label>
            <input
              id="add-material-quality"
              aria-label="Quality"
              type="number"
              min={1}
              max={1000}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground"
              value={quality}
              onChange={(e) => setQuality(Number(e.target.value))}
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onClose()}>
            Cancel
          </Button>
          <Button onClick={() => void handleSubmit()} disabled={isPending}>
            {isPending ? 'Adding…' : 'Add Material'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
