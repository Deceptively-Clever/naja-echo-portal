import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import type { CurrentUser } from '@/features/auth/schemas/currentUserSchema'

interface UserBadgeProps {
  user: CurrentUser
}

export function UserBadge({ user }: UserBadgeProps) {
  const initial = user.displayName.charAt(0).toUpperCase()

  return (
    <div className="flex items-center gap-3" aria-label={`Signed in as ${user.displayName}`}>
      <Avatar>
        {user.avatarUrl && (
          <AvatarImage src={user.avatarUrl} alt={user.displayName} />
        )}
        <AvatarFallback aria-hidden>{initial}</AvatarFallback>
      </Avatar>
      <span className="font-medium">{user.displayName}</span>
    </div>
  )
}
