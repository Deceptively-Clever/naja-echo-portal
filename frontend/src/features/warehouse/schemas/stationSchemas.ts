import { z } from 'zod'

export const stationOptionSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
})

export const stationListResponseSchema = z.object({
  stations: z.array(stationOptionSchema),
})

export type StationOption = z.infer<typeof stationOptionSchema>
export type StationListResponse = z.infer<typeof stationListResponseSchema>
