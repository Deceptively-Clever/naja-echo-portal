import { z } from 'zod'

export const materialRowSchema = z.object({
  id: z.string().uuid(),
  commodityId: z.string().uuid(),
  materialName: z.string(),
  materialCode: z.string().nullable().optional(),
  quantity: z.number(),
  quality: z.number().int().min(1).max(1000),
  ownerUserId: z.string().uuid(),
  ownerDisplayName: z.string(),
  location: z.string(),
  locationId: z.string().uuid().nullable().optional(),
  locationType: z.enum(['Station', 'City']).nullable().optional(),
})

export const materialListResponseSchema = z.object({
  rows: z.array(materialRowSchema),
})

export const materialOwnerOptionSchema = z.object({
  userId: z.string().uuid(),
  displayName: z.string(),
})

export const materialFiltersResponseSchema = z.object({
  owners: z.array(materialOwnerOptionSchema),
  locations: z.array(z.string()),
})

export const commodityCatalogItemSchema = z.object({
  commodityId: z.string().uuid(),
  name: z.string(),
  code: z.string().nullable().optional(),
})

export const commodityCatalogResponseSchema = z.object({
  commodities: z.array(commodityCatalogItemSchema),
})

export const changeMaterialQuantityRequestSchema = z.object({
  quantity: z.number().gt(0, { message: 'Quantity must be greater than 0.' }),
})

export const materialFilterFormSchema = z.object({
  material: z.string(),
  ownerUserId: z.string(),
  location: z.string(),
  locationId: z.string(),
  qualityMin: z.number().int().min(1).max(1000),
  qualityMax: z.number().int().min(1).max(1000),
})

export type MaterialRow = z.infer<typeof materialRowSchema>
export type MaterialListResponse = z.infer<typeof materialListResponseSchema>
export type MaterialOwnerOption = z.infer<typeof materialOwnerOptionSchema>
export type MaterialFiltersResponse = z.infer<typeof materialFiltersResponseSchema>
export type CommodityCatalogItem = z.infer<typeof commodityCatalogItemSchema>
export type CommodityCatalogResponse = z.infer<typeof commodityCatalogResponseSchema>
export type MaterialFilterFormValues = z.infer<typeof materialFilterFormSchema>
