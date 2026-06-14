import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
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

  if (!open) return null

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
    <div role="dialog" aria-modal="true" className="fixed inset-0 flex items-center justify-center bg-black/50">
      <div className="rounded-lg bg-background p-6 shadow-lg w-full max-w-md">
        <h2 className="mb-4 text-lg font-semibold">Add Item to Inventory</h2>

        <div className="mb-3">
          <label htmlFor="add-item-search" className="mb-1 block text-sm font-medium">
            Search Catalog
          </label>
          <input
            id="add-item-search"
            aria-label="Search catalog"
            className="h-9 w-full rounded border px-3 text-sm"
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

        <div className="mb-3">
          <label htmlFor="add-item-location" className="mb-1 block text-sm font-medium">
            Location
          </label>
          <input
            id="add-item-location"
            aria-label="Location"
            className="h-9 w-full rounded border px-3 text-sm"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            placeholder="e.g. Bay 1"
          />
        </div>

        <div className="mb-4">
          <label htmlFor="add-item-quantity" className="mb-1 block text-sm font-medium">
            Quantity
          </label>
          <input
            id="add-item-quantity"
            aria-label="Quantity"
            type="number"
            min={1}
            className="h-9 w-full rounded border px-3 text-sm"
            value={quantity}
            onChange={(e) => setQuantity(Number(e.target.value))}
          />
        </div>

        {error && <p className="mb-3 text-sm text-destructive">{error}</p>}

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => onClose()}>
            Cancel
          </Button>
          <Button onClick={() => void handleSubmit()} disabled={addItem.isPending}>
            {addItem.isPending ? 'Adding…' : 'Add Item'}
          </Button>
        </div>
      </div>
    </div>
  )
}
