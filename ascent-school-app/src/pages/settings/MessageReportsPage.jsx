import { useEffect, useState } from 'react'
import { Card, Table, Tag, Select, Button, Popconfirm, Typography, Alert, Space } from 'antd'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Paragraph, Text } = Typography

const STATUS_OPTIONS = [
  { value: 'Open',     label: 'Open' },
  { value: 'Reviewed', label: 'Reviewed' },
  { value: 'Removed',  label: 'Removed' },
]

const STATUS_COLOR = { Open: 'red', Reviewed: 'blue', Removed: 'default' }

export default function MessageReportsPage() {
  const [rows,    setRows]    = useState([])
  const [status,  setStatus]  = useState('Open')
  const [loading, setLoading] = useState(false)
  const [acting,  setActing]  = useState(null)

  useEffect(() => { load(status) }, [])

  async function load(st) {
    setLoading(true)
    try {
      const qs = st ? `?status=${st}` : ''
      const { data } = await api.get(`/school/message-reports${qs}`)
      setRows(data.data || [])
    } finally {
      setLoading(false)
    }
  }

  function onStatusChange(val) {
    setStatus(val)
    load(val)
  }

  async function resolve(record, action) {
    setActing(record.reportId)
    try {
      await api.post(`/school/message-reports/${record.reportId}/resolve`, { action })
      load(status)
    } finally {
      setActing(null)
    }
  }

  const columns = [
    {
      title: 'Reported', dataIndex: 'createdAt', key: 'createdAt', width: 150,
      render: (v) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : ''),
    },
    {
      title: 'Message', key: 'messageBody',
      render: (_, r) => (
        <div>
          <Paragraph style={{ marginBottom: 4 }} ellipsis={{ rows: 3, expandable: true }}>
            {r.messageBody}
          </Paragraph>
          <Text type="secondary" style={{ fontSize: 12 }}>
            from {r.senderName || '—'} ({r.senderType})
            {r.messageStatus === 'Removed' && <Tag color="default" style={{ marginLeft: 8 }}>removed</Tag>}
          </Text>
        </div>
      ),
    },
    {
      title: 'Child', key: 'student', width: 160,
      render: (_, r) => (
        <div>
          <div>{r.studentName || '—'}</div>
          <Text type="secondary" style={{ fontSize: 12 }}>{r.className}</Text>
        </div>
      ),
    },
    {
      title: 'Reported by', dataIndex: 'reportedByType', key: 'reportedByType', width: 100,
      render: (v) => <Tag>{v}</Tag>,
    },
    { title: 'Reason', dataIndex: 'reason', key: 'reason', width: 200 },
    {
      title: 'Status', dataIndex: 'status', key: 'status', width: 100,
      render: (v) => <Tag color={STATUS_COLOR[v] || 'default'}>{v}</Tag>,
    },
    {
      title: '', key: 'actions', width: 200,
      render: (_, r) =>
        r.status !== 'Open' ? (
          <Text type="secondary" style={{ fontSize: 12 }}>
            {r.reviewedBy ? `by ${r.reviewedBy}` : ''}
          </Text>
        ) : (
          <Space>
            <Popconfirm
              title="Remove this message?"
              description="It will be hidden from both the parent and the teacher."
              okText="Remove"
              okButtonProps={{ danger: true }}
              onConfirm={() => resolve(r, 'Removed')}
            >
              <Button size="small" danger loading={acting === r.reportId}>Remove</Button>
            </Popconfirm>
            <Button size="small" loading={acting === r.reportId} onClick={() => resolve(r, 'Reviewed')}>
              Dismiss
            </Button>
          </Space>
        ),
    },
  ]

  return (
    <Card title="Reported Messages">
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Messages reported by parents or teachers appear here."
        description="Remove hides the message from both sides. Dismiss marks the report reviewed with no action. Reviewing reported content is a Google Play requirement for in-app messaging."
      />

      <div style={{ marginBottom: 16 }}>
        <Select
          options={STATUS_OPTIONS}
          style={{ width: 160 }}
          value={status}
          onChange={onStatusChange}
          allowClear
          placeholder="All statuses"
        />
      </div>

      <Table
        rowKey="reportId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        pagination={false}
        locale={{ emptyText: status === 'Open' ? 'No open reports.' : 'No reports.' }}
      />
    </Card>
  )
}
