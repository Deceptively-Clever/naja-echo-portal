export const characterKeys = {
  all: ['characters'] as const,
  list: () => [...characterKeys.all, 'list'] as const,
  registration: () => [...characterKeys.all, 'registration'] as const,
}
