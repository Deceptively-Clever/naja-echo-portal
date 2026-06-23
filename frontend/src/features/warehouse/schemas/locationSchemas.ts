import { z } from 'zod'

export const locationOptionSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  type: z.enum(['Station', 'City']),
})

export const locationListResponseSchema = z.object({
  locations: z.array(locationOptionSchema),
})

export type LocationOption = z.infer<typeof locationOptionSchema>
export type LocationType = LocationOption['type']
export type LocationListResponse = z.infer<typeof locationListResponseSchema>
