import { Badge } from '@/components/ui/badge'
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { Skeleton } from '@/components/ui/skeleton'
import type { ShipListItem } from '../schemas/shipSchemas'
import { useShipDetail } from '../hooks/useShipDetail'

interface ShipDetailSheetProps {
  ship: ShipListItem | null
  onClose: () => void
}

export function ShipDetailSheet({ ship, onClose }: ShipDetailSheetProps) {
  const { data: detail, isLoading } = useShipDetail(ship?.id ?? null)

  return (
    <Sheet open={ship !== null} onOpenChange={(open) => { if (!open) onClose() }}>
      <SheetContent side="right" className="w-[480px] sm:max-w-xl overflow-y-auto">
        <SheetHeader>
          <SheetTitle className="flex items-center gap-2">
            {ship?.name ?? 'Ship Details'}
            {ship?.status === 'softDeleted' && (
              <Badge variant="secondary">No longer in source feed</Badge>
            )}
          </SheetTitle>
        </SheetHeader>

        {isLoading && (
          <div className="mt-4 flex flex-col gap-2">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-5 w-full" />
            ))}
          </div>
        )}

        {detail && (
          <dl className="mt-4 grid grid-cols-[1fr_2fr] gap-x-4 gap-y-2 text-sm">
            {Object.entries(detail.fields).map(([key, value]) => (
              <>
                <dt key={`${key}-dt`} className="font-medium text-muted-foreground break-words">{key}</dt>
                <dd key={`${key}-dd`} className="break-words">
                  {value === null || value === undefined || value === '' ? (
                    <span className="text-muted-foreground/50 italic">empty</span>
                  ) : (
                    String(value)
                  )}
                </dd>
              </>
            ))}
          </dl>
        )}
      </SheetContent>
    </Sheet>
  )
}
