export const commodityKeys = {
  all: ['commodities'] as const,
  list: (page: number, pageSize: number) => ['commodities', 'list', page, pageSize] as const,
}
