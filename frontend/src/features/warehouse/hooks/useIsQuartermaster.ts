import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'

export function useIsQuartermaster(): boolean {
  const { data: session } = useCurrentUser()
  if (session?.authenticated !== true) return false
  return session.user.roles.includes('Quartermaster') || session.user.roles.includes('Admin')
}
