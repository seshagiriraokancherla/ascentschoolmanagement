import { Table, Button, Modal, Form, Input, Tag, Typography, Space, message } from 'antd'
import { PlusOutlined, KeyOutlined, CopyOutlined } from '@ant-design/icons'
import { useState, useEffect } from 'react'
import api from '../../../api/axiosInstance'

const { Text } = Typography

export default function SchoolsTab({ groupId }) {
  const [schools,  setSchools]  = useState([])
  const [loading,  setLoading]  = useState(false)
  const [open,     setOpen]     = useState(false)
  const [editing,  setEditing]  = useState(null)
  const [saving,   setSaving]   = useState(false)
  const [form]                  = Form.useForm()

  // API key modal state
  const [keySchool,    setKeySchool]    = useState(null)   // school row being managed
  const [keyOpen,      setKeyOpen]      = useState(false)
  const [currentKey,   setCurrentKey]   = useState('')
  const [keyLoading,   setKeyLoading]   = useState(false)
  const [generating,   setGenerating]   = useState(false)

  useEffect(() => { fetchSchools() }, [groupId])

  const fetchSchools = async () => {
    setLoading(true)
    try {
      const res = await api.get(`/control/school-groups/${groupId}/schools`)
      setSchools(res.data.data || [])
    } finally { setLoading(false) }
  }

  const openCreate = () => { setEditing(null); form.resetFields(); setOpen(true) }
  const openEdit   = (row) => { setEditing(row); form.setFieldsValue(row); setOpen(true) }

  const handleSave = async (values) => {
    setSaving(true)
    try {
      if (editing) {
        await api.put(`/control/school-groups/${groupId}/schools/${editing.schoolId}`, values)
      } else {
        await api.post(`/control/school-groups/${groupId}/schools`, values)
      }
      setOpen(false)
      fetchSchools()
    } catch (err) {
      Modal.error({ title: 'Error', content: err.message || 'Save failed.' })
    } finally { setSaving(false) }
  }

  // ── API key modal ────────────────────────────────────────────────────────

  const openKeyModal = async (school) => {
    setKeySchool(school)
    setCurrentKey('')
    setKeyOpen(true)
    setKeyLoading(true)
    try {
      const res = await api.get(`/control/school-groups/${groupId}/schools/${school.schoolId}/api-key`)
      setCurrentKey(res.data.data?.apiKey || '')
    } catch {
      setCurrentKey('')
    } finally { setKeyLoading(false) }
  }

  const handleGenerate = async () => {
    setGenerating(true)
    try {
      const res = await api.post(
        `/control/school-groups/${groupId}/schools/${keySchool.schoolId}/api-key/generate`
      )
      setCurrentKey(res.data.data?.apiKey || '')
      message.success('New API key generated.')
    } catch (err) {
      message.error(err.message || 'Failed to generate key.')
    } finally { setGenerating(false) }
  }

  const copyKey = () => {
    navigator.clipboard.writeText(currentKey)
    message.success('Copied to clipboard.')
  }

  // ── columns ──────────────────────────────────────────────────────────────

  const columns = [
    { title: 'ID',      dataIndex: 'schoolId',    width: 60 },
    { title: 'Name',    dataIndex: 'schoolName' },
    { title: 'Caption', dataIndex: 'schoolCaption' },
    { title: 'City',    dataIndex: 'city' },
    { title: 'Mobile',  dataIndex: 'mobile' },
    { title: 'Status',  dataIndex: 'status',
      render: v => <Tag color={v === 'Active' ? 'green' : 'red'}>{v}</Tag> },
    {
      title: '',
      render: (_, r) => (
        <Space>
          <Button size="small" onClick={() => openEdit(r)}>Edit</Button>
          <Button size="small" icon={<KeyOutlined />} onClick={() => openKeyModal(r)}>VB Key</Button>
        </Space>
      ),
    },
  ]

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '16px 0' }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add School</Button>
      </div>

      <Table rowKey="schoolId" dataSource={schools} columns={columns} loading={loading} pagination={false} />

      {/* ── Create / Edit modal ── */}
      <Modal
        title={editing ? 'Edit School' : 'Add School'}
        open={open}
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={saving}
      >
        <Form form={form} layout="vertical" onFinish={handleSave} style={{ marginTop: 16 }}>
          <Form.Item name="schoolName" label="School Name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="schoolCaption" label="Caption / Short Name">
            <Input />
          </Form.Item>
          <Form.Item name="address" label="Address">
            <Input.TextArea rows={2} />
          </Form.Item>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="city"  label="City"><Input /></Form.Item>
            <Form.Item name="state" label="State"><Input /></Form.Item>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="mobile" label="Mobile"><Input /></Form.Item>
            <Form.Item name="email"  label="Email"><Input /></Form.Item>
          </div>
          {editing && (
            <Form.Item name="status" label="Status">
              <Input placeholder="Active / Inactive" />
            </Form.Item>
          )}
        </Form>
      </Modal>

      {/* ── API Key modal ── */}
      <Modal
        title={`VB Integration Key — ${keySchool?.schoolName || ''}`}
        open={keyOpen}
        onCancel={() => setKeyOpen(false)}
        footer={null}
        width={520}
      >
        <div style={{ marginTop: 8 }}>
          <Text type="secondary" style={{ fontSize: 13 }}>
            Paste this key into the <code>API_KEY</code> constant in <code>AscentAPI.bas</code>.
            Clicking Generate replaces the existing key — update the VB app immediately after.
          </Text>

          <div style={{ margin: '20px 0' }}>
            {keyLoading ? (
              <Text type="secondary">Loading…</Text>
            ) : currentKey ? (
              <Space.Compact style={{ width: '100%' }}>
                <Input
                  value={currentKey}
                  readOnly
                  style={{ fontFamily: 'monospace', fontSize: 13 }}
                />
                <Button icon={<CopyOutlined />} onClick={copyKey} title="Copy to clipboard" />
              </Space.Compact>
            ) : (
              <Text type="secondary">No API key set for this school yet.</Text>
            )}
          </div>

          <Button
            type="primary"
            danger={!!currentKey}
            loading={generating}
            onClick={handleGenerate}
          >
            {currentKey ? 'Regenerate Key' : 'Generate Key'}
          </Button>
        </div>
      </Modal>
    </>
  )
}
