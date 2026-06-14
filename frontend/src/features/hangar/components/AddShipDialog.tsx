import { useEffect, useState } from 'react'
import { Plus, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Dialog, DialogContent, DialogTitle } from '@/components/ui/dialog'
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

  useEffect(() => {
    const id = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(id)
  }, [search])

  const { data, isLoading } = useCatalogSearch(debouncedSearch)
  const { mutate: addShip, isPending } = useAddShip()

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
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose() }}>
      <DialogContent className="max-w-md flex flex-col max-h-[80vh] p-0 gap-0 overflow-hidden">
        {/* Header */}
        <div className="flex items-center px-4 py-3 border-b border-border">
          <DialogTitle className="text-base font-semibold">Add Ship</DialogTitle>
        </div>

        {/* Search */}
        <div className="px-4 py-3 border-b border-border">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search ship catalog…"
            autoFocus
            className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            aria-label="Search ship catalog"
          />
        </div>

        {/* Feedback message */}
        {message && (
          <div className="px-4 pt-3">
            <Alert variant={message.type === 'error' ? 'destructive' : 'default'} role="status">
              <AlertDescription>{message.text}</AlertDescription>
            </Alert>
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
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => handleAdd(item)}
                    disabled={isPending}
                    aria-label={`Add ${item.name}`}
                    className="h-8 w-8 shrink-0"
                  >
                    <Plus aria-hidden />
                  </Button>
                )}
              </li>
            ))}
          </ul>
        </div>
      </DialogContent>
    </Dialog>
  )
}
