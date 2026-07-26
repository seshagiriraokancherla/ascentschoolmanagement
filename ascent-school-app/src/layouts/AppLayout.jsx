/* global __APP_VERSION__, __BUILD_DATE__ */
import { useState, useEffect } from 'react'
import { Layout, Menu, Avatar, Dropdown, Typography, Modal, Form, Input, App as AntApp } from 'antd'
import {
  UserOutlined, LogoutOutlined, TeamOutlined, KeyOutlined,
  SafetyOutlined, SettingOutlined, DatabaseOutlined, SolutionOutlined,
  DollarOutlined, FormOutlined, BookOutlined, NotificationOutlined,
  CalendarOutlined, CarOutlined, VideoCameraOutlined, BarChartOutlined,
  IdcardOutlined, MessageOutlined, HomeOutlined,
} from '@ant-design/icons'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import api from '../api/axiosInstance'
import { useAuthStore }     from '../store/authStore'
import { useBrandingStore } from '../store/brandingStore'
import { PATH_PERM }        from '../config/permissions'

const { Header, Sider, Content, Footer } = Layout

const NAV_ITEMS = [
  {
    key:   '/',
    icon:  <HomeOutlined />,
    label: 'Dashboard',
  },
  {
    key:      'students',
    icon:     <SolutionOutlined />,
    label:    'Students',
    children: [
      { key: '/students',         label: 'Student List' },
      { key: '/students/import',       label: 'Bulk Import' },
      { key: '/students/promote',      label: 'Promote Students' },
      { key: '/students/blood-group',  label: 'Blood Group Search' },
    ],
  },
  {
    key:   'fees',
    icon:  <DollarOutlined />,
    label: 'Fees',
    children: [
      { key: '/fees/structure',           label: 'Fee Structure' },
      { key: '/fees/structure/import',    label: 'Bulk Import Structure' },
      { key: '/fees/collect/admission',   label: 'Admission Fee' },
      { key: '/fees/collect/school',      label: 'School Fee' },
      { key: '/fees/collect/transport',   label: 'Transport Fee' },
      { key: '/fees/collect/hostel',      label: 'Hostel Fee' },
      { key: '/fees/collect/other',       label: 'Other Fee' },
      { key: '/fees/receipts',            label: 'Receipts' },
      { key: '/fees/receipts/import',     label: 'Legacy Receipt Import' },
      { key: '/fees/pending-payments',    label: 'Pending Online Payments' },
      { key: '/fees/concessions',         label: 'Fee Concession' },
    ],
  },
  {
    key:   '/attendance',
    icon:  <CalendarOutlined />,
    label: 'Attendance',
  },
  {
    key:   'staff',
    icon:  <IdcardOutlined />,
    label: 'Staff',
    children: [
      { key: '/staff',                    label: 'Staff Directory'     },
      { key: '/staff/attendance',         label: 'Mark Attendance'     },
      { key: '/staff/attendance/summary', label: 'Attendance Summary'  },
      { key: '/staff/advances',           label: 'Staff Advances'      },
      { key: '/staff/salaries',           label: 'Salaries'            },
    ],
  },
  {
    key:   '/marks',
    icon:  <FormOutlined />,
    label: 'Marks Entry',
  },
  {
    key:   '/transport',
    icon:  <CarOutlined />,
    label: 'Transport',
  },
  {
    key:   '/hostel',
    icon:  <HomeOutlined />,
    label: 'Hostel',
  },
  {
    key:   'homework',
    icon:  <BookOutlined />,
    label: 'Homework',
    children: [
      { key: '/homework/daily', label: 'Daily Homework' },
      { key: '/homework',       label: 'Homework List'  },
    ],
  },
  {
    key:   '/announcements',
    icon:  <NotificationOutlined />,
    label: 'Announcements',
  },
  {
    key:   '/events',
    icon:  <VideoCameraOutlined />,
    label: 'Events Gallery',
  },
  {
    key:   '/reports',
    icon:  <BarChartOutlined />,
    label: 'Reports',
  },
  {
    key:   '/master',
    icon:  <DatabaseOutlined />,
    label: 'Master Data',
  },
  {
    key:   '/sms',
    icon:  <MessageOutlined />,
    label: 'SMS Center',
  },
  {
    key: 'settings',
    icon: <SettingOutlined />,
    label: 'Settings',
    children: [
      { key: '/settings/school',         icon: <SettingOutlined />, label: 'School Settings' },
      { key: '/settings/roles',          icon: <SafetyOutlined />,  label: 'Roles & Permissions' },
      { key: '/settings/users',          icon: <TeamOutlined />,    label: 'User Management' },
      { key: '/settings/payment-gateway',                            label: 'Payment Gateway' },
      { key: '/settings/sms-gateway',                                label: 'SMS Gateway' },
      { key: '/settings/message-reports',                            label: 'Reported Messages' },
      { key: '/settings/r2-storage',                                 label: 'R2 Storage' },
    ],
  },
]

/**
 * Filters nav items by the logged-in user's permissions (PATH_PERM map).
 * A leaf item is kept when it has no mapped permission or the user holds it.
 * A parent item is kept only when at least one child survives the filter.
 */
function filterNav(items, hasPermission) {
  return items
    .map((item) => {
      if (item.children) {
        const children = item.children.filter((c) => {
          const perm = PATH_PERM[c.key]
          return !perm || hasPermission(perm)
        })
        return children.length ? { ...item, children } : null
      }
      const perm = PATH_PERM[item.key]
      return !perm || hasPermission(perm) ? item : null
    })
    .filter(Boolean)
}

