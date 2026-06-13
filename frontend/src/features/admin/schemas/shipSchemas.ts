import { z } from 'zod'

export const shipStatusSchema = z.enum(['active', 'softDeleted'])

export const importShipsResultSchema = z.object({
  added: z.number(),
  updated: z.number(),
  reactivated: z.number(),
  softDeleted: z.number(),
  total: z.number(),
})

export const shipListItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  companyName: z.string().nullable(),
  status: shipStatusSchema,
})

export const pagedShipsSchema = z.object({
  items: z.array(shipListItemSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export const shipDetailSchema = z.object({
  id: z.string().uuid(),
  status: shipStatusSchema,
  fields: z.record(z.string(), z.unknown()),
})

export type ImportShipsResult = z.infer<typeof importShipsResultSchema>
export type ShipListItem = z.infer<typeof shipListItemSchema>
export type PagedShips = z.infer<typeof pagedShipsSchema>
export type ShipDetail = z.infer<typeof shipDetailSchema>
