import { useEffect, useState } from 'react'
import {
  Row, Col, Card, Statistic, Table, Tag, Progress, List,
  Typography, Spin, Button, Space, Divider,
} from 'antd'
import {
  TeamOutlined, CalendarOutlined, DollarOutlined, RiseOutlined,
  BookOutlined, NotificationOutlined, ReloadOutlined, CarOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import { useNavigate } from 'react-router-dom'
import api from '../../api/axiosInstance'

const { Text, Title } = Typography

const fmt = (n) =>
  n >= 100000
    ? `₹${(n / 100000).toFixed(1)}L`
    : n >= 1000
    ? `₹${(n / 1000).toFixed(1)}K`
    : `₹${n}`

export default function DashboardPage() {
  const navigate = useNavigate()
  const [data,    setData]    = useState(null)
  const [loading, setLoading] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const r = await api.get('/school/dashboard')
      setData(r.data.data)
    } catch (_) {}
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  if (loading && !data) return <Spin size="large" style={{ display: 'block', margin: '80px auto' }} />

  if (!data) return null

  const {
    totalActiveStudents,
    attendanceMarkedToday, todayPresent, todayAbsent, todayLate, todayTotalMarked, attendancePct,
    todayCollection, monthCollection, monthReceiptCount,
    last6MonthsCollection, recentReceipts, upcomingHomework, activeAnnouncementsCount,
  } = data

  // Fee trend max for bar scaling
  const maxFee = Math.max(...(last6MonthsCollection?.map(m => m.amount) || [1]), 1)

  const receiptColumns = [
    { title: 'Receipt',  dataIndex: 'receiptNo',   key: 'receiptNo',   width: 100 },
    { title: 'Student',  dataIndex: 'studentName', key: 'studentName' },
    {
      title: 'Amount', dataIndex: 'totalAmount', key: 'totalAmount', width: 100, align: 'right',
      render: v => <Text strong style={{ color: '#52c41a' }}>₹{v.toLocaleString('en-IN')}</Text>,
    },
    {
      title: 'Date', dataIndex: 'paymentDate', key: 'paymentDate', width: 100,
      render: d => dayjs(d).format('DD MMM'),
    },
    {
      title: 'Status', dataIndex: 'status', key: 'status', width: 80,
      render: s => <Tag color={s === 'Active' ? 'success' : 'error'}>{s}</Tag>,
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <Title level={4} style={{ margin: 0 }}>Dashboard</Title>
        <Button icon={<ReloadOutlined />} onClick={load} loading={loading} size="small">Refresh</Button>
      </div>

      {/* ── Row 1: Key stats ─────────────────────────────────────────────── */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable onClick={() => navigate('/students')} style={{ cursor: 'pointer' }}>
            <Statistic
              title="Total Students"
              value={totalActiveStudents}
              prefix={<TeamOutlined style={{ color: '#1677ff' }} />}
              valueStyle={{ color: '#1677ff' }}
            />
            <Text type="secondary" style={{ fontSize: 12 }}>Active enrolments</Text>
          </Card>
        </Col>

        <Col xs={24} sm={12} lg={6}>
          <Card hoverable onClick={() => navigate('/attendance')} style={{ cursor: 'pointer' }}>
            <Statistic
              title="Today's Attendance"
              value={attendanceMarkedToday ? attendancePct : '—'}
              suffix={attendanceMarkedToday ? '%' : ''}
              prefix={<CalendarOutlined style={{ color: attendancePct >= 85 ? '#52c41a' : '#faad14' }} />}
              valueStyle={{ color: attendancePct >= 85 ? '#52c41a' : '#faad14' }}
            />
            {attendanceMarkedToday
              ? <Text type="secondary" style={{ fontSize: 12 }}>P: {todayPresent} · A: {todayAbsent}{todayLate > 0 ? ` · L: ${todayLate}` : ''} of {todayTotalMarked}</Text>
              : <Text type="secondary" style={{ fontSize: 12 }}>Not marked yet today</Text>
            }
          </Card>
        </Col>

        <Col xs={24} sm={12} lg={6}>
          <Card hoverable onClick={() => navigate('/fees/collect')} style={{ cursor: 'pointer' }}>
            <Statistic
              title="This Month's Collection"
              value={monthCollection}
              prefix={<DollarOutlined style={{ color: '#52c41a' }} />}
              formatter={v => `₹${Number(v).toLocaleString('en-IN')}`}
              valueStyle={{ color: '#52c41a' }}
            />
            <Text type="secondary" style={{ fontSize: 12 }}>{monthReceiptCount} receipt{monthReceiptCount !== 1 ? 's' : ''} this month</Text>
          </Card>
        </Col>

        <Col xs={24} sm={12} lg={6}>
          <Card hoverable onClick={() => navigate('/fees/collect')} style={{ cursor: 'pointer' }}>
            <Statistic
              title="Today's Collection"
              value={todayCollection}
              prefix={<RiseOutlined style={{ color: '#722ed1' }} />}
              formatter={v => `₹${Number(v).toLocaleString('en-IN')}`}
              valueStyle={{ color: '#722ed1' }}
            />
            <Text type="secondary" style={{ fontSize: 12 }}>Collected today</Text>
          </Card>
        </Col>
      </Row>

      {/* ── Row 2: Fee trend + Attendance breakdown ──────────────────────── */}
      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={14}>
          <Card
            title="Fee Collection — Last 6 Months"
            extra={<Button type="link" size="small" onClick={() => navigate('/fees/receipts')}>View Receipts</Button>}
          >
            {last6MonthsCollection?.length > 0 ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                {last6MonthsCollection.map(m => (
                  <div key={`${m.year}-${m.month}`} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <Text style={{ width: 72, fontSize: 12, flexShrink: 0 }}>{m.monthLabel}</Text>
                    <Progress
                      percent={Math.round((m.amount / maxFee) * 100)}
                      format={() => fmt(m.amount)}
                      strokeColor="#1677ff"
                      style={{ flex: 1, margin: 0 }}
                    />
                  </div>
                ))}
              </div>
            ) : (
              <Text type="secondary">No fee data in the last 6 months.</Text>
            )}
          </Card>
        </Col>

        <Col xs={24} lg={10}>
          <Card title="Today's Attendance Breakdown" style={{ height: '100%' }}>
            {attendanceMarkedToday ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                    <Text>Present</Text>
                    <Text strong style={{ color: '#52c41a' }}>{todayPresent}</Text>
                  </div>
                  <Progress percent={todayTotalMarked > 0 ? Math.round(todayPresent / todayTotalMarked * 100) : 0} strokeColor="#52c41a" showInfo={false} />
                </div>
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                    <Text>Absent</Text>
                    <Text strong style={{ color: '#ff4d4f' }}>{todayAbsent}</Text>
                  </div>
                  <Progress percent={todayTotalMarked > 0 ? Math.round(todayAbsent / todayTotalMarked * 100) : 0} strokeColor="#ff4d4f" showInfo={false} />
                </div>
                {todayLate > 0 && (
                  <div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                      <Text>Late</Text>
                      <Text strong style={{ color: '#faad14' }}>{todayLate}</Text>
                    </div>
                    <Progress percent={todayTotalMarked > 0 ? Math.round(todayLate / todayTotalMarked * 100) : 0} strokeColor="#faad14" showInfo={false} />
                  </div>
                )}
                <div style={{ textAlign: 'center', marginTop: 4 }}>
                  <Text type="secondary">{todayTotalMarked} students marked across all classes</Text>
                </div>
              </div>
            ) : (
              <div style={{ textAlign: 'center', padding: '24px 0' }}>
                <CalendarOutlined style={{ fontSize: 32, color: '#d9d9d9' }} />
                <div style={{ marginTop: 8 }}>
                  <Text type="secondary">Attendance not marked yet today.</Text>
                </div>
                <Button
                  type="primary"
                  size="small"
                  style={{ marginTop: 12 }}
                  onClick={() => navigate('/attendance')}
                >
                  Mark Now
                </Button>
              </div>
            )}
          </Card>
        </Col>
      </Row>

      {/* ── Row 3: Recent receipts + Upcoming homework + Quick links ─────── */}
      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={14}>
          <Card
            title="Recent Receipts"
            extra={<Button type="link" size="small" onClick={() => navigate('/fees/receipts')}>All Receipts</Button>}
          >
            <Table
              dataSource={recentReceipts}
              columns={receiptColumns}
              rowKey="receiptNo"
              pagination={false}
              size="small"
              locale={{ emptyText: 'No receipts yet.' }}
            />
          </Card>
        </Col>

        <Col xs={24} lg={10}>
          <Card
            title={
              <Space>
                <BookOutlined />
                <span>Upcoming Homework</span>
              </Space>
            }
            extra={<Button type="link" size="small" onClick={() => navigate('/homework')}>Manage</Button>}
            style={{ marginBottom: 16 }}
          >
            {upcomingHomework?.length > 0 ? (
              <List
                size="small"
                dataSource={upcomingHomework}
                renderItem={item => {
                  const due    = dayjs(item.dueDate)
                  const today  = dayjs()
                  const isToday = due.isSame(today, 'day')
                  const isPast  = due.isBefore(today, 'day')
                  return (
                    <List.Item style={{ padding: '6px 0' }}>
                      <div style={{ flex: 1 }}>
                        <div style={{ fontWeight: 500, fontSize: 13 }}>{item.title}</div>
                        <Text type="secondary" style={{ fontSize: 11 }}>
                          {item.subjectName && `${item.subjectName} · `}{item.className}
                        </Text>
                      </div>
                      <Tag color={isPast ? 'error' : isToday ? 'warning' : 'default'} style={{ flexShrink: 0 }}>
                        {isToday ? 'Today' : due.format('DD MMM')}
                      </Tag>
                    </List.Item>
                  )
                }}
              />
            ) : (
              <Text type="secondary">No upcoming homework.</Text>
            )}
          </Card>

          {/* Quick action panel */}
          <Card title="Quick Actions" size="small">
            <Row gutter={[8, 8]}>
              {[
                { label: 'Mark Attendance', icon: <CalendarOutlined />, path: '/attendance', color: '#1677ff' },
                { label: 'Collect Fee',     icon: <DollarOutlined />,   path: '/fees/collect', color: '#52c41a' },
                { label: 'Add Homework',    icon: <BookOutlined />,     path: '/homework',     color: '#722ed1' },
                { label: 'Announcement',    icon: <NotificationOutlined />, path: '/announcements', color: '#fa8c16' },
              ].map(({ label, icon, path, color }) => (
                <Col span={12} key={path}>
                  <Button
                    block
                    icon={icon}
                    onClick={() => navigate(path)}
                    style={{ borderColor: color, color }}
                  >
                    {label}
                  </Button>
                </Col>
              ))}
            </Row>
          </Card>
        </Col>
      </Row>
    </div>
  )
}
