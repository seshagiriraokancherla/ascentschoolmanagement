import { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ConfigProvider, App as AntApp } from 'antd'
import { useBrandingStore } from './store/brandingStore'
import { useAuthStore }     from './store/authStore'
import LoginPage        from './pages/auth/LoginPage'
import AppLayout        from './layouts/AppLayout'
import RolesPage        from './pages/rbac/RolesPage'
import UsersPage        from './pages/rbac/UsersPage'
import MasterDataPage   from './pages/master/MasterDataPage'
import StudentsPage        from './pages/students/StudentsPage'
import StudentFormPage     from './pages/students/StudentFormPage'
import FeeStructurePage    from './pages/fee/FeeStructurePage'
import FeeCollectionPage   from './pages/fee/FeeCollectionPage'
import ReceiptsPage        from './pages/fee/ReceiptsPage'
import GatewaySettingsPage   from './pages/fee/GatewaySettingsPage'
import DashboardPage         from './pages/dashboard/DashboardPage'
import AttendancePage        from './pages/attendance/AttendancePage'
import TransportPage         from './pages/transport/TransportPage'
import MarksEntryPage        from './pages/marks/MarksEntryPage'
import HomeworkPage          from './pages/homework/HomeworkPage'
import AnnouncementsPage     from './pages/announcements/AnnouncementsPage'
import EventsPage            from './pages/events/EventsPage'
import StudentsImportPage    from './pages/students/StudentsImportPage'
import PromoteStudentsPage   from './pages/students/PromoteStudentsPage'
import FeeStructureImportPage from './pages/fee/FeeStructureImportPage'
import ReportsPage            from './pages/reports/ReportsPage'
import StaffPage                   from './pages/staff/StaffPage'
import StaffAttendancePage         from './pages/staff/StaffAttendancePage'
import StaffAttendanceSummaryPage  from './pages/staff/StaffAttendanceSummaryPage'
import StaffAdvancesPage           from './pages/staff/StaffAdvancesPage'
import StaffSalariesPage           from './pages/staff/StaffSalariesPage'
import BloodGroupSearchPage        from './pages/students/BloodGroupSearchPage'
import SMSPage                    from './pages/sms/SMSPage'

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'

const getSubdomain = () => {
  if (import.meta.env.VITE_SUBDOMAIN) return import.meta.env.VITE_SUBDOMAIN
  const parts = window.location.hostname.split('.')
  return parts.length >= 3 ? parts[0] : null
}

function RequireAuth({ children }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  return isAuthenticated ? children : <Navigate to="/login" replace />
}


function App() {
  const { branding, setBranding } = useBrandingStore()
  const login                     = useAuthStore((s) => s.login)

  const subdomain = getSubdomain()
  const headers   = subdomain ? { 'X-Subdomain': subdomain } : {}

  // Load branding before anything else renders (public endpoint, no auth)
  useEffect(() => {
    fetch(`${API_BASE}/branding`, { headers })
      .then((r) => r.json())
      .then((body) => setBranding(body.data))
      .catch(() => {}) // Defaults in brandingStore used on failure
  }, [])

  // Apply CSS variables for non-AntD elements
  useEffect(() => {
    const r = document.documentElement
    r.style.setProperty('--primary-color',   branding.primaryColor)
    r.style.setProperty('--secondary-color', branding.secondaryColor)
    r.style.setProperty('--header-bg',       branding.headerBgColor)
    r.style.setProperty('--nav-text-color',  branding.navTextColor)
    if (branding.displayName) document.title = branding.displayName
    if (branding.faviconPath) {
      const link = document.querySelector("link[rel*='icon']") || document.createElement('link')
      link.rel  = 'shortcut icon'
      link.href = branding.faviconPath
      document.head.appendChild(link)
    }
  }, [branding])

  // Silent refresh on mount — restores session across page reloads
  useEffect(() => {
    const schoolId = localStorage.getItem('schoolId')
    const params   = schoolId ? `?schoolId=${schoolId}` : ''
    fetch(`${API_BASE}/school/auth/refresh${params}`, {
      method:      'POST',
      credentials: 'include',
      headers,
    })
      .then((r) => r.json())
      .then((body) => {
        const data = body.data
        login(data.accessToken, {
          userId:      data.userId,
          fullName:    data.fullName,
          groupId:     data.groupId,
          schoolId:    data.schoolId,
          permissions: data.permissions || [],
        })
      })
      .catch(() => {}) // Not logged in — user goes to /login
  }, [])

  return (
    <ConfigProvider theme={{ token: { colorPrimary: branding.primaryColor || '#1677ff', colorLink: branding.primaryColor || '#1677ff' } }}>
      <AntApp>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />

            <Route
              path="/"
              element={<RequireAuth><AppLayout /></RequireAuth>}
            >
              <Route index element={<DashboardPage />} />
              <Route path="students"              element={<StudentsPage />} />
              <Route path="students/new"          element={<StudentFormPage />} />
              <Route path="students/import"       element={<StudentsImportPage />} />
              <Route path="students/promote"         element={<PromoteStudentsPage />} />
              <Route path="students/blood-group"     element={<BloodGroupSearchPage />} />
              <Route path="students/:id"          element={<StudentFormPage />} />
              <Route path="fees/structure"        element={<FeeStructurePage />} />
              <Route path="fees/structure/import" element={<FeeStructureImportPage />} />
              <Route path="fees/collect"          element={<FeeCollectionPage />} />
              <Route path="fees/receipts"         element={<ReceiptsPage />} />
              <Route path="master"           element={<MasterDataPage />} />
              <Route path="attendance"               element={<AttendancePage />} />
              <Route path="transport"                element={<TransportPage />} />
              <Route path="marks"                    element={<MarksEntryPage />} />
              <Route path="homework"                 element={<HomeworkPage />} />
              <Route path="announcements"            element={<AnnouncementsPage />} />
              <Route path="events"                  element={<EventsPage />} />
              <Route path="reports"                   element={<ReportsPage />} />
              <Route path="staff"                        element={<StaffPage />} />
              <Route path="staff/attendance"             element={<StaffAttendancePage />} />
              <Route path="staff/attendance/summary"     element={<StaffAttendanceSummaryPage />} />
              <Route path="staff/advances"               element={<StaffAdvancesPage />} />
              <Route path="staff/salaries"               element={<StaffSalariesPage />} />
              <Route path="sms"                          element={<SMSPage />} />
              <Route path="settings/roles"           element={<RolesPage />} />
              <Route path="settings/users"           element={<UsersPage />} />
              <Route path="settings/payment-gateway" element={<GatewaySettingsPage />} />
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  )
}

export default App
