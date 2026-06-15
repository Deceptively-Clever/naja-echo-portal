import type { ShipComponentFilters } from '../api/shipComponentsApi'

export const warehouseKeys = {
  all: ['warehouse'] as const,
  inventory: () => [...warehouseKeys.all, 'inventory'] as const,
  inventoryList: (filters?: {
    name?: string
    type?: string
    subtype?: string
    ownerUserId?: string
    location?: string
  }) => [...warehouseKeys.inventory(), filters] as const,
  filters: () => [...warehouseKeys.all, 'filters'] as const,
  catalog: () => [...warehouseKeys.all, 'catalog'] as const,
  catalogSearch: (search?: string) => [...warehouseKeys.catalog(), { search }] as const,
  shipComponents: () => [...warehouseKeys.all, 'shipComponents'] as const,
  shipComponentsList: (filters?: ShipComponentFilters) => [...warehouseKeys.shipComponents(), filters] as const,
}
