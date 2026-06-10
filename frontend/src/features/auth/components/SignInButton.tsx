import { buttonVariants } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { getSignInUrl } from '../api/authApi'

export function SignInButton() {
  return (
    <a
      href={getSignInUrl()}
      className={cn(buttonVariants())}
      aria-label="Sign in with Discord"
    >
      Sign in with Discord
    </a>
  )
}
