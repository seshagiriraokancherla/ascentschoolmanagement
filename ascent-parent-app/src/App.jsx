import { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ConfigProvider, App as AntApp } from 'antd'
import { useAuthStore }    from './store/authStore'
import { useBrandingStore } from './store/brandingStore'
import { connectAuthStore } from './api/axiosInstance'
import api                 from './api/axiosInstance'
import LoginPage           from './pages/auth/LoginPage'
import ChildSelectorPage   from './pages/auth/ChildSelectorPage'
import AppLayout           from './layouts/AppLayout'
import FeeHomePage         from './pages/fees/FeeHomePage'
import PaymentSuccessPage  from './pages/fees/PaymentSuccessPage'

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'

function RequireAuth({ children }) {
  const token = useAuthStore((s) => s.accessToken)
  return token ? children : <Navigate to="/login" replace />
}

function RequireChild({ children }) {
  const child = useAuthStore((s) => s.child)
  const token = useAuthStore((s) => s.accessToken)
  if (!token)  return <Navigate to="/login" replace />
  if (!child)  return <Navigate to="/select-child" replace />
  return children
}

export default function App() {
  const { branding, setBranding } = useBrandingStore()
  const { login, setChild, setToken, logout } = useAuthStore()

  // Connect auth store to axiosInstance for silent refresh
  useEffect(() => {
    connectAuthStore(
      () => useAuthStore.getState().accessToken,
      (t) => { if (t) setToken(t); else logout() }
    )
  }, [])

  // Load branding
  useEffect(() => {
    const schoolCode = import.meta.env.VITE_SCHOOL_CODE || ''
    fetch(`${API_BASE}/branding`, {
      headers: schoolCode ? { 'X-Subdomain': schoolCode } : {},
    })
      .then((r) => r.json())
      .then((b) => setBranding(b.data))
      .catch(() => {})
  }, [])

  // Apply branding CSS
  useEffect(() => {
    const r = document.documentElement
    r.style.setProperty('--primary-color',  branding.primaryColor)
    r.style.setProperty('--header-bg',      branding.headerBgColor)
    r.style.setProperty('--nav-text-color', branding.navTextColor)
    if (branding.displayName) document.title = `${branding.displayName} — Fee Portal`
    if (branding.faviconPath) {
      const link = document.querySelector("link[rel*='icon']") || document.createElement('link')
      link.rel  = 'shortcut icon'
      link.href = branding.faviconPath
      document.head.appendChild(link)
    }
  }, [branding])

  // Silent refresh on mount
  useEffect(() => {
    api.post('/mobile/auth/parent/refresh', null)
      .then((r) => {
        const data = r.data?.data
        if (data?.accessToken) {
          login(data.accessToken, {
            parentId:    data.parentId,
            displayName: data.displayName,
          })
          if (data.studentId) {
            setChild(data.accessToken, {
              studentId:   data.studentId,
              studentName: data.studentName,
              className:   data.className,
              admissionNo: data.admissionNo,
            })
          }
        }
      })
      .catch(() => {})
  }, [])

  return (
    <ConfigProvider theme={{ token: { colorPrimary: branding.primaryColor || '#1677ff' } }}>
      <AntApp>
        <BrowserRouter>
          <Routes>
            <Route path="/login"        element={<LoginPage />} />
            <Route path="/select-child" element={<RequireAuth><ChildSelectorPage /></RequireAuth>} />
            <Route
              path="/"
              element={<RequireChild><AppLayout /></RequireChild>}
            >
              <Route index                element={<Navigate to="/fees" replace />} />
              <Route path="fees"          element={<FeeHomePage />} />
              <Route path="fees/success"  element={<PaymentSuccessPage />} />
            </Route>
            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  )
}
