import { useEffect, useState } from 'react'
import {
  Card, Form, Select, Input, Button, Alert, Typography, Divider, Radio, App as AntApp,
} from 'antd'
import { SaveOutlined, EyeOutlined, EyeInvisibleOutlined, InfoCircleOutlined } from '@ant-design/icons'
import api, { apiError } from '../../api/axiosInstance'
import { API_BASE } from '../../store/brandingStore'

const { Text, Paragraph } = Typography

// Per-gateway wording. Both gateways store their two credentials in the same fields:
// keyId (Razorpay Key ID / Easebuzz merchant Key) and keySecret (Razorpay Key Secret / Easebuzz Salt).
const GATEWAYS = {
  Razorpay: {
    label:             'Razorpay',
    keyLabel:          'Key ID',
    keyPlaceholder:    'rzp_test_... or rzp_live_...',
    secretLabel:       'Key Secret',
    usesEnvironment:   false,   // test vs live is decided by the key itself
    usesWebhookSecret: true,
    webhookPath:       '/school/fees/payment-webhook',
  },
  Easebuzz: {
    label:             'Easebuzz',
    keyLabel:          'Merchant Key',
    keyPlaceholder:    'Merchant Key from the Easebuzz dashboard',
    secretLabel:       'Salt',
    usesEnvironment:   true,    // separate sandbox / live servers
    usesWebhookSecret: false,   // webhooks are confirmed with the Salt
    webhookPath:       '/school/fees/payment-webhook/easebuzz',
  },
}

const GATEWAY_OPTIONS = Object.entries(GATEWAYS).map(([value, g]) => ({ value, label: g.label }))

