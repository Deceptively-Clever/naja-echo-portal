import { Button } from '@/components/ui/button'
import { StatusMessage } from '@/components/StatusMessage'
import { ApiError } from '@/lib/apiClient'
import { useImportShips } from '../hooks/useImportShips'

export function ImportShipsButton() {
  const { mutate, isPending, isSuccess, isError, error, data } = useImportShips()

  const message = (() => {
    if (!isSuccess && !isError) return null
    if (isError) {
      if (error instanceof ApiError && error.status === 409) {
        return { type: 'warning' as const, text: 'An import is already in progress. Please wait and try again.' }
      }
      return { type: 'error' as const, text: 'Import failed. Please try again.' }
    }
    if (data) {
      return {
        type: 'success' as const,
        text: `Import complete: ${data.added} added, ${data.updated} updated, ${data.reactivated} reactivated, ${data.softDeleted} removed. (${data.total} total)`,
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
        {isPending ? 'Importing…' : 'Import Ships'}
      </Button>

      {message && <StatusMessage type={message.type}>{message.text}</StatusMessage>}
    </div>
  )
}
