import { Tabs, Typography, Button, Breadcrumb, Spin, Modal, Form, Input, Select, Descriptions, Divider, App as AntApp } from 'antd'
import { ArrowLeftOutlined, EditOutlined } from '@ant-design/icons'
import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import api from '../../api/axiosInstance'
import SchoolsTab       from './tabs/SchoolsTab'
import SubscriptionTab  from './tabs/SubscriptionTab'
import ModulesTab       from './tabs/ModulesTab'
import BrandingTab      from './tabs/BrandingTab'
import UsersTab         from './tabs/UsersTab'

export default function SchoolGroupDetailPage() {
  const { id }                  = useParams()
  const navigate                = useNavigate()
  const { message }             = AntApp.useApp()
  const [group,   setGroup]     = useState(null)
  const [loading, setLoading]   = useState(true)
  const [editOpen, setEditOpen] = useState(false)
  const [saving,   setSaving]   = useState(false)
  const [form]                  = Form.useForm()

  const fetchGroup = () => {
    api.get(`/control/school-groups/${id}`)
      .then(res => setGroup(res.data.data))
      .catch(() => navigate('/school-groups'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { fetchGroup() }, [id])

  const openEdit = () => {
    form.setFieldsValue({
      groupName:   group.groupName,
      description: group.description,
      status:      group.status,
      dbName:      group.dbName,
      dbUsername:  group.dbUsername,
      dbPassword:  group.dbPassword,
    })
    setEditOpen(true)
  }

  const handleSave = async (values) => {
    setSaving(true)
    try {
      await api.put(`/control/school-groups/${id}`, values)
      message.success('Group settings saved.')
      setEditOpen(false)
      fetchGroup()
    } catch (err) {
      message.error(err.response?.data?.message || 'Save failed.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div style={{ textAlign: 'center', padding: 60 }}><Spin size="large" /></div>
  if (!group)  return null

  const tabs = [
    { key: 'schools',      label: 'Schools',      children: <SchoolsTab groupId={Number(id)} /> },
    { key: 'users',        label: 'Users',        children: <UsersTab groupId={Number(id)} /> },
    { key: 'subscription', label: 'Subscription', children: <SubscriptionTab groupId={Number(id)} /> },
    { key: 'modules',      label: 'Modules',      children: <ModulesTab groupId={Number(id)} /> },
    { key: 'branding',     label: 'Branding',     children: <BrandingTab groupId={Number(id)} /> },
  ]

  return (
    <div>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <a onClick={() => navigate('/school-groups')}>School Groups</a> },
          { title: group.groupName },
        ]}
      />

      <div style={{ background: '#fff', padding: 24, borderRadius: 8, marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 12 }}>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/school-groups')} />
          <Typography.Title level={4} style={{ margin: 0, flex: 1 }}>{group.groupName}</Typography.Title>
          <Button icon={<EditOutlined />} onClick={openEdit}>Edit Settings</Button>
        </div>

        <Descriptions size="small" column={3}>
          <Descriptions.Item label="Subdomain">
            <Typography.Text code>{group.subdomain}.edu-care.in</Typography.Text>
          </Descriptions.Item>
          <Descriptions.Item label="Status">{group.status}</Descriptions.Item>
          <Descriptions.Item label="DB Name">
            <Typography.Text code>{group.dbName || '—'}</Typography.Text>
          </Descriptions.Item>
          <Descriptions.Item label="DB Username">
            <Typography.Text code>{group.dbUsername || '—'}</Typography.Text>
          </Descriptions.Item>
          <Descriptions.Item label="DB Password">
            {group.dbPassword ? '••••••••' : '—'}
          </Descriptions.Item>
          {group.description && (
            <Descriptions.Item label="Description">{group.description}</Descriptions.Item>
          )}
        </Descriptions>
      </div>

      <div style={{ background: '#fff', padding: '0 24px', borderRadius: 8 }}>
        <Tabs defaultActiveKey="schools" items={tabs} />
      </div>

      {/* Edit modal */}
      <Modal
        title="Edit Group Settings"
        open={editOpen}
        onCancel={() => setEditOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={saving}
        okText="Save"
        width={560}
      >
        <Form form={form} layout="vertical" onFinish={handleSave} style={{ marginTop: 16 }}>
          <Form.Item name="groupName" label="Group Name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="status" label="Status" rules={[{ required: true }]}>
            <Select options={[{ value: 'Active', label: 'Active' }, { value: 'Inactive', label: 'Inactive' }]} />
          </Form.Item>

          <Divider style={{ margin: '16px 0 8px' }} />
          <Typography.Text strong style={{ display: 'block', marginBottom: 12 }}>
            Database Connection
          </Typography.Text>

          <Form.Item
            name="dbName"
            label="Database Name"
            tooltip="e.g. ascent_group_3 — set after manually provisioning the tenant DB"
          >
            <Input placeholder="ascent_group_3" />
          </Form.Item>
          <div style={{ display: 'flex', gap: 12 }}>
            <Form.Item name="dbUsername" label="DB Username" style={{ flex: 1 }}>
              <Input placeholder="fastwebhost SQL username" />
            </Form.Item>
            <Form.Item name="dbPassword" label="DB Password" style={{ flex: 1 }}>
              <Input.Password placeholder="fastwebhost SQL password" />
            </Form.Item>
          </div>
        </Form>
      </Modal>
    </div>
  )
}
