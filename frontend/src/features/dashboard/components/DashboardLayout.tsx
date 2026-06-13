import { Outlet } from 'react-router-dom'
import { DashboardHeader } from './DashboardHeader'
import { DashboardSidebar } from './DashboardSidebar'

export function DashboardLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-background">
      <DashboardHeader />
      <div className="flex flex-1 overflow-hidden">
        <DashboardSidebar />
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
