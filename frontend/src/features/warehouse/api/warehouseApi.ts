import { apiFetch } from '@/lib/apiClient'
import {
  inventoryListResponseSchema,
  inventoryFiltersResponseSchema,
  catalogItemsResponseSchema,
  inventoryRowSchema,
  type InventoryListResponse,
  type InventoryFiltersResponse,
  type CatalogItemsResponse,
  type InventoryRow,
} from '../schemas/inventorySchemas'

export interface InventoryFilters {
  name?: string
  type?: string
  subtype?: string
  ownerUserId?: string
  location?: string
}

export async function getInventory(filters: InventoryFilters = {}): Promise<InventoryListResponse> {
  const qs = new URLSearchParams()
  if (filters.name) qs.set('name', filters.name)
  if (filters.type) qs.set('type', filters.type)
  if (filters.subtype) qs.set('subtype', filters.subtype)
  if (filters.ownerUserId) qs.set('ownerUserId', filters.ownerUserId)
  if (filters.location) qs.set('location', filters.location)
  const data = await apiFetch<unknown>(`/api/warehouse/items?${qs}`)
  return inventoryListResponseSchema.parse(data)
}

export async function getInventoryFilters(): Promise<InventoryFiltersResponse> {
  const data = await apiFetch<unknown>('/api/warehouse/items/filters')
  return inventoryFiltersResponseSchema.parse(data)
}

export async function searchCatalogItems(search?: string, limit = 25): Promise<CatalogItemsResponse> {
  const qs = new URLSearchParams()
  if (search) qs.set('search', search)
  qs.set('limit', String(limit))
  const data = await apiFetch<unknown>(`/api/warehouse/catalog/search?${qs}`)
  return catalogItemsResponseSchema.parse(data)
}

export async function addInventoryItem(body: {
  itemId: string
  ownerUserId?: string
  location: string
  quantity: number
}): Promise<InventoryRow> {
  const data = await apiFetch<unknown>('/api/warehouse/items', {
    method: 'POST',
    body: JSON.stringify(body),
  })
  return inventoryRowSchema.parse(data)
}

export async function changeInventoryQuantity(id: string, quantity: number): Promise<InventoryRow> {
  const data = await apiFetch<unknown>(`/api/warehouse/items/${id}/quantity`, {
    method: 'PUT',
    body: JSON.stringify({ quantity }),
  })
  return inventoryRowSchema.parse(data)
}

export async function removeInventoryItem(id: string): Promise<void> {
  await apiFetch<void>(`/api/warehouse/items/${id}`, { method: 'DELETE' })
}
