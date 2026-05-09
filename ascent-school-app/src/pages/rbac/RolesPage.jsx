import {
  Table, Button, Modal, Form, Input, Drawer, Checkbox,
  Space, Tag, Typography, Spin, Alert, Divider, Select,
} from 'antd'
import { PlusOutlined, EditOutlined, KeyOutlined } from '@ant-design/icons'
import { useState, useEffect } from 'react'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography

// Derive module name from permission code (e.g. "STUDENT_FEE.COLLECT" → "STUDENT_FEE")
const moduleOf = (code) => code.split('.')[0]

// Group permissions array into { MODULE: [permission, ...] }
const groupByModule = (permissions) =>
  permissions.reduce((acc, p) => {
    const mod = moduleOf(p.permissionCode)
    if (!acc[mod]) acc[mod] = []
    acc[mod].push(p)
    return acc
  }, {})

export default function RolesPage() {
  const [roles,       setRoles]       = useState([])
  const [permissions, setPermissions] = useState([])   // all available
  const [loading,     setLoading]     = useState(false)

  // Create/edit role modal
  const [roleModalOpen, setRoleModalOpen] = useState(false)
  const [editingRole,   setEditingRole]   = useState(null)   // null = create
  const [roleSaving,    setRoleSaving]    = useState(false)
  const [roleError,     setRoleError]     = useState(null)
  const [roleForm]                        = Form.useForm()

  // Permission drawer
  const [drawerOpen,     setDrawerOpen]     = useState(false)
  const [selectedRole,   setSelectedRole]   = useState(null)
  const [checkedIds,     setCheckedIds]     = useState([])
  const [permSaving,     setPermSaving]     = useState(false)
  const [permError,      setPermError]      = useState(null)

  useEffect(() => { fetchAll() }, [])

  const fetchAll = async () => {
    setLoading(true)
    try {
      const [rolesRes, permsRes] = await Promise.all([
        api.get('/school/roles'),
        api.get('/school/roles/permissions'),
      ])
      setRoles(rolesRes.data.data       || [])
      setPermissions(permsRes.data.data || [])
    } finally {
      setLoading(false)
    }
  }

  // ── Role create / edit ──────────────────────────────────────────────────
  const openCreate = () => {
    setEditingRole(null)
    setRoleError(null)
    roleForm.resetFields()
    setRoleModalOpen(true)
  }

  const openEdit = (role) => {
    setEditingRole(role)
    setRoleError(null)
    roleForm.setFieldsValue({ roleName: role.roleName, description: role.description })
    setRoleModalOpen(true)
  }

  const handleRoleSave = async (values) => {
    setRoleSaving(true)
    setRoleError(null)
    try {
      if (editingRole) {
        const res = await api.put(`/school/roles/${editingRole.roleId}`, values)
        setRoles(res.data.data || [])
      } else {
        const res = await api.post('/school/roles', values)
        setRoles(res.data.data || [])
      }
      setRoleModalOpen(false)
    } catch (err) {
      setRoleError(err.response?.data?.message || 'Failed to save role.')
    } finally {
      setRoleSaving(false)
    }
  }

  // ── Permission drawer ───────────────────────────────────────────────────
  const openPermissions = async (role) => {
    setSelectedRole(role)
    setPermError(null)
    setPermSaving(false)
    setDrawerOpen(true)
    try {
      const res = await api.get(`/school/roles/${role.roleId}/permissions`)
      setCheckedIds(res.data.data || [])
    } catch {
      setPermError('Failed to load permissions.')
    }
  }

  const handlePermSave = async () => {
    setPermSaving(true)
    setPermError(null)
    try {
      await api.put(`/school/roles/${selectedRole.roleId}/permissions`, { permissionIds: checkedIds })
      // Refresh role list to update permission count
      const res = await api.get('/school/roles')
      setRoles(res.data.data || [])
      setDrawerOpen(false)
    } catch (err) {
      setPermError(err.response?.data?.message || 'Failed to save permissions.')
    } finally {
      setPermSaving(false)
    }
  }

  const togglePermission = (id, checked) => {
    setCheckedIds((prev) => checked ? [...prev, id] : prev.filter((x) => x !== id))
  }

  const toggleModule = (modulePerms, checked) => {
    const ids = modulePerms.map((p) => p.permissionId)
    setCheckedIds((prev) =>
      checked
        ? [...new Set([...prev, ...ids])]
        : prev.filter((x) => !ids.includes(x))
    )
  }

  // ── Table columns ───────────────────────────────────────────────────────
  const columns = [
    { title: 'Role', dataIndex: 'roleName', render: (v) => <Text strong>{v}</Text> },
    { title: 'Description', dataIndex: 'description', render: (v) => v || <Text type="secondary">—</Text> },
    {
      title: 'Scope',
      dataIndex: 'schoolId',
      width: 110,
      render: (v) => <Tag color={v ? 'blue' : 'purple'}>{v ? 'Branch' : 'Group-wide'}</Tag>,
    },
    {
      title: 'Permissions',
      dataIndex: 'permissionCount',
      width: 110,
      render: (v) => <Tag>{v}</Tag>,
    },
    {
      title: 'Actions',
      width: 160,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EditOutlined />}  onClick={() => openEdit(r)}>Edit</Button>
          <Button size="small" icon={<KeyOutlined />}   onClick={() => openPermissions(r)}>Permissions</Button>
        </Space>
      ),
    },
  ]

  const grouped = groupByModule(permissions)

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={4} style={{ margin: 0 }}>Roles &amp; Permissions</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Role</Button>
      </div>

      <div style={{ background: '#fff', borderRadius: 8, padding: 16 }}>
        <Table
          rowKey="roleId"
          dataSource={roles}
          columns={columns}
          loading={loading}
          pagination={false}
        />
      </div>

      {/* ── Create / Edit Role Modal ──────────────────────────────── */}
      <Modal
        title={editingRole ? 'Edit Role' : 'Add Role'}
        open={roleModalOpen}
        onCancel={() => setRoleModalOpen(false)}
        onOk={() => roleForm.submit()}
        confirmLoading={roleSaving}
        okText={editingRole ? 'Save' : 'Create'}
        destroyOnClose
      >
        {roleError && <Alert type="error" message={roleError} showIcon style={{ marginBottom: 12 }} />}
        <Form form={roleForm} layout="vertical" onFinish={handleRoleSave} style={{ marginTop: 12 }}>
          <Form.Item name="roleName" label="Role Name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={2} />
          </Form.Item>
          {!editingRole && (
            <Form.Item name="schoolId" label="Scope" extra="Leave blank to make this role available across all branches.">
              <Select allowClear placeholder="Group-wide (all branches)" />
            </Form.Item>
          )}
        </Form>
      </Modal>

      {/* ── Permissions Drawer ────────────────────────────────────── */}
      <Drawer
        title={`Permissions — ${selectedRole?.roleName}`}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        width={480}
        footer={
          <div style={{ textAlign: 'right' }}>
            <Button onClick={() => setDrawerOpen(false)} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" loading={permSaving} onClick={handlePermSave}>Save Permissions</Button>
          </div>
        }
      >
        {permError && <Alert type="error" message={permError} showIcon style={{ marginBottom: 12 }} />}

        {Object.entries(grouped).map(([module, perms]) => {
          const allChecked  = perms.every((p) => checkedIds.includes(p.permissionId))
          const someChecked = perms.some((p)  => checkedIds.includes(p.permissionId))
          return (
            <div key={module} style={{ marginBottom: 20 }}>
              <Divider orientation="left" style={{ margin: '8px 0' }}>
                <Checkbox
                  checked={allChecked}
                  indeterminate={!allChecked && someChecked}
                  onChange={(e) => toggleModule(perms, e.target.checked)}
                >
                  <Text strong>{module.replace(/_/g, ' ')}</Text>
                </Checkbox>
              </Divider>
              <div style={{ paddingLeft: 24, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                {perms.map((p) => (
                  <Checkbox
                    key={p.permissionId}
                    checked={checkedIds.includes(p.permissionId)}
                    onChange={(e) => togglePermission(p.permissionId, e.target.checked)}
                  >
                    {p.permissionCode.split('.')[1]}
                  </Checkbox>
                ))}
              </div>
            </div>
          )
        })}
      </Drawer>
    </>
  )
}
