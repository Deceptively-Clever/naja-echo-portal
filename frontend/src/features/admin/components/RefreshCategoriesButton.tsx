import { Button } from '@/components/ui/button'
import { StatusMessage } from '@/components/StatusMessage'
import { ApiError } from '@/lib/apiClient'
import { useRefreshCategories } from '../hooks/useRefreshCategories'

export function RefreshCategoriesButton() {
  const { mutate, isPending, isSuccess, isError, error, data } = useRefreshCategories()

  const message = (() => {
    if (!isSuccess && !isError) return null
    if (isError) {
      if (error instanceof ApiError && error.status === 409) {
        return { type: 'warning' as const, text: 'A refresh or import is already in progress. Please wait and try again.' }
      }
      return { type: 'error' as const, text: 'Refresh failed. Please try again.' }
    }
    if (data) {
      return {
        type: 'success' as const,
        text: `Refresh complete: ${data.inserted} added, ${data.updated} updated, ${data.unchanged} unchanged. (${data.fetched} total, ${data.durationMs}ms)`,
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
        variant="outline"
      >
        {isPending ? 'Refreshing…' : 'Refresh Categories'}
      </Button>

      {message && <StatusMessage type={message.type}>{message.text}</StatusMessage>}
    </div>
  )
}
