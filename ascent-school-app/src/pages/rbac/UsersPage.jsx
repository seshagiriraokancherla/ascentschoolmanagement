import {
  Table, Button, Modal, Form, Input, Select,
  Tag, Space, Typography, Alert, Popconfirm,
} from 'antd'
import {
  PlusOutlined, KeyOutlined, StopOutlined, CheckCircleOutlined,
} from '@ant-design/icons'
import { useState, useEffect } from 'react'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography

export default function UsersPage() {
  const [users,   setUsers]   = useState([])
  const [schools, setSchools] = useState([])
  const [roles,   setRoles]   = useState([])
  const [loading, setLoading] = useState(false)

  const [open,   setOpen]   = useState(false)
  const [saving, setSaving] = useState(false)
  const [error,  setError]  = useState(null)
  const [form]              = Form.useForm()

  useEffect(() => { fetchAll() }, [])

  const fetchAll = async () => {
    setLoading(true)
    try {
      const [usersRes, schoolsRes, rolesRes] = await Promise.all([
        api.get('/school/users'),
        api.get('/school/users/schools'),
        api.get('/school/users/roles'),
      ])
      setUsers(usersRes.data.data     || [])
      setSchools(schoolsRes.data.data || [])
      setRoles(rolesRes.data.data     || [])
    } finally {
      setLoading(false)
    }
  }

  const openCreate = () => {
    setError(null)
    form.resetFields()
    const principalRole = roles.find((r) => r.roleName === 'Principal')
    form.setFieldsValue({
      roleId:    principalRole?.roleId,
      schoolIds: schools.map((s) => s.schoolId),
    })
    setOpen(true)
  }

  const handleCreate = async (values) => {
    setSaving(true)
    setError(null)
    try {
      const res = await api.post('/school/users', values)
      setUsers(res.data.data || [])
      setOpen(false)
      form.resetFields()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to create user.')
    } finally {
      setSaving(false)
    }
  }

  const handleResetPassword = async (userId) => {
    const pwd = window.prompt('Enter new password:')
    if (!pwd) return
    try {
      await api.put(`/school/users/${userId}/reset-password`, { newPassword: pwd })
      Modal.success({ title: 'Password reset', content: 'The user will need to log in again.' })
    } catch (err) {
      Modal.error({ title: 'Error', content: err.response?.data?.message || 'Failed.' })
    }
  }

  const handleSetStatus = async (userId, status) => {
    try {
      const res = await api.put(`/school/users/${userId}/status`, { status })
      setUsers(res.data.data || [])
    } catch (err) {
      Modal.error({ title: 'Error', content: err.response?.data?.message || 'Failed.' })
    }
  }

  const columns = [
    { title: 'ID',        dataIndex: 'userId',   width: 60 },
    { title: 'Username',  dataIndex: 'username', render: (v) => <Text code>{v}</Text> },
    { title: 'Full Name', dataIndex: 'fullName' },
    { title: 'Email',     dataIndex: 'email',    render: (v) => v || '—' },
    { title: 'Mobile',    dataIndex: 'mobile',   render: (v) => v || '—' },
    {
      title: 'Roles',
      dataIndex: 'roles',
      render: (v) => v ? v.split(', ').map((r) => <Tag key={r} color="blue">{r}</Tag>) : '—',
    },
    {
      title: 'Status',
      dataIndex: 'status',
      width: 90,
      render: (v) => <Tag color={v === 'Active' ? 'green' : 'red'}>{v}</Tag>,
    },
    {
      title: 'Actions',
      width: 160,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<KeyOutlined />} onClick={() => handleResetPassword(r.userId)}>
            Reset Pwd
          </Button>
          {r.status === 'Active' ? (
            <Popconfirm title="Deactivate this user?" onConfirm={() => handleSetStatus(r.userId, 'Inactive')}>
              <Button size="small" icon={<StopOutlined />} danger />
            </Popconfirm>
          ) : (
            <Popconfirm title="Activate this user?" onConfirm={() => handleSetStatus(r.userId, 'Active')}>
              <Button size="small" icon={<CheckCircleOutlined />} />
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ]

  const noSchools = schools.length === 0

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={4} style={{ margin: 0 }}>User Management</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate} disabled={noSchools}>
          Add User
        </Button>
      </div>

      {noSchools && (
        <Alert
          type="warning" showIcon
          message="No school branches found. Add at least one branch before creating users."
          style={{ marginBottom: 16 }}
        />
      )}

      <div style={{ background: '#fff', borderRadius: 8, padding: 16 }}>
        <Table
          rowKey="userId"
          dataSource={users}
          columns={columns}
          loading={loading}
          pagination={{ pageSize: 20 }}
        />
      </div>

      <Modal
        title="Add User"
        open={open}
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={saving}
        okText="Create User"
        width={520}
        destroyOnClose
      >
        {error && <Alert type="error" message={error} showIcon style={{ marginBottom: 12 }} />}

        <Form form={form} layout="vertical" onFinish={handleCreate} style={{ marginTop: 12 }}>
          <Form.Item name="fullName" label="Full Name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>

          <Form.Item
            name="username"
            label="Username"
            rules={[
              { required: true },
              { pattern: /^[a-zA-Z0-9._-]+$/, message: 'Letters, numbers, dots, hyphens, underscores only.' },
            ]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="password"
            label="Password"
            rules={[{ required: true }, { min: 6, message: 'Min 6 characters.' }]}
          >
            <Input.Password />
          </Form.Item>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="email"  label="Email"><Input /></Form.Item>
            <Form.Item name="mobile" label="Mobile"><Input /></Form.Item>
          </div>

          <Form.Item name="roleId" label="Role" rules={[{ required: true, message: 'Select a role.' }]}>
            <Select
              options={roles.map((r) => ({ value: r.roleId, label: r.roleName }))}
              placeholder="Select role"
            />
          </Form.Item>

          <Form.Item
            name="schoolIds"
            label="Assign to Branch(es)"
            rules={[{ required: true, type: 'array', min: 1, message: 'Select at least one branch.' }]}
            extra="The user will have the selected role at each chosen branch."
          >
            <Select
              mode="multiple"
              options={schools.map((s) => ({ value: s.schoolId, label: s.schoolName }))}
              placeholder="Select branches"
            />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
