export const adminUsers = {
  all: ['adminUsers'] as const,
  list: () => [...adminUsers.all, 'list'] as const,
}

export const userKeys = { adminUsers }
