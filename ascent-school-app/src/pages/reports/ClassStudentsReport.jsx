import { useEffect, useState } from 'react'
import { Table, Select, Button, Space, Row, Col, Tag, Typography, Divider, App as AntApp } from 'antd'
import { FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { Text } = Typography

const COLUMNS = [
  { label: 'Adm No',       key: 'admissionNo' },
  { label: 'Student Name', key: 'studentName' },
  { label: 'Gender',       key: 'gender' },
  { label: 'Date of Birth',key: 'dateOfBirth' },
  { label: 'Class',        key: 'className' },
  { label: 'Section',      key: 'sectionName' },
  { label: 'Father Name',  key: 'fatherName' },
  { label: 'Mobile',       key: 'fatherMobile' },
  { label: 'Status',       key: 'status' },
]

export default function ClassStudentsReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [classes,       setClasses]       = useState([])
  const [sections,      setSections]      = useState([])
  const [academicYears, setAcademicYears] = useState([])

  const [yearId,    setYearId]    = useState(null)
  const [classId,   setClassId]   = useState(null)
  const [sectionId, setSectionId] = useState(null)

  const [rows,    setRows]    = useState([])
  const [loading, setLoading] = useState(false)
  const [loaded,  setLoaded]  = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const years = r.data?.data || []
      setAcademicYears(years)
      const current = years.find(y => y.isCurrent)
      if (current) setYearId(current.academicYearId)
    })
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
    if (!yearId || !classId) { message.warning('Select Academic Year and Class.'); return }
    setLoading(true)
    try {
      const q = new URLSearchParams({ academicYearId: yearId, classId })
      if (sectionId) q.set('sectionId', sectionId)
      q.set('status', 'Active')
      const r = await api.get(`/school/students?${q}`)
      setRows(r.data?.data || [])
      setLoaded(true)
    } catch { message.error('Failed to load students.') }
    finally { setLoading(false) }
  }

  const reportTitle = () => {
    const cls  = classes.find(c => c.classId === classId)?.className || ''
    const sec  = sections.find(s => s.sectionId === sectionId)?.sectionName || ''
    const yr   = academicYears.find(y => y.academicYearId === yearId)?.academicYear || ''
    return `Class-wise Students List — ${cls}${sec ? ' ' + sec : ''} (${yr})`
  }

  const toTableRows = () =>
    rows.map(r => COLUMNS.map(c => {
      if (c.key === 'dateOfBirth') return r.dateOfBirth ? dayjs(r.dateOfBirth).format('DD/MM/YYYY') : ''
      return r[c.key] ?? ''
    }))

  const handlePdf = () => {
    if (!rows.length) { message.warning('No data to export.'); return }
    exportPdf({
      schoolName,
      title:    reportTitle(),
      columns:  COLUMNS.map(c => c.label),
      rows:     toTableRows(),
      fileName: `class_students_${classId}${sectionId ? '_' + sectionId : ''}.pdf`,
    })
  }

  const handleCsv = () => {
    if (!rows.length) { message.warning('No data to export.'); return }
    exportCsv({
      columns:  COLUMNS.map(c => c.label),
      rows:     toTableRows(),
      fileName: `class_students_${classId}${sectionId ? '_' + sectionId : ''}.csv`,
    })
  }

  const tableColumns = [
    { title: 'Adm No',       dataIndex: 'admissionNo',  width: 100 },
    { title: 'Student Name', dataIndex: 'studentName',  width: 200 },
    { title: 'Gender',       dataIndex: 'gender',       width: 80 },
    {
      title: 'Date of Birth', dataIndex: 'dateOfBirth', width: 110,
      render: v => v ? dayjs(v).format('DD MMM YYYY') : '',
    },
    { title: 'Section',      dataIndex: 'sectionName',  width: 80 },
    { title: 'Father Name',  dataIndex: 'fatherName',   width: 160 },
    { title: 'Mobile',       dataIndex: 'fatherMobile', width: 120 },
    {
      title: 'Status', dataIndex: 'status', width: 90,
      render: v => <Tag color={v === 'Active' ? 'green' : 'default'}>{v}</Tag>,
    },
  ]

  return (
    <div>
      {/* Filters */}
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Select
            style={{ width: 160 }}
            placeholder="Academic Year"
            value={yearId}
            onChange={v => { setYearId(v); setRows([]); setLoaded(false) }}
            options={academicYears.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
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
          <>
            <Col style={{ marginLeft: 'auto' }}>
              <Space>
                <Button icon={<FilePdfOutlined />} danger onClick={handlePdf}>
                  Export PDF
                </Button>
                <Button icon={<FileExcelOutlined />} onClick={handleCsv}>
                  Export CSV
                </Button>
              </Space>
            </Col>
          </>
        )}
      </Row>

      {loaded && (
        <>
          <Divider style={{ margin: '8px 0' }} />
          <div style={{ marginBottom: 8 }}>
            <Text strong>{schoolName}</Text>
            <Text type="secondary" style={{ marginLeft: 8 }}>{reportTitle()}</Text>
            <Text type="secondary" style={{ float: 'right', fontSize: 12 }}>
              {rows.length} student{rows.length !== 1 ? 's' : ''}
            </Text>
          </div>
          <Table
            rowKey="studentId"
            dataSource={rows}
            columns={tableColumns}
            size="small"
            pagination={false}
            scroll={{ x: 'max-content' }}
            loading={loading}
          />
        </>
      )}

      {loaded && rows.length === 0 && !loading && (
        <Text type="secondary">No active students found for the selected filters.</Text>
      )}
    </div>
  )
}
