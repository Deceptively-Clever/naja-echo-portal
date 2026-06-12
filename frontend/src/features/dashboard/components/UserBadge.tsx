import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import type { AuthenticatedSession } from '@/features/auth/schemas/sessionStateSchema'

interface UserBadgeProps {
  user: AuthenticatedSession['user']
}

export function UserBadge({ user }: UserBadgeProps) {
  const initial = user.displayName.charAt(0).toUpperCase()

  return (
    <div className="flex items-center gap-3" aria-label={`Signed in as ${user.displayName}`}>
      <Avatar>
        <AvatarFallback aria-hidden>{initial}</AvatarFallback>
      </Avatar>
      <span className="font-medium">{user.displayName}</span>
    </div>
  )
}
