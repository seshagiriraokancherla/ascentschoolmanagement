import { useEffect, useState } from 'react'
import {
  Card, Form, Input, Switch, Button, Table, Modal, Space, Tag, Typography,
  Alert, Divider, Popconfirm, App as AntApp,
} from 'antd'
import { SaveOutlined, PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Text, Paragraph } = Typography

// Single source of truth for the placeholders the server (SmsController.BuildMessage)
// actually substitutes. Keep in sync with the backend.
const PLACEHOLDERS = [
  { token: '{name}',        desc: 'Student name' },
  { token: '{class}',       desc: 'Class name' },
  { token: '{admissionNo}', desc: 'Admission number' },
  { token: '{date}',        desc: 'Date (Absent only)' },
  { token: '{amount}',      desc: 'Outstanding amount (Fee Due only)' },
]

export default function SmsGatewayPage() {
  const { message } = AntApp.useApp()
  const [accountForm] = Form.useForm()
  const [tplForm]     = Form.useForm()

  const [loading, setLoading]   = useState(true)
  const [saving, setSaving]     = useState(false)
  const [config, setConfig]     = useState(null)     // { ...account, hasApiKey, templates: [] }
  const [templates, setTemplates] = useState([])
  const [tplModal, setTplModal] = useState({ open: false, editing: null })

  const load = () => {
    setLoading(true)
    api.get('/school/sms/config')
      .then((res) => {
        const cfg = res.data?.data
        setConfig(cfg)
        setTemplates(cfg?.templates || [])
        accountForm.setFieldsValue({
          provider:  cfg?.provider  || 'smslogin',
          apiUrl:    cfg?.apiUrl    || 'https://smslogin.mobi/v3/api.php',
          username:  cfg?.username  || '',
          apiKey:    '',            // never pre-filled
          senderId:  cfg?.senderId  || '',
          isEnabled: cfg?.isEnabled ?? true,
        })
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const saveAccount = async () => {
    const v = await accountForm.validateFields()
    setSaving(true)
    try {
      await api.put('/school/sms/config', {
        Provider:  v.provider,
        ApiUrl:    v.apiUrl,
        Username:  v.username,
        ApiKey:    v.apiKey || null,   // blank = keep existing
        SenderId:  v.senderId,
        IsEnabled: v.isEnabled,
      })
      message.success('SMS gateway settings saved.')
      load()
    } catch (e) {
      message.error(e?.response?.data?.message || 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  const openTpl = (row) => {
    tplForm.resetFields()
    if (row) {
      tplForm.setFieldsValue({
        templateKey:  row.templateKey,
        title:        row.title,
        templateId:   row.templateId,
        messageText:  row.messageText,
        placeholders: row.placeholders,
        isActive:     row.isActive,
      })
    } else {
      tplForm.setFieldsValue({ isActive: true })
    }
    setTplModal({ open: true, editing: row })
  }

  const saveTpl = async () => {
    const v = await tplForm.validateFields()
    try {
      await api.post('/school/sms/templates', {
        TemplateKey:  v.templateKey.trim().toUpperCase(),
        Title:        v.title,
        TemplateId:   v.templateId,
        MessageText:  v.messageText || '',
        Placeholders: v.placeholders || null,
        IsActive:     v.isActive,
      })
      message.success('Template saved.')
      setTplModal({ open: false, editing: null })
      load()
    } catch (e) {
      message.error(e?.response?.data?.message || 'Failed to save template.')
    }
  }

  // Append a placeholder token to the Message Text field.
  const insertToken = (token) => {
    const cur = tplForm.getFieldValue('messageText') || ''
    tplForm.setFieldsValue({ messageText: cur + token })
  }

  const deleteTpl = async (key) => {
    try {
      await api.delete(`/school/sms/templates/${encodeURIComponent(key)}`)
      message.success('Template deleted.')
      load()
    } catch (e) {
      message.error(e?.response?.data?.message || 'Failed to delete.')
    }
  }

  const columns = [
    { title: 'Key',   dataIndex: 'templateKey', key: 'k', width: 120, render: (v) => <Tag>{v}</Tag> },
    { title: 'Title', dataIndex: 'title', key: 't', width: 160 },
    {
      title: 'DLT Template ID', dataIndex: 'templateId', key: 'id', width: 180,
      render: (v) => v ? <Text code>{v}</Text> : <Text type="danger">not set</Text>,
    },
    { title: 'Message', dataIndex: 'messageText', key: 'm', ellipsis: true,
      render: (v) => v || <Text type="secondary">(typed at send time)</Text> },
    {
      title: 'Active', dataIndex: 'isActive', key: 'a', width: 80,
      render: (v) => v ? <Tag color="green">Active</Tag> : <Tag>Off</Tag>,
    },
    {
      title: 'Actions', key: 'x', width: 120,
      render: (_, row) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openTpl(row)} />
          <Popconfirm title="Delete this template?" onConfirm={() => deleteTpl(row.templateKey)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div style={{ maxWidth: 900 }}>
      <Card title="SMS Gateway Account" loading={loading} style={{ marginBottom: 16 }}>
        <Alert
          type="info" showIcon style={{ marginBottom: 16 }}
          message="Per-school SMS settings"
          description="These credentials and templates are used by the SMS Center (Absent / Fee Due / Custom). Parent-login OTP is unaffected and uses a separate system account."
        />
        <Form form={accountForm} layout="vertical" disabled={loading}>
          <Space size="large" wrap>
            <Form.Item name="provider" label="Provider" style={{ width: 200 }}>
              <Input placeholder="smslogin" />
            </Form.Item>
            <Form.Item name="senderId" label="Sender ID (DLT header)" rules={[{ required: true }]} style={{ width: 200 }}>
              <Input placeholder="ASNTNF" maxLength={20} />
            </Form.Item>
            <Form.Item name="isEnabled" label="Enabled" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Space>
          <Form.Item name="apiUrl" label="API URL" rules={[{ required: true }]}>
            <Input placeholder="https://smslogin.mobi/v3/api.php" />
          </Form.Item>
          <Space size="large" wrap style={{ width: '100%' }}>
            <Form.Item name="username" label="Username" rules={[{ required: true }]} style={{ width: 260 }}>
              <Input />
            </Form.Item>
            <Form.Item
              name="apiKey"
              label={config?.hasApiKey ? 'API Key (configured ✓ — leave blank to keep)' : 'API Key'}
              rules={config?.hasApiKey ? [] : [{ required: true, message: 'API key required' }]}
              style={{ width: 320 }}
            >
              <Input.Password placeholder={config?.hasApiKey ? '••••••••' : ''} autoComplete="new-password" />
            </Form.Item>
          </Space>
          <Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={saveAccount}>
            Save Account
          </Button>
        </Form>
      </Card>

      <Card
        title="SMS Templates"
        loading={loading}
        extra={<Button type="primary" icon={<PlusOutlined />} onClick={() => openTpl(null)}>Add Template</Button>}
      >
        <Paragraph type="secondary">
          Each template must match a DLT-approved message registered with your provider. Use placeholders{' '}
          <Text code>{'{name}'}</Text>, <Text code>{'{class}'}</Text>, <Text code>{'{admissionNo}'}</Text>,{' '}
          <Text code>{'{date}'}</Text> (Absent), <Text code>{'{amount}'}</Text> (Fee Due) where the
          system fills values. Keys used by the SMS Center: <Text code>ABSENT</Text>, <Text code>FEE_DUE</Text>,{' '}
          <Text code>CUSTOM</Text> (Custom uses the message typed at send time).
        </Paragraph>
        <Table
          rowKey="templateKey"
          size="small"
          columns={columns}
          dataSource={templates}
          pagination={false}
        />
      </Card>

      <Modal
        title={tplModal.editing ? 'Edit Template' : 'Add Template'}
        open={tplModal.open}
        onOk={saveTpl}
        onCancel={() => setTplModal({ open: false, editing: null })}
        okText="Save"
        destroyOnClose
      >
        <Form form={tplForm} layout="vertical">
          <Form.Item
            name="templateKey" label="Template Key"
            rules={[{ required: true, message: 'Key required' }]}
            extra="ABSENT / FEE_DUE / CUSTOM are used by the SMS Center. Other keys are for future use."
          >
            <Input disabled={!!tplModal.editing} placeholder="ABSENT" />
          </Form.Item>
          <Form.Item name="title" label="Title">
            <Input placeholder="Absent Notification" maxLength={100} />
          </Form.Item>
          <Form.Item name="templateId" label="DLT Template ID" rules={[{ required: true, message: 'DLT template id required' }]}>
            <Input placeholder="1707xxxxxxxxxxxxxxx" maxLength={50} />
          </Form.Item>
          <Form.Item
            name="messageText" label="Message Text"
            extra="Leave blank for CUSTOM (message is typed at send time)."
          >
            <Input.TextArea rows={3} maxLength={500} />
          </Form.Item>
          <div style={{ marginTop: -8, marginBottom: 16 }}>
            <Text type="secondary" style={{ fontSize: 12 }}>Available placeholders (click to insert): </Text>
            <div style={{ marginTop: 6 }}>
              {PLACEHOLDERS.map(p => (
                <Tag
                  key={p.token}
                  color="blue"
                  title={p.desc}
                  style={{ cursor: 'pointer', marginBottom: 6 }}
                  onClick={() => insertToken(p.token)}
                >
                  {p.token}
                </Tag>
              ))}
            </div>
            <Text type="secondary" style={{ fontSize: 12 }}>
              {PLACEHOLDERS.map(p => `${p.token} = ${p.desc}`).join(' · ')}
            </Text>
          </div>
          <Form.Item
            name="placeholders" label="Placeholders (hint)"
            extra="Optional note for your reference only - not used by the system."
          >
            <Input placeholder="{name},{class},{admissionNo},{date}" maxLength={200} />
          </Form.Item>
          <Form.Item name="isActive" label="Active" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
