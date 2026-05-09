import { useEffect, useState } from 'react'
import { Table, Select, Button, Space, Row, Col, Typography, App as AntApp, Tag } from 'antd'
import { FilePdfOutlined, FileExcelOutlined, SearchOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'

const { Text } = Typography

const CSV_COLUMNS = [
  { label: 'Adm No',         key: 'admissionNo'    },
  { label: 'Student Name',   key: 'studentName'    },
  { label: 'Class',          key: 'className'      },
  { label: 'Section',        key: 'sectionName'    },
  { label: 'Academic Year',  key: 'academicYear'   },
  { label: 'Father Name',    key: 'fatherName'     },
  { label: 'Mobile',         key: 'fatherMobile'   },
  { label: 'Reason',         key: 'detainedReason' },
]

export default function DetainedStudentsReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [years,        setYears]        = useState([])
  const [classes,      setClasses]      = useState([])
  const [sections,     setSections]     = useState([])
  const [academicYearId, setAcademicYearId] = useState(null)
  const [classId,      setClassId]      = useState(null)
  const [sectionId,    setSectionId]    = useState(null)
  const [data,         setData]         = useState([])
  const [loading,      setLoading]      = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years').then(r => {
      const list = r.data?.data || []
      setYears(list)
    })
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
  }, [])

  const loadSections = async (cid) => {
    setSectionId(null); setSections([])
    if (!cid) return
    try {
      const r = await api.get(`/school/master/sections?classId=${cid}`)
      setSections(r.data?.data || [])
    } catch { /* ignore */ }
  }

  const load = async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (academicYearId) params.append('academicYearId', academicYearId)
      if (classId)        params.append('classId',        classId)
      if (sectionId)      params.append('sectionId',      sectionId)
      const r = await api.get(`/school/reports/detained-students?${params}`)
      setData(r.data?.data || [])
    } catch {
      message.error('Failed to load report.')
    } finally {
      setLoading(false)
    }
  }

  // Load on mount with no filters
  useEffect(() => { load() }, []) // eslint-disable-line

  const columns = [
    { title: '#',             key: 'idx',   width: 50,  render: (_, __, i) => i + 1 },
    { title: 'Adm No',        dataIndex: 'admissionNo',  width: 110 },
    { title: 'Student Name',  dataIndex: 'studentName',  ellipsis: true },
    { title: 'Class',         dataIndex: 'className',    width: 90 },
    { title: 'Section',       dataIndex: 'sectionName',  width: 80 },
    { title: 'Academic Year', dataIndex: 'academicYear', width: 110 },
    { title: 'Father Name',   dataIndex: 'fatherName',   ellipsis: true },
    { title: 'Mobile',        dataIndex: 'fatherMobile', width: 125 },
    {
      title: 'Detention Reason', dataIndex: 'detainedReason', ellipsis: true,
      render: v => v ? <Tag color="volcano">{v}</Tag> : <Text type="secondary">—</Text>,
    },
  ]

  const exportToPdf = () => {
    if (!data.length) return
    exportPdf({
      title:      'Detained Students',
      schoolName,
      columns:    CSV_COLUMNS.map(c => c.label),
      rows:       data.map(r => CSV_COLUMNS.map(c => r[c.key] ?? '')),
      landscape:  true,
    })
  }

  const exportToCsv = () => {
    if (!data.length) return
    exportCsv({ columns: CSV_COLUMNS, rows: data, filename: 'detained_students' })
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <Row gutter={[12, 8]} align="bottom">
        <Col xs={24} sm={8} md={5}>
          <div style={{ marginBottom: 4 }}><Text strong>Academic Year</Text></div>
          <Select
            style={{ width: '100%' }}
            placeholder="All years"
            allowClear
            value={academicYearId}
            onChange={setAcademicYearId}
            options={(years || []).map(y => ({ value: y.academicYearId, label: y.academicYear }))}
          />
        </Col>
        <Col xs={24} sm={6} md={4}>
          <div style={{ marginBottom: 4 }}><Text strong>Class</Text></div>
          <Select
            style={{ width: '100%' }}
            placeholder="All classes"
            allowClear
            value={classId}
            onChange={cid => { setClassId(cid); loadSections(cid) }}
            options={(classes || []).map(c => ({ value: c.classId, label: c.className }))}
          />
        </Col>
        <Col xs={12} sm={4} md={3}>
          <div style={{ marginBottom: 4 }}><Text strong>Section</Text></div>
          <Select
            style={{ width: '100%' }}
            placeholder="All"
            allowClear
            disabled={!classId}
            value={sectionId}
            onChange={setSectionId}
            options={(sections || []).map(s => ({ value: s.sectionId, label: s.sectionName }))}
          />
        </Col>
        <Col xs={12} sm={4} md={3}>
          <Button type="primary" icon={<SearchOutlined />} onClick={load} loading={loading} style={{ width: '100%' }}>
            Load
          </Button>
        </Col>
      </Row>

      {data.length > 0 && (
        <Row justify="end">
          <Space>
            <Text type="secondary">{data.length} students</Text>
            <Button size="small" icon={<FilePdfOutlined />} onClick={exportToPdf}>PDF</Button>
            <Button size="small" icon={<FileExcelOutlined />} onClick={exportToCsv}>CSV</Button>
          </Space>
        </Row>
      )}

      <Table
        size="small"
        rowKey="studentId"
        columns={columns}
        dataSource={data}
        loading={loading}
        pagination={{ pageSize: 50, showTotal: t => `${t} students` }}
      />
    </div>
  )
}
