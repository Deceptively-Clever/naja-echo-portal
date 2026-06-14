import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/apiClient'
import { RefreshCategoriesButton } from './RefreshCategoriesButton'
import { CategorySelector } from './CategorySelector'
import { useCategories } from '../hooks/useCategories'
import { useImportItems } from '../hooks/useImportItems'
import type { ImportItemsResult } from '../schemas/itemSchemas'

export function ItemsImportTab() {
  const { data: categoryData, isLoading: loadingCategories } = useCategories()
  const { mutate, isPending, isSuccess, isError, error, data } = useImportItems()
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | undefined>(undefined)

  const categories = categoryData?.categories ?? []
  const hasCategories = categories.length > 0

  const message = (() => {
    if (!isSuccess && !isError) return null
    if (isError) {
      if (error instanceof ApiError && error.status === 409) {
        return { type: 'warning' as const, text: 'An import is already in progress. Please wait and try again.' }
      }
      return { type: 'error' as const, text: 'Import failed. Please try again.' }
    }
    if (data) {
      return buildSuccessMessage(data)
    }
    return null
  })()

  const selectedCategory = categories.find((c) => c.uexId === selectedCategoryId)

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">Item Categories</h2>
          <p className="text-sm text-muted-foreground">
            Refresh categories before importing items. Only categories with type "item" are eligible.
          </p>
        </div>
        <RefreshCategoriesButton />
      </div>

      {loadingCategories ? (
        <p className="text-sm text-muted-foreground">Loading categories…</p>
      ) : !hasCategories ? (
        <p className="text-sm text-muted-foreground">
          No eligible categories found. Refresh categories first.
        </p>
      ) : (
        <div className="flex flex-col gap-4">
          <CategorySelector
            categories={categories}
            selectedId={selectedCategoryId}
            onSelect={(id) => setSelectedCategoryId((prev) => (prev === id ? undefined : id))}
          />

          <div className="flex items-center gap-3">
            <Button
              onClick={() => mutate(selectedCategoryId)}
              disabled={isPending}
              aria-busy={isPending}
            >
              {isPending
                ? 'Importing…'
                : selectedCategoryId
                  ? `Import "${selectedCategory?.name}"`
                  : 'Import All'}
            </Button>
            {selectedCategoryId && (
              <Button variant="ghost" size="sm" onClick={() => setSelectedCategoryId(undefined)}>
                Clear selection
              </Button>
            )}
          </div>

          <p className="text-xs text-muted-foreground">
            {categories.length} eligible {categories.length === 1 ? 'category' : 'categories'}.
            {categoryData?.lastRefreshedAt
              ? ` Last refreshed: ${new Date(categoryData.lastRefreshedAt).toLocaleString()}.`
              : ''}
          </p>
        </div>
      )}

      {message && (
        <p
          role="status"
          aria-live="polite"
          className={
            message.type === 'success'
              ? 'text-sm text-green-600 dark:text-green-400'
              : message.type === 'warning'
                ? 'text-sm text-yellow-600 dark:text-yellow-400'
                : 'text-sm text-destructive'
          }
        >
          {message.text}
        </p>
      )}
    </div>
  )
}

function buildSuccessMessage(data: ImportItemsResult) {
  const statusLabel =
    data.status === 'Success'
      ? 'complete'
      : data.status === 'CompletedWithErrors'
        ? 'completed with errors'
        : 'failed'

  const parts = [
    `Import ${statusLabel}:`,
    `${data.itemsInserted} added,`,
    `${data.itemsUpdated} updated,`,
    `${data.itemsUnchanged} unchanged,`,
    `${data.itemsSoftDeleted} removed.`,
  ]

  if (data.itemsSkippedNoUuid > 0) {
    parts.push(`${data.itemsSkippedNoUuid} skipped (no UUID).`)
  }

  if (data.categoriesFailed > 0) {
    parts.push(`${data.categoriesFailed} ${data.categoriesFailed === 1 ? 'category' : 'categories'} failed.`)
  }

  parts.push(`(${data.durationMs}ms)`)

  return {
    type: (data.status === 'Failed' ? 'error' : data.status === 'CompletedWithErrors' ? 'warning' : 'success') as
      'success' | 'warning' | 'error',
    text: parts.join(' '),
  }
}
