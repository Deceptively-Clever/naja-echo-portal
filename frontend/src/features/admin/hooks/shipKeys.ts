export const shipKeys = {
  all: ['ships'] as const,
  lists: () => [...shipKeys.all, 'list'] as const,
  list: (page: number, pageSize: number) => [...shipKeys.lists(), { page, pageSize }] as const,
  details: () => [...shipKeys.all, 'detail'] as const,
  detail: (id: string) => [...shipKeys.details(), id] as const,
}
