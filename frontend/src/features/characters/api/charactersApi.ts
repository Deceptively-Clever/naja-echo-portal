import { apiFetch } from '@/lib/apiClient'
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
  const data = await apiFetch<unknown>('/api/characters/verify', {
    method: 'POST',
    body: JSON.stringify({ handle }),
  })
  return characterResponseSchema.parse(data)
}

export async function getCharacters(): Promise<CharacterListResponse> {
  const data = await apiFetch<unknown>('/api/characters/')
  return characterListResponseSchema.parse(data)
}
