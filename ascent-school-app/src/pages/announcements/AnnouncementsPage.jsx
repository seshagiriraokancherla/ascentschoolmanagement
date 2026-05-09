import { useEffect, useState } from 'react'
import {
  Card, Table, Button, Modal, Form, Input, Select,
  Popconfirm, Tag, Switch, Space, Typography, App as AntApp,
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, PushpinOutlined, PaperClipOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography
const { TextArea } = Input

export default function AnnouncementsPage() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()

  const [announcements, setAnnouncements] = useState([])
  const [classes,       setClasses]       = useState([])
  const [loading,       setLoading]       = useState(false)
  const [modal,         setModal]         = useState({ open: false, editing: null })
  const [saving,        setSaving]        = useState(false)
  const [scope,         setScope]         = useState('School')

  const load = async () => {
    setLoading(true)
    try {
      const r = await api.get('/school/announcements')
      setAnnouncements(r.data.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data.data || []))
    load()
  }, [])

  const openCreate = () => {
    form.resetFields()
    form.setFieldsValue({ scope: 'School', isPinned: false })
    setScope('School')
    setModal({ open: true, editing: null })
  }

  const openEdit = (record) => {
    form.setFieldsValue({
      title:         record.title,
      description:   record.description,
      scope:         record.scope,
      classId:       record.classId,
      isPinned:      record.isPinned,
      attachmentUrl: record.attachmentUrl,
    })
    setScope(record.scope)
    setModal({ open: true, editing: record })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      if (modal.editing) {
        await api.put(`/school/announcements/${modal.editing.announcementId}`, values)
        message.success('Announcement updated.')
      } else {
        await api.post('/school/announcements', values)
        message.success('Announcement created.')
      }
      setModal({ open: false, editing: null })
      load()
    } catch (e) {
      message.error(e.message || 'Failed to save announcement.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (id) => {
    try {
      await api.delete(`/school/announcements/${id}`)
      message.success('Announcement deleted.')
      load()
    } catch (e) {
      message.error('Failed to delete.')
    }
  }

  const columns = [
    {
      title: '',
      key: 'pin',
      width: 32,
      render: (_, r) => r.isPinned ? <PushpinOutlined style={{ color: '#faad14' }} /> : null,
    },
    {
      title: 'Title',
      dataIndex: 'title',
      key: 'title',
      render: (t, r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{t}</div>
          {r.description && (
            <Text type="secondary" style={{ fontSize: 12 }}>
              {r.description.slice(0, 100)}{r.description.length > 100 ? '…' : ''}
            </Text>
          )}
        </div>
      ),
    },
    {
      title: 'Scope',
      dataIndex: 'scope',
      key: 'scope',
      width: 100,
      render: (s, r) => s === 'Class' ? <Tag color="blue">{r.className || 'Class'}</Tag> : <Tag>School</Tag>,
    },
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 120,
      render: d => dayjs(d).format('DD MMM YYYY'),
    },
    {
      title:  'Attachment',
      key:    'attachment',
      width:  100,
      render: (_, r) => r.attachmentUrl
        ? <a href={r.attachmentUrl} target="_blank" rel="noopener noreferrer">
            <PaperClipOutlined /> PDF/Doc
          </a>
        : null,
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 100,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EditOutlined />}   onClick={() => openEdit(r)} />
          <Popconfirm title="Delete this announcement?" onConfirm={() => handleDelete(r.announcementId)}>
            <Button size="small" icon={<DeleteOutlined />} danger />
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Announcements</Title>

      <Card
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            New Announcement
          </Button>
        }
      >
        <Table
          dataSource={announcements}
          columns={columns}
          rowKey="announcementId"
          loading={loading}
          pagination={{ pageSize: 20 }}
        />
      </Card>

      <Modal
        title={modal.editing ? 'Edit Announcement' : 'New Announcement'}
        open={modal.open}
        onOk={handleSave}
        onCancel={() => setModal({ open: false, editing: null })}
        confirmLoading={saving}
        width={520}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="title" label="Title" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <TextArea rows={4} />
          </Form.Item>
          <Form.Item name="scope" label="Scope" rules={[{ required: true }]}>
            <Select
              options={[
                { label: 'School-wide', value: 'School' },
                { label: 'Specific Class', value: 'Class' },
              ]}
              onChange={v => {
                setScope(v)
                if (v === 'School') form.setFieldValue('classId', null)
              }}
            />
          </Form.Item>
          {scope === 'Class' && (
            <Form.Item name="classId" label="Class" rules={[{ required: true }]}>
              <Select
                placeholder="Select class"
                options={classes.map(c => ({ label: c.className, value: c.classId }))}
              />
            </Form.Item>
          )}
          <Form.Item name="isPinned" label="Pin to top" valuePropName="checked">
            <Switch />
          </Form.Item>
          <Form.Item
            name="attachmentUrl"
            label="Attachment URL (optional)"
            extra="Google Drive or Cloudinary link to a PDF/doc. Users can download and view it."
          >
            <Input placeholder="https://drive.google.com/file/d/..." />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
