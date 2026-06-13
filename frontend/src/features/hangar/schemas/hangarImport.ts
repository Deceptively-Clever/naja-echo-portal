import { z } from 'zod'

export const importShipRecordSchema = z.object({
  name: z.string(),
  shipName: z.string().optional(),
  unidentified: z.string().optional(),
}).passthrough()

export const importHangarResultSchema = z.object({
  totalRecords: z.number().int(),
  importedShips: z.number().int(),
  unmatchedRecords: z.number().int(),
  unmatchedShipNames: z.array(z.string()),
})

export type ImportShipRecord = z.infer<typeof importShipRecordSchema>
export type ImportHangarResult = z.infer<typeof importHangarResultSchema>
