import { apiFetch } from '@/lib/apiClient'
import {
  importShipsResultSchema,
  pagedShipsSchema,
  shipDetailSchema,
  type ImportShipsResult,
  type PagedShips,
  type ShipDetail,
} from '../schemas/shipSchemas'

export async function importShips(): Promise<ImportShipsResult> {
  const data = await apiFetch<unknown>('/api/admin/ships/import', { method: 'POST' })
  return importShipsResultSchema.parse(data)
}

export async function getShips(page = 1, pageSize = 25): Promise<PagedShips> {
  const data = await apiFetch<unknown>(`/api/admin/ships?page=${page}&pageSize=${pageSize}`)
  return pagedShipsSchema.parse(data)
}

export async function getShipById(id: string): Promise<ShipDetail> {
  const data = await apiFetch<unknown>(`/api/admin/ships/${id}`)
  return shipDetailSchema.parse(data)
}
