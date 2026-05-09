import { useEffect, useState } from 'react'
import {
  Card, Table, Button, Modal, Form, Input, Select,
  Popconfirm, Tag, Switch, Space, Typography, App as AntApp, DatePicker,
} from 'antd'
import {
  PlusOutlined, EditOutlined, DeleteOutlined,
  PushpinOutlined, PlayCircleOutlined, PictureOutlined, PaperClipOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Title, Text, Link } = Typography
const { TextArea } = Input

export default function EventsPage() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()

  const [events,   setEvents]   = useState([])
  const [classes,  setClasses]  = useState([])
  const [loading,  setLoading]  = useState(false)
  const [modal,    setModal]    = useState({ open: false, editing: null })
  const [saving,   setSaving]   = useState(false)
  const [scope,    setScope]    = useState('School')
  const [mediaType, setMediaType] = useState('image')

  const load = async () => {
    setLoading(true)
    try {
      const r = await api.get('/school/events')
      setEvents(r.data.data || [])
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
    form.setFieldsValue({ scope: 'School', mediaType: 'image', isPinned: false })
    setScope('School')
    setMediaType('image')
    setModal({ open: true, editing: null })
  }

  const openEdit = (record) => {
    form.setFieldsValue({
      title:        record.title,
      description:  record.description,
      eventDate:    record.eventDate ? dayjs(record.eventDate) : null,
      mediaType:    record.mediaType,
      mediaUrl:     record.mediaUrl,
      thumbnailUrl:  record.thumbnailUrl,
      attachmentUrl: record.attachmentUrl,
      scope:         record.scope,
      classId:      record.classId,
      isPinned:     record.isPinned,
    })
    setScope(record.scope)
    setMediaType(record.mediaType)
    setModal({ open: true, editing: record })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    // Convert DatePicker value to ISO date string
    const payload = {
      ...values,
      eventDate: values.eventDate ? values.eventDate.format('YYYY-MM-DD') : null,
    }
    setSaving(true)
    try {
      if (modal.editing) {
        await api.put(`/school/events/${modal.editing.eventId}`, payload)
        message.success('Event updated.')
      } else {
        await api.post('/school/events', payload)
        message.success('Event created.')
      }
      setModal({ open: false, editing: null })
      load()
    } catch (e) {
      message.error(e.message || 'Failed to save event.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (id) => {
    try {
      await api.delete(`/school/events/${id}`)
      message.success('Event deleted.')
      load()
    } catch (e) {
      message.error('Failed to delete event.')
    }
  }

  const columns = [
    {
      title: '',
      key:   'pin',
      width: 32,
      render: (_, r) => r.isPinned ? <PushpinOutlined style={{ color: '#faad14' }} /> : null,
    },
    {
      title:     'Title',
      dataIndex: 'title',
      key:       'title',
      render: (t, r) => (
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            {r.mediaType === 'video'
              ? <PlayCircleOutlined style={{ color: '#ff4d4f' }} />
              : <PictureOutlined   style={{ color: '#1677ff' }} />}
            <span style={{ fontWeight: 500 }}>{t}</span>
          </div>
          {r.description && (
            <Text type="secondary" style={{ fontSize: 12 }}>
              {r.description.slice(0, 80)}{r.description.length > 80 ? '…' : ''}
            </Text>
          )}
        </div>
      ),
    },
    {
      title:     'Date',
      dataIndex: 'eventDate',
      key:       'eventDate',
      width:     120,
      render:    d => d ? dayjs(d).format('DD MMM YYYY') : '—',
    },
    {
      title:  'Scope',
      key:    'scope',
      width:  110,
      render: (_, r) => r.scope === 'Class'
        ? <Tag color="blue">{r.className || 'Class'}</Tag>
        : <Tag>School</Tag>,
    },
    {
      title:  'Media',
      key:    'media',
      width:  70,
      render: (_, r) => (
        <Link href={r.mediaUrl} target="_blank" rel="noopener noreferrer">
          Open
        </Link>
      ),
    },
    {
      title:  'Attachment',
      key:    'attachment',
      width:  90,
      render: (_, r) => r.attachmentUrl
        ? <Link href={r.attachmentUrl} target="_blank" rel="noopener noreferrer">
            <PaperClipOutlined /> PDF/Doc
          </Link>
        : null,
    },
    {
      title:  'Actions',
      key:    'actions',
      width:  90,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Delete this event?" onConfirm={() => handleDelete(r.eventId)}>
            <Button size="small" icon={<DeleteOutlined />} danger />
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>School Events</Title>

      <Card
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            New Event
          </Button>
        }
      >
        <Table
          dataSource={events}
          columns={columns}
          rowKey="eventId"
          loading={loading}
          pagination={{ pageSize: 20 }}
        />
      </Card>

      <Modal
        title={modal.editing ? 'Edit Event' : 'New Event'}
        open={modal.open}
        onOk={handleSave}
        onCancel={() => setModal({ open: false, editing: null })}
        confirmLoading={saving}
        width={540}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="title" label="Title" rules={[{ required: true }]}>
            <Input maxLength={200} />
          </Form.Item>

          <Form.Item name="description" label="Description">
            <TextArea rows={3} maxLength={1000} showCount />
          </Form.Item>

          <Form.Item name="eventDate" label="Event Date" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item name="mediaType" label="Media Type" rules={[{ required: true }]}>
            <Select
              options={[
                { label: 'Image (Cloudinary / Google Drive)', value: 'image' },
                { label: 'Video (YouTube)',                   value: 'video' },
              ]}
              onChange={v => setMediaType(v)}
            />
          </Form.Item>

          <Form.Item
            name="mediaUrl"
            label={mediaType === 'video' ? 'YouTube URL' : 'Image URL (Cloudinary)'}
            rules={[{ required: true, message: 'Media URL is required' }]}
            extra={
              mediaType === 'video'
                ? 'Paste the YouTube video link (e.g. https://youtu.be/xxxx)'
                : 'Paste the Cloudinary or Google Drive direct image link'
            }
          >
            <Input placeholder={mediaType === 'video' ? 'https://youtu.be/...' : 'https://res.cloudinary.com/...'} />
          </Form.Item>

          <Form.Item
            name="thumbnailUrl"
            label="Thumbnail URL (optional)"
            extra="Leave blank — for images the media URL is used as thumbnail; for videos a default YouTube thumbnail is shown."
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="attachmentUrl"
            label="Attachment URL (optional)"
            extra="Paste a Google Drive or Cloudinary link to a PDF/doc. Users can download and view it."
          >
            <Input placeholder="https://drive.google.com/file/d/..." />
          </Form.Item>

          <Form.Item name="scope" label="Audience" rules={[{ required: true }]}>
            <Select
              options={[
                { label: 'Entire school', value: 'School' },
                { label: 'Specific class', value: 'Class' },
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
        </Form>
      </Modal>
    </div>
  )
}
