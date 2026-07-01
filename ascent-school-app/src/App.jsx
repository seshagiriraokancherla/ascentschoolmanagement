import { useEffect, useState } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ConfigProvider, App as AntApp, Spin } from 'antd'
import { useBrandingStore } from './store/brandingStore'
import { useAuthStore }     from './store/authStore'
import LoginPage        from './pages/auth/LoginPage'
import AppLayout        from './layouts/AppLayout'
import RolesPage        from './pages/rbac/RolesPage'
import UsersPage        from './pages/rbac/UsersPage'
import MasterDataPage   from './pages/master/MasterDataPage'
import StudentsPage        from './pages/students/StudentsPage'
import StudentFormPage     from './pages/students/StudentFormPage'
import FeeStructurePage       from './pages/fee/FeeStructurePage'
import AdmissionFeePage       from './pages/fee/AdmissionFeePage'
import SchoolFeePage          from './pages/fee/SchoolFeePage'
import TransportFeePage       from './pages/fee/TransportFeePage'
import HostelFeePage          from './pages/fee/HostelFeePage'
import OtherFeePage           from './pages/fee/OtherFeePage'
import ReceiptsPage           from './pages/fee/ReceiptsPage'
import PendingPaymentsPage     from './pages/fee/PendingPaymentsPage'
import GatewaySettingsPage    from './pages/fee/GatewaySettingsPage'
import DashboardPage         from './pages/dashboard/DashboardPage'
import AttendancePage        from './pages/attendance/AttendancePage'
import TransportPage         from './pages/transport/TransportPage'
import HostelPage            from './pages/hostel/HostelPage'
import MarksEntryPage        from './pages/marks/MarksEntryPage'
import HomeworkPage          from './pages/homework/HomeworkPage'
import DailyHomeworkPage     from './pages/homework/DailyHomeworkPage'
import AnnouncementsPage     from './pages/announcements/AnnouncementsPage'
import EventsPage            from './pages/events/EventsPage'
import StudentsImportPage    from './pages/students/StudentsImportPage'
import PromoteStudentsPage   from './pages/students/PromoteStudentsPage'
import FeeStructureImportPage from './pages/fee/FeeStructureImportPage'
import FeeReceiptsImportPage  from './pages/fee/FeeReceiptsImportPage'
import ReportsPage            from './pages/reports/ReportsPage'
import StaffPage                   from './pages/staff/StaffPage'
import StaffAttendancePage         from './pages/staff/StaffAttendancePage'
import StaffAttendanceSummaryPage  from './pages/staff/StaffAttendanceSummaryPage'
import StaffAdvancesPage           from './pages/staff/StaffAdvancesPage'
import StaffSalariesPage           from './pages/staff/StaffSalariesPage'
import BloodGroupSearchPage        from './pages/students/BloodGroupSearchPage'
import SMSPage                    from './pages/sms/SMSPage'
import FeeConcessionPage          from './pages/fee/FeeConcessionPage'
import SchoolSettingsPage         from './pages/settings/SchoolSettingsPage'
import SmsGatewayPage             from './pages/settings/SmsGatewayPage'
import NoAccess                   from './pages/NoAccess'
import { PATH_PERM }              from './config/permissions'

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

/**
 * Page-level permission gate. `path` is looked up in PATH_PERM; if the user
 * lacks the required permission, a 403 page is shown instead of the page.
 * Pages with no mapping (or a null mapping, e.g. Dashboard) are always allowed.
 */
function Protected({ path, children }) {
  const hasPermission = useAuthStore((s) => s.hasPermission)
  const perm = PATH_PERM[path]
  if (perm && !hasPermission(perm)) return <NoAccess />
  return children
}


