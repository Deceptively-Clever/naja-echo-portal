import { useState } from 'react'
import { Menu } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/hooks/useCurrentUser'
import { AccountMenu } from './AccountMenu'
import { DashboardMobileNav } from './DashboardMobileNav'
import { navItems } from '../navigation/navItems'
import { ThemeToggle } from '@/features/theme/ThemeToggle'

export function DashboardHeader() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const { data: session } = useCurrentUser()
  const roles = session?.authenticated ? session.user.roles : []

  return (
    <header className="sticky top-0 z-40 flex h-14 items-center border-b border-border bg-card px-4">
      <div className="flex flex-1 items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          className="md:hidden"
          aria-label="Open navigation"
          onClick={() => setMobileNavOpen(true)}
        >
          <Menu className="h-5 w-5" aria-hidden />
        </Button>

        <Link
          to="/dashboard"
          className="flex items-center gap-2 font-semibold text-brand focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring rounded-sm"
          aria-label="Naja Echó Portal home"
        >
          <span className="text-lg tracking-tight">Naja Echó</span>
        </Link>
      </div>

      <div className="flex items-center gap-2">
        <ThemeToggle />
        <AccountMenu />
      </div>

      <DashboardMobileNav
        items={navItems}
        roles={roles}
        open={mobileNavOpen}
        onOpenChange={setMobileNavOpen}
      />
    </header>
  )
}
