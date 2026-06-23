import { z } from 'zod'

export const addItemFormSchema = z.object({
  itemId: z.string().uuid({ message: 'Select an item from the catalog.' }),
  ownerUserId: z.string().uuid({ message: 'Select an owner.' }),
  locationId: z.string().uuid({ message: 'Select a location.' }).optional(),
  locationType: z.enum(['Station', 'City']).optional(),
  quantity: z.number().int().min(1, { message: 'Quantity must be at least 1.' }),
  quality: z.number().int().min(1, { message: 'Quality must be at least 1.' }).max(1000, { message: 'Quality must be at most 1000.' }),
})

export type AddItemFormValues = z.infer<typeof addItemFormSchema>
