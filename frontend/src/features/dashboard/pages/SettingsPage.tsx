import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { PageHeader } from '../components/PageHeader'

export function SettingsPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Settings"
        description="Configure your preferences and application settings."
      />
      <Card>
        <CardContent className="pt-6">
          <p className="text-sm text-muted-foreground">
            Settings configuration is being built. Check back soon.
          </p>
          <div className="mt-3">
            <Badge variant="muted">Coming soon</Badge>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
