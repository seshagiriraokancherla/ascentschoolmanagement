import { useEffect, useState } from 'react'
import {
  Card, Table, Button, Modal, Form, Input, Select,
  Popconfirm, Tag, Switch, Space, Typography, App as AntApp,
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, PushpinOutlined, PaperClipOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'
import MediaUploader from '../../components/MediaUploader'

const { Title, Text } = Typography
const { TextArea } = Input

export default function AnnouncementsPage() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()

  const [announcements, setAnnouncements] = useState([])
  const [classes,       setClasses]       = useState([])
  const [sections,      setSections]      = useState([])
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

  const loadSections = async (classId) => {
    if (!classId) { setSections([]); return }
    try {
      const r = await api.get(`/school/master/sections?classId=${classId}`)
      setSections(r.data.data || [])
    } catch { setSections([]) }
  }

  const openCreate = () => {
    form.resetFields()
    form.setFieldsValue({ scope: 'School', isPinned: false })
    setScope('School')
    setSections([])
    setModal({ open: true, editing: null })
  }

  const openEdit = (record) => {
    form.setFieldsValue({
      title:         record.title,
      description:   record.description,
      scope:         record.scope,
      classId:       record.classId,
      sectionId:     record.sectionId,
      isPinned:      record.isPinned,
      attachmentUrl: record.attachmentUrl,
    })
    setScope(record.scope)
    loadSections(record.classId)
    setModal({ open: true, editing: record })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      if (modal.editing) {
        await api.put(`/school/announcements/${modal.editing.announcementId}`, values)
        message.success('Announcement updated.')
        setModal({ open: false, editing: null })
      } else {
        const res = await api.post('/school/announcements', values)
        const newId = res.data?.data
        message.success('Announcement created — you can attach files below.')
        setModal({ open: true, editing: { announcementId: newId, ...values } })  // stay open in edit mode
      }
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
      message.error(apiError(e, 'Failed to delete.'))
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
      width: 130,
      render: (s, r) => s === 'Class'
        ? <Tag color="blue">{r.className || 'Class'}{r.sectionName ? ` · ${r.sectionName}` : ''}</Tag>
        : <Tag>School</Tag>,
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
                if (v === 'School') {
                  form.setFieldsValue({ classId: null, sectionId: null })
                  setSections([])
                }
              }}
            />
          </Form.Item>
          {scope === 'Class' && (
            <>
              <Form.Item name="classId" label="Class" rules={[{ required: true }]}>
                <Select
                  placeholder="Select class"
                  options={classes.map(c => ({ label: c.className, value: c.classId }))}
                  onChange={v => {
                    form.setFieldValue('sectionId', null)
                    loadSections(v)
                  }}
                />
              </Form.Item>
              <Form.Item name="sectionId" label="Section" tooltip="Leave blank to reach the whole class (all sections).">
                <Select
                  placeholder="All Sections (whole class)"
                  allowClear
                  options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
                />
              </Form.Item>
            </>
          )}
          <Form.Item name="isPinned" label="Pin to top" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>

        <div style={{ marginTop: 8 }}>
          <Text strong>Attachments</Text>
          {modal.editing
            ? <div style={{ marginTop: 8 }}>
                <MediaUploader entityType="announcement" entityId={modal.editing.announcementId} classes={['image', 'doc', 'audio']} max={3} />
              </div>
            : <div style={{ marginTop: 4 }}>
                <Text type="secondary" style={{ fontSize: 12 }}>Save the announcement first, then edit it to upload files.</Text>
              </div>}
        </div>
      </Modal>
    </div>
  )
}
