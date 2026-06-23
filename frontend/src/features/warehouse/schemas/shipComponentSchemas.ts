import { z } from 'zod'

export const shipComponentRowSchema = z.object({
  id: z.string().uuid(),
  itemId: z.string().uuid(),
  name: z.string(),
  type: z.string().nullable().optional(),
  class: z.string().nullable().optional(),
  size: z.number().int().nullable().optional(),
  grade: z.string().nullable().optional(),
  quantity: z.number().int().min(1),
  quality: z.number().int().min(1).max(1000),
  ownerUserId: z.string().uuid(),
  ownerDisplayName: z.string(),
  location: z.string(),
  locationId: z.string().uuid().nullable().optional(),
  locationType: z.enum(['Station', 'City']).nullable().optional(),
})

export const shipComponentListResponseSchema = z.object({
  items: z.array(shipComponentRowSchema),
})

export const shipComponentOwnerOptionSchema = z.object({
  userId: z.string().uuid(),
  displayName: z.string(),
})

export const shipComponentFiltersResponseSchema = z.object({
  types: z.array(z.string()),
  classes: z.array(z.string()),
  sizes: z.array(z.number().int()),
  grades: z.array(z.string()),
  owners: z.array(shipComponentOwnerOptionSchema),
  locations: z.array(z.string()),
  unknownClass: z.boolean(),
  unknownSize: z.boolean(),
  unknownGrade: z.boolean(),
})

export const shipComponentFiltersFormStateSchema = z.object({
  name: z.string(),
  type: z.array(z.string()),
  class: z.array(z.string()),
  size: z.array(z.number().int()),
  grade: z.array(z.string()),
  ownerUserId: z.array(z.string()),
  location: z.array(z.string()),
  unknownClass: z.boolean(),
  unknownSize: z.boolean(),
  unknownGrade: z.boolean(),
})

export type ShipComponentRow = z.infer<typeof shipComponentRowSchema>
export type ShipComponentListResponse = z.infer<typeof shipComponentListResponseSchema>
export type ShipComponentOwnerOption = z.infer<typeof shipComponentOwnerOptionSchema>
export type ShipComponentFiltersResponse = z.infer<typeof shipComponentFiltersResponseSchema>
export type ShipComponentFiltersFormState = z.infer<typeof shipComponentFiltersFormStateSchema>
