import { useEffect, useState } from 'react'
import { X, Plus, Check } from 'lucide-react'
import { useCatalogSearch } from '../hooks/useCatalogSearch'
import { useAddShip } from '../hooks/useAddShip'
import type { CatalogSearchItem } from '../schemas/catalogSearchItem'

interface AddShipDialogProps {
  open: boolean
  onClose: () => void
}

export function AddShipDialog({ open, onClose }: AddShipDialogProps) {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  // Debounce keystrokes so we issue one catalog request per pause, not per character
  useEffect(() => {
    const id = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(id)
  }, [search])

  const { data, isLoading } = useCatalogSearch(debouncedSearch)
  const { mutate: addShip, isPending } = useAddShip()

  if (!open) return null

  const items = data?.items ?? []

  const handleAdd = (item: CatalogSearchItem) => {
    if (item.alreadyOwned || isPending) return
    setMessage(null)
    addShip(item.shipId, {
      onSuccess: () => setMessage({ type: 'success', text: `${item.name} added to your hangar!` }),
      onError: () => setMessage({ type: 'error', text: `Failed to add ${item.name}. Please try again.` }),
    })
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
      role="dialog"
      aria-modal="true"
      aria-label="Add Ship"
    >
      <div className="relative bg-background border border-border rounded-lg shadow-xl w-full max-w-md mx-4 flex flex-col max-h-[80vh]">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border">
          <h2 className="text-base font-semibold">Add Ship</h2>
          <button onClick={onClose} aria-label="Close dialog" className="text-muted-foreground hover:text-foreground">
            <X className="h-4 w-4" aria-hidden />
          </button>
        </div>

        {/* Search */}
        <div className="p-4 border-b border-border">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search ship catalog…"
            autoFocus
            className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            aria-label="Search ship catalog"
          />
        </div>

        {/* Feedback message */}
        {message && (
          <div
            role="status"
            className={`mx-4 mt-3 p-2 rounded text-sm ${
              message.type === 'success'
                ? 'bg-green-50 text-green-800 dark:bg-green-900/20 dark:text-green-300'
                : 'bg-destructive/10 text-destructive'
            }`}
          >
            {message.text}
          </div>
        )}

        {/* Results */}
        <div className="overflow-y-auto flex-1 p-2">
          {isLoading && <p className="text-sm text-muted-foreground text-center py-4">Searching…</p>}
          {!isLoading && items.length === 0 && (
            <p className="text-sm text-muted-foreground text-center py-4">
              {search ? 'No ships found.' : 'Type to search the ship catalog.'}
            </p>
          )}
          <ul className="divide-y divide-border" role="list">
            {items.map((item) => (
              <li
                key={item.shipId}
                className={`flex items-center justify-between gap-2 p-2 rounded ${
                  item.alreadyOwned ? 'opacity-50' : 'hover:bg-muted/50 cursor-pointer'
                }`}
              >
                <div className="min-w-0">
                  <p className="text-sm font-medium truncate">{item.name}</p>
                  {item.companyName && (
                    <p className="text-xs text-muted-foreground truncate">{item.companyName}</p>
                  )}
                </div>
                {item.alreadyOwned ? (
                  <span className="flex items-center gap-1 text-xs text-muted-foreground shrink-0">
                    <Check className="h-3 w-3" aria-hidden />
                    Owned
                  </span>
                ) : (
                  <button
                    onClick={() => handleAdd(item)}
                    disabled={isPending}
                    aria-label={`Add ${item.name}`}
                    className="p-1 rounded hover:bg-primary/10 text-primary disabled:opacity-50"
                  >
                    <Plus className="h-4 w-4" aria-hidden />
                  </button>
                )}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  )
}
