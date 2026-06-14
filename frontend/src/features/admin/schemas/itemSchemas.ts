import { z } from 'zod'

export const categoryListItemSchema = z.object({
  uexId: z.number(),
  name: z.string(),
  section: z.string().nullable(),
  type: z.string(),
  isGameRelated: z.boolean(),
  isMining: z.boolean(),
  sourceDateModified: z.string().nullable(),
  localItemCount: z.number(),
  lastImportedAt: z.string().nullable(),
})

export const categoryListResponseSchema = z.object({
  categories: z.array(categoryListItemSchema),
  lastRefreshedAt: z.string().nullable(),
})

export const refreshCategoriesResultSchema = z.object({
  fetched: z.number(),
  inserted: z.number(),
  updated: z.number(),
  unchanged: z.number(),
  failed: z.number(),
  startedAt: z.string(),
  completedAt: z.string(),
  durationMs: z.number(),
})

export const categoryImportErrorSchema = z.object({
  categoryUexId: z.number(),
  categoryName: z.string().nullable(),
  message: z.string(),
})

export const importItemsResultSchema = z.object({
  status: z.enum(['Success', 'CompletedWithErrors', 'Failed']),
  categoriesProcessed: z.number(),
  categoriesSucceeded: z.number(),
  categoriesFailed: z.number(),
  itemsFetched: z.number(),
  itemsInserted: z.number(),
  itemsUpdated: z.number(),
  itemsUnchanged: z.number(),
  itemsSkippedNoUuid: z.number(),
  itemsSoftDeleted: z.number(),
  itemsFailed: z.number(),
  startedAt: z.string(),
  completedAt: z.string(),
  durationMs: z.number(),
  errors: z.array(categoryImportErrorSchema),
})

export type CategoryListItem = z.infer<typeof categoryListItemSchema>
export type CategoryListResponse = z.infer<typeof categoryListResponseSchema>
export type RefreshCategoriesResult = z.infer<typeof refreshCategoriesResultSchema>
export type CategoryImportError = z.infer<typeof categoryImportErrorSchema>
export type ImportItemsResult = z.infer<typeof importItemsResultSchema>
