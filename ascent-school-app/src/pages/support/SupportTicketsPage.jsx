import { useEffect, useState } from 'react'
import {
  Card, Table, Button, Tag, Drawer, Descriptions, Typography,
  Modal, Form, Input, Select, Space, App as AntApp,
} from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'

const { Text } = Typography
const { TextArea } = Input

const TYPE_OPTIONS = [
  { value: 'Issue',  label: 'Issue (something is broken)' },
  { value: 'Change', label: 'Change (a request/improvement)' },
]

const PRIORITY_OPTIONS = [
  { value: 'Low',      label: 'Low' },
  { value: 'Medium',   label: 'Medium' },
  { value: 'High',     label: 'High' },
  { value: 'Critical', label: 'Critical' },
]

// A plain reference list, not DB-driven — matches the rest of the app's convention
// for small fixed classifications (e.g. Gender) that don't need a master-data table.
const MODULE_OPTIONS = [
  'Students', 'Fees', 'Attendance', 'Marks', 'Transport', 'Hostel',
  'Homework', 'Announcements', 'Reports', 'Messages', 'Master Data', 'Settings', 'Other',
].map((m) => ({ value: m, label: m }))

const STATUS_COLOR = { Open: 'blue', InProgress: 'gold', Done: 'green', Cancelled: 'default' }
const STATUS_LABEL = { Open: 'Open', InProgress: 'In Progress', Done: 'Done', Cancelled: 'Cancelled' }
const PRIORITY_COLOR = { Low: 'default', Medium: 'blue', High: 'orange', Critical: 'red' }

export default function SupportTicketsPage() {
  const { message } = AntApp.useApp()

  const [tickets, setTickets] = useState([])
  const [loading, setLoading] = useState(false)

  const [createOpen, setCreateOpen] = useState(false)
  const [creating,   setCreating]   = useState(false)
  const [form] = Form.useForm()

  const [drawerOpen, setDrawerOpen] = useState(false)
  const [detail,     setDetail]     = useState(null)

  const load = async () => {
    setLoading(true)
    try {
      const res = await api.get('/school/support-tickets')
      setTickets(res.data?.data || [])
    } catch (e) {
      message.error(apiError(e, 'Failed to load tickets.'))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const openCreate = () => {
    form.resetFields()
    form.setFieldsValue({ ticketType: 'Issue', priority: 'Medium' })
    setCreateOpen(true)
  }

  const handleCreate = async () => {
    const values = await form.validateFields()
    setCreating(true)
    try {
      await api.post('/school/support-tickets', {
        TicketType:  values.ticketType,
        Priority:    values.priority,
        Subject:     values.subject,
        Description: values.description,
        Module:      values.module || null,
      })
      message.success('Ticket submitted.')
      setCreateOpen(false)
      load()
    } catch (e) {
      message.error(apiError(e, 'Failed to submit ticket.'))
    } finally {
      setCreating(false)
    }
  }

  const viewTicket = (record) => {
    setDetail(record)
    setDrawerOpen(true)
  }

  const columns = [
    { title: 'S.No', key: 'serialNo', width: 60, render: (_, __, i) => i + 1 },
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
      title: 'Created', dataIndex: 'createdAt', key: 'createdAt', width: 160,
      render: (v) => v ? dayjs(v).format('DD-MM-YYYY HH:mm') : '—',
    },
    {
      title: 'Actions', key: 'actions', width: 100,
      render: (_, record) => (
        <Button size="small" onClick={() => viewTicket(record)}>View</Button>
      ),
    },
  ]

  return (
    <>
      <Card
        title="Support Tickets"
        extra={<Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Raise a Ticket</Button>}
      >
        <Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
          Report an issue or request a change. Every ticket raised from this school is visible here to
          everyone — check the list before raising a new one in case it's already been reported.
        </Text>

        <Table
          rowKey="ticketId"
          dataSource={tickets}
          columns={columns}
          loading={loading}
          size="small"
          pagination={{ pageSize: 20, showTotal: (t) => `${t} tickets` }}
        />
      </Card>

      {/* Raise Ticket Modal */}
      <Modal
        title="Raise a Ticket"
        open={createOpen}
        onOk={handleCreate}
        onCancel={() => setCreateOpen(false)}
        confirmLoading={creating}
        okText="Submit"
        width={560}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="ticketType" label="Type" rules={[{ required: true }]}>
            <Select options={TYPE_OPTIONS} />
          </Form.Item>
          <Form.Item name="priority" label="Priority" rules={[{ required: true }]}>
            <Select options={PRIORITY_OPTIONS} />
          </Form.Item>
          <Form.Item name="module" label="Module (optional)">
            <Select options={MODULE_OPTIONS} placeholder="Which part of the app?" allowClear showSearch />
          </Form.Item>
          <Form.Item
            name="subject"
            label="Subject"
            rules={[{ required: true, message: 'Please enter a short subject.' }, { max: 200 }]}
          >
            <Input placeholder="Short summary" />
          </Form.Item>
          <Form.Item
            name="description"
            label="Description"
            rules={[{ required: true, message: 'Please describe the issue or request.' }]}
          >
            <TextArea rows={5} placeholder="What happened, or what would you like changed? Include steps to reproduce if it's a bug." />
          </Form.Item>
        </Form>
      </Modal>

      {/* Ticket Detail Drawer */}
      <Drawer
        title={detail ? `Ticket — ${detail.subject}` : ''}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        width={480}
      >
        {detail && (
          <>
            <Space style={{ marginBottom: 16 }}>
              <Tag color={STATUS_COLOR[detail.status] || 'default'}>{STATUS_LABEL[detail.status] || detail.status}</Tag>
              <Tag color={PRIORITY_COLOR[detail.priority] || 'default'}>{detail.priority}</Tag>
              <Tag>{detail.ticketType}</Tag>
            </Space>

            <Descriptions bordered size="small" column={1} style={{ marginBottom: 16 }}>
              <Descriptions.Item label="Module">{detail.module || '—'}</Descriptions.Item>
              <Descriptions.Item label="Raised By">{detail.raisedByName}</Descriptions.Item>
              <Descriptions.Item label="Created">
                {detail.createdAt ? dayjs(detail.createdAt).format('DD-MM-YYYY HH:mm') : '—'}
              </Descriptions.Item>
              {detail.updatedAt && (
                <Descriptions.Item label="Last Updated">
                  {dayjs(detail.updatedAt).format('DD-MM-YYYY HH:mm')}
                </Descriptions.Item>
              )}
            </Descriptions>

            <Text strong>Description</Text>
            <div style={{ whiteSpace: 'pre-wrap', marginTop: 8, marginBottom: 16 }}>{detail.description}</div>

            {detail.resolutionNotes && (
              <>
                <Text strong>Resolution Notes</Text>
                <div style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{detail.resolutionNotes}</div>
              </>
            )}
          </>
        )}
      </Drawer>
    </>
  )
}
