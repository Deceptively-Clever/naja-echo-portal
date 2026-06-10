import { Button } from '@/components/ui/button'
import { useSignOut } from '../hooks/useSignOut'

export function SignOutButton() {
  const { mutate, isPending } = useSignOut()

  return (
    <Button
      variant="outline"
      onClick={() => mutate()}
      disabled={isPending}
      aria-label="Sign out"
    >
      {isPending ? 'Signing out…' : 'Sign out'}
    </Button>
  )
}
