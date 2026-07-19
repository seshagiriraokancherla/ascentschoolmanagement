import { useEffect, useState } from 'react'
import {
  Card, Form, Input, Button, Switch, Alert, Typography, App as AntApp,
} from 'antd'
import { SaveOutlined, CloudOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Text, Paragraph } = Typography

export default function R2StoragePage() {
  const { message } = AntApp.useApp()
  const [form]      = Form.useForm()

  const [loading, setLoading] = useState(true)
  const [saving,  setSaving]  = useState(false)
  const [hasSecret, setHasSecret] = useState(false)

  useEffect(() => {
    api.get('/school/settings/r2')
      .then(r => {
        const c = r.data?.data
        if (c) {
          setHasSecret(!!c.hasSecretKey)
          form.setFieldsValue({
            accountId:     c.accountId,
            accessKeyId:   c.accessKeyId,
            bucketName:    c.bucketName,
            publicBaseUrl: c.publicBaseUrl,
            isEnabled:     c.isEnabled,
          })
        } else {
          form.setFieldsValue({ isEnabled: true })
        }
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [])

  const handleSave = async () => {
    const v = await form.validateFields()
    setSaving(true)
    try {
      const res = await api.put('/school/settings/r2', {
        accountId:       v.accountId?.trim(),
        accessKeyId:     v.accessKeyId?.trim(),
        secretAccessKey: v.secretAccessKey || '',   // blank keeps existing
        bucketName:      v.bucketName?.trim(),
        publicBaseUrl:   v.publicBaseUrl?.trim(),
        isEnabled:       !!v.isEnabled,
      })
      const c = res.data?.data
      setHasSecret(!!c?.hasSecretKey)
      form.setFieldsValue({ secretAccessKey: '' })
      message.success('R2 storage settings saved.')
    } catch (e) {
      message.error(e?.response?.data?.message || 'Failed to save settings.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card
      title={<><CloudOutlined /> Cloudflare R2 Storage</>}
      loading={loading}
      extra={<Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={handleSave}>Save</Button>}
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Per-school file storage (student photos, homework, announcements, events)."
        description={
          <Paragraph style={{ margin: 0, fontSize: 13 }}>
            Enter your Cloudflare R2 credentials. Create an <b>Object Read &amp; Write</b> API token in
            R2 → "Manage R2 API Tokens" (gives the Access Key ID + Secret). The <b>Account ID</b> is in the
            S3 endpoint <Text code>https://&lt;ACCOUNT_ID&gt;.r2.cloudflarestorage.com</Text>. Enable public
            access on the bucket (R2.dev subdomain or a custom domain) and paste that as the
            <b> Public Base URL</b>. Also add a CORS rule allowing <b>PUT</b> from this school's site.
          </Paragraph>
        }
      />

      <Form form={form} layout="vertical" style={{ maxWidth: 640 }}>
        <Form.Item name="accountId" label="Account ID" rules={[{ required: true }]}>
          <Input placeholder="e.g. a1b2c3d4e5f6..." />
        </Form.Item>
        <Form.Item name="accessKeyId" label="Access Key ID" rules={[{ required: true }]}>
          <Input placeholder="R2 API token Access Key ID" />
        </Form.Item>
        <Form.Item
          name="secretAccessKey"
          label="Secret Access Key"
          rules={hasSecret ? [] : [{ required: true, message: 'Secret is required.' }]}
          extra={hasSecret ? 'A secret is stored. Leave blank to keep it, or type a new one to replace.' : null}
        >
          <Input.Password placeholder={hasSecret ? '•••••••• (unchanged)' : 'R2 API token Secret'} autoComplete="new-password" />
        </Form.Item>
        <Form.Item name="bucketName" label="Bucket Name" rules={[{ required: true }]}>
          <Input placeholder="e.g. myschool-media" />
        </Form.Item>
        <Form.Item
          name="publicBaseUrl"
          label="Public Base URL"
          rules={[{ required: true }, { type: 'url', message: 'Enter a valid URL.' }]}
          extra="Public bucket URL, no trailing slash. e.g. https://pub-xxxx.r2.dev or https://files.yourschool.edu-care.in"
        >
          <Input placeholder="https://pub-xxxx.r2.dev" />
        </Form.Item>
        <Form.Item name="isEnabled" label="Enabled" valuePropName="checked">
          <Switch />
        </Form.Item>
      </Form>
    </Card>
  )
}
