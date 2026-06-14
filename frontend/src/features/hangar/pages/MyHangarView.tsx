import { useState } from 'react'
import { useMyHangar } from '../hooks/useMyHangar'
import { ShipCardGallery } from '../components/ShipCardGallery'
import { RemoveShipButton } from '../components/RemoveShipButton'
import { AddShipDialog } from '../components/AddShipDialog'
import { ImportHangarDialog } from '../components/ImportHangarDialog'
import { Button } from '@/components/ui/button'
import { Plus, Upload } from 'lucide-react'
import type { HangarShipCard } from '../schemas/hangarShipCard'

export function MyHangarView() {
  const [search, setSearch] = useState('')
  const [addOpen, setAddOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)

  const { data, isLoading, fetchNextPage, hasNextPage } = useMyHangar(search || undefined)

  const ships = data?.pages.flatMap((p) => p.items) ?? []

  const emptyMessage = search
    ? 'No ships match your search.'
    : 'Your hangar is empty. Add your first ship!'

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">My Hangar</h1>
        <div className="flex gap-2">
          <Button size="sm" variant="outline" onClick={() => setImportOpen(true)}>
            <Upload data-icon="inline-start" aria-hidden />
            Import
          </Button>
          <Button size="sm" onClick={() => setAddOpen(true)}>
            <Plus data-icon="inline-start" aria-hidden />
            Add Ship
          </Button>
        </div>
      </div>

      <ShipCardGallery
        ships={ships}
        search={search}
        onSearchChange={setSearch}
        emptyStateMessage={emptyMessage}
        isLoading={isLoading}
        onLoadMore={fetchNextPage}
        hasMore={hasNextPage}
        renderOverlay={(ship) => (
          <RemoveShipButton ship={ship as HangarShipCard} />
        )}
      />

      <AddShipDialog open={addOpen} onClose={() => setAddOpen(false)} />
      <ImportHangarDialog open={importOpen} onClose={() => setImportOpen(false)} />
    </div>
  )
}
