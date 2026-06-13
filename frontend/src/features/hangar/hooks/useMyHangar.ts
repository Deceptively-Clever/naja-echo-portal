import { useInfiniteQuery } from '@tanstack/react-query'
import { getMyHangar } from '../api/hangarApi'
import { hangarKeys } from './hangarQueryKeys'

export function useMyHangar(search?: string, pageSize = 25) {
  return useInfiniteQuery({
    queryKey: hangarKeys.myList(search),
    queryFn: ({ pageParam = 1 }) => getMyHangar({ search, page: pageParam as number, pageSize }),
    initialPageParam: 1,
    getNextPageParam: (last) =>
      last.page < last.totalPages ? last.page + 1 : undefined,
  })
}
