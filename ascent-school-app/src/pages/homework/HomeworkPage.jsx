import { useEffect, useState } from 'react'
import {
  Card, Table, Button, Modal, Form, Input, Select, DatePicker,
  Popconfirm, Tag, Space, Typography, App as AntApp, Row, Col,
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, PaperClipOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography
const { TextArea } = Input

export default function HomeworkPage() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()

  const [homework,  setHomework]  = useState([])
  const [classes,   setClasses]   = useState([])
  const [sections,  setSections]  = useState([])
  const [subjects,  setSubjects]  = useState([])
  const [loading,   setLoading]   = useState(false)
  const [modal,     setModal]     = useState({ open: false, editing: null })
  const [saving,    setSaving]    = useState(false)
  const [classFilter, setClassFilter] = useState(null)

  const loadHomework = async (classId) => {
    setLoading(true)
    try {
      const url = classId ? `/school/homework?classId=${classId}` : '/school/homework'
      const r = await api.get(url)
      setHomework(r.data.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data.data || []))
    api.get('/school/master/subjects').then(r => setSubjects(r.data.data || []))
    loadHomework(null)
  }, [])

  const loadSections = async (classId) => {
    if (!classId) { setSections([]); return }
    try {
      const r = await api.get(`/school/master/sections?classId=${classId}`)
      setSections(r.data.data || [])
    } catch { setSections([]) }
  }

  const onClassChange = (val) => {
    form.setFieldsValue({ sectionId: null })
    loadSections(val)
  }

  const openCreate = () => {
    form.resetFields()
    setSections([])
    form.setFieldsValue({ assignedDate: dayjs(), dueDate: dayjs().add(1, 'day') })
    setModal({ open: true, editing: null })
  }

  const openEdit = (record) => {
    form.setFieldsValue({
      title:         record.title,
      description:   record.description,
      subjectId:     record.subjectId,
      classId:       record.classId,
      sectionId:     record.sectionId,
      assignedDate:  dayjs(record.assignedDate),
      dueDate:       dayjs(record.dueDate),
      attachmentUrl: record.attachmentUrl,
    })
    loadSections(record.classId)
    setModal({ open: true, editing: record })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        ...values,
        assignedDate: values.assignedDate.format('YYYY-MM-DD'),
        dueDate:      values.dueDate.format('YYYY-MM-DD'),
      }
      if (modal.editing) {
        await api.put(`/school/homework/${modal.editing.homeworkId}`, body)
        message.success('Homework updated.')
      } else {
        await api.post('/school/homework', body)
        message.success('Homework created.')
      }
      setModal({ open: false, editing: null })
      loadHomework(classFilter)
    } catch (e) {
      message.error(e.message || 'Failed to save homework.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (id) => {
    try {
      await api.delete(`/school/homework/${id}`)
      message.success('Homework deleted.')
      loadHomework(classFilter)
    } catch (e) {
      message.error('Failed to delete.')
    }
  }

  const columns = [
    {
      title: 'Title',
      dataIndex: 'title',
      key: 'title',
      render: (t, r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{t}</div>
          {r.description && <Text type="secondary" style={{ fontSize: 12 }}>{r.description.slice(0, 80)}{r.description.length > 80 ? '…' : ''}</Text>}
        </div>
      ),
    },
    { title: 'Subject',  dataIndex: 'subjectName',  key: 'subjectName',  width: 120 },
    { title: 'Class',    dataIndex: 'className',    key: 'className',    width: 100 },
    { title: 'Section',  dataIndex: 'sectionName',  key: 'sectionName',  width: 80  },
    {
      title: 'Assigned',
      dataIndex: 'assignedDate',
      key: 'assignedDate',
      width: 110,
      render: d => dayjs(d).format('DD MMM YYYY'),
    },
    {
      title: 'Due Date',
      dataIndex: 'dueDate',
      key: 'dueDate',
      width: 110,
      render: d => {
        const due   = dayjs(d)
        const today = dayjs()
        const color = due.isBefore(today, 'day') ? 'red' : due.isSame(today, 'day') ? 'orange' : 'default'
        return <Tag color={color}>{due.format('DD MMM YYYY')}</Tag>
      },
    },
    {
      title: 'Attachment',
      dataIndex: 'attachmentUrl',
      key: 'attachmentUrl',
      width: 100,
      render: url => url
        ? <a href={url} target="_blank" rel="noreferrer"><PaperClipOutlined /> View</a>
        : null,
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 100,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EditOutlined />}   onClick={() => openEdit(r)} />
          <Popconfirm title="Delete this homework?" onConfirm={() => handleDelete(r.homeworkId)}>
            <Button size="small" icon={<DeleteOutlined />} danger />
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Homework</Title>

      <Card
        extra={
          <Space>
            <Select
              style={{ width: 150 }}
              placeholder="All classes"
              allowClear
              value={classFilter}
              onChange={v => { setClassFilter(v); loadHomework(v) }}
              options={classes.map(c => ({ label: c.className, value: c.classId }))}
            />
            <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
              Add Homework
            </Button>
          </Space>
        }
      >
        <Table
          dataSource={homework}
          columns={columns}
          rowKey="homeworkId"
          loading={loading}
          pagination={{ pageSize: 20 }}
        />
      </Card>

      <Modal
        title={modal.editing ? 'Edit Homework' : 'New Homework'}
        open={modal.open}
        onOk={handleSave}
        onCancel={() => setModal({ open: false, editing: null })}
        confirmLoading={saving}
        width={560}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="title" label="Title" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <TextArea rows={3} />
          </Form.Item>
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item name="classId" label="Class">
                <Select
                  placeholder="Select class"
                  allowClear
                  options={classes.map(c => ({ label: c.className, value: c.classId }))}
                  onChange={onClassChange}
                />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="sectionId" label="Section">
                <Select
                  placeholder="Select section"
                  allowClear
                  disabled={sections.length === 0}
                  options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
                />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="subjectId" label="Subject">
                <Select
                  placeholder="Select subject"
                  allowClear
                  options={subjects.map(s => ({ label: s.subjectName, value: s.subjectId }))}
                />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="assignedDate" label="Assigned Date" rules={[{ required: true }]}>
                <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="dueDate" label="Due Date" rules={[{ required: true }]}>
                <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item
            name="attachmentUrl"
            label="Attachment URL"
            extra="Optional — paste a Google Drive or Cloudinary PDF/doc link"
          >
            <Input placeholder="https://drive.google.com/..." prefix={<PaperClipOutlined />} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}

