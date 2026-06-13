import { z } from 'zod'

export const hangarOwnerSchema = z.object({
  userId: z.string().uuid(),
  displayName: z.string(),
})

export const hangarShipCardSchema = z.object({
  shipId: z.string().uuid(),
  name: z.string(),
  companyName: z.string().nullable().optional(),
  urlPhoto: z.string().nullable().optional(),
  scu: z.number().nullable().optional(),
  crew: z.string().nullable().optional(),
})

export const orgHangarShipCardSchema = hangarShipCardSchema.extend({
  ownerCount: z.number(),
  owners: z.array(hangarOwnerSchema),
})

export const pagedHangarShipCardsSchema = z.object({
  items: z.array(hangarShipCardSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export const pagedOrgHangarShipCardsSchema = z.object({
  items: z.array(orgHangarShipCardSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type HangarShipCard = z.infer<typeof hangarShipCardSchema>
export type OrgHangarShipCard = z.infer<typeof orgHangarShipCardSchema>
export type HangarOwner = z.infer<typeof hangarOwnerSchema>
export type PagedHangarShipCards = z.infer<typeof pagedHangarShipCardsSchema>
export type PagedOrgHangarShipCards = z.infer<typeof pagedOrgHangarShipCardsSchema>