export default function GatewaySettingsPage() {
  const { message } = AntApp.useApp()
  const [form]      = Form.useForm()

  const [loading,  setLoading]  = useState(true)
  const [saving,   setSaving]   = useState(false)
  const [existing, setExisting] = useState(null)   // current saved config
  const [showSecret, setShowSecret] = useState(false)

  const gatewayName = Form.useWatch('gatewayName', form) || existing?.gatewayName || 'Razorpay'
  const gw          = GATEWAYS[gatewayName] || GATEWAYS.Razorpay
  // Switching gateway: the stored secret belongs to the OLD gateway, so a new one is required.
  const switching   = !!existing && existing.gatewayName !== gatewayName
  const secretKept  = !!existing && !switching
  const webhookUrl  = `${API_BASE}${gw.webhookPath}`

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
            environment:   cfg.environment === 'test' ? 'test' : 'prod',
            keySecret:     '',          // never pre-filled — user must re-enter to change
            webhookSecret: '',
          })
        }
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [form])

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      await api.put('/school/fees/gateway-settings', {
        GatewayName:   values.gatewayName,
        DisplayName:   values.displayName,
        KeyId:         values.keyId,
        KeySecret:     values.keySecret || null,
        WebhookSecret: gw.usesWebhookSecret ? (values.webhookSecret || null) : null,
        Environment:   gw.usesEnvironment ? values.environment : null,
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

  const instructions = gatewayName === 'Easebuzz' ? (
    <ul style={{ margin: '4px 0 0', paddingLeft: 20 }}>
      <li>Get the <Text strong>Merchant Key</Text> and <Text strong>Salt</Text> from the Easebuzz dashboard.</li>
      <li>
        Easebuzz has separate <Text strong>Test</Text> and <Text strong>Live</Text> accounts — choose the
        Environment that matches the keys. Test keys only work in Test, live keys only in Live.
      </li>
      <li>
        In the Easebuzz dashboard, set the <Text strong>Transaction Webhook</Text> URL to:
        <br /><Text code copyable>{webhookUrl}</Text>
      </li>
      <li>The Salt is a password — never share it. It is stored securely and never shown again.</li>
    </ul>
  ) : (
    <ul style={{ margin: '4px 0 0', paddingLeft: 20 }}>
      <li>Create a Razorpay account at <Text strong>razorpay.com</Text> and generate API keys.</li>
      <li>Use <Text strong>Test Mode</Text> keys while developing; switch to Live keys before going live.</li>
      <li>
        Configure the webhook URL in Razorpay Dashboard → Settings → Webhooks:
        <br /><Text code copyable>{webhookUrl}</Text>
        <br />Subscribe to event: <Text code>payment.captured</Text>
      </li>
      <li>Copy the Webhook Secret from Razorpay and paste it below — this enables webhook signature verification.</li>
    </ul>
  )

  return (
    <Card title="Payment Gateway Settings" loading={loading}>
      <Alert
        type="info"
        showIcon
        icon={<InfoCircleOutlined />}
        style={{ marginBottom: 24 }}
        message={`Setup instructions — ${gw.label}`}
        description={instructions}
      />

      {existing && (
        <Alert
          type="success"
          showIcon
          style={{ marginBottom: 24 }}
          message={
            <span>
              Active gateway: <strong>{existing.displayName || existing.gatewayName}</strong>
              {' '}({existing.gatewayName}
              {existing.gatewayName === 'Easebuzz' && (existing.environment === 'test' ? ', Test' : ', Live')})
              {' · '}{(GATEWAYS[existing.gatewayName] || GATEWAYS.Razorpay).keyLabel}: <Text code>{existing.keyId}</Text>
              {(GATEWAYS[existing.gatewayName] || GATEWAYS.Razorpay).usesWebhookSecret && (
                <>
                  {' · '}Webhook secret: {existing.hasWebhookSecret ? <Text type="success">configured</Text> : <Text type="warning">not set</Text>}
                </>
              )}
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
          <Select options={GATEWAY_OPTIONS} />
        </Form.Item>

        {switching && (
          <Alert
            type="warning" showIcon style={{ marginBottom: 16 }}
            message={`Switching from ${existing.gatewayName} to ${gatewayName}`}
            description={`Enter the ${gw.label} ${gw.keyLabel} and ${gw.secretLabel}. After saving, all new online payments use ${gw.label}. Payments already started on ${existing.gatewayName} must be checked in the ${existing.gatewayName} dashboard.`}
          />
        )}

        <Form.Item
          name="displayName"
          label="Display Name (shown to staff during collection)"
          initialValue="Online Payment"
          rules={[{ required: true, message: 'Enter a display name.' }]}
        >
          <Input placeholder={`e.g. Online Payment (${gw.label})`} />
        </Form.Item>

        {gw.usesEnvironment && (
          <Form.Item
            name="environment"
            label="Environment"
            initialValue="prod"
            rules={[{ required: true }]}
            extra="Use Test with sandbox keys while trying it out; switch to Live with the live keys before parents pay."
          >
            <Radio.Group optionType="button" buttonStyle="solid"
              options={[{ value: 'test', label: 'Test (sandbox)' }, { value: 'prod', label: 'Live' }]} />
          </Form.Item>
        )}

        <Form.Item
          name="keyId"
          label={gw.keyLabel}
          rules={[{ required: true, message: `${gw.keyLabel} is required.` }]}
        >
          <Input placeholder={gw.keyPlaceholder} />
        </Form.Item>

        <Form.Item
          name="keySecret"
          label={secretKept ? `${gw.secretLabel} (leave blank to keep existing)` : gw.secretLabel}
          rules={secretKept ? [] : [{ required: true, message: `${gw.secretLabel} is required.` }]}
        >
          <Input.Password
            visibilityToggle={{
              visible:  showSecret,
              onVisibleChange: setShowSecret,
            }}
            placeholder={secretKept ? '•••••••••••• (leave blank to keep)' : `Enter ${gw.secretLabel.toLowerCase()}`}
            iconRender={(v) => v ? <EyeOutlined /> : <EyeInvisibleOutlined />}
          />
        </Form.Item>

        {gw.usesWebhookSecret && (
          <>
            <Divider plain>Webhook (optional but recommended)</Divider>

            <Form.Item
              name="webhookSecret"
              label={
                existing?.hasWebhookSecret && !switching
                  ? 'Webhook Secret (leave blank to keep existing)'
                  : 'Webhook Secret'
              }
              extra="Found in Razorpay Dashboard under the webhook you create."
            >
              <Input.Password
                placeholder={existing?.hasWebhookSecret && !switching ? '•••••••••••• (leave blank to keep)' : 'Paste webhook secret'}
              />
            </Form.Item>
          </>
        )}

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
        collection, the {gw.label} checkout will open automatically.
      </Paragraph>
    </Card>
  )
}
