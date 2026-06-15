import { z } from 'zod'

export const inventoryRowSchema = z.object({
  id: z.string().uuid(),
  itemId: z.string().uuid(),
  name: z.string(),
  type: z.string().nullable().optional(),
  subtype: z.string().nullable().optional(),
  quantity: z.number().int().min(1),
  quality: z.number().int().min(1).max(1000),
  ownerUserId: z.string().uuid(),
  ownerDisplayName: z.string(),
  location: z.string(),
})

export const inventoryListResponseSchema = z.object({
  items: z.array(inventoryRowSchema),
})

export const ownerOptionSchema = z.object({
  userId: z.string().uuid(),
  displayName: z.string(),
})

export const inventoryFiltersResponseSchema = z.object({
  types: z.array(z.string()),
  subtypes: z.array(z.string()),
  owners: z.array(ownerOptionSchema),
})

export const catalogItemSchema = z.object({
  itemId: z.string().uuid(),
  name: z.string(),
  type: z.string().nullable().optional(),
  subtype: z.string().nullable().optional(),
})

export const catalogItemsResponseSchema = z.object({
  items: z.array(catalogItemSchema),
})

export type InventoryRow = z.infer<typeof inventoryRowSchema>
export type InventoryListResponse = z.infer<typeof inventoryListResponseSchema>
export type OwnerOption = z.infer<typeof ownerOptionSchema>
export type InventoryFiltersResponse = z.infer<typeof inventoryFiltersResponseSchema>
export type CatalogItem = z.infer<typeof catalogItemSchema>
export type CatalogItemsResponse = z.infer<typeof catalogItemsResponseSchema>
