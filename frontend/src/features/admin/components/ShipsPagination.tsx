import { Button } from '@/components/ui/button'

interface ShipsPaginationProps {
  page: number
  totalPages: number
  totalCount: number
  pageSize: number
  onPageChange: (page: number) => void
}

export function ShipsPagination({ page, totalPages, totalCount, pageSize, onPageChange }: ShipsPaginationProps) {
  const start = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex items-center justify-between gap-4 py-2">
      <p className="text-sm text-muted-foreground">
        {totalCount === 0 ? 'No records' : `${start}–${end} of ${totalCount}`}
      </p>
      <div className="flex gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Previous page"
        >
          Previous
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Next page"
        >
          Next
        </Button>
      </div>
    </div>
  )
}
