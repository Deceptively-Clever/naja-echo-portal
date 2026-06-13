import { useInfiniteQuery } from '@tanstack/react-query'
import { getOrgHangar } from '../api/hangarApi'
import { hangarKeys } from './hangarQueryKeys'

export function useOrgHangar(search?: string, mine?: boolean, memberId?: string, pageSize = 25) {
  return useInfiniteQuery({
    queryKey: hangarKeys.orgList(search, mine, memberId),
    queryFn: ({ pageParam = 1 }) =>
      getOrgHangar({ search, mine, memberId, page: pageParam as number, pageSize }),
    initialPageParam: 1,
    getNextPageParam: (last) =>
      last.page < last.totalPages ? last.page + 1 : undefined,
  })
}
