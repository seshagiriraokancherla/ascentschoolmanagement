import { useEffect, useState } from 'react'
import { Select, Button, Space, Row, Col, Table, Typography, Divider, App as AntApp, DatePicker } from 'antd'
import { FilePdfOutlined, FileTextOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { RangePicker } = DatePicker
const { Text } = Typography

const STATUS_COLOR = { P: '#52c41a', A: '#ff4d4f', L: '#fa8c16' }

// "YYYY-MM-DD" → "01\nJun" (two-line header for narrow columns)
function dateColTitle(key) {
  const d = dayjs(key)
  return `${d.format('DD')}\n${d.format('MMM')}`
}

export default function AttendanceRegisterReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [years,    setYears]    = useState([])
  const [classes,  setClasses]  = useState([])
  const [sections, setSections] = useState([])

  const [yearId,     setYearId]     = useState(null)
  const [classId,    setClassId]    = useState(null)
  const [sectionId,  setSectionId]  = useState(null)
  const [dateRange,  setDateRange]  = useState(null)

  const [register, setRegister] = useState(null)
  const [loading,  setLoading]  = useState(false)

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
    setRegister(null)
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
    if (!dateRange?.[0] || !dateRange?.[1]) {
      message.warning('Select a date range.')
      return
    }
    setLoading(true)
    setRegister(null)
    try {
      const q = new URLSearchParams({
        academicYearId: yearId,
        classId,
        sectionId,
        dateFrom: dateRange[0].format('YYYY-MM-DD'),
        dateTo:   dateRange[1].format('YYYY-MM-DD'),
      })
      const r = await api.get(`/school/reports/attendance-register?${q}`)
      setRegister(r.data?.data)
    } catch { message.error('Failed to load attendance register.') }
    finally { setLoading(false) }
  }

  // ── Dynamic columns ────────────────────────────────────────────────────────

  const dates = register?.dates || []

  const tableColumns = register ? [
    { title: 'S.No', dataIndex: 'serialNo',   fixed: 'left', width: 46,  align: 'center' },
    { title: 'Adm No', dataIndex: 'admissionNo', fixed: 'left', width: 90 },
    { title: 'Name',   dataIndex: 'studentName', fixed: 'left', width: 160 },
    ...dates.map(dk => ({
      title: (
        <div style={{ textAlign: 'center', lineHeight: 1.2, fontSize: 10 }}>
          {dayjs(dk).format('DD')}<br />
          <span style={{ color: '#888' }}>{dayjs(dk).format('MMM')}</span>
        </div>
      ),
      key:   dk,
      width: 36,
      align: 'center',
      render: (_, row) => {
        const v = row.dateData?.[dk] || ''
        return (
          <span style={{ color: STATUS_COLOR[v] || '#ccc', fontWeight: v ? 600 : 400, fontSize: 11 }}>
            {v || '·'}
          </span>
        )
      },
    })),
    {
      title: 'P',
      dataIndex: 'present',
      fixed: 'right',
      width: 38,
      align: 'center',
      render: v => <Text style={{ color: STATUS_COLOR.P, fontWeight: 600 }}>{v}</Text>,
    },
    {
      title: 'A',
      dataIndex: 'absent',
      fixed: 'right',
      width: 38,
      align: 'center',
      render: v => <Text style={{ color: STATUS_COLOR.A, fontWeight: 600 }}>{v}</Text>,
    },
    {
      title: 'L',
      dataIndex: 'late',
      fixed: 'right',
      width: 38,
      align: 'center',
      render: v => v > 0
        ? <Text style={{ color: STATUS_COLOR.L, fontWeight: 600 }}>{v}</Text>
        : <Text type="secondary">0</Text>,
    },
    {
      title: '%',
      fixed: 'right',
      width: 50,
      align: 'center',
      render: (_, r) => {
        const total = r.present + r.absent + r.late
        return total > 0
          ? <Text strong>{Math.round((r.present / total) * 100)}%</Text>
          : '—'
      },
    },
  ] : []

  // ── Export helpers ─────────────────────────────────────────────────────────

  const reportTitle = () => {
    const range = dateRange
      ? `${dateRange[0].format('DD MMM YYYY')} – ${dateRange[1].format('DD MMM YYYY')}`
      : ''
    return `Attendance Register — ${register?.className} ${register?.sectionName} (${range})`
  }

  const exportCols = register
    ? ['S.No', 'Adm No', 'Name', ...dates.map(dk => dayjs(dk).format('DD-MMM')), 'P', 'A', 'L', '%']
    : []

  const toRows = () => (register?.rows || []).map(r => {
    const total = r.present + r.absent + r.late
    return [
      r.serialNo,
      r.admissionNo,
      r.studentName,
      ...dates.map(dk => r.dateData?.[dk] || ''),
      r.present,
      r.absent,
      r.late,
      total > 0 ? `${Math.round((r.present / total) * 100)}%` : '—',
    ]
  })

  const handlePdf = () => {
    if (!register?.rows?.length) { message.warning('No data to export.'); return }
    exportPdf({
      schoolName,
      title:    reportTitle(),
      columns:  exportCols,
      rows:     toRows(),
      fileName: `attendance_register_${classId}_${sectionId}.pdf`,
      tableOptions: {
        styles:       { fontSize: 6, cellPadding: 1 },
        headStyles:   { fillColor: [22, 119, 255], textColor: 255, fontStyle: 'bold', fontSize: 6 },
        columnStyles: { 0: { cellWidth: 10 }, 1: { cellWidth: 18 }, 2: { cellWidth: 32 } },
      },
    })
  }

  const handleCsv = () => {
    if (!register?.rows?.length) { message.warning('No data to export.'); return }
    exportCsv({ columns: exportCols, rows: toRows(), fileName: `attendance_register_${classId}_${sectionId}.csv` })
  }

  // ── Summary totals ─────────────────────────────────────────────────────────

  const grandP = (register?.rows || []).reduce((s, r) => s + r.present, 0)
  const grandA = (register?.rows || []).reduce((s, r) => s + r.absent,  0)
  const grandL = (register?.rows || []).reduce((s, r) => s + r.late,    0)
  const grandT = grandP + grandA + grandL

  return (
    <div>
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }} wrap>
        <Col>
          <Select
            style={{ width: 140 }}
            placeholder="Year *"
            value={yearId}
            onChange={v => { setYearId(v); setRegister(null) }}
            options={years.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
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
            onChange={v => { setSectionId(v); setRegister(null) }}
            options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
            disabled={sections.length === 0}
            allowClear
          />
        </Col>
        <Col>
          <RangePicker
            value={dateRange}
            onChange={v => { setDateRange(v); setRegister(null) }}
            format="DD/MM/YYYY"
            allowClear
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Generate
          </Button>
        </Col>
        {register?.rows?.length > 0 && (
          <Col style={{ marginLeft: 'auto' }}>
            <Space>
              <Button icon={<FilePdfOutlined />} onClick={handlePdf}>PDF</Button>
              <Button icon={<FileTextOutlined />} onClick={handleCsv}>CSV</Button>
            </Space>
          </Col>
        )}
      </Row>

      {register && (
        <>
          <Divider style={{ margin: '8px 0' }} />
          <Row justify="space-between" style={{ marginBottom: 8 }}>
            <Col>
              <Text strong>{reportTitle()}</Text>
              <span style={{ marginLeft: 16, fontSize: 12 }}>
                <Text style={{ color: STATUS_COLOR.P }}>P = Present</Text>
                <Text style={{ color: STATUS_COLOR.A, marginLeft: 8 }}>A = Absent</Text>
                <Text style={{ color: STATUS_COLOR.L, marginLeft: 8 }}>L = Late</Text>
                <Text type="secondary" style={{ marginLeft: 8 }}>
                  {dates.length} working day{dates.length !== 1 ? 's' : ''}
                </Text>
              </span>
            </Col>
            <Col>
              <Text type="secondary" style={{ fontSize: 12 }}>
                {register.rows?.length || 0} students
              </Text>
            </Col>
          </Row>

          <Table
            rowKey="serialNo"
            dataSource={register.rows || []}
            columns={tableColumns}
            size="small"
            pagination={false}
            scroll={{ x: 'max-content' }}
            loading={loading}
            summary={register.rows?.length > 0 ? () => (
              <Table.Summary.Row style={{ fontWeight: 700, background: '#f0f5ff' }}>
                <Table.Summary.Cell index={0} colSpan={3 + dates.length}>Grand Total</Table.Summary.Cell>
                <Table.Summary.Cell align="center" style={{ color: STATUS_COLOR.P }}>{grandP}</Table.Summary.Cell>
                <Table.Summary.Cell align="center" style={{ color: STATUS_COLOR.A }}>{grandA}</Table.Summary.Cell>
                <Table.Summary.Cell align="center" style={{ color: STATUS_COLOR.L }}>{grandL}</Table.Summary.Cell>
                <Table.Summary.Cell align="center">
                  {grandT > 0 ? `${Math.round((grandP / grandT) * 100)}%` : '—'}
                </Table.Summary.Cell>
              </Table.Summary.Row>
            ) : undefined}
          />
        </>
      )}

      {register && !register.rows?.length && !loading && (
        <Text type="secondary">No active students found for this selection.</Text>
      )}
    </div>
  )
}
