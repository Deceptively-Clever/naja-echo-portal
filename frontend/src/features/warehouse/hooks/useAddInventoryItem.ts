import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addInventoryItem } from '../api/warehouseApi'
import { addShipComponent } from '../api/shipComponentsApi'
import { warehouseKeys } from './warehouseQueryKeys'

export function useAddInventoryItem(scope: 'inventory' | 'ship-components' = 'inventory') {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: scope === 'ship-components' ? addShipComponent : addInventoryItem,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: warehouseKeys.inventory() })
      if (scope === 'ship-components') {
        void queryClient.invalidateQueries({ queryKey: warehouseKeys.shipComponents() })
      }
    },
  })
}
