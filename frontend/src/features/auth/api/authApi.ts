import { apiFetch } from '@/lib/apiClient'
import { sessionStateSchema, type SessionState } from '../schemas/sessionStateSchema'

export function getSignInUrl(): string {
  return '/api/auth/discord/login'
}

export async function getSessionState(): Promise<SessionState> {
  const data = await apiFetch<unknown>('/api/auth/me')
  return sessionStateSchema.parse(data)
}

export async function signOut(): Promise<void> {
  await apiFetch('/api/auth/signout', { method: 'POST' })
}