export default function AppLayout() {
  const navigate        = useNavigate()
  const location        = useLocation()
  const logout          = useAuthStore((s) => s.logout)
  const user            = useAuthStore((s) => s.user)
  const hasPermission   = useAuthStore((s) => s.hasPermission)
  const { branding }    = useBrandingStore()
  const navItems        = filterNav(NAV_ITEMS, hasPermission)
  const headerBg   = branding.headerBgColor  || '#001529'
  const headerText = branding.navTextColor   || '#ffffff'
  const [collapsed, setCollapsed] = useState(false)
  const [apiInfo,   setApiInfo]   = useState(null)   // { version, buildDate }

  useEffect(() => {
    api.get('/version')
      .then(r => setApiInfo(r.data?.data || null))
      .catch(() => {})
  }, [])

  const { message } = AntApp.useApp()
  const [pwdForm]   = Form.useForm()
  const [pwdOpen,   setPwdOpen]   = useState(false)
  const [pwdSaving, setPwdSaving] = useState(false)

  const handleLogout = async () => {
    try { await api.post('/school/auth/logout') } catch (_) {}
    localStorage.removeItem('schoolId')
    logout()
    navigate('/login', { replace: true })
  }

  const handleChangePassword = async () => {
    const v = await pwdForm.validateFields()
    setPwdSaving(true)
    try {
      await api.post('/school/auth/change-password', {
        currentPassword: v.currentPassword,
        newPassword:     v.newPassword,
      })
      message.success('Password changed. Please log in again.')
      setPwdOpen(false)
      pwdForm.resetFields()
      // Tokens were revoked server-side — force re-login.
      localStorage.removeItem('schoolId')
      logout()
      navigate('/login', { replace: true })
    } catch (e) {
      message.error(e?.response?.data?.message || 'Failed to change password.')
    } finally {
      setPwdSaving(false)
    }
  }

  const userMenuItems = [
    { key: 'change-password', icon: <KeyOutlined />, label: 'Change Password',
      onClick: () => { pwdForm.resetFields(); setPwdOpen(true) } },
    { key: 'logout', icon: <LogoutOutlined />, label: 'Logout', onClick: handleLogout },
  ]

  // Derive open keys from current path
  const openKeys = navItems
    .filter((item) => item.children?.some((c) => location.pathname.startsWith(c.key)))
    .map((item) => item.key)

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {/* ── Header ─────────────────────────────────────────────────── */}
      <Header
        style={{
          position:   'sticky',
          top:        0,
          zIndex:     100,
          background: headerBg,
          display:    'flex',
          alignItems: 'center',
          padding:    '0 24px',
          gap:        16,
        }}
      >
        {branding.logoPath && (
          <img src={branding.logoPath} alt="logo" style={{ height: 36, objectFit: 'contain' }} onError={(e) => { e.target.style.display = 'none' }} />
        )}
        <Typography.Text strong style={{ color: headerText, fontSize: 16, flex: 1 }}>
          {branding.displayName}
        </Typography.Text>

        <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
          <div style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 8 }}>
            <Avatar icon={<UserOutlined />} style={{ background: '#ffffff33' }} />
            <Typography.Text style={{ color: headerText }}>{user?.fullName}</Typography.Text>
          </div>
        </Dropdown>
      </Header>

      <Layout>
        {/* ── Sidebar ────────────────────────────────────────────────── */}
        <Sider
          collapsible
          collapsed={collapsed}
          onCollapse={setCollapsed}
          theme="light"
          style={{ borderRight: '1px solid #f0f0f0' }}
        >
          <Menu
            mode="inline"
            selectedKeys={[location.pathname]}
            defaultOpenKeys={openKeys}
            items={navItems}
            onClick={({ key }) => navigate(key)}
            style={{ borderRight: 0, paddingTop: 8 }}
          />
        </Sider>

        {/* ── Content + Footer ────────────────────────────────────────── */}
        <Layout style={{ flexDirection: 'column' }}>
          <Content style={{ padding: 24, background: '#f0f2f5', flex: 1 }}>
            <Outlet />
          </Content>
          <Footer style={{ textAlign: 'center', padding: '12px 24px', background: '#f0f2f5', color: '#8c8c8c', fontSize: 12 }}>
            Powered by Ascent Info Solutions
            {' · '}UI v{__APP_VERSION__} ({__BUILD_DATE__})
            {apiInfo && (
              <>{' · '}API v{apiInfo.version}{apiInfo.buildDate ? ` (${apiInfo.buildDate})` : ''}</>
            )}
          </Footer>
        </Layout>
      </Layout>

      <Modal
        title="Change Password"
        open={pwdOpen}
        onOk={handleChangePassword}
        onCancel={() => setPwdOpen(false)}
        confirmLoading={pwdSaving}
        okText="Change Password"
        destroyOnClose
      >
        <Form form={pwdForm} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="currentPassword" label="Current Password" rules={[{ required: true, message: 'Enter your current password.' }]}>
            <Input.Password autoComplete="current-password" />
          </Form.Item>
          <Form.Item name="newPassword" label="New Password"
            rules={[{ required: true }, { min: 6, message: 'At least 6 characters.' }]}>
            <Input.Password autoComplete="new-password" />
          </Form.Item>
          <Form.Item name="confirmPassword" label="Confirm New Password" dependencies={['newPassword']}
            rules={[
              { required: true, message: 'Re-enter the new password.' },
              ({ getFieldValue }) => ({
                validator: (_, value) =>
                  !value || getFieldValue('newPassword') === value
                    ? Promise.resolve()
                    : Promise.reject(new Error('Passwords do not match.')),
              }),
            ]}>
            <Input.Password autoComplete="new-password" />
          </Form.Item>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            You'll be logged out and need to sign in again with the new password.
          </Typography.Text>
        </Form>
      </Modal>
    </Layout>
  )
}
