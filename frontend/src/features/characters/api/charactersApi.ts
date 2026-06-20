import { apiFetch, ApiError } from '@/lib/apiClient'
import {
  pendingRegistrationResponseSchema,
  characterResponseSchema,
  characterListResponseSchema,
  type PendingRegistrationResponse,
  type CharacterResponse,
  type CharacterListResponse,
} from '../schemas/characterSchemas'

export async function getRegistration(): Promise<PendingRegistrationResponse | null> {
  const data = await apiFetch<unknown>('/api/characters/registration')
  if (data === null || data === undefined) return null
  return pendingRegistrationResponseSchema.parse(data)
}

export async function startRegistration(): Promise<PendingRegistrationResponse> {
  const data = await apiFetch<unknown>('/api/characters/registration', { method: 'POST' })
  return pendingRegistrationResponseSchema.parse(data)
}

export async function verifyCharacter(handle: string): Promise<CharacterResponse> {
  const response = await fetch('/api/characters/verify', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ handle }),
  })

  if (!response.ok) {
    let title = `Request failed: ${response.status}`
    try {
      const body = await response.json() as { title?: string }
      if (body.title) title = body.title
    } catch { /* ignore parse errors */ }
    throw new ApiError(response.status, title)
  }

  return characterResponseSchema.parse(await response.json())
}

export async function getCharacters(): Promise<CharacterListResponse> {
  const data = await apiFetch<unknown>('/api/characters/')
  return characterListResponseSchema.parse(data)
}
