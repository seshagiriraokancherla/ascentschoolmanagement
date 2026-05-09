import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, Select, Switch, Tag, Space } from 'antd'
import { PlusOutlined, EditOutlined, WifiOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

export default function PaymentModesTab() {
  const [rows,    setRows]    = useState([])
  const [loading, setLoading] = useState(false)
  const [open,    setOpen]    = useState(false)
  const [editing, setEditing] = useState(null)
  const [saving,  setSaving]  = useState(false)
  const [form] = Form.useForm()

  const load = async () => {
    setLoading(true)
    try {
      const res = await api.get('/school/master/payment-modes')
      setRows(res.data?.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const openCreate = () => {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ status: 'Active', isOnline: false })
    setOpen(true)
  }

  const openEdit = (record) => {
    setEditing(record)
    form.setFieldsValue({
      modeName:    record.modeName,
      description: record.description,
      status:      record.status,
      isOnline:    record.isOnline ?? false,
    })
    setOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        ModeName:    values.modeName,
        Description: values.description,
        Status:      values.status,
        IsOnline:    values.isOnline ?? false,
      }
      let res
      if (editing) {
        res = await api.put(`/school/master/payment-modes/${editing.paymentModeId}`, body)
      } else {
        res = await api.post('/school/master/payment-modes', body)
      }
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Mode Name',   dataIndex: 'modeName',    key: 'modeName' },
    { title: 'Description', dataIndex: 'description', key: 'description' },
    {
      title: 'Online',
      dataIndex: 'isOnline',
      key: 'isOnline',
      width: 90,
      render: (v) => v
        ? <Tag icon={<WifiOutlined />} color="blue">Gateway</Tag>
        : <Tag color="default">Offline</Tag>,
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
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Payment Mode</Button>
      </div>

      <Table
        rowKey="paymentModeId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 10 }}
      />

      <Modal
        title={editing ? 'Edit Payment Mode' : 'Add Payment Mode'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="modeName" label="Mode Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Cash, Cheque, Online" />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="status" label="Status">
            <Select options={STATUS_OPTIONS} />
          </Form.Item>
          <Form.Item
            name="isOnline"
            label="Is Online (Gateway)"
            valuePropName="checked"
            extra="Enable this to trigger the payment gateway checkout when this mode is selected during fee collection."
          >
            <Switch checkedChildren={<WifiOutlined />} unCheckedChildren="Off" />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
