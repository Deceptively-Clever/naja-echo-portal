import { DashboardNav } from './DashboardNav'
import { navItems } from '../navigation/navItems'

export function DashboardSidebar() {
  return (
    <aside className="hidden w-56 shrink-0 border-r border-border bg-card md:flex md:flex-col">
      <nav aria-label="Primary navigation" className="flex flex-col gap-1 p-3 pt-4">
        <DashboardNav items={navItems} />
      </nav>
    </aside>
  )
}
