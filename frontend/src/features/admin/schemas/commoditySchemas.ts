import { z } from 'zod'

export const importCommoditiesResultSchema = z.object({
  fetched: z.number(),
  skipped: z.number(),
  inserted: z.number(),
  updated: z.number(),
  restored: z.number(),
  softDeleted: z.number(),
  startedAt: z.string(),
  completedAt: z.string(),
  durationMs: z.number(),
  warning: z.string().nullable().optional(),
})

export const commodityStatusSchema = z.enum(['active', 'softDeleted'])

export const commodityListItemSchema = z.object({
  id: z.string().uuid(),
  uexId: z.number(),
  name: z.string(),
  code: z.string().nullable(),
  kind: z.string().nullable(),
  status: commodityStatusSchema,
})

export const pagedCommoditiesSchema = z.object({
  items: z.array(commodityListItemSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type ImportCommoditiesResult = z.infer<typeof importCommoditiesResultSchema>
export type CommodityListItem = z.infer<typeof commodityListItemSchema>
export type PagedCommodities = z.infer<typeof pagedCommoditiesSchema>
