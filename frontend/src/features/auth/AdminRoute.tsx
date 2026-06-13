import { Navigate, Outlet } from 'react-router-dom'
import { useCurrentUser } from './hooks/useCurrentUser'

export function AdminRoute() {
  const { data: session, isLoading } = useCurrentUser()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <span className="text-muted-foreground">Loading…</span>
      </div>
    )
  }

  if (!session?.authenticated) {
    return <Navigate to="/" replace />
  }

  if (!session.user.roles.includes('Admin')) {
    return <Navigate to="/dashboard" replace />
  }

  return <Outlet />
}
