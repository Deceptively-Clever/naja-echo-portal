import { Button } from '@/components/ui/button'
import { StatusMessage } from '@/components/StatusMessage'
import { ApiError } from '@/lib/apiClient'
import { useImportCommodities } from '../hooks/useImportCommodities'

export function ImportCommoditiesButton() {
  const { mutate, isPending, isSuccess, isError, error, data } = useImportCommodities()

  const message = (() => {
    if (!isSuccess && !isError) return null
    if (isError) {
      if (error instanceof ApiError && error.status === 409) {
        return { type: 'warning' as const, text: 'An import is already in progress. Please wait and try again.' }
      }
      return { type: 'error' as const, text: 'Import failed. Please try again.' }
    }
    if (data) {
      if (data.warning) {
        return { type: 'warning' as const, text: data.warning }
      }
      return {
        type: 'success' as const,
        text: `Import complete: ${data.inserted} added, ${data.updated} updated, ${data.unchanged} unchanged, ${data.restored} restored, ${data.softDeleted} removed, ${data.skipped} skipped. (${data.fetched} fetched, ${data.durationMs}ms)`,
      }
    }
    return null
  })()

  return (
    <div className="flex flex-col gap-2">
      <Button
        onClick={() => mutate()}
        disabled={isPending}
        aria-busy={isPending}
      >
        {isPending ? 'Importing…' : 'Import Commodities'}
      </Button>

      {message && <StatusMessage type={message.type}>{message.text}</StatusMessage>}
    </div>
  )
}
