import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogFooter, DialogTitle } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useCommoditySearch } from '../hooks/useCommoditySearch'
import { useAddMaterial } from '../hooks/useAddMaterial'
import { useMaterialFilters } from '../hooks/useMaterialFilters'
import type { CommodityCatalogItem } from '../schemas/materialSchemas'

interface Props {
  open: boolean
  onClose: (opts?: { rememberedLocation?: string; rememberedOwnerId?: string }) => void
  currentUserId: string
  rememberedLocation?: string
  rememberedOwnerId?: string
}

export function AddMaterialDialog({
  open,
  onClose,
  currentUserId,
  rememberedLocation = '',
  rememberedOwnerId = '',
}: Props) {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedCommodity, setSelectedCommodity] = useState<CommodityCatalogItem | null>(null)
  const [ownerUserId, setOwnerUserId] = useState(rememberedOwnerId || currentUserId)
  const [location, setLocation] = useState(rememberedLocation)
  const [quantity, setQuantity] = useState(1)
  const [quality, setQuality] = useState(500)
  const [error, setError] = useState('')
  const [prevOpen, setPrevOpen] = useState(open)

  if (prevOpen !== open) {
    setPrevOpen(open)
    if (open) {
      setSearch('')
      setDebouncedSearch('')
      setSelectedCommodity(null)
      setOwnerUserId(rememberedOwnerId || currentUserId)
      setLocation(rememberedLocation)
      setQuantity(1)
      setQuality(500)
      setError('')
    }
  }

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(timer)
  }, [search])

  const { data: catalogData } = useCommoditySearch(debouncedSearch || undefined)
  const { data: filtersData } = useMaterialFilters()
  const addMaterial = useAddMaterial()

  const owners = filtersData?.owners ?? []
  const locations = filtersData?.locations ?? []
  const results = catalogData?.commodities ?? []

  async function handleSubmit() {
    if (!selectedCommodity) { setError('Select a commodity from the catalog.'); return }
    if (!location.trim()) { setError('Location is required.'); return }
    if (quantity <= 0) { setError('Quantity must be greater than 0.'); return }
    if (!Number.isInteger(quality) || quality < 1 || quality > 1000) {
      setError('Quality must be an integer between 1 and 1000.')
      return
    }
    setError('')

    await addMaterial.mutateAsync({
      commodityId: selectedCommodity.commodityId,
      ownerUserId,
      location: location.trim(),
      quantity,
      quality,
    })

    onClose({ rememberedLocation: location.trim(), rememberedOwnerId: ownerUserId })
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
            <div className="mt-1 h-[180px] overflow-auto rounded border bg-background shadow text-sm">
              {results.length > 0 && !selectedCommodity && (
                <ul>
                  {results.map((c) => (
                    <li
                      key={c.commodityId}
                      className="cursor-pointer px-3 py-2 hover:bg-muted"
                      onClick={() => { setSelectedCommodity(c); setSearch(c.name) }}
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
            <label htmlFor="add-material-location" className="text-sm font-medium">
              Location
            </label>
            <input
              id="add-material-location"
              aria-label="Location"
              list="add-material-location-suggestions"
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              placeholder="e.g. Bay 1"
            />
            <datalist id="add-material-location-suggestions">
              {locations.map((loc) => (
                <option key={loc} value={loc}>{loc}</option>
              ))}
            </datalist>
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="add-material-quantity" className="text-sm font-medium">
              Quantity
            </label>
            <input
              id="add-material-quantity"
              aria-label="Quantity"
              type="number"
              step="0.01"
              min={0.01}
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
