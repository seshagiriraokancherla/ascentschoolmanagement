import { useState } from 'react'
import { DatePicker, Select, Button, Row, Col, Table, Space, App as AntApp } from 'antd'
import { FilePdfOutlined, FileTextOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { RangePicker } = DatePicker

export default function DailyAttendanceSummaryReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [dateRange,  setDateRange]  = useState(null)
  const [classes,    setClasses]    = useState([])
  const [sections,   setSections]   = useState([])
  const [classId,    setClassId]    = useState(null)
  const [sectionId,  setSectionId]  = useState(null)
  const [data,       setData]       = useState([])
  const [loading,    setLoading]    = useState(false)

  const loadClasses = async () => {
    if (classes.length) return
    try {
      const r = await api.get('/school/master/classes')
      setClasses((r.data?.data || []).map(c => ({ value: c.classId, label: c.className })))
    } catch { /* silent */ }
  }

  const handleClassChange = async (val) => {
    setClassId(val)
    setSectionId(null)
    setSections([])
    if (!val) return
    try {
      const r = await api.get(`/school/master/sections?classId=${val}`)
      setSections((r.data?.data || []).map(s => ({ value: s.sectionId, label: s.sectionName })))
    } catch { /* silent */ }
  }

  const handleLoad = async () => {
    if (!dateRange?.[0] || !dateRange?.[1]) {
      message.warning('Please select a date range.')
      return
    }
    setLoading(true)
    try {
      const params = new URLSearchParams({
        dateFrom: dateRange[0].format('YYYY-MM-DD'),
        dateTo:   dateRange[1].format('YYYY-MM-DD'),
      })
      if (classId)   params.append('classId',   classId)
      if (sectionId) params.append('sectionId', sectionId)
      const r = await api.get(`/school/reports/daily-attendance-summary?${params}`)
      setData(r.data?.data || [])
    } catch { message.error('Failed to load report.') }
    finally { setLoading(false) }
  }

  const toRows = () => data.map(r => [
    r.attendanceDate, r.className, r.sectionName,
    r.totalStudents, r.present, r.absent, r.late, r.halfDay || 0,
    r.totalStudents > 0 ? `${Math.round(((r.present + 0.5 * (r.halfDay || 0)) / r.totalStudents) * 100)}%` : '—',
  ])

  const COLS = ['Date', 'Class', 'Section', 'Total', 'Present', 'Absent', 'Late', 'Half Day', 'Attendance %']

  const handlePdf = () => {
    if (!data.length) { message.warning('No data to export.'); return }
    const range = `${dateRange[0].format('DD/MM/YYYY')} – ${dateRange[1].format('DD/MM/YYYY')}`
    exportPdf({
      schoolName,
      title:    `Day-wise Class-wise Attendance Summary (${range})`,
      columns:  COLS,
      rows:     toRows(),
      fileName: 'daily_attendance_summary.pdf',
    })
  }

  const handleCsv = () => {
    if (!data.length) { message.warning('No data to export.'); return }
    exportCsv({ columns: COLS, rows: toRows(), fileName: 'daily_attendance_summary.csv' })
  }

  const columns = [
    { title: 'Date',    dataIndex: 'attendanceDate', width: 110 },
    { title: 'Class',   dataIndex: 'className',      width: 120 },
    { title: 'Section', dataIndex: 'sectionName',    width: 90 },
    { title: 'Total',   dataIndex: 'totalStudents',  width: 70,  align: 'center' },
    {
      title: 'Present', dataIndex: 'present', width: 80, align: 'center',
      render: v => <span style={{ color: '#52c41a', fontWeight: 600 }}>{v}</span>,
    },
    {
      title: 'Absent', dataIndex: 'absent', width: 80, align: 'center',
      render: v => <span style={{ color: '#ff4d4f', fontWeight: 600 }}>{v}</span>,
    },
    {
      title: 'Late', dataIndex: 'late', width: 70, align: 'center',
      render: v => <span style={{ color: '#fa8c16', fontWeight: 600 }}>{v}</span>,
    },
    {
      title: 'Half Day', dataIndex: 'halfDay', width: 80, align: 'center',
      render: v => <span style={{ color: '#1677ff', fontWeight: 600 }}>{v || 0}</span>,
    },
    {
      title: 'Attendance %', width: 110, align: 'center',
      render: (_, r) => r.totalStudents > 0
        ? `${Math.round(((r.present + 0.5 * (r.halfDay || 0)) / r.totalStudents) * 100)}%`
        : '—',
    },
  ]

  return (
    <div>
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <RangePicker
            value={dateRange}
            onChange={setDateRange}
            format="DD/MM/YYYY"
            allowClear
          />
        </Col>
        <Col flex="160px">
          <Select
            style={{ width: '100%' }}
            placeholder="All Classes"
            allowClear
            options={classes}
            value={classId}
            onChange={handleClassChange}
            onFocus={loadClasses}
          />
        </Col>
        <Col flex="130px">
          <Select
            style={{ width: '100%' }}
            placeholder="All Sections"
            allowClear
            options={sections}
            value={sectionId}
            onChange={setSectionId}
            disabled={!classId}
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Load
          </Button>
        </Col>
        {data.length > 0 && (
          <Col style={{ marginLeft: 'auto' }}>
            <Space>
              <Button icon={<FilePdfOutlined />} onClick={handlePdf}>PDF</Button>
              <Button icon={<FileTextOutlined />} onClick={handleCsv}>CSV</Button>
            </Space>
          </Col>
        )}
      </Row>

      <Table
        size="small"
        rowKey={(r, i) => `${r.attendanceDate}-${r.className}-${r.sectionName}-${i}`}
        columns={columns}
        dataSource={data}
        loading={loading}
        pagination={{ pageSize: 50, showSizeChanger: true }}
        scroll={{ x: 'max-content' }}
        summary={data.length > 0 ? () => {
          const total   = data.reduce((s, r) => s + r.totalStudents, 0)
          const present = data.reduce((s, r) => s + r.present,       0)
          const absent  = data.reduce((s, r) => s + r.absent,        0)
          const late    = data.reduce((s, r) => s + r.late,          0)
          const halfDay = data.reduce((s, r) => s + (r.halfDay || 0), 0)
          return (
            <Table.Summary.Row style={{ fontWeight: 700, background: '#f0f5ff' }}>
              <Table.Summary.Cell index={0} colSpan={3}>Grand Total</Table.Summary.Cell>
              <Table.Summary.Cell index={3} align="center">{total}</Table.Summary.Cell>
              <Table.Summary.Cell index={4} align="center" style={{ color: '#52c41a' }}>{present}</Table.Summary.Cell>
              <Table.Summary.Cell index={5} align="center" style={{ color: '#ff4d4f' }}>{absent}</Table.Summary.Cell>
              <Table.Summary.Cell index={6} align="center" style={{ color: '#fa8c16' }}>{late}</Table.Summary.Cell>
              <Table.Summary.Cell index={7} align="center" style={{ color: '#1677ff' }}>{halfDay}</Table.Summary.Cell>
              <Table.Summary.Cell index={8} align="center">
                {total > 0 ? `${Math.round(((present + 0.5 * halfDay) / total) * 100)}%` : '—'}
              </Table.Summary.Cell>
            </Table.Summary.Row>
          )
        } : undefined}
      />
    </div>
  )
}
