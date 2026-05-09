import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ConfigProvider, App as AntApp } from 'antd'
import useAuthStore from './store/authStore'
import LoginPage            from './pages/auth/LoginPage'
import MainLayout           from './layouts/MainLayout'
import SchoolGroupsPage     from './pages/school-groups/SchoolGroupsPage'
import SchoolGroupDetailPage from './pages/school-groups/SchoolGroupDetailPage'

const ASCENT_PRIMARY = '#1677ff'

function RequireAuth({ children }) {
  const isAuthenticated = useAuthStore(s => s.isAuthenticated)
  // Check sessionStorage on page reload (store not hydrated yet)
  const hasToken = isAuthenticated || !!sessionStorage.getItem('controlAccessToken')
  return hasToken ? children : <Navigate to="/login" replace />
}

function App() {
  return (
    <ConfigProvider theme={{ token: { colorPrimary: ASCENT_PRIMARY } }}>
      <AntApp>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route
              path="/"
              element={<RequireAuth><MainLayout /></RequireAuth>}
            >
              <Route index element={<Navigate to="/school-groups" replace />} />
              <Route path="school-groups"     element={<SchoolGroupsPage />} />
              <Route path="school-groups/:id" element={<SchoolGroupDetailPage />} />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  )
}

export default App
