import type { LucideIcon } from 'lucide-react'
import { LayoutDashboard } from 'lucide-react'

export interface NavItem {
  label: string
  path: string
  icon: LucideIcon
  end?: boolean
  access?: string
}

export const navItems: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: LayoutDashboard, end: true },
]
