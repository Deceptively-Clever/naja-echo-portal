import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { LandingPage } from '@/features/auth/pages/LandingPage'
import { AuthCallbackPage } from '@/features/auth/pages/AuthCallbackPage'
import { AuthErrorPage } from '@/features/auth/pages/AuthErrorPage'
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage'
import { ProfilePage } from '@/features/dashboard/pages/ProfilePage'
import { SettingsPage } from '@/features/dashboard/pages/SettingsPage'
import { DashboardLayout } from '@/features/dashboard/components/DashboardLayout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { AdminRoute } from '@/features/auth/AdminRoute'
import { DataImportPage } from '@/features/admin/pages/DataImportPage'
import { MyHangarView } from '@/features/hangar/pages/MyHangarView'
import { OrgHangarView } from '@/features/hangar/pages/OrgHangarView'
import { WarehouseItemsView } from '@/features/warehouse/pages/WarehouseItemsView'

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
            <Route path="/hangar" element={<Navigate to="/hangar/mine" replace />} />
            <Route path="/hangar/mine" element={<MyHangarView />} />
            <Route path="/hangar/org" element={<OrgHangarView />} />
            <Route path="/warehouse" element={<Navigate to="/warehouse/items" replace />} />
            <Route path="/warehouse/items" element={<WarehouseItemsView />} />
            <Route path="/dashboard/profile" element={<ProfilePage />} />
            <Route path="/dashboard/settings" element={<SettingsPage />} />
            <Route element={<AdminRoute />}>
              <Route path="/dashboard/admin/data-import" element={<DataImportPage />} />
            </Route>
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
