import { useMutation } from '@tanstack/react-query'
import { importItems } from '../api/itemsApi'

export function useImportItems() {
  return useMutation({
    mutationFn: (categoryUexId?: number) => importItems(categoryUexId),
  })
}
