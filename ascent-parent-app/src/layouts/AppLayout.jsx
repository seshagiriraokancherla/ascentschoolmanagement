import { Layout, Typography, Avatar, Dropdown, Tag } from 'antd'
import { UserOutlined, LogoutOutlined, SwapOutlined } from '@ant-design/icons'
import { Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore }     from '../store/authStore'
import { useBrandingStore } from '../store/brandingStore'
import api from '../api/axiosInstance'

const { Header, Content, Footer } = Layout

export default function AppLayout() {
  const navigate           = useNavigate()
  const { child, logout }  = useAuthStore()
  const { branding }       = useBrandingStore()
  const headerBg  = branding.headerBgColor || '#001529'
  const headerText = branding.navTextColor  || '#ffffff'

  const handleLogout = async () => {
    try { await api.post('/mobile/auth/parent/logout', null) } catch (_) {}
    logout()
    navigate('/login', { replace: true })
  }

  const handleSwitch = () => {
    useAuthStore.getState().setChild(useAuthStore.getState().accessToken, null)
    navigate('/select-child', { replace: true })
  }

  const menuItems = [
    { key: 'switch', icon: <SwapOutlined />, label: 'Switch Child', onClick: handleSwitch },
    { key: 'logout', icon: <LogoutOutlined />, label: 'Logout',       onClick: handleLogout },
  ]

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ background: headerBg, display: 'flex', alignItems: 'center', padding: '0 24px', gap: 12 }}>
        {branding.logoPath && (
          <img src={branding.logoPath} alt="logo" style={{ height: 36, objectFit: 'contain' }} />
        )}
        <Typography.Text strong style={{ color: headerText, fontSize: 16, flex: 1 }}>
          {branding.displayName} — Fee Portal
        </Typography.Text>

        <Dropdown menu={{ items: menuItems }} placement="bottomRight">
          <div style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 8 }}>
            <Avatar icon={<UserOutlined />} style={{ background: '#ffffff33' }} />
            <div style={{ textAlign: 'right' }}>
              <Typography.Text style={{ color: headerText, display: 'block', lineHeight: 1.2 }}>
                {child?.studentName}
              </Typography.Text>
              <Tag color="blue" style={{ marginTop: 2, fontSize: 11 }}>{child?.className}</Tag>
            </div>
          </div>
        </Dropdown>
      </Header>

      <Content style={{ padding: 24, background: '#f0f2f5' }}>
        <Outlet />
      </Content>

      <Footer style={{ textAlign: 'center', padding: '12px 24px', background: '#f0f2f5', color: '#8c8c8c', fontSize: 12 }}>
        Powered by Ascent Info Solutions
      </Footer>
    </Layout>
  )
}
