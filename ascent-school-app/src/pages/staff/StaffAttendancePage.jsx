import { useState } from 'react'
import {
  DatePicker, Button, Table, Tag, Input, Space, Row, Col,
  Typography, Alert, Tooltip, App as AntApp,
} from 'antd'
import { SaveOutlined, CheckCircleOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'

const { Text } = Typography

// Status config: value, label, color, keyboard shortcut
const STATUSES = [
  { value: 'Present',  label: 'P',       color: '#52c41a', bg: '#f6ffed', key: 'p' },
  { value: 'Absent',   label: 'A',       color: '#ff4d4f', bg: '#fff2f0', key: 'a' },
  { value: 'Late',     label: 'Late',    color: '#fa8c16', bg: '#fff7e6', key: 'l' },
  { value: 'HalfDay',  label: 'HD',      color: '#1677ff', bg: '#e6f4ff', key: 'h' },
  { value: 'OnLeave',  label: 'OL',      color: '#722ed1', bg: '#f9f0ff', key: 'o' },
]

const statusConfig = Object.fromEntries(STATUSES.map(s => [s.value, s]))

function StatusButton({ value, active, onClick }) {
  const cfg = statusConfig[value]
  return (
    <button
      onClick={onClick}
      style={{
        padding: '2px 8px', borderRadius: 4, cursor: 'pointer', fontSize: 11,
        fontWeight: active ? 700 : 400,
        border:     active ? `2px solid ${cfg.color}` : '1px solid #d9d9d9',
        background: active ? cfg.bg : '#fff',
        color:      active ? cfg.color : '#555',
        transition: 'all 0.15s',
      }}
    >
      {cfg.label}
    </button>
  )
}

export default function StaffAttendancePage() {
  const { message } = AntApp.useApp()

  const [date,    setDate]    = useState(dayjs())
  const [grid,    setGrid]    = useState(null)
  const [entries, setEntries] = useState({})   // staffId → { status, remarks }
  const [loading, setLoading] = useState(false)
  const [saving,  setSaving]  = useState(false)

  const handleLoad = async () => {
    if (!date) { message.warning('Select a date.'); return }
    setLoading(true)
    setGrid(null)
    setEntries({})
    try {
      const r = await api.get(`/school/staff/attendance?date=${date.format('YYYY-MM-DD')}`)
      const data = r.data?.data
      setGrid(data)
      // Pre-fill entries from existing records
      const init = {}
      ;(data?.staff || []).forEach(s => {
        init[s.staffId] = { status: s.status || 'Present', remarks: s.remarks || '' }
      })
      setEntries(init)
    } catch (e) { message.error(apiError(e, 'Failed to load attendance.')) }
    finally { setLoading(false) }
  }

  const setStatus = (staffId, status) =>
    setEntries(prev => ({ ...prev, [staffId]: { ...prev[staffId], status } }))

  const setRemarks = (staffId, remarks) =>
    setEntries(prev => ({ ...prev, [staffId]: { ...prev[staffId], remarks } }))

  const markAllPresent = () => {
    const updated = {}
    ;(grid?.staff || []).forEach(s => {
      updated[s.staffId] = { status: 'Present', remarks: entries[s.staffId]?.remarks || '' }
    })
    setEntries(updated)
  }

  const handleSave = async () => {
    if (!grid?.staff?.length) return
    setSaving(true)
    try {
      const payload = {
        date:    date.format('YYYY-MM-DD'),
        entries: (grid.staff || []).map(s => ({
          staffId: s.staffId,
          status:  entries[s.staffId]?.status  || 'Present',
          remarks: entries[s.staffId]?.remarks || '',
        })),
      }
      await api.post('/school/staff/attendance', payload)
      message.success(`Attendance saved for ${date.format('DD-MM-YYYY')}.`)
      // Refresh to reflect IsMarked
      handleLoad()
    } catch (e) { message.error(apiError(e, 'Failed to save attendance.')) }
    finally { setSaving(false) }
  }

  // Live summary
  const summary = grid
    ? STATUSES.map(s => ({
        ...s,
        count: (grid.staff || []).filter(st => (entries[st.staffId]?.status || 'Present') === s.value).length,
      }))
    : []

  const columns = [
    { title: '#', width: 45, align: 'center', render: (_, __, i) => i + 1 },
    { title: 'Emp Code',    dataIndex: 'employeeCode', width: 100,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Name',        dataIndex: 'staffName',    width: 200 },
    { title: 'Designation', dataIndex: 'designation',  width: 130,
      render: v => v ? <Tag color="blue" style={{ fontSize: 11 }}>{v}</Tag> : null },
    {
      title: 'Status', width: 260,
      render: (_, record) => (
        <Space size={4}>
          {STATUSES.map(s => (
            <StatusButton
              key={s.value}
              value={s.value}
              active={entries[record.staffId]?.status === s.value}
              onClick={() => setStatus(record.staffId, s.value)}
            />
          ))}
        </Space>
      ),
    },
    {
      title: 'Remarks', width: 180,
      render: (_, record) => (
        <Input
          size="small"
          placeholder="Optional"
          value={entries[record.staffId]?.remarks || ''}
          onChange={e => setRemarks(record.staffId, e.target.value)}
          style={{ fontSize: 12 }}
        />
      ),
    },
  ]

  return (
    <div>
      {/* ── Controls ──────────────────────────────────────────────────────── */}
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <DatePicker
            value={date}
            onChange={v => { setDate(v); setGrid(null) }}
            format="DD-MM-YYYY"
            disabledDate={d => d && d.isAfter(dayjs(), 'day')}
            allowClear={false}
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Load
          </Button>
        </Col>
        {grid && (
          <>
            <Col>
              <Tooltip title="Set all staff to Present">
                <Button icon={<CheckCircleOutlined />} onClick={markAllPresent}>
                  All Present
                </Button>
              </Tooltip>
            </Col>
            <Col style={{ marginLeft: 'auto' }}>
              <Button
                type="primary" icon={<SaveOutlined />}
                onClick={handleSave} loading={saving}
              >
                Save Attendance
              </Button>
            </Col>
          </>
        )}
      </Row>

      {/* ── Already-marked notice ─────────────────────────────────────────── */}
      {grid?.isMarked && (
        <Alert
          type="info"
          message={`Attendance for ${date.format('DD-MM-YYYY')} is already marked. You can update it below.`}
          showIcon
          style={{ marginBottom: 12 }}
        />
      )}

      {/* ── Live summary chips ────────────────────────────────────────────── */}
      {grid && (
        <Row gutter={8} style={{ marginBottom: 12 }}>
          {summary.map(s => (
            <Col key={s.value}>
              <Tag
                style={{
                  background: s.bg, color: s.color, border: `1px solid ${s.color}`,
                  fontWeight: 600, fontSize: 12, padding: '2px 10px',
                }}
              >
                {s.label}: {s.count}
              </Tag>
            </Col>
          ))}
          <Col>
            <Text type="secondary" style={{ fontSize: 12 }}>
              / {grid.staff?.length} staff
            </Text>
          </Col>
        </Row>
      )}

      {/* ── Grid ─────────────────────────────────────────────────────────── */}
      {grid && (
        <Table
          rowKey="staffId"
          dataSource={grid.staff || []}
          columns={columns}
          size="small"
          pagination={false}
          scroll={{ x: 'max-content' }}
          rowClassName={record => {
            const s = entries[record.staffId]?.status
            if (s === 'Absent')  return 'att-row-absent'
            if (s === 'Late')    return 'att-row-late'
            if (s === 'OnLeave') return 'att-row-leave'
            return ''
          }}
        />
      )}
    </div>
  )
}
