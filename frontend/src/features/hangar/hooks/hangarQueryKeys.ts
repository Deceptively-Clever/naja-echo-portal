export const hangarKeys = {
  all: ['hangar'] as const,
  mine: () => [...hangarKeys.all, 'mine'] as const,
  myList: (search?: string, page?: number) =>
    [...hangarKeys.mine(), { search, page }] as const,
  org: () => [...hangarKeys.all, 'org'] as const,
  orgList: (search?: string, mine?: boolean, memberId?: string, sortBy?: string, page?: number) =>
    [...hangarKeys.org(), { search, mine, memberId, sortBy, page }] as const,
  orgMembers: () => [...hangarKeys.org(), 'members'] as const,
  catalog: () => [...hangarKeys.all, 'catalog'] as const,
  catalogSearch: (search?: string, page?: number) =>
    [...hangarKeys.catalog(), { search, page }] as const,
}
