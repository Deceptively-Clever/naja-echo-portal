import { z } from 'zod'

const entityImportCountsSchema = z.object({
  added: z.number().int().nonnegative(),
  updated: z.number().int().nonnegative(),
  reactivated: z.number().int().nonnegative(),
  softDeleted: z.number().int().nonnegative(),
  total: z.number().int().nonnegative(),
})

const stationImportCountsSchema = entityImportCountsSchema.extend({
  skipped: z.number().int().nonnegative(),
})

export const importLocationsResponseSchema = z.object({
  starSystems: entityImportCountsSchema,
  spaceStations: stationImportCountsSchema,
})

export type ImportLocationsResponse = z.infer<typeof importLocationsResponseSchema>
export type EntityImportCounts = z.infer<typeof entityImportCountsSchema>
export type StationImportCounts = z.infer<typeof stationImportCountsSchema>
