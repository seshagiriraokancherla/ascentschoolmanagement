import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, List, Avatar, Typography, Spin, Tag, Button, App as AntApp } from 'antd'
import { UserOutlined, RightOutlined, LogoutOutlined } from '@ant-design/icons'
import { useAuthStore }     from '../../store/authStore'
import { useBrandingStore } from '../../store/brandingStore'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography

export default function ChildSelectorPage() {
  const navigate            = useNavigate()
  const { message }         = AntApp.useApp()
  const { setChild, logout } = useAuthStore()
  const { branding }        = useBrandingStore()
  const [children, setChildren]   = useState([])
  const [loading,  setLoading]    = useState(true)
  const [selecting, setSelecting] = useState(null)

  useEffect(() => {
    api.get('/mobile/auth/parent/children')
      .then((r) => setChildren(r.data?.data || []))
      .catch((e) => message.error(e.message || 'Failed to load children'))
      .finally(() => setLoading(false))
  }, [])

  const handleSelect = async (child) => {
    setSelecting(child.linkId)
    try {
      const r    = await api.post('/mobile/auth/parent/select-child', { LinkId: child.linkId })
      const data = r.data?.data
      setChild(data.accessToken, {
        studentId:   data.studentId,
        studentName: data.fullName,   // response field is fullName (camelCase serialization)
        className:   data.className,
        sectionName: data.sectionName,
        admissionNo: data.admissionNo,
      })
      navigate('/fees', { replace: true })
    } catch (e) {
      message.error(e.message || 'Failed to select child')
    } finally {
      setSelecting(null)
    }
  }

  const handleLogout = async () => {
    try { await api.post('/mobile/auth/parent/logout', null) } catch (_) {}
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: '#f0f2f5',
    }}>
      <Card
        style={{ width: 440, boxShadow: '0 4px 24px rgba(0,0,0,0.12)' }}
        title={
          <div style={{ textAlign: 'center', padding: '8px 0' }}>
            {branding.logoPath && (
              <img src={branding.logoPath} alt="logo" style={{ height: 40, marginBottom: 8 }} />
            )}
            <Title level={4} style={{ margin: 0 }}>Select Child</Title>
            <Text type="secondary">Choose whose fees to view</Text>
          </div>
        }
        extra={
          <Button type="text" icon={<LogoutOutlined />} onClick={handleLogout} danger>
            Logout
          </Button>
        }
      >
        {loading ? (
          <div style={{ textAlign: 'center', padding: 32 }}><Spin size="large" /></div>
        ) : children.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 32 }}>
            <Text type="secondary">No children linked to this mobile number.</Text>
            <br />
            <Text type="secondary">Please contact the school office.</Text>
          </div>
        ) : (
          <List
            dataSource={children}
            renderItem={(child) => (
              <List.Item
                style={{
                  cursor: 'pointer', borderRadius: 8, padding: '12px 16px',
                  transition: 'background 0.15s',
                }}
                onClick={() => handleSelect(child)}
                className="child-item"
              >
                <List.Item.Meta
                  avatar={
                    <Avatar
                      size={48}
                      icon={<UserOutlined />}
                      style={{ background: '#1677ff' }}
                    />
                  }
                  title={<Text strong>{child.studentName}</Text>}
                  description={
                    <Tag color="blue">{child.className || 'Class not set'}</Tag>
                  }
                />
                {selecting === child.linkId
                  ? <Spin size="small" />
                  : <RightOutlined style={{ color: '#8c8c8c' }} />
                }
              </List.Item>
            )}
          />
        )}
      </Card>

      <style>{`
        .child-item:hover { background: #f5f5f5 !important; }
      `}</style>
    </div>
  )
}
