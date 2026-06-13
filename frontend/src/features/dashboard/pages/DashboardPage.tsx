import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'
import { PageHeader } from '../components/PageHeader'
import { BarChart3, Calendar, Users, Rocket } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

interface PlaceholderCardProps {
  title: string
  description: string
  icon: LucideIcon
}

function PlaceholderCard({ title, description, icon: Icon }: PlaceholderCardProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-base font-medium">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" aria-hidden />
      </CardHeader>
      <CardContent>
        <p className="text-sm text-muted-foreground">{description}</p>
        <div className="mt-3">
          <Badge variant="muted">Coming soon</Badge>
        </div>
      </CardContent>
    </Card>
  )
}

const placeholderCards: PlaceholderCardProps[] = [
  {
    title: 'Org Overview',
    description: 'Summary of your organization members, ships, and standing.',
    icon: BarChart3,
  },
  {
    title: 'Upcoming Operations',
    description: 'Scheduled fleet operations and events for your org.',
    icon: Calendar,
  },
  {
    title: 'Member Activity',
    description: 'Recent activity from your organization members.',
    icon: Users,
  },
  {
    title: 'Getting Started',
    description: 'Steps to get your Naja Echo org set up and running.',
    icon: Rocket,
  },
]

export function DashboardPage() {
  const { data: session } = useCurrentUser()
  const displayName = session?.authenticated ? session.user.displayName : null

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title={displayName ? `Welcome, ${displayName}` : 'Welcome'}
        description="Here's an overview of your Naja Echo organization."
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {placeholderCards.map((card) => (
          <PlaceholderCard key={card.title} {...card} />
        ))}
      </div>
    </div>
  )
}
