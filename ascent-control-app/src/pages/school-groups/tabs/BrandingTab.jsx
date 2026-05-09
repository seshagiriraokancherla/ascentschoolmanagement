import { Form, Input, Button, Spin, Alert, Row, Col, Divider, Upload, Image, Typography } from 'antd'
import { UploadOutlined } from '@ant-design/icons'
import { useState, useEffect } from 'react'
import api from '../../../api/axiosInstance'

const { Text } = Typography
const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:62845'

const ColorInput = ({ value, onChange }) => (
  <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
    <input type="color" value={value || '#ffffff'} onChange={e => onChange(e.target.value)}
      style={{ width: 40, height: 32, border: '1px solid #d9d9d9', borderRadius: 4, cursor: 'pointer' }} />
    <Input value={value} onChange={e => onChange(e.target.value)} placeholder="#1677ff" style={{ flex: 1 }} />
  </div>
)

/** Image field: shows preview + Upload button. Calls onPathChange(path) after upload. */
function ImageUploadField({ label, value, onPathChange, accept = 'image/*' }) {
  const [uploading, setUploading] = useState(false)
  const [error, setError]         = useState(null)

  const handleUpload = async ({ file }) => {
    setUploading(true)
    setError(null)
    const formData = new FormData()
    formData.append('file', file)

    try {
      const res = await fetch(`${API_BASE}/control/uploads/image`, {
        method:      'POST',
        credentials: 'include',
        headers: {
          Authorization: `Bearer ${sessionStorage.getItem('controlAccessToken')}`,
        },
        body: formData,
      })
      const body = await res.json()
      if (!res.ok) throw new Error(body.message || 'Upload failed.')
      onPathChange(body.data.path)
    } catch (err) {
      setError(err.message)
    } finally {
      setUploading(false)
    }
  }

  const previewUrl = value ? (value.startsWith('http') ? value : `${API_BASE}${value}`) : null

  return (
    <div style={{ marginBottom: 4 }}>
      <Text type="secondary" style={{ fontSize: 12 }}>{label}</Text>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginTop: 4 }}>
        {previewUrl && (
          <Image
            src={previewUrl}
            width={56}
            height={56}
            style={{ objectFit: 'contain', border: '1px solid #f0f0f0', borderRadius: 4 }}
            preview={{ mask: 'Preview' }}
          />
        )}
        <div style={{ flex: 1 }}>
          <Upload
            accept={accept}
            showUploadList={false}
            customRequest={handleUpload}
            beforeUpload={(file) => {
              if (file.size > 2 * 1024 * 1024) { setError('File must be under 2 MB.'); return Upload.LIST_IGNORE }
              return true
            }}
          >
            <Button icon={<UploadOutlined />} loading={uploading} size="small">
              {value ? 'Replace' : 'Upload'}
            </Button>
          </Upload>
          {value && (
            <Text type="secondary" style={{ fontSize: 11, display: 'block', marginTop: 4 }}>
              {value}
            </Text>
          )}
          {error && <Text type="danger" style={{ fontSize: 11, display: 'block', marginTop: 4 }}>{error}</Text>}
        </div>
      </div>
    </div>
  )
}

export default function BrandingTab({ groupId }) {
  const [loading, setLoading] = useState(true)
  const [saving,  setSaving]  = useState(false)
  const [success, setSuccess] = useState(false)
  const [form]                = Form.useForm()

  // Track image paths separately so upload updates the form
  const [logoPath,    setLogoPath]    = useState(null)
  const [faviconPath, setFaviconPath] = useState(null)
  const [loginBgPath, setLoginBgPath] = useState(null)

  useEffect(() => {
    api.get(`/control/school-groups/${groupId}/branding`)
      .then(res => {
        const data = res.data.data
        if (data) {
          form.setFieldsValue(data)
          setLogoPath(data.logoPath || null)
          setFaviconPath(data.faviconPath || null)
          setLoginBgPath(data.loginBgPath || null)
        }
      })
      .finally(() => setLoading(false))
  }, [groupId])

  const handleSave = async (values) => {
    setSaving(true)
    setSuccess(false)
    try {
      await api.put(`/control/school-groups/${groupId}/branding`, {
        ...values,
        logoPath,
        faviconPath,
        loginBgPath,
      })
      setSuccess(true)
    } catch (err) {
      alert(err.response?.data?.message || 'Save failed.')
    } finally { setSaving(false) }
  }

  if (loading) return <div style={{ padding: 40, textAlign: 'center' }}><Spin /></div>

  return (
    <div style={{ maxWidth: 700, padding: '24px 0' }}>
      {success && <Alert type="success" message="Branding saved." showIcon style={{ marginBottom: 16 }} />}

      <Form form={form} layout="vertical" onFinish={handleSave}>
        <Divider orientation="left">Identity</Divider>
        <Row gutter={16}>
          <Col span={12}><Form.Item name="displayName" label="Display Name"><Input /></Form.Item></Col>
          <Col span={12}><Form.Item name="tagline" label="Tagline"><Input /></Form.Item></Col>
        </Row>

        <Divider orientation="left">Logo & Images</Divider>
        <Row gutter={16}>
          <Col span={12}>
            <ImageUploadField
              label="Logo"
              value={logoPath}
              onPathChange={setLogoPath}
            />
          </Col>
          <Col span={12}>
            <ImageUploadField
              label="Favicon"
              value={faviconPath}
              onPathChange={setFaviconPath}
              accept=".ico,.png,.gif"
            />
          </Col>
        </Row>
        <div style={{ marginTop: 16 }}>
          <ImageUploadField
            label="Login Background Image"
            value={loginBgPath}
            onPathChange={setLoginBgPath}
          />
        </div>

        <Divider orientation="left" style={{ marginTop: 24 }}>Colors</Divider>
        <Row gutter={16}>
          <Col span={12}><Form.Item name="primaryColor" label="Primary Color"><ColorInput /></Form.Item></Col>
          <Col span={12}><Form.Item name="secondaryColor" label="Secondary Color"><ColorInput /></Form.Item></Col>
        </Row>
        <Row gutter={16}>
          <Col span={12}><Form.Item name="headerBgColor" label="Header Background"><ColorInput /></Form.Item></Col>
          <Col span={12}><Form.Item name="navTextColor" label="Nav Text Color"><ColorInput /></Form.Item></Col>
        </Row>

        <Divider orientation="left">Receipts & Reports</Divider>
        <Form.Item name="receiptFooterText" label="Receipt / Report Footer">
          <Input.TextArea rows={2} placeholder="School address, contact details..." />
        </Form.Item>

        <Button type="primary" htmlType="submit" loading={saving}>Save Branding</Button>
      </Form>
    </div>
  )
}
