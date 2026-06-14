import { useMutation } from '@tanstack/react-query'
import { importCommodities } from '../api/commoditiesApi'

export function useImportCommodities() {
  return useMutation({
    mutationFn: importCommodities,
  })
}
