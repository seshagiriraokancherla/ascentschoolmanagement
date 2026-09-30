import { useEffect, useRef, useState } from 'react'
import {
  Card, Table, Button, Modal, Form, Input, Select, DatePicker,
  Popconfirm, Space, Typography, App as AntApp, Row, Col,
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, PaperClipOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'
import MediaUploader from '../../components/MediaUploader'

const { Title, Text } = Typography
const { TextArea } = Input

// Treat both new ('Active') and legacy ('Y') as active rows.
const isActive = (s) => ['Active', 'Y', null, undefined, ''].includes(s.status)

export default function HomeworkPage() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()

  const [homework,  setHomework]  = useState([])
  const [classes,   setClasses]   = useState([])
  const [sections,  setSections]  = useState([])
  const [subjects,  setSubjects]  = useState([])   // shown in the dropdown
  const [subjectsAll, setSubjectsAll] = useState([]) // school master list (all statuses)
  const [usingAll,  setUsingAll]  = useState(false)
  const [subjLoading, setSubjLoading] = useState(false)
  const [loading,   setLoading]   = useState(false)
  const [modal,     setModal]     = useState({ open: false, editing: null })
  const [saving,    setSaving]    = useState(false)
  const [classFilter, setClassFilter] = useState(null)
  const [page,      setPage]      = useState(1)
  const [pageSize,  setPageSize]  = useState(20)
  const [total,     setTotal]     = useState(0)

  const loadHomework = async (classId, p = 1, ps = 20) => {
    setLoading(true)
    try {
      const params = new URLSearchParams({ page: p, pageSize: ps })
      if (classId) params.append('classId', classId)
      const r = await api.get(`/school/homework?${params}`)
      const d = r.data?.data || {}
      setHomework(d.items || [])
      setTotal(d.total || 0)
      setPage(p)
      setPageSize(ps)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data.data || []))
    api.get('/school/master/subjects').then(r => {
      const all = r.data.data || []
      setSubjectsAll(all)
      setSubjects(all.filter(isActive))   // no class chosen yet
    })
    loadHomework(null, 1, 20)
  }, [])

  // Subject dropdown scoped to the class's class_subjects mapping:
  //   no class chosen (allowed here — a homework row need not have one) → all active
  //   class with a mapping                                             → that mapping
  //   class with no mapping                                            → all active
  // keepSubjectId is the record's own subject when editing: kept in the list even if it
  // is no longer mapped or no longer active, so opening and saving an old row cannot
  // silently blank its subject. Returns the list actually shown.
  const subjReq = useRef(0)

  const loadSubjects = async (classId, keepSubjectId) => {
    const req = ++subjReq.current
    const active = subjectsAll.filter(isActive)

    const apply = (list) => {
      let next = list
      if (keepSubjectId && !next.some(s => s.subjectId === keepSubjectId)) {
        const own = subjectsAll.find(s => s.subjectId === keepSubjectId)
        if (own) next = [...next, own]
      }
      setSubjects(next)
      return next
    }

    if (!classId) { setUsingAll(false); return apply(active) }

    setSubjLoading(true)
    try {
      const r = await api.get(`/school/class-subjects/for-class?classId=${classId}`)
      if (req !== subjReq.current) return null    // a newer class won the race
      // for-class returns the mapping regardless of the subject's own status, so drop
      // any that are no longer active (skip the filter until the master list loads).
      const mapped = subjectsAll.length
        ? (r.data?.data || []).filter(m => active.some(a => a.subjectId === m.subjectId))
        : (r.data?.data || [])
      setUsingAll(mapped.length === 0)
      return apply(mapped.length ? mapped : active)
    } catch {
      if (req !== subjReq.current) return null
      setUsingAll(true)
      return apply(active)
    } finally {
      if (req === subjReq.current) setSubjLoading(false)
    }
  }

  const loadSections = async (classId) => {
    if (!classId) { setSections([]); return }
    try {
      const r = await api.get(`/school/master/sections?classId=${classId}`)
      setSections(r.data.data || [])
    } catch { setSections([]) }
  }

  const onClassChange = async (val) => {
    form.setFieldsValue({ sectionId: null })
    loadSections(val)

    // The subject picked for the previous class may not belong to the new one.
    const list = await loadSubjects(val)
    const current = form.getFieldValue('subjectId')
    if (list && current && !list.some(s => s.subjectId === current))
      form.setFieldsValue({ subjectId: null })
  }

  const openCreate = () => {
    form.resetFields()
    setSections([])
    loadSubjects(null)
    form.setFieldsValue({ assignedDate: dayjs() })
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
      attachmentUrl: record.attachmentUrl,
    })
    loadSections(record.classId)
    loadSubjects(record.classId, record.subjectId)   // keep the row's own subject
    setModal({ open: true, editing: record })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        ...values,
        assignedDate: values.assignedDate.format('YYYY-MM-DD'),
      }
      if (modal.editing) {
        await api.put(`/school/homework/${modal.editing.homeworkId}`, body)
        message.success('Homework updated.')
        setModal({ open: false, editing: null })
        loadHomework(classFilter, page, pageSize)   // stay on the current page
      } else {
        const res = await api.post('/school/homework', body)
        const newId = res.data?.data
        message.success('Homework created — you can attach files below.')
        setModal({ open: true, editing: { homeworkId: newId, ...values } })  // stay open in edit mode
        loadHomework(classFilter, 1, pageSize)       // new item is newest — jump to page 1
      }
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
      loadHomework(classFilter, page, pageSize)
    } catch (e) {
      message.error(apiError(e, 'Failed to delete.'))
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
              onChange={v => { setClassFilter(v); loadHomework(v, 1, pageSize) }}
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
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            showTotal: t => `${t} homework item${t !== 1 ? 's' : ''}`,
            onChange: (p, ps) => loadHomework(classFilter, p, ps),
          }}
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
              <Form.Item
                name="subjectId"
                label="Subject"
                extra={usingAll ? 'No subjects mapped to this class — showing all.' : undefined}
              >
                <Select
                  placeholder="Select subject"
                  allowClear
                  loading={subjLoading}
                  options={subjects.map(s => ({ label: s.subjectName, value: s.subjectId }))}
                />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="assignedDate" label="Assigned Date" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
        </Form>

        <div style={{ marginTop: 8 }}>
          <Text strong>Attachments</Text>
          {modal.editing
            ? <div style={{ marginTop: 8 }}>
                <MediaUploader entityType="homework" entityId={modal.editing.homeworkId} classes={['image', 'doc', 'audio']} max={3} />
              </div>
            : <div style={{ marginTop: 4 }}>
                <Text type="secondary" style={{ fontSize: 12 }}>Save the homework first, then edit it to upload files.</Text>
              </div>}
        </div>
      </Modal>
    </div>
  )
}

