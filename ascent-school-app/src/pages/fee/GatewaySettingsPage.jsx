import { useEffect, useState } from 'react'
import {
  Card, Form, Select, Input, Button, Alert, Space, Typography, Divider, App as AntApp,
} from 'antd'
import { SaveOutlined, EyeOutlined, EyeInvisibleOutlined, InfoCircleOutlined } from '@ant-design/icons'
import api, { apiError } from '../../api/axiosInstance'

const { Text, Paragraph } = Typography

const SUPPORTED_GATEWAYS = [
  { value: 'Razorpay', label: 'Razorpay' },
  // Future: { value: 'Cashfree', label: 'Cashfree' },
  // Future: { value: 'PayU',    label: 'PayU' },
]

export default function GatewaySettingsPage() {
  const { message } = AntApp.useApp()
  const [form]      = Form.useForm()

  const [loading,  setLoading]  = useState(true)
  const [saving,   setSaving]   = useState(false)
  const [existing, setExisting] = useState(null)   // current saved config
  const [showSecret, setShowSecret] = useState(false)

  useEffect(() => {
    api.get('/school/fees/gateway-settings')
      .then((res) => {
        const cfg = res.data?.data
        setExisting(cfg)
        if (cfg) {
          form.setFieldsValue({
            gatewayName:   cfg.gatewayName,
            displayName:   cfg.displayName,
            keyId:         cfg.keyId,
            keySecret:     '',          // never pre-filled — user must re-enter to change
            webhookSecret: '',
          })
        }
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [])

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      await api.put('/school/fees/gateway-settings', {
        GatewayName:   values.gatewayName,
        DisplayName:   values.displayName,
        KeyId:         values.keyId,
        KeySecret:     values.keySecret     || null,
        WebhookSecret: values.webhookSecret || null,
        IsActive:      true,
      })
      message.success('Gateway settings saved.')
      // Refresh to show updated state
      const res = await api.get('/school/fees/gateway-settings')
      setExisting(res.data?.data)
      form.setFieldsValue({ keySecret: '', webhookSecret: '' })
    } catch (e) {
      message.error(apiError(e, 'Failed to save gateway settings.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card title="Payment Gateway Settings" loading={loading}>
      <Alert
        type="info"
        showIcon
        icon={<InfoCircleOutlined />}
        style={{ marginBottom: 24 }}
        message="Setup instructions"
        description={
          <ul style={{ margin: '4px 0 0', paddingLeft: 20 }}>
            <li>Create a Razorpay account at <Text strong>razorpay.com</Text> and generate API keys.</li>
            <li>Use <Text strong>Test Mode</Text> keys while developing; switch to Live keys before going live.</li>
            <li>
              After hosting, configure the webhook URL in Razorpay Dashboard → Settings → Webhooks:
              <br />
              <Text code>https://&#123;your-domain&#125;/school/fees/payment-webhook</Text>
              <br />
              Subscribe to event: <Text code>payment.captured</Text>
            </li>
            <li>Copy the Webhook Secret from Razorpay and paste it below — this enables webhook signature verification.</li>
          </ul>
        }
      />

      {existing && (
        <Alert
          type="success"
          showIcon
          style={{ marginBottom: 24 }}
          message={
            <span>
              Active gateway: <strong>{existing.displayName || existing.gatewayName}</strong>
              {' · '}Key ID: <Text code>{existing.keyId}</Text>
              {' · '}Webhook secret: {existing.hasWebhookSecret ? <Text type="success">configured</Text> : <Text type="warning">not set</Text>}
            </span>
          }
        />
      )}

      <Form form={form} layout="vertical" style={{ maxWidth: 560 }}>
        <Form.Item
          name="gatewayName"
          label="Payment Gateway"
          initialValue="Razorpay"
          rules={[{ required: true }]}
        >
          <Select options={SUPPORTED_GATEWAYS} />
        </Form.Item>

        <Form.Item
          name="displayName"
          label="Display Name (shown to staff during collection)"
          initialValue="Online Payment"
          rules={[{ required: true, message: 'Enter a display name.' }]}
        >
          <Input placeholder="e.g. Online Payment (Razorpay)" />
        </Form.Item>

        <Form.Item
          name="keyId"
          label="Key ID"
          rules={[{ required: true, message: 'Key ID is required.' }]}
        >
          <Input placeholder="rzp_test_... or rzp_live_..." />
        </Form.Item>

        <Form.Item
          name="keySecret"
          label={
            existing
              ? 'Key Secret (leave blank to keep existing)'
              : 'Key Secret'
          }
          rules={existing ? [] : [{ required: true, message: 'Key Secret is required on first save.' }]}
        >
          <Input.Password
            visibilityToggle={{
              visible:  showSecret,
              onVisibleChange: setShowSecret,
            }}
            placeholder={existing ? '•••••••••••• (leave blank to keep)' : 'Enter key secret'}
            iconRender={(v) => v ? <EyeOutlined /> : <EyeInvisibleOutlined />}
          />
        </Form.Item>

        <Divider plain>Webhook (optional but recommended)</Divider>

        <Form.Item
          name="webhookSecret"
          label={
            existing?.hasWebhookSecret
              ? 'Webhook Secret (leave blank to keep existing)'
              : 'Webhook Secret'
          }
          extra="Found in Razorpay Dashboard under the webhook you create."
        >
          <Input.Password
            placeholder={existing?.hasWebhookSecret ? '•••••••••••• (leave blank to keep)' : 'Paste webhook secret'}
          />
        </Form.Item>

        <Form.Item style={{ marginTop: 8 }}>
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={saving}
            onClick={handleSave}
          >
            Save Gateway Settings
          </Button>
        </Form.Item>
      </Form>

      <Divider />
      <Paragraph type="secondary" style={{ fontSize: 12 }}>
        <strong>Payment modes:</strong> Go to Master Data → Payment Modes to add an "Online" mode
        and enable the <Text code>Is Online</Text> toggle. When staff selects that mode during fee
        collection, the Razorpay checkout will open automatically.
      </Paragraph>
    </Card>
  )
}
