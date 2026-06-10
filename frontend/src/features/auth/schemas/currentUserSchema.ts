import { z } from 'zod'

export const CurrentUserSchema = z.object({
  id: z.string().uuid(),
  displayName: z.string(),
  avatarUrl: z.string().url().nullable(),
})

export type CurrentUser = z.infer<typeof CurrentUserSchema>
