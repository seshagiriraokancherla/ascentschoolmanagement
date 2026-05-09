import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, InputNumber, Select, Tag } from 'antd'
import { PlusOutlined, EditOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

export default function SectionsTab({ classes }) {
  const [classId,  setClassId]  = useState(null)
  const [rows,     setRows]     = useState([])
  const [loading,  setLoading]  = useState(false)
  const [open,     setOpen]     = useState(false)
  const [editing,  setEditing]  = useState(null)
  const [saving,   setSaving]   = useState(false)
  const [form] = Form.useForm()

  const classOptions = (classes || []).map((c) => ({
    value: c.classId,
    label: c.className,
  }))

  async function load(cId) {
    if (!cId) { setRows([]); return }
    setLoading(true)
    try {
      const { data } = await api.get(`/school/master/sections?classId=${cId}`)
      setRows(data.data || [])
    } finally {
      setLoading(false)
    }
  }

  function onClassChange(val) {
    setClassId(val)
    load(val)
  }

  function openCreate() {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ status: 'Active' })
    setOpen(true)
  }

  function openEdit(record) {
    setEditing(record)
    form.setFieldsValue({
      sectionName: record.sectionName,
      description: record.description,
      sequenceNo:  record.sequenceNo,
      status:      record.status,
    })
    setOpen(true)
  }

  async function onSave() {
    const vals = await form.validateFields()
    setSaving(true)
    try {
      const body = { classId, ...vals }
      if (editing) {
        await api.put(`/school/master/sections/${editing.sectionId}`, body)
      } else {
        await api.post('/school/master/sections', body)
      }
      setOpen(false)
      load(classId)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Section',     dataIndex: 'sectionName', key: 'sectionName' },
    { title: 'Description', dataIndex: 'description', key: 'description' },
    { title: 'Order',       dataIndex: 'sequenceNo',  key: 'sequenceNo', width: 80 },
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
          placeholder="Select a class"
          options={classOptions}
          style={{ width: 220 }}
          value={classId}
          onChange={onClassChange}
        />
        <Button
          type="primary"
          icon={<PlusOutlined />}
          disabled={!classId}
          onClick={openCreate}
        >
          Add Section
        </Button>
      </div>

      <Table
        rowKey="sectionId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        pagination={false}
        locale={{ emptyText: classId ? 'No sections yet for this class.' : 'Select a class to view sections.' }}
      />

      <Modal
        title={editing ? 'Edit Section' : 'Add Section'}
        open={open}
        onOk={onSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        okText={editing ? 'Save' : 'Create'}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item
            name="sectionName"
            label="Section Name"
            rules={[{ required: true, message: 'Section name is required.' }]}
          >
            <Input placeholder="e.g. A, B, C" maxLength={10} />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input maxLength={50} />
          </Form.Item>
          <Form.Item name="sequenceNo" label="Display Order">
            <InputNumber min={1} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="status" label="Status" rules={[{ required: true }]}>
            <Select options={STATUS_OPTIONS} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
