import { Form, Input, Button, Card, Typography, Alert } from 'antd'
import { UserOutlined, LockOutlined } from '@ant-design/icons'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import useAuthStore from '../../store/authStore'

const { Title } = Typography
const API_BASE   = import.meta.env.VITE_API_URL || 'http://localhost:62845'

export default function LoginPage() {
  const [loading, setLoading] = useState(false)
  const [error,   setError]   = useState(null)
  const navigate              = useNavigate()
  const login                 = useAuthStore(s => s.login)

  const onFinish = async (values) => {
    setLoading(true)
    setError(null)
    try {
      const res = await axios.post(`${API_BASE}/control/auth/login`, values, { withCredentials: true })
      const { accessToken, userId, fullName, role } = res.data.data
      login({ userId, fullName, role }, accessToken)
      navigate('/school-groups')
    } catch (err) {
      setError(err.response?.data?.message || 'Login failed. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#f0f2f5' }}>
      <Card style={{ width: 380, boxShadow: '0 4px 24px rgba(0,0,0,0.1)' }}>
        <div style={{ textAlign: 'center', marginBottom: 32 }}>
          <Title level={3} style={{ margin: 0 }}>Ascent Schools</Title>
          <Typography.Text type="secondary">Control Panel</Typography.Text>
        </div>

        {error && <Alert message={error} type="error" showIcon style={{ marginBottom: 16 }} />}

        <Form layout="vertical" onFinish={onFinish}>
          <Form.Item name="username" rules={[{ required: true, message: 'Enter username' }]}>
            <Input prefix={<UserOutlined />} placeholder="Username" size="large" />
          </Form.Item>
          <Form.Item name="password" rules={[{ required: true, message: 'Enter password' }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="Password" size="large" />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0 }}>
            <Button type="primary" htmlType="submit" block size="large" loading={loading}>
              Sign In
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  )
}
