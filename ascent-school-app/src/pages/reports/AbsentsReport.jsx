import { useEffect, useState } from 'react'
import { Table, Select, Button, Space, Row, Col, Typography, Divider, App as AntApp, DatePicker } from 'antd'
import { FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { Text } = Typography
const { RangePicker } = DatePicker

const COLUMNS = [
  { label: 'Date',         key: 'attendanceDate' },
  { label: 'Adm No',       key: 'admissionNo' },
  { label: 'Student Name', key: 'studentName' },
  { label: 'Class',        key: 'className' },
  { label: 'Section',      key: 'sectionName' },
  { label: 'Father Name',  key: 'fatherName' },
  { label: 'Mobile',       key: 'fatherMobile' },
]

export default function AbsentsReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [classes,  setClasses]  = useState([])
  const [sections, setSections] = useState([])

  const [dateRange,  setDateRange]  = useState(null)
  const [classId,    setClassId]    = useState(null)
  const [sectionId,  setSectionId]  = useState(null)
  const [rows,       setRows]       = useState([])
  const [loading,    setLoading]    = useState(false)
  const [loaded,     setLoaded]     = useState(false)

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
  }, [])

  const onClassChange = async (val) => {
    setClassId(val)
    setSectionId(null)
    setSections([])
    setRows([])
    setLoaded(false)
    if (!val) return
    try {
      const r = await api.get(`/school/master/sections?classId=${val}`)
      setSections(r.data?.data || [])
    } catch { setSections([]) }
  }

  const handleLoad = async () => {
    if (!dateRange?.[0] || !dateRange?.[1]) {
      message.warning('Select a date range.')
      return
    }
    setLoading(true)
    try {
      const q = new URLSearchParams({
        dateFrom: dateRange[0].format('YYYY-MM-DD'),
        dateTo:   dateRange[1].format('YYYY-MM-DD'),
      })
      if (classId)   q.set('classId',   classId)
      if (sectionId) q.set('sectionId', sectionId)
      const r = await api.get(`/school/reports/absents?${q}`)
      setRows(r.data?.data || [])
      setLoaded(true)
    } catch { message.error('Failed to load absents report.') }
    finally { setLoading(false) }
  }

  const reportTitle = () => {
    const from = dateRange?.[0]?.format('DD MMM YYYY') || ''
    const to   = dateRange?.[1]?.format('DD MMM YYYY') || ''
    const cls  = classes.find(c => c.classId === classId)?.className || 'All Classes'
    const sec  = sections.find(s => s.sectionId === sectionId)?.sectionName
    return `Absents Statement — ${cls}${sec ? ' ' + sec : ''} (${from} to ${to})`
  }

  const toExportRows = () => rows.map(r => COLUMNS.map(c => r[c.key] ?? ''))

  const handlePdf = () => {
    if (!rows.length) { message.warning('No data to export.'); return }
    exportPdf({
      schoolName,
      title:    reportTitle(),
      columns:  COLUMNS.map(c => c.label),
      rows:     toExportRows(),
      fileName: 'absents_statement.pdf',
    })
  }

  const handleCsv = () => {
    if (!rows.length) { message.warning('No data to export.'); return }
    exportCsv({
      columns:  COLUMNS.map(c => c.label),
      rows:     toExportRows(),
      fileName: 'absents_statement.csv',
    })
  }

  const tableColumns = [
    { title: 'Date',         dataIndex: 'attendanceDate', width: 100 },
    { title: 'Adm No',       dataIndex: 'admissionNo',    width: 100 },
    { title: 'Student Name', dataIndex: 'studentName',    width: 200 },
    { title: 'Class',        dataIndex: 'className',      width: 120 },
    { title: 'Section',      dataIndex: 'sectionName',    width: 80 },
    { title: 'Father Name',  dataIndex: 'fatherName',     width: 160 },
    { title: 'Mobile',       dataIndex: 'fatherMobile',   width: 120 },
  ]

  return (
    <div>
      <Row gutter={12} align="middle" wrap style={{ marginBottom: 16 }}>
        <Col>
          <RangePicker
            format="DD/MM/YYYY"
            value={dateRange}
            onChange={val => { setDateRange(val); setRows([]); setLoaded(false) }}
            allowClear
          />
        </Col>
        <Col>
          <Select
            style={{ width: 150 }}
            placeholder="All Classes"
            value={classId}
            onChange={onClassChange}
            options={classes.map(c => ({ label: c.className, value: c.classId }))}
            allowClear
          />
        </Col>
        <Col>
          <Select
            style={{ width: 130 }}
            placeholder="All Sections"
            value={sectionId}
            onChange={v => { setSectionId(v); setRows([]); setLoaded(false) }}
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
        {loaded && rows.length > 0 && (
          <Col style={{ marginLeft: 'auto' }}>
            <Space>
              <Button icon={<FilePdfOutlined />} danger onClick={handlePdf}>Export PDF</Button>
              <Button icon={<FileExcelOutlined />} onClick={handleCsv}>Export CSV</Button>
            </Space>
          </Col>
        )}
      </Row>

      {loaded && (
        <>
          <Divider style={{ margin: '8px 0' }} />
          <div style={{ marginBottom: 8 }}>
            <Text strong>{schoolName}</Text>
            <Text type="secondary" style={{ marginLeft: 8 }}>{reportTitle()}</Text>
            <Text type="secondary" style={{ float: 'right', fontSize: 12 }}>
              {rows.length} absence record{rows.length !== 1 ? 's' : ''}
            </Text>
          </div>
          <Table
            rowKey={(_, i) => i}
            dataSource={rows}
            columns={tableColumns}
            size="small"
            pagination={{ pageSize: 50, showTotal: t => `${t} records` }}
            loading={loading}
            scroll={{ x: 'max-content' }}
          />
        </>
      )}

      {loaded && rows.length === 0 && !loading && (
        <Text type="secondary">No absences found for the selected filters.</Text>
      )}
    </div>
  )
}
