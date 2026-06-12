import { UserBadge } from '../components/UserBadge'
import { SignOutButton } from '@/features/auth/components/SignOutButton'
import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function DashboardPage() {
  const { data: session } = useCurrentUser()

  if (!session?.authenticated) return null

  return (
    <main className="flex min-h-screen flex-col items-center justify-center bg-gray-50 px-4">
      <Card className="w-full max-w-md">
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Dashboard</CardTitle>
          <SignOutButton />
        </CardHeader>
        <CardContent>
          <UserBadge user={session.user} />
          <p className="mt-4 text-sm text-gray-500">
            Welcome to NajaEchoPortal. More org tools coming soon.
          </p>
        </CardContent>
      </Card>
    </main>
  )
}
