import { useEffect, useState } from 'react'
import {
  Card, Table, Button, Modal, Form, Input, Select, DatePicker,
  Popconfirm, Tag, Space, Typography, App as AntApp,
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'

const { Title, Text } = Typography
const { TextArea } = Input

const CATEGORIES = ['Holiday', 'Exam', 'Celebration', 'Event']
const CATEGORY_COLOR = {
  Holiday:     'red',
  Exam:        'gold',
  Celebration: 'magenta',
  Event:       'blue',
}

// "05 Aug 2026 (Wed)" for a single day, "02–05 Oct 2026" for a range.
function formatSpan(start, end) {
  const s = dayjs(start)
  const e = dayjs(end)
  if (s.isSame(e, 'day')) return `${s.format('DD MMM YYYY')} (${s.format('ddd')})`
  if (s.isSame(e, 'month')) return `${s.format('DD')}–${e.format('DD MMM YYYY')}`
  return `${s.format('DD MMM')} – ${e.format('DD MMM YYYY')}`
}

export default function CalendarPage() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()

  const [events,        setEvents]        = useState([])
  const [years,         setYears]         = useState([])
  const [yearId,        setYearId]        = useState(null)
  const [month,         setMonth]         = useState(dayjs())
  const [loading,       setLoading]       = useState(false)
  const [modal,         setModal]         = useState({ open: false, editing: null })
  const [saving,        setSaving]        = useState(false)

  const load = async (m = month, y = yearId) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams({ month: m.month() + 1, year: m.year() })
      if (y) qs.set('academicYearId', y)
      const r = await api.get(`/school/calendar?${qs.toString()}`)
      setEvents(r.data.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const list = r.data.data || []
      setYears(list)
      const current = list.find(a => a.isCurrent) || list[0]
      const yid = current ? current.academicYearId : null
      setYearId(yid)
      load(month, yid)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const openCreate = () => {
    form.resetFields()
    form.setFieldsValue({ category: 'Holiday', startDate: month.date(1) })
    setModal({ open: true, editing: null })
  }

  const openEdit = (record) => {
    form.setFieldsValue({
      title:       record.title,
      description: record.description,
      category:    record.category,
      startDate:   dayjs(record.startDate),
      endDate:     record.endDate && !dayjs(record.endDate).isSame(dayjs(record.startDate), 'day')
                     ? dayjs(record.endDate) : null,
    })
    setModal({ open: true, editing: record })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    const payload = {
      title:          values.title,
      description:    values.description,
      category:       values.category,
      startDate:      values.startDate.format('YYYY-MM-DD'),
      endDate:        (values.endDate || values.startDate).format('YYYY-MM-DD'),
      academicYearId: yearId || null,
    }
    setSaving(true)
    try {
      if (modal.editing) {
        await api.put(`/school/calendar/${modal.editing.calendarEventId}`, payload)
        message.success('Calendar entry updated.')
      } else {
        await api.post('/school/calendar', payload)
        message.success('Calendar entry added.')
      }
      setModal({ open: false, editing: null })
      // Jump the month view to the entry's start month so it's visible.
      const startMonth = dayjs(payload.startDate)
      setMonth(startMonth)
      load(startMonth, yearId)
    } catch (e) {
      message.error(e.message || 'Failed to save calendar entry.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (id) => {
    try {
      await api.delete(`/school/calendar/${id}`)
      message.success('Calendar entry deleted.')
      load()
    } catch (e) {
      message.error(apiError(e, 'Failed to delete.'))
    }
  }

  const columns = [
    {
      title: 'Date',
      key: 'span',
      width: 190,
      render: (_, r) => <Text strong>{formatSpan(r.startDate, r.endDate)}</Text>,
    },
    {
      title: 'Category',
      dataIndex: 'category',
      key: 'category',
      width: 130,
      render: c => <Tag color={CATEGORY_COLOR[c] || 'default'}>{c}</Tag>,
    },
    {
      title: 'Event / Holiday',
      dataIndex: 'title',
      key: 'title',
      render: (t, r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{t}</div>
          {r.description && (
            <Text type="secondary" style={{ fontSize: 12 }}>
              {r.description.slice(0, 120)}{r.description.length > 120 ? '…' : ''}
            </Text>
          )}
        </div>
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 100,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Delete this entry?" onConfirm={() => handleDelete(r.calendarEventId)}>
            <Button size="small" icon={<DeleteOutlined />} danger />
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Calendar</Title>

      <Card
        title={
          <Space wrap>
            <span>Month:</span>
            <DatePicker
              picker="month"
              value={month}
              allowClear={false}
              format="MMM YYYY"
              onChange={(m) => { setMonth(m); load(m, yearId) }}
            />
            <span style={{ marginLeft: 8 }}>Academic Year:</span>
            <Select
              style={{ minWidth: 140 }}
              value={yearId}
              options={years.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
              onChange={(v) => { setYearId(v); load(month, v) }}
            />
          </Space>
        }
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Add Entry
          </Button>
        }
      >
        <Table
          dataSource={events}
          columns={columns}
          rowKey="calendarEventId"
          loading={loading}
          pagination={false}
          locale={{ emptyText: `No entries for ${month.format('MMMM YYYY')}` }}
        />
      </Card>

      <Modal
        title={modal.editing ? 'Edit Calendar Entry' : 'Add Calendar Entry'}
        open={modal.open}
        onOk={handleSave}
        onCancel={() => setModal({ open: false, editing: null })}
        confirmLoading={saving}
        width={480}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="title" label="Event / Holiday name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Dussehra Holidays, FA-1 Exams, Annual Day" />
          </Form.Item>
          <Form.Item name="category" label="Category" rules={[{ required: true }]}>
            <Select options={CATEGORIES.map(c => ({ label: c, value: c }))} />
          </Form.Item>
          <Form.Item name="startDate" label="Start Date" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} format="DD MMM YYYY" />
          </Form.Item>
          <Form.Item
            name="endDate"
            label="End Date"
            tooltip="Leave blank for a single-day entry."
            dependencies={['startDate']}
            rules={[({ getFieldValue }) => ({
              validator(_, value) {
                const start = getFieldValue('startDate')
                if (!value || !start || !value.isBefore(start, 'day')) return Promise.resolve()
                return Promise.reject(new Error('End date cannot be before start date.'))
              },
            })]}
          >
            <DatePicker style={{ width: '100%' }} format="DD MMM YYYY" placeholder="Single day" />
          </Form.Item>
          <Form.Item name="description" label="Description (optional)">
            <TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
