import { useState } from 'react'
import { Form, Input, Button, Typography, Alert } from 'antd'
import { useNavigate } from 'react-router-dom'
import api from '../../api/axiosInstance'
import { useAuthStore }    from '../../store/authStore'
import { useBrandingStore } from '../../store/brandingStore'

const { Title, Text } = Typography

export default function LoginPage() {
  const navigate  = useNavigate()
  const login     = useAuthStore((s) => s.login)
  const { branding } = useBrandingStore()

  const [loading, setLoading] = useState(false)
  const [error,   setError]   = useState(null)

  const handleSubmit = async (values) => {
    setLoading(true)
    setError(null)
    try {
      const res  = await api.post('/school/auth/login', values)
      const data = res.data.data

      // Persist schoolId so refresh works across page reloads
      localStorage.setItem('schoolId', String(data.schoolId))

      login(data.accessToken, {
        userId:      data.userId,
        fullName:    data.fullName,
        groupId:     data.groupId,
        schoolId:    data.schoolId,
        permissions: data.permissions || [],
      })

      navigate('/', { replace: true })
    } catch (err) {
      setError(err.response?.data?.message || 'Login failed. Please check your credentials.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: branding.loginBgPath
          ? `url(${branding.loginBgPath}) center/cover no-repeat`
          : '#f0f2f5',
      }}
    >
      <div
        style={{
          width: 380,
          background: '#fff',
          borderRadius: 8,
          padding: '40px 40px 32px',
          boxShadow: '0 4px 24px rgba(0,0,0,0.10)',
        }}
      >
        {/* School logo */}
        {branding.logoPath && (
          <div style={{ textAlign: 'center', marginBottom: 16 }}>
            <img src={branding.logoPath} alt="logo" style={{ maxHeight: 64, maxWidth: 220, objectFit: 'contain' }} />
          </div>
        )}

        <Title level={4} style={{ textAlign: 'center', marginBottom: 4 }}>
          {branding.displayName}
        </Title>

        {branding.tagline && (
          <Text type="secondary" style={{ display: 'block', textAlign: 'center', marginBottom: 24 }}>
            {branding.tagline}
          </Text>
        )}

        {!branding.tagline && <div style={{ marginBottom: 24 }} />}

        {error && (
          <Alert type="error" message={error} showIcon style={{ marginBottom: 16 }} />
        )}

        <Form layout="vertical" onFinish={handleSubmit} autoComplete="off">
          <Form.Item
            name="username"
            label="Username"
            rules={[{ required: true, message: 'Enter your username.' }]}
          >
            <Input size="large" autoFocus />
          </Form.Item>

          <Form.Item
            name="password"
            label="Password"
            rules={[{ required: true, message: 'Enter your password.' }]}
          >
            <Input.Password size="large" />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 8 }}>
            <Button type="primary" htmlType="submit" size="large" block loading={loading}>
              Sign In
            </Button>
          </Form.Item>
        </Form>
      </div>
    </div>
  )
}
