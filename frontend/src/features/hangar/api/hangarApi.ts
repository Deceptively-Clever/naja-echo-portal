import { apiFetch } from '@/lib/apiClient'
import {
  pagedHangarShipCardsSchema,
  pagedOrgHangarShipCardsSchema,
  hangarShipCardSchema,
  type PagedHangarShipCards,
  type PagedOrgHangarShipCards,
  type HangarShipCard,
} from '../schemas/hangarShipCard'
import {
  pagedCatalogSearchItemsSchema,
  type PagedCatalogSearchItems,
} from '../schemas/catalogSearchItem'
import { owningMemberSchema, type OwningMember } from '../schemas/owningMember'
import { z } from 'zod'

export async function getMyHangar(params: {
  search?: string
  page?: number
  pageSize?: number
}): Promise<PagedHangarShipCards> {
  const qs = new URLSearchParams()
  if (params.search) qs.set('search', params.search)
  if (params.page != null) qs.set('page', String(params.page))
  if (params.pageSize != null) qs.set('pageSize', String(params.pageSize))
  const data = await apiFetch<unknown>(`/api/hangar/mine?${qs}`)
  return pagedHangarShipCardsSchema.parse(data)
}

export async function getOrgHangar(params: {
  search?: string
  mine?: boolean
  memberId?: string
  page?: number
  pageSize?: number
}): Promise<PagedOrgHangarShipCards> {
  const qs = new URLSearchParams()
  if (params.search) qs.set('search', params.search)
  if (params.mine) qs.set('mine', 'true')
  if (params.memberId) qs.set('memberId', params.memberId)
  if (params.page != null) qs.set('page', String(params.page))
  if (params.pageSize != null) qs.set('pageSize', String(params.pageSize))
  const data = await apiFetch<unknown>(`/api/hangar/org?${qs}`)
  return pagedOrgHangarShipCardsSchema.parse(data)
}

export async function getOwningMembers(): Promise<OwningMember[]> {
  const data = await apiFetch<unknown>('/api/hangar/org/members')
  return z.array(owningMemberSchema).parse(data)
}

export async function searchCatalog(params: {
  search?: string
  page?: number
  pageSize?: number
}): Promise<PagedCatalogSearchItems> {
  const qs = new URLSearchParams()
  if (params.search) qs.set('search', params.search)
  if (params.page != null) qs.set('page', String(params.page))
  if (params.pageSize != null) qs.set('pageSize', String(params.pageSize))
  const data = await apiFetch<unknown>(`/api/hangar/catalog/search?${qs}`)
  return pagedCatalogSearchItemsSchema.parse(data)
}

export async function addShip(body: { shipId: string }): Promise<HangarShipCard> {
  const data = await apiFetch<unknown>('/api/hangar/mine', {
    method: 'POST',
    body: JSON.stringify(body),
  })
  return hangarShipCardSchema.parse(data)
}

export async function removeShip(shipId: string): Promise<void> {
  await apiFetch<void>(`/api/hangar/mine/${shipId}`, { method: 'DELETE' })
}
