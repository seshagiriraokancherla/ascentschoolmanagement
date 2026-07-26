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

// Suggest a username from a staff record: employee code if present, else the
// name normalized to lowercase alphanumerics. Always editable by the operator.
const suggestUsername = (s) =>
  (s.employeeCode?.trim() || (s.staffName || '').trim().toLowerCase().replace(/[^a-z0-9]+/g, '')) || ''

export default function UsersPage() {
  const [users,   setUsers]   = useState([])
  const [schools, setSchools] = useState([])
  const [roles,   setRoles]   = useState([])
  const [staff,   setStaff]   = useState([])
  const [loading, setLoading] = useState(false)

  const [open,   setOpen]   = useState(false)
  const [saving, setSaving] = useState(false)
  const [error,  setError]  = useState(null)
  const [form]              = Form.useForm()

  useEffect(() => { fetchAll() }, [])

  const fetchAll = async () => {
    setLoading(true)
    try {
      const [usersRes, schoolsRes, rolesRes, staffRes] = await Promise.all([
        api.get('/school/users'),
        api.get('/school/users/schools'),
        api.get('/school/users/roles'),
        api.get('/school/users/staff'),
      ])
      setUsers(usersRes.data.data     || [])
      setSchools(schoolsRes.data.data || [])
      setRoles(rolesRes.data.data     || [])
      setStaff(staffRes.data.data     || [])
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

  // Auto-fill name/email/mobile + suggest a username when a staff member is picked.
  const onStaffSelect = (staffId) => {
    const s = staff.find((x) => x.staffId === staffId)
    if (!s) return
    form.setFieldsValue({
      fullName: s.staffName,
      email:    s.email  || '',
      mobile:   s.mobile || '',
      username: suggestUsername(s),
    })
  }

  const handleCreate = async (values) => {
    setSaving(true)
    setError(null)
    try {
      await api.post('/school/users', values)
      setOpen(false)
      form.resetFields()
      fetchAll()   // refresh users + the assignable-staff list (linked staff drops off)
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
    { title: 'Username',  dataIndex: 'username', render: (v) => <Text strong>{v}</Text> },
    { title: 'Full Name', dataIndex: 'fullName' },
    {
      title: 'Staff',
      dataIndex: 'staffName',
      render: (v, r) => v
        ? <span>{v}{r.designation ? <Text type="secondary"> · {r.designation}</Text> : null}</span>
        : <Text type="secondary">—</Text>,
    },
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
  const noStaff   = staff.length === 0

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={4} style={{ margin: 0 }}>User Management</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate} disabled={noSchools || noStaff}>
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

      {!noSchools && noStaff && (
        <Alert
          type="info" showIcon
          message="No staff available for a login."
          description="Users are created from staff records. Add a staff member (Staff module) — or all active staff already have a login."
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
          <Form.Item
            name="employeeId"
            label="Staff Member"
            rules={[{ required: true, message: 'Select a staff member.' }]}
            extra="Users are created from staff records. Name, email and mobile come from the selected staff."
          >
            <Select
              showSearch
              placeholder="Select staff member"
              onChange={onStaffSelect}
              optionFilterProp="label"
              options={staff.map((s) => ({
                value: s.staffId,
                label: `${s.staffName}${s.employeeCode ? ` (${s.employeeCode})` : ''}${s.designation ? ` — ${s.designation}` : ''}`,
              }))}
            />
          </Form.Item>

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
            <Form.Item name="email"  label="Email"><Input readOnly placeholder="From staff record" /></Form.Item>
            <Form.Item name="mobile" label="Mobile"><Input readOnly placeholder="From staff record" /></Form.Item>
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
