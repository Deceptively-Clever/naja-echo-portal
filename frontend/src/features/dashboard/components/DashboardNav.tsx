import { NavLink } from 'react-router-dom'
import { cn } from '@/lib/utils'
import type { NavItem } from '../navigation/navItems'

interface DashboardNavProps {
  items: NavItem[]
  onNavigate?: () => void
}

export function DashboardNav({ items, onNavigate }: DashboardNavProps) {
  return (
    <ul className="flex flex-col gap-1" role="list">
      {items.map((item) => (
        <li key={item.path}>
          <NavLink
            to={item.path}
            end={item.end}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-1',
                isActive
                  ? 'border-l-2 border-primary bg-accent/30 pl-[calc(0.75rem-2px)] text-foreground'
                  : 'border-l-2 border-transparent text-muted-foreground hover:bg-accent/20 hover:text-foreground'
              )
            }
          >
            {({ isActive }) => (
              <>
                <item.icon className="h-4 w-4 shrink-0" aria-hidden />
                <span>{item.label}</span>
                {isActive && <span className="sr-only">(current)</span>}
              </>
            )}
          </NavLink>
        </li>
      ))}
    </ul>
  )
}
