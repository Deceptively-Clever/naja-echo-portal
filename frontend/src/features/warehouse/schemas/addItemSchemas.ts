import { z } from 'zod'

export const addItemFormSchema = z.object({
  itemId: z.string().uuid({ message: 'Select an item from the catalog.' }),
  ownerUserId: z.string().uuid({ message: 'Select an owner.' }),
  location: z.string().min(1, { message: 'Location is required.' }),
  quantity: z.number().int().min(1, { message: 'Quantity must be at least 1.' }),
})

export type AddItemFormValues = z.infer<typeof addItemFormSchema>
