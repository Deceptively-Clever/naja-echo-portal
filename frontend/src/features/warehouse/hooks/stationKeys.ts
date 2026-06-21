export const stationKeys = {
  all: ['stations'] as const,
  search: (search?: string, limit?: number) => [...stationKeys.all, 'search', search, limit] as const,
}
