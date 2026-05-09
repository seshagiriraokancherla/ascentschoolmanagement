import { Layout, Menu, Avatar, Dropdown, Typography, Space } from 'antd'
import {
  BankOutlined,
  LogoutOutlined,
  UserOutlined,
} from '@ant-design/icons'
import { useNavigate, useLocation, Outlet } from 'react-router-dom'
import useAuthStore from '../store/authStore'
import api from '../api/axiosInstance'

const { Sider, Header, Content, Footer } = Layout

const NAV_ITEMS = [
  { key: '/school-groups', icon: <BankOutlined />, label: 'School Groups' },
]

export default function MainLayout() {
  const navigate  = useNavigate()
  const location  = useLocation()
  const { user, logout } = useAuthStore()

  const handleLogout = async () => {
    try { await api.post('/control/auth/logout') } catch { /* ignore */ }
    logout()
    navigate('/login')
  }

  const userMenu = {
    items: [
      { key: 'logout', icon: <LogoutOutlined />, label: 'Logout', danger: true },
    ],
    onClick: ({ key }) => { if (key === 'logout') handleLogout() },
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider theme="dark" width={220}>
        <div style={{ padding: '20px 16px', borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
          <Typography.Text strong style={{ color: '#fff', fontSize: 16 }}>
            Ascent Control
          </Typography.Text>
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname.split('/').slice(0, 2).join('/')]}
          items={NAV_ITEMS}
          onClick={({ key }) => navigate(key)}
          style={{ marginTop: 8 }}
        />
      </Sider>

      <Layout>
        <Header style={{ background: '#fff', padding: '0 24px', display: 'flex', alignItems: 'center', justifyContent: 'flex-end', boxShadow: '0 1px 4px rgba(0,0,0,0.08)' }}>
          <Dropdown menu={userMenu} placement="bottomRight">
            <Space style={{ cursor: 'pointer' }}>
              <Avatar icon={<UserOutlined />} />
              <span>{user?.fullName}</span>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>{user?.role}</Typography.Text>
            </Space>
          </Dropdown>
        </Header>

        <Content style={{ margin: 24, background: '#f0f2f5', minHeight: 'calc(100vh - 112px)' }}>
          <Outlet />
        </Content>
        <Footer style={{ textAlign: 'center', padding: '12px 24px', background: '#f0f2f5', color: '#8c8c8c', fontSize: 12 }}>
          Powered by Ascent Info Solutions
        </Footer>
      </Layout>
    </Layout>
  )
}
