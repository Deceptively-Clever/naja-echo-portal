import { apiFetch } from '@/lib/apiClient'
import {
  categoryListResponseSchema,
  refreshCategoriesResultSchema,
  importItemsResultSchema,
  type CategoryListResponse,
  type RefreshCategoriesResult,
  type ImportItemsResult,
} from '../schemas/itemSchemas'

export async function getCategories(): Promise<CategoryListResponse> {
  const data = await apiFetch<unknown>('/api/admin/items/categories')
  return categoryListResponseSchema.parse(data)
}

export async function refreshCategories(): Promise<RefreshCategoriesResult> {
  const data = await apiFetch<unknown>('/api/admin/items/categories/refresh', { method: 'POST' })
  return refreshCategoriesResultSchema.parse(data)
}

export async function importItems(categoryUexId?: number): Promise<ImportItemsResult> {
  const data = await apiFetch<unknown>('/api/admin/items/import', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ categoryUexId: categoryUexId ?? null }),
  })
  return importItemsResultSchema.parse(data)
}
