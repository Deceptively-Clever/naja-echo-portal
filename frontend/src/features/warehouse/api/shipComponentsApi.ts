import { apiFetch } from '@/lib/apiClient'
import {
  shipComponentListResponseSchema,
  shipComponentFiltersResponseSchema,
  type ShipComponentListResponse,
  type ShipComponentFiltersResponse,
} from '../schemas/shipComponentSchemas'
import { inventoryRowSchema, type InventoryRow } from '../schemas/inventorySchemas'
import { z } from 'zod'

const systemsCatalogItemSchema = z.object({
  itemId: z.string().uuid(),
  name: z.string(),
  type: z.string().nullable().optional(),
})

const systemsCatalogResponseSchema = z.object({
  items: z.array(systemsCatalogItemSchema),
})

export type SystemsCatalogItem = z.infer<typeof systemsCatalogItemSchema>
export type SystemsCatalogResponse = z.infer<typeof systemsCatalogResponseSchema>

export interface ShipComponentFilters {
  name?: string
  type?: string[]
  class?: string[]
  size?: number[]
  grade?: string[]
  ownerUserId?: string[]
  location?: string[]
  unknownClass?: boolean
  unknownSize?: boolean
  unknownGrade?: boolean
}

export async function getShipComponents(filters: ShipComponentFilters = {}): Promise<ShipComponentListResponse> {
  const qs = new URLSearchParams()
  if (filters.name) qs.set('name', filters.name)
  filters.type?.forEach(v => qs.append('type', v))
  filters.class?.forEach(v => qs.append('class', v))
  filters.size?.forEach(v => qs.append('size', String(v)))
  filters.grade?.forEach(v => qs.append('grade', v))
  filters.ownerUserId?.forEach(v => qs.append('ownerUserId', v))
  filters.location?.forEach(v => qs.append('location', v))
  if (filters.unknownClass) qs.set('unknownClass', 'true')
  if (filters.unknownSize) qs.set('unknownSize', 'true')
  if (filters.unknownGrade) qs.set('unknownGrade', 'true')
  const data = await apiFetch<unknown>(`/api/warehouse/ship-components?${qs}`)
  return shipComponentListResponseSchema.parse(data)
}

export async function getShipComponentFilters(): Promise<ShipComponentFiltersResponse> {
  const data = await apiFetch<unknown>('/api/warehouse/ship-components/filters')
  return shipComponentFiltersResponseSchema.parse(data)
}

export async function searchSystemsCatalog(search?: string, limit = 25): Promise<SystemsCatalogResponse> {
  const qs = new URLSearchParams()
  if (search) qs.set('search', search)
  qs.set('limit', String(limit))
  const data = await apiFetch<unknown>(`/api/warehouse/ship-components/catalog/search?${qs}`)
  return systemsCatalogResponseSchema.parse(data)
}

export async function addShipComponent(body: {
  itemId: string
  ownerUserId?: string
  location: string
  quantity: number
  quality?: number
}): Promise<InventoryRow> {
  const data = await apiFetch<unknown>('/api/warehouse/ship-components', {
    method: 'POST',
    body: JSON.stringify(body),
  })
  return inventoryRowSchema.parse(data)
}
