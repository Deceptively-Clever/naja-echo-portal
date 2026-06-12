import { z } from 'zod'

const authenticatedUserSchema = z.object({
  id: z.string().uuid(),
  displayName: z.string(),
  discordUsername: z.string(),
})

const authenticatedSessionSchema = z.object({
  authenticated: z.literal(true),
  user: authenticatedUserSchema,
})

const anonymousSessionSchema = z.object({
  authenticated: z.literal(false),
})

export const sessionStateSchema = z.discriminatedUnion('authenticated', [
  authenticatedSessionSchema,
  anonymousSessionSchema,
])

export type SessionState = z.infer<typeof sessionStateSchema>
export type AuthenticatedSession = z.infer<typeof authenticatedSessionSchema>
export type AnonymousSession = z.infer<typeof anonymousSessionSchema>
