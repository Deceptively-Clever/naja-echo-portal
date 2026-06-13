import { z } from 'zod'
import { hangarShipCardSchema } from './hangarShipCard'

export const catalogSearchItemSchema = hangarShipCardSchema.extend({
  alreadyOwned: z.boolean(),
})

export const pagedCatalogSearchItemsSchema = z.object({
  items: z.array(catalogSearchItemSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type CatalogSearchItem = z.infer<typeof catalogSearchItemSchema>
export type PagedCatalogSearchItems = z.infer<typeof pagedCatalogSearchItemsSchema>
