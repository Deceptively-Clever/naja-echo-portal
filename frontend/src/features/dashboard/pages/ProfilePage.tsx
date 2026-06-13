import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { PageHeader } from '../components/PageHeader'

export function ProfilePage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Profile"
        description="Manage your account and personal information."
      />
      <Card>
        <CardContent className="pt-6">
          <p className="text-sm text-muted-foreground">
            Profile management is being built. Check back soon.
          </p>
          <div className="mt-3">
            <Badge variant="muted">Coming soon</Badge>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
