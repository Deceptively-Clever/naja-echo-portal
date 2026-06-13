import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { PageHeader } from '../components/PageHeader'
import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'

export function ProfilePage() {
  const { data: session } = useCurrentUser()
  const user = session?.authenticated ? session.user : null

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Profile"
        description="Manage your account and personal information."
      />
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Account</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {user ? (
            <>
              <div className="flex flex-col gap-1">
                <span className="text-sm font-medium">Display Name</span>
                <span className="text-sm text-muted-foreground">{user.displayName}</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-sm font-medium">Discord</span>
                <span className="text-sm text-muted-foreground">{user.discordUsername}</span>
              </div>
              <div className="flex flex-col gap-2">
                <span className="text-sm font-medium">Roles</span>
                {user.roles.length > 0 ? (
                  <div className="flex flex-wrap gap-2">
                    {user.roles.map((role) => (
                      <Badge key={role} variant="secondary">{role}</Badge>
                    ))}
                  </div>
                ) : (
                  <span className="text-sm text-muted-foreground">No roles assigned</span>
                )}
              </div>
            </>
          ) : (
            <p className="text-sm text-muted-foreground">Loading profile...</p>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
