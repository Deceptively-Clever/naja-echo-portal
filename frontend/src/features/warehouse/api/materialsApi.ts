import { apiFetch } from '@/lib/apiClient'
import {
  materialListResponseSchema,
  materialFiltersResponseSchema,
  commodityCatalogResponseSchema,
  materialRowSchema,
  type MaterialListResponse,
  type MaterialFiltersResponse,
  type CommodityCatalogResponse,
  type MaterialRow,
} from '../schemas/materialSchemas'

export interface MaterialFilters {
  material?: string
  ownerUserId?: string
  location?: string
  qualityMin?: number
  qualityMax?: number
}

export async function getMaterials(filters: MaterialFilters = {}): Promise<MaterialListResponse> {
  const qs = new URLSearchParams()
  if (filters.material) qs.set('material', filters.material)
  if (filters.ownerUserId) qs.set('ownerUserId', filters.ownerUserId)
  if (filters.location) qs.set('location', filters.location)
  if (filters.qualityMin !== undefined) qs.set('qualityMin', String(filters.qualityMin))
  if (filters.qualityMax !== undefined) qs.set('qualityMax', String(filters.qualityMax))
  const data = await apiFetch<unknown>(`/api/warehouse/materials?${qs}`)
  return materialListResponseSchema.parse(data)
}

export async function getMaterialFilters(): Promise<MaterialFiltersResponse> {
  const data = await apiFetch<unknown>('/api/warehouse/materials/filters')
  return materialFiltersResponseSchema.parse(data)
}

export async function searchCommodities(search?: string, limit = 25): Promise<CommodityCatalogResponse> {
  const qs = new URLSearchParams()
  if (search) qs.set('search', search)
  qs.set('limit', String(limit))
  const data = await apiFetch<unknown>(`/api/warehouse/materials/catalog/search?${qs}`)
  return commodityCatalogResponseSchema.parse(data)
}

export async function addMaterial(body: {
  commodityId: string
  ownerUserId?: string
  location: string
  quantity: number
  quality?: number
}): Promise<MaterialRow> {
  const data = await apiFetch<unknown>('/api/warehouse/materials', {
    method: 'POST',
    body: JSON.stringify(body),
  })
  return materialRowSchema.parse(data)
}

export async function changeMaterialQuantity(id: string, quantity: number): Promise<MaterialRow> {
  const data = await apiFetch<unknown>(`/api/warehouse/materials/${id}/quantity`, {
    method: 'PUT',
    body: JSON.stringify({ quantity }),
  })
  return materialRowSchema.parse(data)
}

export async function removeMaterial(id: string): Promise<void> {
  await apiFetch<void>(`/api/warehouse/materials/${id}`, { method: 'DELETE' })
}
