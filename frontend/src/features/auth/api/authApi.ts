import { apiFetch } from '@/lib/apiClient'
import { CurrentUserSchema, type CurrentUser } from '../schemas/currentUserSchema'

export function getSignInUrl(): string {
  return '/api/auth/discord/login'
}

export async function getCurrentUser(): Promise<CurrentUser> {
  const data = await apiFetch<unknown>('/api/auth/me')
  return CurrentUserSchema.parse(data)
}

export async function signOut(): Promise<void> {
  await apiFetch('/api/auth/signout', { method: 'POST' })
}
