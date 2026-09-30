import { useEffect, useState } from 'react'
import {
  Card, Table, Tag, Drawer, Descriptions, Typography,
  Select, Input, Button, Row, Col, Space, Form, message,
} from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Text } = Typography
const { TextArea } = Input

const STATUS_OPTIONS = [
  { value: '',           label: 'All Status' },
  { value: 'Open',       label: 'Open' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Done',       label: 'Done' },
  { value: 'Cancelled',  label: 'Cancelled' },
]
const TYPE_OPTIONS = [
  { value: '',       label: 'All Types' },
  { value: 'Issue',  label: 'Issue' },
  { value: 'Change', label: 'Change' },
]

const STATUS_COLOR   = { Open: 'blue', InProgress: 'gold', Done: 'green', Cancelled: 'default' }
const STATUS_LABEL   = { Open: 'Open', InProgress: 'In Progress', Done: 'Done', Cancelled: 'Cancelled' }
const PRIORITY_COLOR = { Low: 'default', Medium: 'blue', High: 'orange', Critical: 'red' }

export default function SupportTicketsPage() {
  const [tickets, setTickets] = useState([])
  const [groups,  setGroups]  = useState([])
  const [schools, setSchools] = useState([])
  const [loading, setLoading] = useState(false)

  const [groupId,  setGroupId]  = useState(null)
  const [schoolId, setSchoolId] = useState(null)
  const [status,   setStatus]   = useState('')
  const [type,     setType]     = useState('')
  const [search,   setSearch]   = useState('')

  const [drawerOpen, setDrawerOpen] = useState(false)
  const [detail,     setDetail]     = useState(null)
  const [saving,     setSaving]     = useState(false)
  const [form] = Form.useForm()

  const load = async () => {
    setLoading(true)
    try {
      const q = new URLSearchParams()
      if (groupId)  q.set('groupId',    groupId)
      if (schoolId) q.set('schoolId',   schoolId)
      if (status)   q.set('status',     status)
      if (type)     q.set('ticketType', type)
      if (search)   q.set('search',     search)
      const res = await api.get(`/control/support-tickets?${q}`)
      setTickets(res.data?.data || [])
    } catch (err) {
      message.error(err.response?.data?.message || 'Failed to load tickets.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  useEffect(() => {
    api.get('/control/school-groups').then((r) => setGroups(r.data?.data || [])).catch(() => {})
  }, [])

  // Schools are scoped to the selected group; cleared when the group changes.
  useEffect(() => {
    setSchoolId(null)
    setSchools([])
    if (!groupId) return
    api.get(`/control/school-groups/${groupId}/schools`)
      .then((r) => setSchools(r.data?.data || []))
      .catch(() => {})
  }, [groupId])

  const openTicket = (record) => {
    setDetail(record)
    form.setFieldsValue({
      status:          record.status,
      resolutionNotes: record.resolutionNotes || '',
    })
    setDrawerOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const res = await api.put(`/control/support-tickets/${detail.ticketId}/status`, {
        Status:          values.status,
        ResolutionNotes: values.resolutionNotes || null,
      })
      message.success('Ticket updated.')
      setDetail(res.data?.data)
      load()
    } catch (err) {
      message.error(err.response?.data?.message || 'Failed to update ticket.')
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'S.No', key: 'serialNo', width: 55, render: (_, __, i) => i + 1 },
    { title: 'Group', dataIndex: 'groupName', key: 'groupName', width: 150 },
    { title: 'School', dataIndex: 'schoolName', key: 'schoolName', width: 150, render: (v) => v || '—' },
    { title: 'Subject', dataIndex: 'subject', key: 'subject' },
    { title: 'Type', dataIndex: 'ticketType', key: 'ticketType', width: 90 },
    {
      title: 'Priority', dataIndex: 'priority', key: 'priority', width: 100,
      render: (v) => <Tag color={PRIORITY_COLOR[v] || 'default'}>{v}</Tag>,
    },
    {
      title: 'Status', dataIndex: 'status', key: 'status', width: 120,
      render: (v) => <Tag color={STATUS_COLOR[v] || 'default'}>{STATUS_LABEL[v] || v}</Tag>,
    },
    { title: 'Raised By', dataIndex: 'raisedByName', key: 'raisedByName', width: 150 },
    {
      title: 'Created', dataIndex: 'createdAt', key: 'createdAt', width: 150,
      render: (v) => v ? new Date(v).toLocaleString() : '—',
    },
    {
      title: 'Actions', key: 'actions', width: 90,
      render: (_, record) => <Button size="small" onClick={() => openTicket(record)}>View</Button>,
    },
  ]

  return (
    <>
      <Card title="Support Tickets">
        <Row gutter={12} style={{ marginBottom: 16 }} wrap>
          <Col>
            <Select
              style={{ width: 200 }}
              placeholder="All Groups"
              allowClear
              value={groupId}
              onChange={setGroupId}
              options={groups.map((g) => ({ value: g.groupId, label: g.groupName }))}
              showSearch
              filterOption={(input, option) => option.label.toLowerCase().includes(input.toLowerCase())}
            />
          </Col>
          <Col>
            <Select
              style={{ width: 180 }}
              placeholder="All Schools"
              allowClear
              value={schoolId}
              onChange={setSchoolId}
              disabled={!groupId}
              options={schools.map((s) => ({ value: s.schoolId, label: s.schoolName }))}
            />
          </Col>
          <Col>
            <Select style={{ width: 140 }} options={STATUS_OPTIONS} value={status} onChange={setStatus} />
          </Col>
          <Col>
            <Select style={{ width: 130 }} options={TYPE_OPTIONS} value={type} onChange={setType} />
          </Col>
          <Col flex="auto">
            <Input
              placeholder="Search subject, description or raised by"
              prefix={<SearchOutlined />}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onPressEnter={load}
              allowClear
            />
          </Col>
          <Col>
            <Button type="primary" icon={<SearchOutlined />} onClick={load}>Search</Button>
          </Col>
        </Row>

        <Table
          rowKey="ticketId"
          dataSource={tickets}
          columns={columns}
          loading={loading}
          size="small"
          pagination={{ pageSize: 20, showTotal: (t) => `${t} tickets` }}
        />
      </Card>

      <Drawer
        title={detail ? `Ticket — ${detail.subject}` : ''}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        width={480}
      >
        {detail && (
          <>
            <Space style={{ marginBottom: 16 }}>
              <Tag color={PRIORITY_COLOR[detail.priority] || 'default'}>{detail.priority}</Tag>
              <Tag>{detail.ticketType}</Tag>
            </Space>

            <Descriptions bordered size="small" column={1} style={{ marginBottom: 16 }}>
              <Descriptions.Item label="Group">{detail.groupName}</Descriptions.Item>
              <Descriptions.Item label="School">{detail.schoolName || '—'}</Descriptions.Item>
              <Descriptions.Item label="DB Name">{detail.dbName}</Descriptions.Item>
              <Descriptions.Item label="Module">{detail.module || '—'}</Descriptions.Item>
              <Descriptions.Item label="Raised By">
                {detail.raisedByName}{detail.raisedByUsername ? ` (${detail.raisedByUsername})` : ''}
              </Descriptions.Item>
              <Descriptions.Item label="Created">{new Date(detail.createdAt).toLocaleString()}</Descriptions.Item>
              {detail.updatedAt && (
                <Descriptions.Item label="Last Updated">{new Date(detail.updatedAt).toLocaleString()}</Descriptions.Item>
              )}
              {detail.resolvedAt && (
                <Descriptions.Item label="Resolved">{new Date(detail.resolvedAt).toLocaleString()}</Descriptions.Item>
              )}
            </Descriptions>

            <Text strong>Description</Text>
            <div style={{ whiteSpace: 'pre-wrap', marginTop: 8, marginBottom: 16 }}>{detail.description}</div>

            <Form form={form} layout="vertical">
              <Form.Item name="status" label="Status" rules={[{ required: true }]}>
                <Select options={STATUS_OPTIONS.filter((o) => o.value)} />
              </Form.Item>
              <Form.Item name="resolutionNotes" label="Resolution Notes">
                <TextArea rows={4} placeholder="What was done / why it's cancelled, etc." />
              </Form.Item>
              <Button type="primary" loading={saving} onClick={handleSave} block>
                Save
              </Button>
            </Form>
          </>
        )}
      </Drawer>
    </>
  )
}
