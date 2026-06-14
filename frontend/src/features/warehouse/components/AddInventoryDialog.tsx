import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogFooter, DialogTitle } from '@/components/ui/dialog'
import { useCatalogItemSearch } from '../hooks/useCatalogItemSearch'
import { useAddInventoryItem } from '../hooks/useAddInventoryItem'
import type { CatalogItem } from '../schemas/inventorySchemas'

interface Props {
  open: boolean
  onClose: (opts?: { rememberedLocation?: string; rememberedOwnerId?: string }) => void
  currentUserId: string
  rememberedLocation?: string
  rememberedOwnerId?: string
}

export function AddInventoryDialog({
  open,
  onClose,
  currentUserId,
  rememberedLocation = '',
  rememberedOwnerId = '',
}: Props) {
  const [search, setSearch] = useState('')
  const [selectedItem, setSelectedItem] = useState<CatalogItem | null>(null)
  const [ownerUserId, setOwnerUserId] = useState(rememberedOwnerId || currentUserId)
  const [location, setLocation] = useState(rememberedLocation)
  const [quantity, setQuantity] = useState(1)
  const [error, setError] = useState('')

  useEffect(() => {
    if (open) {
      setSearch('')
      setSelectedItem(null)
      setOwnerUserId(rememberedOwnerId || currentUserId)
      setLocation(rememberedLocation)
      setQuantity(1)
      setError('')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const { data: catalogData } = useCatalogItemSearch(search || undefined)
  const addItem = useAddInventoryItem()

  const results = catalogData?.items ?? []

  async function handleSubmit() {
    if (!selectedItem) { setError('Select an item from the catalog.'); return }
    if (!location.trim()) { setError('Location is required.'); return }
    if (quantity < 1) { setError('Quantity must be at least 1.'); return }
    setError('')

    await addItem.mutateAsync({
      itemId: selectedItem.itemId,
      ownerUserId,
      location: location.trim(),
      quantity,
    })

    onClose({ rememberedLocation: location.trim(), rememberedOwnerId: ownerUserId })
  }

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose() }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Add Item to Inventory</DialogTitle>
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
            {results.length > 0 && !selectedItem && (
              <ul className="mt-1 max-h-40 overflow-auto rounded border bg-background shadow text-sm">
                {results.map((item) => (
                  <li
                    key={item.itemId}
                    className="cursor-pointer px-3 py-2 hover:bg-muted"
                    onClick={() => { setSelectedItem(item); setSearch(item.name) }}
                  >
                    {item.name}
                    {item.type && <span className="ml-1 text-muted-foreground">({item.type})</span>}
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="add-item-location" className="text-sm font-medium">
              Location
            </label>
            <input
              id="add-item-location"
              aria-label="Location"
              className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              placeholder="e.g. Bay 1"
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

          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onClose()}>
            Cancel
          </Button>
          <Button onClick={() => void handleSubmit()} disabled={addItem.isPending}>
            {addItem.isPending ? 'Adding…' : 'Add Item'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
