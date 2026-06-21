import { apiFetch } from '@/lib/apiClient'
import {
  adminUserCharacterSchema,
  adminUserListResponseSchema,
  type AdminUser,
  type AdminUserCharacter,
  type AdminUserListResponse,
} from '../schemas/userSchemas'

export async function getAdminUsers(): Promise<AdminUserListResponse> {
  const data = await apiFetch<unknown>('/api/admin/users')
  return adminUserListResponseSchema.parse(data)
}

export async function addCharacterForUser(userId: string, handle: string): Promise<AdminUserCharacter> {
  const data = await apiFetch<unknown>(`/api/admin/users/${userId}/characters`, {
    method: 'POST',
    body: JSON.stringify({ handle }),
  })
  return adminUserCharacterSchema.parse(data)
}

export async function assignRolesForUser(userId: string, roles: string[]): Promise<void> {
  await apiFetch<unknown>(`/api/admin/users/${userId}/roles`, {
    method: 'PUT',
    body: JSON.stringify({ roles }),
  })
}

export type { AdminUser, AdminUserCharacter, AdminUserListResponse }
