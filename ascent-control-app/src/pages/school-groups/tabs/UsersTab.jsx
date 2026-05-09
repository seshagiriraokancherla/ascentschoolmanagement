import {
  Table, Button, Modal, Form, Input, Select, Tag,
  Popconfirm, Space, Typography, Alert
} from 'antd'
import {
  PlusOutlined, KeyOutlined, StopOutlined, CheckCircleOutlined
} from '@ant-design/icons'
import { useState, useEffect } from 'react'
import api from '../../../api/axiosInstance'

export default function UsersTab({ groupId }) {
  const [users,   setUsers]   = useState([])
  const [schools, setSchools] = useState([])
  const [roles,   setRoles]   = useState([])
  const [loading, setLoading] = useState(false)
  const [open,    setOpen]    = useState(false)
  const [saving,  setSaving]  = useState(false)
  const [error,   setError]   = useState(null)
  const [form]                = Form.useForm()

  useEffect(() => { fetchAll() }, [groupId])

  const fetchAll = async () => {
    setLoading(true)
    try {
      const [usersRes, schoolsRes, rolesRes] = await Promise.all([
        api.get(`/control/school-groups/${groupId}/users`),
        api.get(`/control/school-groups/${groupId}/schools`),
        api.get(`/control/school-groups/${groupId}/roles`),
      ])
      setUsers(usersRes.data.data   || [])
      setSchools(schoolsRes.data.data || [])
      setRoles(rolesRes.data.data    || [])
    } finally {
      setLoading(false)
    }
  }

  const openCreate = () => {
    setError(null)
    form.resetFields()
    // Default: all schools selected, Principal role
    const principalRole = roles.find(r => r.roleName === 'Principal')
    form.setFieldsValue({
      roleId:    principalRole?.roleId,
      schoolIds: schools.map(s => s.schoolId),
    })
    setOpen(true)
  }

  const handleCreate = async (values) => {
    setSaving(true)
    setError(null)
    try {
      await api.post(`/control/school-groups/${groupId}/users`, values)
      setOpen(false)
      form.resetFields()
      fetchAll()
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
      await api.put(`/control/school-groups/${groupId}/users/${userId}/reset-password`, { newPassword: pwd })
      Modal.success({ title: 'Password reset', content: 'The user will need to log in again.' })
    } catch (err) {
      Modal.error({ title: 'Error', content: err.response?.data?.message || 'Failed.' })
    }
  }

  const handleSetStatus = async (userId, status) => {
    try {
      await api.put(`/control/school-groups/${groupId}/users/${userId}/status`, { status })
      fetchAll()
    } catch (err) {
      Modal.error({ title: 'Error', content: err.response?.data?.message || 'Failed.' })
    }
  }

  const columns = [
    { title: 'ID',       dataIndex: 'userId',   width: 60 },
    { title: 'Username', dataIndex: 'username', render: v => <Typography.Text code>{v}</Typography.Text> },
    { title: 'Full Name',dataIndex: 'fullName' },
    { title: 'Email',    dataIndex: 'email' },
    { title: 'Mobile',   dataIndex: 'mobile' },
    { title: 'Roles',    dataIndex: 'roles',    render: v => v ? v.split(', ').map(r => <Tag key={r} color="blue">{r}</Tag>) : '-' },
    {
      title: 'Status',
      dataIndex: 'status',
      width: 90,
      render: v => <Tag color={v === 'Active' ? 'green' : 'red'}>{v}</Tag>
    },
    {
      title: 'Actions',
      width: 160,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<KeyOutlined />} onClick={() => handleResetPassword(r.userId)}>
            Reset Pwd
          </Button>
          {r.status === 'Active'
            ? (
              <Popconfirm title="Deactivate this user?" onConfirm={() => handleSetStatus(r.userId, 'Inactive')}>
                <Button size="small" icon={<StopOutlined />} danger />
              </Popconfirm>
            ) : (
              <Popconfirm title="Activate this user?" onConfirm={() => handleSetStatus(r.userId, 'Active')}>
                <Button size="small" icon={<CheckCircleOutlined />} />
              </Popconfirm>
            )
          }
        </Space>
      ),
    },
  ]

  const noSchools = schools.length === 0

  return (
    <>
      {noSchools && (
        <Alert
          type="warning"
          showIcon
          message="Add at least one school branch before creating users."
          style={{ margin: '16px 0' }}
        />
      )}

      <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '16px 0' }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate} disabled={noSchools}>
          Add User
        </Button>
      </div>

      <Table rowKey="userId" dataSource={users} columns={columns} loading={loading} pagination={false} />

      <Modal
        title="Add School User"
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
              { pattern: /^[a-zA-Z0-9._-]+$/, message: 'Letters, numbers, dots, hyphens, underscores only.' }
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
            <Form.Item name="email" label="Email">
              <Input />
            </Form.Item>
            <Form.Item name="mobile" label="Mobile">
              <Input />
            </Form.Item>
          </div>

          <Form.Item name="roleId" label="Role" rules={[{ required: true, message: 'Select a role.' }]}>
            <Select
              options={roles.map(r => ({ value: r.roleId, label: r.roleName }))}
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
              options={schools.map(s => ({ value: s.schoolId, label: s.schoolName }))}
              placeholder="Select branches"
            />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
