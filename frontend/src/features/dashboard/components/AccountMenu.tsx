import { LogOut, User, Settings } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuItem,
} from '@/components/ui/dropdown-menu'
import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'
import { useSignOut } from '@/features/auth/hooks/useSignOut'

export function AccountMenu() {
  const { data: session } = useCurrentUser()
  const { mutate: signOut, isPending } = useSignOut()
  const navigate = useNavigate()

  if (!session?.authenticated) return null

  const { displayName, discordUsername } = session.user
  const initial = displayName.charAt(0).toUpperCase()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className="flex cursor-pointer items-center gap-2 rounded-md p-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={`Account menu for ${displayName}`}
        >
          <Avatar className="h-8 w-8">
            <AvatarFallback className="text-xs" aria-hidden>
              {initial}
            </AvatarFallback>
          </Avatar>
          <span className="hidden text-sm font-medium text-foreground md:block">{displayName}</span>
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-52">
        <DropdownMenuLabel className="font-normal">
          <div className="flex flex-col gap-0.5">
            <span className="font-medium text-foreground">{displayName}</span>
            <span className="text-xs text-muted-foreground">{discordUsername}</span>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={() => navigate('/dashboard/profile')} className="gap-2">
          <User className="h-4 w-4" aria-hidden />
          Profile
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => navigate('/dashboard/settings')} className="gap-2">
          <Settings className="h-4 w-4" aria-hidden />
          Settings
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onClick={() => signOut()}
          disabled={isPending}
          className="gap-2 text-destructive focus:text-destructive"
        >
          <LogOut className="h-4 w-4" aria-hidden />
          {isPending ? 'Signing out…' : 'Sign out'}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
