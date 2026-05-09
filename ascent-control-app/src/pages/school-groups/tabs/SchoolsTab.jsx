import { Table, Button, Modal, Form, Input, Tag, Typography } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import { useState, useEffect } from 'react'
import api from '../../../api/axiosInstance'

export default function SchoolsTab({ groupId }) {
  const [schools, setSchools] = useState([])
  const [loading, setLoading] = useState(false)
  const [open,    setOpen]    = useState(false)
  const [editing, setEditing] = useState(null)
  const [saving,  setSaving]  = useState(false)
  const [form]                = Form.useForm()

  useEffect(() => { fetchSchools() }, [groupId])

  const fetchSchools = async () => {
    setLoading(true)
    try {
      const res = await api.get(`/control/school-groups/${groupId}/schools`)
      setSchools(res.data.data || [])
    } finally { setLoading(false) }
  }

  const openCreate = () => { setEditing(null); form.resetFields(); setOpen(true) }
  const openEdit   = (row) => { setEditing(row); form.setFieldsValue(row); setOpen(true) }

  const handleSave = async (values) => {
    setSaving(true)
    try {
      if (editing) {
        await api.put(`/control/school-groups/${groupId}/schools/${editing.schoolId}`, values)
      } else {
        await api.post(`/control/school-groups/${groupId}/schools`, values)
      }
      setOpen(false)
      fetchSchools()
    } catch (err) {
      Modal.error({ title: 'Error', content: err.response?.data?.message || 'Save failed.' })
    } finally { setSaving(false) }
  }

  const columns = [
    { title: 'ID',       dataIndex: 'schoolId',   width: 60 },
    { title: 'Name',     dataIndex: 'schoolName' },
    { title: 'Caption',  dataIndex: 'schoolCaption' },
    { title: 'City',     dataIndex: 'city' },
    { title: 'Mobile',   dataIndex: 'mobile' },
    { title: 'Email',    dataIndex: 'email' },
    { title: 'Status',   dataIndex: 'status', render: v => <Tag color={v === 'Active' ? 'green' : 'red'}>{v}</Tag> },
    { title: '', render: (_, r) => <Button size="small" onClick={() => openEdit(r)}>Edit</Button> },
  ]

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '16px 0' }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add School</Button>
      </div>

      <Table rowKey="schoolId" dataSource={schools} columns={columns} loading={loading} pagination={false} />

      <Modal
        title={editing ? 'Edit School' : 'Add School'}
        open={open}
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={saving}
      >
        <Form form={form} layout="vertical" onFinish={handleSave} style={{ marginTop: 16 }}>
          <Form.Item name="schoolName" label="School Name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="schoolCaption" label="Caption / Short Name">
            <Input />
          </Form.Item>
          <Form.Item name="address" label="Address">
            <Input.TextArea rows={2} />
          </Form.Item>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="city" label="City"><Input /></Form.Item>
            <Form.Item name="state" label="State"><Input /></Form.Item>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="mobile" label="Mobile"><Input /></Form.Item>
            <Form.Item name="email" label="Email"><Input /></Form.Item>
          </div>
          {editing && (
            <Form.Item name="status" label="Status">
              <Input placeholder="Active / Inactive" />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </>
  )
}
