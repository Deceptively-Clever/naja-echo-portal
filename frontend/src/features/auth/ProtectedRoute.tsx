import { Navigate, Outlet } from 'react-router-dom'
import { useCurrentUser } from './hooks/useCurrentUser'

export function ProtectedRoute() {
  const { data: user, isLoading } = useCurrentUser()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <span className="text-gray-500">Loading…</span>
      </div>
    )
  }

  if (!user) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
