import { useMutation, useQueryClient } from '@tanstack/react-query'
import { importHangar } from '../api/hangarApi'
import { hangarKeys } from './hangarQueryKeys'
import type { ImportShipRecord } from '../schemas/hangarImport'

export function useImportHangar() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (items: ImportShipRecord[]) => importHangar(items),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hangarKeys.mine() })
      queryClient.invalidateQueries({ queryKey: hangarKeys.org() })
      queryClient.invalidateQueries({ queryKey: hangarKeys.orgMembers() })
      queryClient.invalidateQueries({ queryKey: hangarKeys.catalog() })
    },
  })
}
