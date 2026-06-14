import { z } from 'zod'

export const changeQuantityFormSchema = z.object({
  quantity: z.number().int().min(1, { message: 'Quantity must be at least 1.' }),
})

export type ChangeQuantityFormValues = z.infer<typeof changeQuantityFormSchema>
