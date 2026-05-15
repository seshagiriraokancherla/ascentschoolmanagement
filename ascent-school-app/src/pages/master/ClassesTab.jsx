import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, Select, InputNumber, Tag } from 'antd'
import { PlusOutlined, EditOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

export default function ClassesTab() {
  const [rows,          setRows]          = useState([])
  const [classGroups,   setClassGroups]   = useState([])
  const [feeCategories, setFeeCategories] = useState([])
  const [loading,       setLoading]       = useState(false)
  const [open,          setOpen]          = useState(false)
  const [editing,       setEditing]       = useState(null)
  const [saving,        setSaving]        = useState(false)
  const [form] = Form.useForm()

  const groupOptions    = classGroups.map((g)   => ({ value: g.classGroupId,  label: g.groupName }))
  const categoryOptions = feeCategories.map((c) => ({ value: c.feeCategoryId, label: c.categoryName }))

  const load = async () => {
    setLoading(true)
    try {
      const [classRes, groupRes, catRes] = await Promise.all([
        api.get('/school/master/classes'),
        api.get('/school/master/class-groups'),
        api.get('/school/master/fee-categories'),
      ])
      setRows(classRes.data?.data || [])
      setClassGroups(groupRes.data?.data || [])
      setFeeCategories(catRes.data?.data || [])
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
      className:     record.className,
      branchName:    record.branchName,
      description:   record.description,
      status:        record.status,
      classGroupId:  record.classGroupId,
      feeCategoryId: record.feeCategoryId,
      sequenceNo:    record.sequenceNo,
    })
    setOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        ClassName:     values.className,
        BranchName:    values.branchName,
        Description:   values.description,
        Status:        values.status,
        ClassGroupId:  values.classGroupId  || null,
        FeeCategoryId: values.feeCategoryId || null,
        SequenceNo:    values.sequenceNo    || null,
      }
      let res
      if (editing) {
        res = await api.put(`/school/master/classes/${editing.classId}`, body)
      } else {
        res = await api.post('/school/master/classes', body)
      }
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Seq', dataIndex: 'sequenceNo', key: 'sequenceNo', width: 50 },
    { title: 'Class Name',   dataIndex: 'className',      key: 'className' },
    { title: 'Branch Name',  dataIndex: 'branchName',     key: 'branchName' },
    { title: 'Class Group',  dataIndex: 'classGroupName', key: 'classGroupName' },
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
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Class</Button>
      </div>

      <Table
        rowKey="classId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 15 }}
      />

      <Modal
        title={editing ? 'Edit Class' : 'Add Class'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="className" label="Class Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Grade 1" />
          </Form.Item>
          <Form.Item name="branchName" label="Branch Name">
            <Input placeholder="e.g. A" />
          </Form.Item>
          <Form.Item name="classGroupId" label="Class Group">
            <Select options={groupOptions} placeholder="Select group" allowClear />
          </Form.Item>
          <Form.Item name="feeCategoryId" label="Default Fee Category">
            <Select options={categoryOptions} placeholder="Select category" allowClear />
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
