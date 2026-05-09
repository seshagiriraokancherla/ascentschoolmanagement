import { Table, Button, Modal, Form, Input, Space, Tag, Typography } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../../api/axiosInstance'

export default function SchoolGroupsPage() {
  const [groups,  setGroups]  = useState([])
  const [loading, setLoading] = useState(false)
  const [open,    setOpen]    = useState(false)
  const [saving,  setSaving]  = useState(false)
  const [form]                = Form.useForm()
  const navigate              = useNavigate()

  useEffect(() => { fetchGroups() }, [])

  const fetchGroups = async () => {
    setLoading(true)
    try {
      const res = await api.get('/control/school-groups')
      setGroups(res.data.data || [])
    } catch { /* handled by interceptor */ }
    finally { setLoading(false) }
  }

  const handleCreate = async (values) => {
    setSaving(true)
    try {
      await api.post('/control/school-groups', values)
      setOpen(false)
      form.resetFields()
      fetchGroups()
    } catch (err) {
      Modal.error({ title: 'Error', content: err.response?.data?.message || 'Failed to create school group.' })
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'ID',         dataIndex: 'groupId',     width: 60 },
    { title: 'Name',       dataIndex: 'groupName',   render: (v, r) => <a onClick={() => navigate(`/school-groups/${r.groupId}`)}>{v}</a> },
    { title: 'Subdomain',  dataIndex: 'subdomain',   render: v => <Typography.Text code>{v}.edu-care.in</Typography.Text> },
    { title: 'Description',dataIndex: 'description' },
    { title: 'Status',     dataIndex: 'status',      render: v => <Tag color={v === 'Active' ? 'green' : 'red'}>{v}</Tag> },
    { title: 'Created',    dataIndex: 'createdAt',   render: v => new Date(v).toLocaleDateString() },
    {
      title: 'Actions',
      render: (_, r) => (
        <Button size="small" onClick={() => navigate(`/school-groups/${r.groupId}`)}>
          Manage
        </Button>
      ),
    },
  ]

  return (
    <div style={{ background: '#fff', padding: 24, borderRadius: 8 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={4} style={{ margin: 0 }}>School Groups</Typography.Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setOpen(true)}>
          New School Group
        </Button>
      </div>

      <Table rowKey="groupId" dataSource={groups} columns={columns} loading={loading} pagination={{ pageSize: 20 }} />

      <Modal
        title="New School Group"
        open={open}
        onCancel={() => { setOpen(false); form.resetFields() }}
        onOk={() => form.submit()}
        confirmLoading={saving}
        okText="Create & Provision DB"
      >
        <Form form={form} layout="vertical" onFinish={handleCreate} style={{ marginTop: 16 }}>
          <Form.Item name="groupName" label="Group Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Sri Vidya Schools" />
          </Form.Item>
          <Form.Item
            name="subdomain"
            label="Subdomain"
            rules={[
              { required: true },
              { pattern: /^[a-z0-9-]+$/, message: 'Lowercase letters, numbers, hyphens only.' }
            ]}
          >
            <Input addonAfter=".edu-care.in" placeholder="srividya" />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
