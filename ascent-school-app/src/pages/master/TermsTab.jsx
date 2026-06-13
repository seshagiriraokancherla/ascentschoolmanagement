import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, Select, InputNumber, Tag } from 'antd'
import { PlusOutlined, EditOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

export default function TermsTab() {
  const [rows,         setRows]         = useState([])
  const [academicYears, setAcademicYears] = useState([])
  const [loading,      setLoading]      = useState(false)
  const [open,         setOpen]         = useState(false)
  const [editing,      setEditing]      = useState(null)
  const [saving,       setSaving]       = useState(false)
  const [form] = Form.useForm()

  const yearOptions = academicYears.map((y) => ({
    value: y.academicYearId,
    label: y.academicYear,
  }))

  const load = async () => {
    setLoading(true)
    try {
      const [dataRes, yearRes] = await Promise.all([
        api.get('/school/master/terms'),
        api.get('/school/master/academic-years?activeOnly=true'),
      ])
      setRows(dataRes.data?.data || [])
      setAcademicYears(yearRes.data?.data || [])
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
      termName:       record.termName,
      yearName:       record.yearName,
      orderNo:        record.orderNo,
      description:    record.description,
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
        TermName:       values.termName,
        YearName:       values.yearName,
        OrderNo:        values.orderNo        || null,
        Description:    values.description,
        AcademicYearId: values.academicYearId || null,
        Status:         values.status,
      }
      let res
      if (editing) {
        res = await api.put(`/school/master/terms/${editing.termId}`, body)
      } else {
        res = await api.post('/school/master/terms', body)
      }
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Order', dataIndex: 'orderNo',   key: 'orderNo', width: 60 },
    { title: 'Term Name',  dataIndex: 'termName',  key: 'termName' },
    { title: 'Year Name',  dataIndex: 'yearName',  key: 'yearName' },
    { title: 'Description', dataIndex: 'description', key: 'description' },
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
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Term</Button>
      </div>

      <Table
        rowKey="termId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 15 }}
      />

      <Modal
        title={editing ? 'Edit Term' : 'Add Term'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="termName" label="Term Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Term 1" />
          </Form.Item>
          <Form.Item name="yearName" label="Year Name">
            <Input placeholder="e.g. 2024-2025" />
          </Form.Item>
          <Form.Item name="academicYearId" label="Academic Year">
            <Select options={yearOptions} placeholder="Select year" allowClear />
          </Form.Item>
          <Form.Item name="orderNo" label="Order No">
            <InputNumber style={{ width: '100%' }} min={1} />
          </Form.Item>
          <Form.Item name="description" label="Description">
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
