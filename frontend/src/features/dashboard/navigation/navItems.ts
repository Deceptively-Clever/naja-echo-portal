import type { LucideIcon } from 'lucide-react'
import { Database, LayoutDashboard } from 'lucide-react'

export interface NavItem {
  label: string
  path: string
  icon: LucideIcon
  end?: boolean
  access?: string
  group?: string
}

export const navItems: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: LayoutDashboard, end: true },
  {
    label: 'Data Import',
    path: '/dashboard/admin/data-import',
    icon: Database,
    access: 'admin',
    group: 'Admin',
  },
]
