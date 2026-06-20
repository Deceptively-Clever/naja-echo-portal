import { z } from 'zod'

export const pendingRegistrationResponseSchema = z.object({
  token: z.string(),
  expiresAt: z.string().datetime({ offset: true }),
})

export const verifyCharacterRequestSchema = z.object({
  handle: z.string().min(1).max(100),
})

export const characterResponseSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  handle: z.string(),
  createdAt: z.string().datetime({ offset: true }),
})

export const characterListResponseSchema = z.object({
  characters: z.array(characterResponseSchema),
})

export type PendingRegistrationResponse = z.infer<typeof pendingRegistrationResponseSchema>
export type CharacterResponse = z.infer<typeof characterResponseSchema>
export type CharacterListResponse = z.infer<typeof characterListResponseSchema>
