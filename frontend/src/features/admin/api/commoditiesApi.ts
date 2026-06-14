import { apiFetch } from '@/lib/apiClient'
import {
  importCommoditiesResultSchema,
  pagedCommoditiesSchema,
  type ImportCommoditiesResult,
  type PagedCommodities,
} from '../schemas/commoditySchemas'

export async function importCommodities(): Promise<ImportCommoditiesResult> {
  const data = await apiFetch<unknown>('/api/admin/commodities/import', { method: 'POST' })
  return importCommoditiesResultSchema.parse(data)
}

export async function getCommodities(page = 1, pageSize = 25): Promise<PagedCommodities> {
  const data = await apiFetch<unknown>(`/api/admin/commodities?page=${page}&pageSize=${pageSize}`)
  return pagedCommoditiesSchema.parse(data)
}