function App() {
  const { branding, setBranding } = useBrandingStore()
  const login                     = useAuthStore((s) => s.login)

  // Gate rendering until the mount silent-refresh has resolved. Without this,
  // RequireAuth sees isAuthenticated=false on a page reload (access token is in
  // memory only) and redirects to /login BEFORE the refresh completes — so a
  // valid session would still get bounced to the login page on every F5.
  const [authChecked, setAuthChecked] = useState(false)

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
      .finally(() => setAuthChecked(true)) // release the render gate either way
  }, [])

  // Hold rendering (don't mount Routes/RequireAuth) until the session check is done.
  if (!authChecked) {
    return (
      <div style={{ height: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <Spin size="large" />
      </div>
    )
  }

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
              <Route path="students"              element={<Protected path="/students"><StudentsPage /></Protected>} />
              <Route path="students/new"          element={<Protected path="/students/new"><StudentFormPage /></Protected>} />
              <Route path="students/import"       element={<Protected path="/students/import"><StudentsImportPage /></Protected>} />
              <Route path="students/promote"         element={<Protected path="/students/promote"><PromoteStudentsPage /></Protected>} />
              <Route path="students/blood-group"     element={<Protected path="/students/blood-group"><BloodGroupSearchPage /></Protected>} />
              <Route path="students/:id"          element={<Protected path="/students/:id"><StudentFormPage /></Protected>} />
              <Route path="fees/structure"             element={<Protected path="/fees/structure"><FeeStructurePage /></Protected>} />
              <Route path="fees/structure/import"      element={<Protected path="/fees/structure/import"><FeeStructureImportPage /></Protected>} />
              <Route path="fees/collect/admission"     element={<Protected path="/fees/collect/admission"><AdmissionFeePage /></Protected>} />
              <Route path="fees/collect/school"        element={<Protected path="/fees/collect/school"><SchoolFeePage /></Protected>} />
              <Route path="fees/collect/transport"     element={<Protected path="/fees/collect/transport"><TransportFeePage /></Protected>} />
              <Route path="fees/collect/hostel"        element={<Protected path="/fees/collect/hostel"><HostelFeePage /></Protected>} />
              <Route path="fees/collect/other"         element={<Protected path="/fees/collect/other"><OtherFeePage /></Protected>} />
              <Route path="fees/receipts"              element={<Protected path="/fees/receipts"><ReceiptsPage /></Protected>} />
              <Route path="fees/receipts/import"      element={<Protected path="/fees/receipts/import"><FeeReceiptsImportPage /></Protected>} />
              <Route path="fees/pending-payments"     element={<Protected path="/fees/pending-payments"><PendingPaymentsPage /></Protected>} />
              <Route path="fees/concessions"          element={<Protected path="/fees/concessions"><FeeConcessionPage /></Protected>} />
              <Route path="master"           element={<Protected path="/master"><MasterDataPage /></Protected>} />
              <Route path="attendance"               element={<Protected path="/attendance"><AttendancePage /></Protected>} />
              <Route path="transport"                element={<Protected path="/transport"><TransportPage /></Protected>} />
              <Route path="hostel"                  element={<Protected path="/hostel"><HostelPage /></Protected>} />
              <Route path="marks"                    element={<Protected path="/marks"><MarksEntryPage /></Protected>} />
              <Route path="homework/daily"           element={<Protected path="/homework/daily"><DailyHomeworkPage /></Protected>} />
              <Route path="homework"                 element={<Protected path="/homework"><HomeworkPage /></Protected>} />
              <Route path="announcements"            element={<Protected path="/announcements"><AnnouncementsPage /></Protected>} />
              <Route path="events"                  element={<Protected path="/events"><EventsPage /></Protected>} />
              <Route path="reports"                   element={<Protected path="/reports"><ReportsPage /></Protected>} />
              <Route path="staff"                        element={<Protected path="/staff"><StaffPage /></Protected>} />
              <Route path="staff/attendance"             element={<Protected path="/staff/attendance"><StaffAttendancePage /></Protected>} />
              <Route path="staff/attendance/summary"     element={<Protected path="/staff/attendance/summary"><StaffAttendanceSummaryPage /></Protected>} />
              <Route path="staff/advances"               element={<Protected path="/staff/advances"><StaffAdvancesPage /></Protected>} />
              <Route path="staff/salaries"               element={<Protected path="/staff/salaries"><StaffSalariesPage /></Protected>} />
              <Route path="sms"                          element={<Protected path="/sms"><SMSPage /></Protected>} />
              <Route path="settings/school"          element={<Protected path="/settings/school"><SchoolSettingsPage /></Protected>} />
              <Route path="settings/roles"           element={<Protected path="/settings/roles"><RolesPage /></Protected>} />
              <Route path="settings/users"           element={<Protected path="/settings/users"><UsersPage /></Protected>} />
              <Route path="settings/payment-gateway" element={<Protected path="/settings/payment-gateway"><GatewaySettingsPage /></Protected>} />
              <Route path="settings/sms-gateway"     element={<Protected path="/settings/sms-gateway"><SmsGatewayPage /></Protected>} />
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  )
}

export default App
