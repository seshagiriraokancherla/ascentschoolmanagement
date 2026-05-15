import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Form, Input, Button, Card, Typography, Steps, App as AntApp } from 'antd'
import { MobileOutlined, SafetyOutlined } from '@ant-design/icons'
import { useAuthStore }     from '../../store/authStore'
import { useBrandingStore } from '../../store/brandingStore'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography

export default function LoginPage() {
  const navigate           = useNavigate()
  const { message }        = AntApp.useApp()
  const { login }          = useAuthStore()
  const { branding }       = useBrandingStore()
  const [step, setStep]    = useState(0)   // 0 = mobile, 1 = OTP
  const [mobile, setMobile] = useState('')
  const [sending, setSending] = useState(false)
  const [verifying, setVerifying] = useState(false)
  const [countdown, setCountdown] = useState(0)
  const [form] = Form.useForm()

  // Redirect if already authenticated with child
  const child = useAuthStore((s) => s.child)
  const token = useAuthStore((s) => s.accessToken)
  useEffect(() => {
    if (token && child) navigate('/fees', { replace: true })
    else if (token)     navigate('/select-child', { replace: true })
  }, [token, child])

  // Resend countdown
  useEffect(() => {
    if (countdown <= 0) return
    const t = setTimeout(() => setCountdown((c) => c - 1), 1000)
    return () => clearTimeout(t)
  }, [countdown])

  const handleSendOtp = async (values) => {
    setSending(true)
    try {
      await api.post('/mobile/auth/parent/request-otp', {
        Mobile:   values.mobile,
        DeviceId: getDeviceId(),
      })
      setMobile(values.mobile)
      setStep(1)
      setCountdown(30)
      message.success('OTP sent to your mobile number')
    } catch (e) {
      message.error(e.message || 'Failed to send OTP')
    } finally {
      setSending(false)
    }
  }

  const handleVerifyOtp = async (values) => {
    setVerifying(true)
    try {
      const r = await api.post('/mobile/auth/parent/verify-otp', {
        Mobile:   mobile,
        Otp:      values.otp,
        DeviceId: getDeviceId(),
      })
      const data = r.data?.data
      login(data.accessToken, { parentId: data.parentId, displayName: data.displayName })
      navigate('/select-child', { replace: true })
    } catch (e) {
      message.error(e.message || 'Invalid OTP. Please try again.')
    } finally {
      setVerifying(false)
    }
  }

  const handleResend = async () => {
    if (countdown > 0) return
    setSending(true)
    try {
      await api.post('/mobile/auth/parent/request-otp', {
        Mobile:   mobile,
        DeviceId: getDeviceId(),
      })
      setCountdown(30)
      message.success('OTP resent')
    } catch (e) {
      message.error(e.message || 'Failed to resend OTP')
    } finally {
      setSending(false)
    }
  }

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: branding.loginBgPath ? `url(${branding.loginBgPath}) center/cover` : '#f0f2f5',
    }}>
      <Card style={{ width: 400, boxShadow: '0 4px 24px rgba(0,0,0,0.12)' }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          {branding.logoPath && (
            <img src={branding.logoPath} alt="logo" style={{ height: 60, marginBottom: 12 }} />
          )}
          <Title level={4} style={{ margin: 0 }}>{branding.displayName}</Title>
          {branding.tagline && <Text type="secondary">{branding.tagline}</Text>}
          <div style={{ marginTop: 8 }}>
            <Text type="secondary">Parent Fee Portal</Text>
          </div>
        </div>

        <Steps
          current={step}
          size="small"
          style={{ marginBottom: 24 }}
          items={[
            { title: 'Mobile', icon: <MobileOutlined /> },
            { title: 'OTP',    icon: <SafetyOutlined /> },
          ]}
        />

        {step === 0 && (
          <Form form={form} layout="vertical" onFinish={handleSendOtp}>
            <Form.Item
              name="mobile"
              label="Registered Mobile Number"
              rules={[
                { required: true, message: 'Enter your mobile number' },
                { pattern: /^\d{10}$/, message: 'Enter a valid 10-digit mobile number' },
              ]}
            >
              <Input prefix={<MobileOutlined />} placeholder="10-digit mobile number" maxLength={10} size="large" />
            </Form.Item>
            <Button type="primary" htmlType="submit" block size="large" loading={sending}>
              Send OTP
            </Button>
          </Form>
        )}

        {step === 1 && (
          <Form layout="vertical" onFinish={handleVerifyOtp}>
            <Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
              OTP sent to <strong>{mobile}</strong>.{' '}
              <Button type="link" size="small" style={{ padding: 0 }} onClick={() => setStep(0)}>
                Change number
              </Button>
            </Text>
            <Form.Item
              name="otp"
              label="Enter OTP"
              rules={[
                { required: true, message: 'Enter the OTP' },
                { len: 6, message: 'OTP must be 6 digits' },
              ]}
            >
              <Input.OTP length={6} size="large" />
            </Form.Item>
            <Button type="primary" htmlType="submit" block size="large" loading={verifying}>
              Verify OTP
            </Button>
            <div style={{ textAlign: 'center', marginTop: 12 }}>
              {countdown > 0 ? (
                <Text type="secondary">Resend in {countdown}s</Text>
              ) : (
                <Button type="link" loading={sending} onClick={handleResend}>
                  Resend OTP
                </Button>
              )}
            </div>
          </Form>
        )}
      </Card>
    </div>
  )
}

function getDeviceId() {
  let id = localStorage.getItem('parentDeviceId')
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem('parentDeviceId', id)
  }
  return id
}
