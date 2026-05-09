import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, Select, Space, Tag } from 'antd'
import { PlusOutlined, EditOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

const FREQ_OPTIONS = [
  { value: 'Monthly',   label: 'Monthly' },
  { value: 'Quarterly', label: 'Quarterly' },
  { value: 'Yearly',    label: 'Yearly' },
]

const MONTHS = [
  'January','February','March','April','May','June',
  'July','August','September','October','November','December',
].map((m) => ({ value: m, label: m }))

export default function AcademicYearsTab() {
  const [rows,    setRows]    = useState([])
  const [loading, setLoading] = useState(false)
  const [open,    setOpen]    = useState(false)
  const [editing, setEditing] = useState(null)
  const [saving,  setSaving]  = useState(false)
  const [form] = Form.useForm()

  const load = async () => {
    setLoading(true)
    try {
      const res = await api.get('/school/master/academic-years')
      setRows(res.data?.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const openCreate = () => {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ status: 'Active', newAdmissionsEnabled: 'Y' })
    setOpen(true)
  }

  const openEdit = (record) => {
    setEditing(record)
    form.setFieldsValue({
      academicYear:             record.academicYear,
      startMonth:               record.startMonth,
      endMonth:                 record.endMonth,
      status:                   record.status,
      registrationFeeFrequency: record.registrationFeeFrequency,
      transportFeeFrequency:    record.transportFeeFrequency,
      hostelFeeFrequency:       record.hostelFeeFrequency,
      boardingType:             record.boardingType,
      newAdmissionsEnabled:     record.newAdmissionsEnabled,
    })
    setOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        AcademicYear:             values.academicYear,
        StartMonth:               values.startMonth,
        EndMonth:                 values.endMonth,
        Status:                   values.status,
        RegistrationFeeFrequency: values.registrationFeeFrequency,
        TransportFeeFrequency:    values.transportFeeFrequency,
        HostelFeeFrequency:       values.hostelFeeFrequency,
        BoardingType:             values.boardingType,
        NewAdmissionsEnabled:     values.newAdmissionsEnabled,
      }
      let res
      if (editing) {
        res = await api.put(`/school/master/academic-years/${editing.academicYearId}`, body)
      } else {
        res = await api.post('/school/master/academic-years', body)
      }
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Academic Year', dataIndex: 'academicYear', key: 'academicYear' },
    { title: 'Start Month',   dataIndex: 'startMonth',   key: 'startMonth' },
    { title: 'End Month',     dataIndex: 'endMonth',     key: 'endMonth' },
    {
      title: 'New Admissions',
      dataIndex: 'newAdmissionsEnabled',
      key: 'newAdmissionsEnabled',
      render: (v) => v === 'Y' ? <Tag color="green">Enabled</Tag> : <Tag>Disabled</Tag>,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (v) => <Tag color={v === 'Active' ? 'green' : 'default'}>{v}</Tag>,
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_, record) => (
        <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(record)}>Edit</Button>
      ),
    },
  ]

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Academic Year</Button>
      </div>

      <Table
        rowKey="academicYearId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 10 }}
      />

      <Modal
        title={editing ? 'Edit Academic Year' : 'Add Academic Year'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        width={600}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="academicYear" label="Academic Year" rules={[{ required: true }]}>
            <Input placeholder="e.g. 2024-2025" />
          </Form.Item>

          <Space style={{ width: '100%' }} size="large">
            <Form.Item name="startMonth" label="Start Month" style={{ flex: 1 }}>
              <Select options={MONTHS} placeholder="Select month" style={{ width: 180 }} />
            </Form.Item>
            <Form.Item name="endMonth" label="End Month" style={{ flex: 1 }}>
              <Select options={MONTHS} placeholder="Select month" style={{ width: 180 }} />
            </Form.Item>
          </Space>

          <Space style={{ width: '100%' }} size="large">
            <Form.Item name="registrationFeeFrequency" label="Registration Fee Frequency" style={{ flex: 1 }}>
              <Select options={FREQ_OPTIONS} placeholder="Select" style={{ width: 180 }} allowClear />
            </Form.Item>
            <Form.Item name="transportFeeFrequency" label="Transport Fee Frequency" style={{ flex: 1 }}>
              <Select options={FREQ_OPTIONS} placeholder="Select" style={{ width: 180 }} allowClear />
            </Form.Item>
          </Space>

          <Space style={{ width: '100%' }} size="large">
            <Form.Item name="hostelFeeFrequency" label="Hostel Fee Frequency" style={{ flex: 1 }}>
              <Select options={FREQ_OPTIONS} placeholder="Select" style={{ width: 180 }} allowClear />
            </Form.Item>
            <Form.Item name="boardingType" label="Boarding Type" style={{ flex: 1 }}>
              <Input style={{ width: 180 }} placeholder="Day / Residential" />
            </Form.Item>
          </Space>

          <Space style={{ width: '100%' }} size="large">
            <Form.Item name="newAdmissionsEnabled" label="New Admissions" style={{ flex: 1 }}>
              <Select style={{ width: 150 }} options={[{ value: 'Y', label: 'Enabled' }, { value: 'N', label: 'Disabled' }]} />
            </Form.Item>
            <Form.Item name="status" label="Status" style={{ flex: 1 }}>
              <Select style={{ width: 150 }} options={STATUS_OPTIONS} />
            </Form.Item>
          </Space>
        </Form>
      </Modal>
    </>
  )
}
