export const locationKeys = {
  all: ['locations'] as const,
  search: (search?: string, limit?: number) => [...locationKeys.all, 'search', search, limit] as const,
}
