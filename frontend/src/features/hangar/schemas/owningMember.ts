import { z } from 'zod'

export const owningMemberSchema = z.object({
  userId: z.string().uuid(),
  displayName: z.string(),
})

export const addShipRequestSchema = z.object({
  shipId: z.string().uuid(),
})

export type OwningMember = z.infer<typeof owningMemberSchema>
export type AddShipRequest = z.infer<typeof addShipRequestSchema>
