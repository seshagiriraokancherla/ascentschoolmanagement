import { useEffect, useState } from 'react'
import { Table, Select, Button, Space, Row, Col, Typography, Divider, App as AntApp, DatePicker } from 'antd'
import { FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { Text } = Typography

const STATUS_COLOR = { P: '#52c41a', A: '#ff4d4f', L: '#fa8c16' }

export default function AttendanceSheetReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [classes,  setClasses]  = useState([])
  const [sections, setSections] = useState([])

  const [classId,   setClassId]   = useState(null)
  const [sectionId, setSectionId] = useState(null)
  const [monthYear, setMonthYear] = useState(null)   // dayjs object

  const [sheet,   setSheet]   = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
  }, [])

  const onClassChange = async (val) => {
    setClassId(val)
    setSectionId(null)
    setSections([])
    setSheet(null)
    if (!val) return
    try {
      const r = await api.get(`/school/master/sections?classId=${val}`)
      setSections(r.data?.data || [])
    } catch { setSections([]) }
  }

  const handleLoad = async () => {
    if (!classId || !sectionId || !monthYear) {
      message.warning('Select Class, Section and Month.')
      return
    }
    setLoading(true)
    try {
      const q = new URLSearchParams({
        classId, sectionId,
        month: monthYear.month() + 1,
        year:  monthYear.year(),
      })
      const r = await api.get(`/school/reports/attendance-sheet?${q}`)
      setSheet(r.data?.data)
    } catch { message.error('Failed to load attendance sheet.') }
    finally { setLoading(false) }
  }

  // ── Build dynamic columns ──────────────────────────────────────────────────

  const days = sheet ? Array.from({ length: sheet.daysInMonth }, (_, i) => i + 1) : []

  const tableColumns = sheet ? [
    { title: 'Adm No',  dataIndex: 'admissionNo', fixed: 'left', width: 90 },
    { title: 'Name',    dataIndex: 'studentName', fixed: 'left', width: 160 },
    ...days.map(d => ({
      title: d,
      key:   `d${d}`,
      width: 28,
      align: 'center',
      render: (_, row) => {
        const v = row.days?.[d] || ''
        return <span style={{ color: STATUS_COLOR[v] || 'inherit', fontWeight: v ? 600 : 400, fontSize: 11 }}>{v}</span>
      },
    })),
    { title: 'P', dataIndex: 'present', width: 36, align: 'center', render: v => <Text style={{ color: STATUS_COLOR.P }}>{v}</Text> },
    { title: 'A', dataIndex: 'absent',  width: 36, align: 'center', render: v => <Text style={{ color: STATUS_COLOR.A }}>{v}</Text> },
    { title: 'L', dataIndex: 'late',    width: 36, align: 'center', render: v => <Text style={{ color: STATUS_COLOR.L }}>{v}</Text> },
  ] : []

  // ── Export helpers ─────────────────────────────────────────────────────────

  const reportTitle = () =>
    `Attendance Sheet — ${sheet?.className} ${sheet?.sectionName} (${monthYear?.format('MMM YYYY')})`

  const toExportRows = () =>
    (sheet?.rows || []).map(row => [
      row.admissionNo,
      row.studentName,
      ...days.map(d => row.days?.[d] || ''),
      row.present,
      row.absent,
      row.late,
    ])

  const exportColumns = sheet
    ? ['Adm No', 'Name', ...days.map(String), 'P', 'A', 'L']
    : []

  const handlePdf = () => {
    if (!sheet?.rows?.length) { message.warning('No data to export.'); return }
    exportPdf({
      schoolName,
      title:    reportTitle(),
      columns:  exportColumns,
      rows:     toExportRows(),
      fileName: `attendance_sheet_${classId}_${sectionId}_${monthYear.format('MMYYYY')}.pdf`,
      tableOptions: {
        styles:          { fontSize: 6, cellPadding: 1 },
        headStyles:      { fillColor: [22, 119, 255], textColor: 255, fontStyle: 'bold', fontSize: 6 },
        columnStyles:    { 0: { cellWidth: 18 }, 1: { cellWidth: 32 } },
      },
    })
  }

  const handleCsv = () => {
    if (!sheet?.rows?.length) { message.warning('No data to export.'); return }
    exportCsv({
      columns:  exportColumns,
      rows:     toExportRows(),
      fileName: `attendance_sheet_${classId}_${sectionId}_${monthYear.format('MMYYYY')}.csv`,
    })
  }

  return (
    <div>
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Select
            style={{ width: 150 }}
            placeholder="Class *"
            value={classId}
            onChange={onClassChange}
            options={classes.map(c => ({ label: c.className, value: c.classId }))}
            allowClear
          />
        </Col>
        <Col>
          <Select
            style={{ width: 130 }}
            placeholder="Section *"
            value={sectionId}
            onChange={v => { setSectionId(v); setSheet(null) }}
            options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
            disabled={sections.length === 0}
            allowClear
          />
        </Col>
        <Col>
          <DatePicker
            picker="month"
            placeholder="Month *"
            value={monthYear}
            onChange={v => { setMonthYear(v); setSheet(null) }}
            format="MMM YYYY"
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Generate
          </Button>
        </Col>
        {sheet?.rows?.length > 0 && (
          <Col style={{ marginLeft: 'auto' }}>
            <Space>
              <Button icon={<FilePdfOutlined />} danger onClick={handlePdf}>Export PDF</Button>
              <Button icon={<FileExcelOutlined />} onClick={handleCsv}>Export CSV</Button>
            </Space>
          </Col>
        )}
      </Row>

      {sheet && (
        <>
          <Divider style={{ margin: '8px 0' }} />
          <div style={{ marginBottom: 8 }}>
            <Text strong>{schoolName}</Text>
            <Text type="secondary" style={{ marginLeft: 8 }}>{reportTitle()}</Text>
            <span style={{ marginLeft: 16 }}>
              <Text style={{ color: STATUS_COLOR.P, fontSize: 12 }}>P = Present</Text>
              <Text style={{ color: STATUS_COLOR.A, fontSize: 12, marginLeft: 8 }}>A = Absent</Text>
              <Text style={{ color: STATUS_COLOR.L, fontSize: 12, marginLeft: 8 }}>L = Late</Text>
            </span>
            <Text type="secondary" style={{ float: 'right', fontSize: 12 }}>
              {sheet.rows?.length || 0} students
            </Text>
          </div>
          <Table
            rowKey="studentId"
            dataSource={sheet.rows || []}
            columns={tableColumns}
            size="small"
            pagination={false}
            scroll={{ x: 'max-content' }}
            loading={loading}
          />
        </>
      )}

      {sheet && !sheet.rows?.length && !loading && (
        <Text type="secondary">No active students found for this class and section.</Text>
      )}
    </div>
  )
}
