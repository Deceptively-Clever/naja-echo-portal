export const itemKeys = {
  all: ['items'] as const,
  categories: () => [...itemKeys.all, 'categories'] as const,
}
