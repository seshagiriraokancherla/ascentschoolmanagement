import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, Select, InputNumber, Tag } from 'antd'
import { PlusOutlined, EditOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

export default function FeeTypesTab() {
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
        api.get('/school/master/fee-types'),
        api.get('/school/master/academic-years'),
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
      feeTypeName:    record.feeTypeName,
      academicYearId: record.academicYearId,
      sequenceNo:     record.sequenceNo,
      description:    record.description,
      status:         record.status,
    })
    setOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        FeeTypeName:    values.feeTypeName,
        AcademicYearId: values.academicYearId || null,
        SequenceNo:     values.sequenceNo     || null,
        Description:    values.description,
        Status:         values.status,
      }
      let res
      if (editing) {
        res = await api.put(`/school/master/fee-types/${editing.feeTypeId}`, body)
      } else {
        res = await api.post('/school/master/fee-types', body)
      }
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Seq', dataIndex: 'sequenceNo', key: 'sequenceNo', width: 50 },
    { title: 'Fee Type Name', dataIndex: 'feeTypeName',  key: 'feeTypeName' },
    { title: 'Description',   dataIndex: 'description',  key: 'description' },
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
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Fee Type</Button>
      </div>

      <Table
        rowKey="feeTypeId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 10 }}
      />

      <Modal
        title={editing ? 'Edit Fee Type' : 'Add Fee Type'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="feeTypeName" label="Fee Type Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Tuition Fee" />
          </Form.Item>
          <Form.Item name="academicYearId" label="Academic Year">
            <Select options={yearOptions} placeholder="All years" allowClear />
          </Form.Item>
          <Form.Item name="sequenceNo" label="Sequence No">
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
