import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

export type StatusType = 'success' | 'warning' | 'error'

const statusClasses: Record<StatusType, string> = {
  success: 'text-green-600 dark:text-green-400',
  warning: 'text-yellow-600 dark:text-yellow-400',
  error: 'text-destructive',
}

interface StatusMessageProps {
  type: StatusType
  className?: string
  children: ReactNode
}

/** Inline status text with the shared success/warning/error color scale. */
export function StatusMessage({ type, className, children }: StatusMessageProps) {
  return (
    <p
      role="status"
      aria-live="polite"
      className={cn('text-sm', statusClasses[type], className)}
    >
      {children}
    </p>
  )
}
