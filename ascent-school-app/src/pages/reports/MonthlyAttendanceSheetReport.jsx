import { useEffect, useState } from 'react'
import { Select, Button, Space, Row, Col, Table, Typography, Divider, App as AntApp } from 'antd'
import { FilePdfOutlined, FileTextOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { Text } = Typography

const STATUS_COLOR = { present: '#52c41a', absent: '#ff4d4f', late: '#fa8c16', halfDay: '#1677ff' }

function monthLabel(key) {
  // key = "YYYY-MM"
  return dayjs(key + '-01').format('MMM YY')
}

export default function MonthlyAttendanceSheetReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [years,    setYears]    = useState([])
  const [classes,  setClasses]  = useState([])
  const [sections, setSections] = useState([])

  const [yearId,    setYearId]    = useState(null)
  const [classId,   setClassId]   = useState(null)
  const [sectionId, setSectionId] = useState(null)

  const [sheet,   setSheet]   = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const years = r.data?.data || []
      setYears(years)
      const current = years.find(y => y.isCurrent)
      if (current) setYearId(current.academicYearId)
    })
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
    if (!yearId || !classId || !sectionId) {
      message.warning('Select Academic Year, Class and Section.')
      return
    }
    setLoading(true)
    setSheet(null)
    try {
      const q = new URLSearchParams({ academicYearId: yearId, classId, sectionId })
      const r = await api.get(`/school/reports/monthly-attendance-sheet?${q}`)
      setSheet(r.data?.data)
    } catch { message.error('Failed to load report.') }
    finally { setLoading(false) }
  }

  // ── Dynamic columns ────────────────────────────────────────────────────────

  const months = sheet?.months || []

  const tableColumns = sheet ? [
    { title: 'Adm No', dataIndex: 'admissionNo', fixed: 'left', width: 90 },
    { title: 'Name',   dataIndex: 'studentName', fixed: 'left', width: 160 },
    ...months.map(mk => ({
      title: monthLabel(mk),
      key: mk,
      width: 90,
      align: 'center',
      render: (_, row) => {
        const m = row.monthData?.[mk]
        if (!m) return <Text type="secondary" style={{ fontSize: 11 }}>—</Text>
        return (
          <span style={{ fontSize: 11, lineHeight: 1.4 }}>
            <span style={{ color: STATUS_COLOR.present }}>{m.present}</span>
            {' / '}
            <span style={{ color: STATUS_COLOR.absent }}>{m.absent}</span>
            {m.late > 0 && <><span style={{ color: '#999' }}>/</span><span style={{ color: STATUS_COLOR.late }}>{m.late}</span></>}
            {m.halfDay > 0 && <><span style={{ color: '#999' }}>/</span><span style={{ color: STATUS_COLOR.halfDay }}>{m.halfDay}</span></>}
          </span>
        )
      },
    })),
    {
      title: 'Total P',
      dataIndex: 'totalPresent',
      width: 72,
      align: 'center',
      render: v => <Text strong style={{ color: STATUS_COLOR.present }}>{v}</Text>,
    },
    {
      title: 'Total A',
      dataIndex: 'totalAbsent',
      width: 72,
      align: 'center',
      render: v => <Text strong style={{ color: STATUS_COLOR.absent }}>{v}</Text>,
    },
    {
      title: 'Total L',
      dataIndex: 'totalLate',
      width: 72,
      align: 'center',
      render: v => v > 0
        ? <Text strong style={{ color: STATUS_COLOR.late }}>{v}</Text>
        : <Text type="secondary">0</Text>,
    },
    {
      title: 'Total HD',
      dataIndex: 'totalHalfDay',
      width: 76,
      align: 'center',
      render: v => v > 0
        ? <Text strong style={{ color: STATUS_COLOR.halfDay }}>{v}</Text>
        : <Text type="secondary">0</Text>,
    },
    {
      title: '%',
      width: 60,
      align: 'center',
      render: (_, r) => {
        const total = r.totalPresent + r.totalAbsent + r.totalLate + (r.totalHalfDay || 0)
        return total > 0
          ? <Text strong>{Math.round(((r.totalPresent + 0.5 * (r.totalHalfDay || 0)) / total) * 100)}%</Text>
          : '—'
      },
    },
  ] : []

  // ── Export helpers ─────────────────────────────────────────────────────────

  const reportTitle = () =>
    `Monthly Attendance Sheet — ${sheet?.className} ${sheet?.sectionName} (${sheet?.academicYear})`

  const exportCols = sheet
    ? ['Adm No', 'Name', ...months.map(monthLabel), 'Total P', 'Total A', 'Total L', 'Total HD', '%']
    : []

  const toRows = () => (sheet?.rows || []).map(r => {
    const total = r.totalPresent + r.totalAbsent + r.totalLate + (r.totalHalfDay || 0)
    return [
      r.admissionNo,
      r.studentName,
      ...months.map(mk => {
        const m = r.monthData?.[mk]
        return m ? `P:${m.present} A:${m.absent} L:${m.late} HD:${m.halfDay || 0}` : ''
      }),
      r.totalPresent,
      r.totalAbsent,
      r.totalLate,
      r.totalHalfDay || 0,
      total > 0 ? `${Math.round(((r.totalPresent + 0.5 * (r.totalHalfDay || 0)) / total) * 100)}%` : '—',
    ]
  })

  const handlePdf = () => {
    if (!sheet?.rows?.length) { message.warning('No data to export.'); return }
    exportPdf({
      schoolName,
      title:    reportTitle(),
      columns:  exportCols,
      rows:     toRows(),
      fileName: `monthly_attendance_${classId}_${sectionId}.pdf`,
      tableOptions: {
        styles:     { fontSize: 7, cellPadding: 1.5 },
        headStyles: { fillColor: [22, 119, 255], textColor: 255, fontStyle: 'bold', fontSize: 7 },
        columnStyles: { 0: { cellWidth: 18 }, 1: { cellWidth: 34 } },
      },
    })
  }

  const handleCsv = () => {
    if (!sheet?.rows?.length) { message.warning('No data to export.'); return }
    exportCsv({ columns: exportCols, rows: toRows(), fileName: `monthly_attendance_${classId}_${sectionId}.csv` })
  }

  // ── Summary row totals ─────────────────────────────────────────────────────

  const grandP = (sheet?.rows || []).reduce((s, r) => s + r.totalPresent, 0)
  const grandA = (sheet?.rows || []).reduce((s, r) => s + r.totalAbsent,  0)
  const grandL = (sheet?.rows || []).reduce((s, r) => s + r.totalLate,    0)
  const grandH = (sheet?.rows || []).reduce((s, r) => s + (r.totalHalfDay || 0), 0)
  const grandT = grandP + grandA + grandL + grandH

  return (
    <div>
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Select
            style={{ width: 140 }}
            placeholder="Year *"
            value={yearId}
            onChange={v => { setYearId(v); setSheet(null) }}
            options={(years).map(y => ({ label: y.academicYear, value: y.academicYearId }))}
            allowClear
          />
        </Col>
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
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Generate
          </Button>
        </Col>
        {sheet?.rows?.length > 0 && (
          <Col style={{ marginLeft: 'auto' }}>
            <Space>
              <Button icon={<FilePdfOutlined />} onClick={handlePdf}>PDF</Button>
              <Button icon={<FileTextOutlined />} onClick={handleCsv}>CSV</Button>
            </Space>
          </Col>
        )}
      </Row>

      {sheet && (
        <>
          <Divider style={{ margin: '8px 0' }} />
          <Row justify="space-between" style={{ marginBottom: 8 }}>
            <Col>
              <Text strong>{reportTitle()}</Text>
              <span style={{ marginLeft: 16, fontSize: 12 }}>
                <Text style={{ color: STATUS_COLOR.present }}>P = Present</Text>
                <Text style={{ color: STATUS_COLOR.absent,  marginLeft: 8 }}>A = Absent</Text>
                <Text style={{ color: STATUS_COLOR.late,    marginLeft: 8 }}>L = Late</Text>
                <Text style={{ color: STATUS_COLOR.halfDay, marginLeft: 8 }}>H = Half Day</Text>
                <Text type="secondary" style={{ marginLeft: 8 }}>— cell: P / A / L / H counts per month</Text>
              </span>
            </Col>
            <Col>
              <Text type="secondary" style={{ fontSize: 12 }}>{sheet.rows?.length || 0} students</Text>
            </Col>
          </Row>

          <Table
            rowKey="admissionNo"
            dataSource={sheet.rows || []}
            columns={tableColumns}
            size="small"
            pagination={false}
            scroll={{ x: 'max-content' }}
            loading={loading}
            summary={sheet.rows?.length > 0 ? () => (
              <Table.Summary.Row style={{ fontWeight: 700, background: '#f0f5ff' }}>
                <Table.Summary.Cell index={0} colSpan={2 + months.length}>Grand Total</Table.Summary.Cell>
                <Table.Summary.Cell align="center" style={{ color: STATUS_COLOR.present }}>{grandP}</Table.Summary.Cell>
                <Table.Summary.Cell align="center" style={{ color: STATUS_COLOR.absent  }}>{grandA}</Table.Summary.Cell>
                <Table.Summary.Cell align="center" style={{ color: STATUS_COLOR.late    }}>{grandL}</Table.Summary.Cell>
                <Table.Summary.Cell align="center" style={{ color: STATUS_COLOR.halfDay }}>{grandH}</Table.Summary.Cell>
                <Table.Summary.Cell align="center">
                  {grandT > 0 ? `${Math.round(((grandP + 0.5 * grandH) / grandT) * 100)}%` : '—'}
                </Table.Summary.Cell>
              </Table.Summary.Row>
            ) : undefined}
          />
        </>
      )}

      {sheet && !sheet.rows?.length && !loading && (
        <Text type="secondary">No active students found for this selection.</Text>
      )}
    </div>
  )
}
