import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, Select, Tag } from 'antd'
import { PlusOutlined, EditOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

export default function FeeCategoriesTab() {
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
      const [catRes, yearRes] = await Promise.all([
        api.get('/school/master/fee-categories'),
        api.get('/school/master/academic-years?activeOnly=true'),
      ])
      setRows(catRes.data?.data || [])
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
      categoryName:   record.categoryName,
      academicYearId: record.academicYearId,
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
        CategoryName:   values.categoryName,
        AcademicYearId: values.academicYearId || null,
        Description:    values.description,
        Status:         values.status,
      }
      let res
      if (editing) {
        res = await api.put(`/school/master/fee-categories/${editing.feeCategoryId}`, body)
      } else {
        res = await api.post('/school/master/fee-categories', body)
      }
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Category Name',  dataIndex: 'categoryName',  key: 'categoryName' },
    { title: 'Description',    dataIndex: 'description',   key: 'description' },
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
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Fee Category</Button>
      </div>

      <Table
        rowKey="feeCategoryId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 10 }}
      />

      <Modal
        title={editing ? 'Edit Fee Category' : 'Add Fee Category'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="categoryName" label="Category Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. General" />
          </Form.Item>
          <Form.Item name="academicYearId" label="Academic Year">
            <Select options={yearOptions} placeholder="All years" allowClear />
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
