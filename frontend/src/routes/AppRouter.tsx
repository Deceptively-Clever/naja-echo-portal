import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { LandingPage } from '@/features/auth/pages/LandingPage'
import { AuthCallbackPage } from '@/features/auth/pages/AuthCallbackPage'
import { AuthErrorPage } from '@/features/auth/pages/AuthErrorPage'
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage'
import { ProfilePage } from '@/features/dashboard/pages/ProfilePage'
import { SettingsPage } from '@/features/dashboard/pages/SettingsPage'
import { DashboardLayout } from '@/features/dashboard/components/DashboardLayout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/auth/callback" element={<AuthCallbackPage />} />
        <Route path="/auth/error" element={<AuthErrorPage />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<DashboardLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/dashboard/profile" element={<ProfilePage />} />
            <Route path="/dashboard/settings" element={<SettingsPage />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
