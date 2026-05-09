import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, Select, Tag } from 'antd'
import { PlusOutlined, EditOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

const TYPE_OPTIONS = [
  { value: 'Theory',    label: 'Theory' },
  { value: 'Practical', label: 'Practical' },
  { value: 'Language',  label: 'Language' },
  { value: 'Activity',  label: 'Activity' },
]

export default function SubjectsTab({ academicYears }) {
  const [rows,    setRows]    = useState([])
  const [loading, setLoading] = useState(false)
  const [open,    setOpen]    = useState(false)
  const [editing, setEditing] = useState(null)
  const [saving,  setSaving]  = useState(false)
  const [form] = Form.useForm()

  const yearOptions = (academicYears || []).map((y) => ({
    value: y.academicYearId,
    label: y.academicYear,
  }))

  const load = async () => {
    setLoading(true)
    try {
      const res = await api.get('/school/master/subjects')
      setRows(res.data?.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const openCreate = () => {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ status: 'Active' })
    setOpen(true)
  }

  const openEdit = (record) => {
    setEditing(record)
    form.setFieldsValue({
      subjectName:    record.subjectName,
      shortName:      record.shortName,
      subjectType:    record.subjectType,
      description:    record.description,
      remarks:        record.remarks,
      academicYearId: record.academicYearId,
      status:         record.status,
    })
    setOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        SubjectName:    values.subjectName,
        ShortName:      values.shortName,
        SubjectType:    values.subjectType,
        Description:    values.description,
        Remarks:        values.remarks,
        AcademicYearId: values.academicYearId || null,
        Status:         values.status,
      }
      let res
      if (editing) {
        res = await api.put(`/school/master/subjects/${editing.subjectId}`, body)
      } else {
        res = await api.post('/school/master/subjects', body)
      }
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Subject Name', dataIndex: 'subjectName', key: 'subjectName' },
    { title: 'Short Name',   dataIndex: 'shortName',   key: 'shortName', width: 100 },
    { title: 'Type',         dataIndex: 'subjectType', key: 'subjectType', width: 100 },
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
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Subject</Button>
      </div>

      <Table
        rowKey="subjectId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 15 }}
      />

      <Modal
        title={editing ? 'Edit Subject' : 'Add Subject'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="subjectName" label="Subject Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Mathematics" />
          </Form.Item>
          <Form.Item name="shortName" label="Short Name">
            <Input placeholder="e.g. MATH" />
          </Form.Item>
          <Form.Item name="subjectType" label="Subject Type">
            <Select options={TYPE_OPTIONS} placeholder="Select type" allowClear />
          </Form.Item>
          <Form.Item name="academicYearId" label="Academic Year">
            <Select options={yearOptions} placeholder="All years" allowClear />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="remarks" label="Remarks">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="status" label="Status">
            <Select options={STATUS_OPTIONS} />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
