import { useEffect, useState } from 'react'
import {
  Table, Button, Space, Input, Select, Tag, Modal, Form,
  DatePicker, Typography, Row, Col, Popconfirm, App as AntApp,
} from 'antd'
import { PlusOutlined, EditOutlined, UserOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'

const { Text } = Typography
const { Search } = Input

const DESIGNATIONS = [
  'Principal', 'Vice Principal', 'Head Master', 'Head Mistress',
  'Teacher', 'PRT', 'TGT', 'PGT',
  'Admin Clerk', 'Fee Clerk', 'Librarian',
  'Lab Assistant', 'Sports Coach', 'Driver', 'Attender', 'Other',
]

export default function StaffPage() {
  const { message } = AntApp.useApp()

  const [staff,   setStaff]   = useState([])
  const [loading, setLoading] = useState(false)
  const [search,  setSearch]  = useState('')
  const [statusFilter, setStatusFilter] = useState('Active')

  const [modalOpen, setModalOpen] = useState(false)
  const [editing,   setEditing]   = useState(null)   // null = create, obj = edit
  const [saving,    setSaving]    = useState(false)
  const [form]                    = Form.useForm()

  const loadStaff = async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (search)       params.append('search', search)
      if (statusFilter) params.append('status', statusFilter)
      const r = await api.get(`/school/staff?${params}`)
      setStaff(r.data?.data || [])
    } catch (e) { message.error(apiError(e, 'Failed to load staff.')) }
    finally { setLoading(false) }
  }

  useEffect(() => { loadStaff() }, [search, statusFilter])

  const openCreate = () => {
    setEditing(null)
    form.resetFields()
    setModalOpen(true)
  }

  const openEdit = (record) => {
    setEditing(record)
    form.setFieldsValue({
      staffName:    record.staffName,
      employeeCode: record.employeeCode || '',
      designation:  record.designation  || '',
      department:   record.department   || '',
      mobile:       record.mobile       || '',
      email:        record.email        || '',
      joinDate:     record.joinDate ? dayjs(record.joinDate, 'DD-MM-YYYY') : null,
    })
    setModalOpen(true)
  }

  const handleSave = async () => {
    let values
    try { values = await form.validateFields() } catch { return }
    setSaving(true)
    try {
      const payload = {
        ...values,
        joinDate: values.joinDate ? values.joinDate.format('YYYY-MM-DD') : null,
      }
      if (editing) {
        await api.put(`/school/staff/${editing.staffId}`, payload)
        message.success('Staff member updated.')
      } else {
        await api.post('/school/staff', payload)
        message.success('Staff member added.')
      }
      setModalOpen(false)
      loadStaff()
    } catch (e) { message.error(apiError(e, 'Failed to save.')) }
    finally { setSaving(false) }
  }

  const handleToggleStatus = async (record) => {
    const newStatus = record.status === 'Active' ? 'Inactive' : 'Active'
    try {
      await api.put(`/school/staff/${record.staffId}/status`, { status: newStatus })
      message.success(`Staff member ${newStatus === 'Active' ? 'activated' : 'deactivated'}.`)
      loadStaff()
    } catch (e) { message.error(apiError(e, 'Failed to update status.')) }
  }

  const columns = [
    { title: 'Emp Code',    dataIndex: 'employeeCode', width: 100,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Name',        dataIndex: 'staffName',    width: 200 },
    { title: 'Designation', dataIndex: 'designation',  width: 140,
      render: v => v ? <Tag color="blue">{v}</Tag> : <Text type="secondary">—</Text> },
    { title: 'Department',  dataIndex: 'department',   width: 130,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Mobile',      dataIndex: 'mobile',       width: 120,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Join Date',   dataIndex: 'joinDate',     width: 110,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Status',      dataIndex: 'status',       width: 90,
      render: v => <Tag color={v === 'Active' ? 'success' : 'default'}>{v}</Tag> },
    {
      title: 'Actions', width: 130, align: 'center',
      render: (_, record) => (
        <Space size={4}>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(record)}>Edit</Button>
          <Popconfirm
            title={`${record.status === 'Active' ? 'Deactivate' : 'Activate'} this staff member?`}
            onConfirm={() => handleToggleStatus(record)}
            okText="Yes"
          >
            <Button size="small" danger={record.status === 'Active'}>
              {record.status === 'Active' ? 'Deactivate' : 'Activate'}
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <Row justify="space-between" align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Space>
            <Search
              placeholder="Search name / code"
              style={{ width: 220 }}
              onSearch={setSearch}
              onChange={e => !e.target.value && setSearch('')}
              allowClear
            />
            <Select
              style={{ width: 120 }}
              value={statusFilter}
              onChange={setStatusFilter}
              options={[
                { value: '',         label: 'All Status' },
                { value: 'Active',   label: 'Active'     },
                { value: 'Inactive', label: 'Inactive'   },
              ]}
            />
          </Space>
        </Col>
        <Col>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Add Staff
          </Button>
        </Col>
      </Row>

      <Table
        rowKey="staffId"
        dataSource={staff}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 20, showTotal: t => `${t} staff` }}
        scroll={{ x: 'max-content' }}
      />

      <Modal
        title={
          <Space>
            <UserOutlined />
            {editing ? 'Edit Staff Member' : 'Add Staff Member'}
          </Space>
        }
        open={modalOpen}
        onOk={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLoading={saving}
        width={560}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Row gutter={12}>
            <Col span={14}>
              <Form.Item name="staffName" label="Full Name" rules={[{ required: true, message: 'Name is required' }]}>
                <Input placeholder="e.g. Ramesh Kumar" />
              </Form.Item>
            </Col>
            <Col span={10}>
              <Form.Item name="employeeCode" label="Employee Code">
                <Input placeholder="e.g. EMP001" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="designation" label="Designation">
                <Select
                  placeholder="Select or type"
                  showSearch
                  allowClear
                  options={DESIGNATIONS.map(d => ({ label: d, value: d }))}
                  filterOption={(input, opt) => opt.label.toLowerCase().includes(input.toLowerCase())}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="department" label="Department">
                <Input placeholder="e.g. Science, Admin" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="mobile" label="Mobile">
                <Input placeholder="10-digit mobile" maxLength={15} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="joinDate" label="Join Date">
                <DatePicker style={{ width: '100%' }} format="DD-MM-YYYY" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="email" label="Email">
            <Input placeholder="email@example.com" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
