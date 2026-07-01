import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, InputNumber, Select, Tag } from 'antd'
import { PlusOutlined, EditOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

// Exam types are managed per academic year (used by Marks Entry + exam reports).
// Backend: GET/POST/PUT /school/marks/exam-types  (DTO: ExamTypeName, AcademicYearId, DisplayOrder)
export default function ExamTypesTab() {
  const [rows,          setRows]          = useState([])
  const [academicYears, setAcademicYears] = useState([])
  const [yearId,        setYearId]        = useState(null)
  const [loading,       setLoading]       = useState(false)
  const [open,          setOpen]          = useState(false)
  const [editing,       setEditing]       = useState(null)
  const [saving,        setSaving]        = useState(false)
  const [form] = Form.useForm()

  const yearOptions = academicYears.map((y) => ({
    value: y.academicYearId,
    label: y.academicYear,
  }))

  async function load(aYearId) {
    setLoading(true)
    try {
      const res = await api.get(`/school/marks/exam-types${aYearId ? `?academicYearId=${aYearId}` : ''}`)
      setRows(res.data?.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true')
       .then((r) => {
         const years = r.data?.data || []
         setAcademicYears(years)
         const current = years.find((y) => y.isCurrent)
         if (current) onYearChange(current.academicYearId)
         else load(null)
       })
       .catch(() => {})
  }, [])

  function onYearChange(val) {
    setYearId(val)
    load(val)
  }

  function openCreate() {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ academicYearId: yearId, displayOrder: (rows.length + 1) })
    setOpen(true)
  }

  function openEdit(record) {
    setEditing(record)
    form.setFieldsValue({
      academicYearId: record.academicYearId,
      examTypeName:   record.examTypeName,
      displayOrder:   record.displayOrder,
    })
    setOpen(true)
  }

  async function handleSave() {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        ExamTypeName:   values.examTypeName,
        AcademicYearId: values.academicYearId,
        DisplayOrder:   values.displayOrder || 0,
      }
      if (editing) {
        await api.put(`/school/marks/exam-types/${editing.examTypeId}`, body)
      } else {
        await api.post('/school/marks/exam-types', body)
      }
      setOpen(false)
      // Reload the list scoped to the year the saved row belongs to
      onYearChange(values.academicYearId ?? yearId)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Order', dataIndex: 'displayOrder', key: 'displayOrder', width: 80 },
    { title: 'Exam Type', dataIndex: 'examTypeName', key: 'examTypeName' },
    {
      title: 'Status', dataIndex: 'status', key: 'status', width: 100,
      render: (v) => <Tag color={v === 'Active' ? 'green' : 'default'}>{v}</Tag>,
    },
    {
      title: '', key: 'actions', width: 60,
      render: (_, record) => (
        <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(record)} />
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'center' }}>
        <Select
          placeholder="Filter by academic year"
          options={yearOptions}
          style={{ width: 200 }}
          value={yearId}
          onChange={onYearChange}
          allowClear
          onClear={() => onYearChange(null)}
        />
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
          Add Exam Type
        </Button>
      </div>

      <Table
        rowKey="examTypeId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={false}
        locale={{ emptyText: yearId ? 'No exam types yet for this year.' : 'Select a year or add exam types.' }}
      />

      <Modal
        title={editing ? 'Edit Exam Type' : 'Add Exam Type'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item
            name="academicYearId"
            label="Academic Year"
            rules={[{ required: true, message: 'Academic year is required.' }]}
          >
            <Select options={yearOptions} placeholder="Select year" />
          </Form.Item>
          <Form.Item
            name="examTypeName"
            label="Exam Type Name"
            rules={[{ required: true, message: 'Name is required.' }]}
          >
            <Input placeholder="e.g. FA 1, SA 1, Term Exam" maxLength={50} />
          </Form.Item>
          <Form.Item name="displayOrder" label="Display Order">
            <InputNumber min={0} style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
