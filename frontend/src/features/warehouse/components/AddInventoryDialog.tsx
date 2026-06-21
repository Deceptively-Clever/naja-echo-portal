import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogFooter, DialogTitle } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useCatalogItemSearch } from '../hooks/useCatalogItemSearch'
import { useAddInventoryItem } from '../hooks/useAddInventoryItem'
import { useSystemsCatalogSearch } from '../hooks/useSystemsCatalogSearch'
import { useInventoryFilters } from '../hooks/useInventoryFilters'
import { StationCombobox } from './StationCombobox'
import { useDebounce } from '../hooks/useDebounce'
import type { CatalogItem } from '../schemas/inventorySchemas'
import type { SystemsCatalogItem } from '../api/shipComponentsApi'
import type { StationOption } from '../schemas/stationSchemas'

type AnyItem = CatalogItem | SystemsCatalogItem

interface BaseProps {
  open: boolean
  onClose: (opts?: { rememberedStation?: StationOption; rememberedOwnerId?: string }) => void
  currentUserId: string
  rememberedStation?: StationOption
  rememberedOwnerId?: string
}

interface InventoryProps extends BaseProps {
  scope?: 'inventory'
}

interface ShipComponentProps extends BaseProps {
  scope: 'ship-components'
}

type Props = InventoryProps | ShipComponentProps

export function AddInventoryDialog({
  open,
  onClose,
  currentUserId,
  rememberedStation,
  rememberedOwnerId = '',
  scope = 'inventory',
}: Props) {
  const [search, setSearch] = useState('')
  const [selectedItem, setSelectedItem] = useState<AnyItem | null>(null)
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
      setSelectedItem(null)
      setOwnerUserId(rememberedOwnerId || currentUserId)
      setStationId(rememberedStation?.id)
      setStationName(rememberedStation?.name ?? '')
      setQuantity(1)
      setQuality(500)
      setError('')
    }
  }

  const isShipComponents = scope === 'ship-components'
  const debouncedSearch = useDebounce(search, 300)

  const { data: catalogData } = useCatalogItemSearch(
    !isShipComponents ? (debouncedSearch || undefined) : undefined,
    !isShipComponents
  )
  const { data: systemsCatalogData } = useSystemsCatalogSearch(
    isShipComponents ? (debouncedSearch || undefined) : undefined,
    isShipComponents
  )
  const { data: filtersData } = useInventoryFilters()
  const addItem = useAddInventoryItem(scope)

  const owners = filtersData?.owners ?? []

  const results: AnyItem[] = isShipComponents
    ? (systemsCatalogData?.items ?? [])
    : (catalogData?.items ?? [])

  async function handleSubmit() {
    if (!selectedItem) { setError('Select an item from the catalog.'); return }
    if (!stationId) { setError('Select a station.'); return }
    if (quantity < 1) { setError('Quantity must be at least 1.'); return }
    if (!Number.isInteger(quality) || quality < 1 || quality > 1000) {
      setError('Quality must be an integer between 1 and 1000.')
      return
    }
    setError('')

    try {
      if (isShipComponents) {
        await addItem.mutateAsync({
          itemId: selectedItem.itemId,
          ownerUserId,
          location: stationName,
          quantity,
          quality,
        })
      } else {
        await addItem.mutateAsync({
          itemId: selectedItem.itemId,
          ownerUserId,
          location: stationName,
          quantity,
          quality,
          stationId,
        })
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
      return
    }

    onClose({ rememberedStation: { id: stationId, name: stationName }, rememberedOwnerId: ownerUserId })
  }

  const isPending = addItem.isPending
  const title = isShipComponents ? 'Add Ship Component' : 'Add Item to Inventory'

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose() }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-3 px-6">
          <div className="flex flex-col gap-1">
            <label htmlFor="add-item-search" className="text-sm font-medium">
              Search Catalog
            </label>
            <input
              id="add-item-search"
              aria-label="Search catalog"
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground"
              value={search}
              onChange={(e) => { setSearch(e.target.value); setSelectedItem(null) }}
              placeholder="Search items…"
            />
            <div
              role="listbox"
              aria-label="Search results"
              className="mt-1 h-[180px] overflow-auto rounded border bg-background shadow text-sm"
            >
              {results.length > 0 && !selectedItem && (
                <ul>
                  {results.map((item) => (
                    <li
                      key={item.itemId}
                      role="option"
                      aria-selected={false}
                      tabIndex={0}
                      className="cursor-pointer px-3 py-2 hover:bg-muted focus-visible:bg-muted outline-none"
                      onClick={() => { setSelectedItem(item); setSearch(item.name) }}
                      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { setSelectedItem(item); setSearch(item.name) } }}
                    >
                      {item.name}
                      {item.type && <span className="ml-1 text-muted-foreground">({item.type})</span>}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>

          {isShipComponents && selectedItem && (
            <div className="rounded border bg-muted/40 p-3 text-sm flex flex-col gap-1">
              <div className="text-muted-foreground">Name: <span className="text-foreground font-medium">{selectedItem.name}</span></div>
              <div className="text-muted-foreground">Type: <span className="text-foreground">{selectedItem.type ?? '—'}</span></div>
              <div className="text-muted-foreground text-xs">Class, Size, and Grade are derived from UEX data on first add.</div>
            </div>
          )}

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
            <label htmlFor="add-item-quantity" className="text-sm font-medium">
              Quantity
            </label>
            <input
              id="add-item-quantity"
              aria-label="Quantity"
              type="number"
              min={1}
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground"
              value={quantity}
              onChange={(e) => setQuantity(Number(e.target.value))}
            />
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="add-item-quality" className="text-sm font-medium">
              Quality
            </label>
            <input
              id="add-item-quality"
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
            {isPending ? 'Adding…' : 'Add Item'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
