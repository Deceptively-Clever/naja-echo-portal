import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { DashboardNav } from './DashboardNav'
import type { NavItem } from '../navigation/navItems'

interface DashboardMobileNavProps {
  items: NavItem[]
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function DashboardMobileNav({ items, open, onOpenChange }: DashboardMobileNavProps) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="left" className="p-0">
        <SheetHeader className="border-b border-border px-4 py-3">
          <SheetTitle>Navigation</SheetTitle>
        </SheetHeader>
        <nav aria-label="Mobile navigation" className="p-3">
          <DashboardNav items={items} onNavigate={() => onOpenChange(false)} />
        </nav>
      </SheetContent>
    </Sheet>
  )
}
