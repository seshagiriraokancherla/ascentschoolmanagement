import { useEffect, useState } from 'react'
import { Table, Select, Button, Space, Row, Col, Typography, Divider, App as AntApp } from 'antd'
import { FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'

const { Text } = Typography

const COLUMNS = [
  { label: 'Adm No',       key: 'admissionNo' },
  { label: 'Student Name', key: 'studentName' },
  { label: 'Class',        key: 'className' },
  { label: 'Section',      key: 'sectionName' },
  { label: 'Route',        key: 'routeName' },
  { label: 'Bus',          key: 'busName' },
  { label: 'Trip',         key: 'busTrip' },
  { label: 'Father Name',  key: 'fatherName' },
  { label: 'Mobile',       key: 'fatherMobile' },
]

export default function TransportStudentsReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [academicYears, setAcademicYears] = useState([])
  const [routes,        setRoutes]        = useState([])

  const [yearId,      setYearId]      = useState(null)
  const [busRouteId,  setBusRouteId]  = useState(null)
  const [rows,        setRows]        = useState([])
  const [loading,     setLoading]     = useState(false)
  const [loaded,      setLoaded]      = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years').then(r => setAcademicYears(r.data?.data || []))
    api.get('/school/transport/routes').then(r => setRoutes(r.data?.data || []))
  }, [])

  const handleLoad = async () => {
    if (!yearId) { message.warning('Select an Academic Year.'); return }
    setLoading(true)
    try {
      const q = new URLSearchParams({ academicYearId: yearId })
      if (busRouteId) q.set('busRouteId', busRouteId)
      const r = await api.get(`/school/reports/transport-students?${q}`)
      setRows(r.data?.data || [])
      setLoaded(true)
    } catch { message.error('Failed to load transport students report.') }
    finally { setLoading(false) }
  }

  const reportTitle = () => {
    const yr    = academicYears.find(y => y.academicYearId === yearId)?.academicYear || ''
    const route = routes.find(r => r.busRouteId === busRouteId)?.routeName
    return `Transport Students — ${route ? route + ' — ' : ''}${yr}`
  }

  const toExportRows = () => rows.map(r => COLUMNS.map(c => r[c.key] ?? ''))

  const handlePdf = () => {
    if (!rows.length) { message.warning('No data to export.'); return }
    exportPdf({
      schoolName,
      title:    reportTitle(),
      columns:  COLUMNS.map(c => c.label),
      rows:     toExportRows(),
      fileName: `transport_students_${yearId}.pdf`,
    })
  }

  const handleCsv = () => {
    if (!rows.length) { message.warning('No data to export.'); return }
    exportCsv({
      columns:  COLUMNS.map(c => c.label),
      rows:     toExportRows(),
      fileName: `transport_students_${yearId}.csv`,
    })
  }

  const tableColumns = [
    { title: 'Adm No',       dataIndex: 'admissionNo',  width: 100 },
    { title: 'Student Name', dataIndex: 'studentName',  width: 200 },
    { title: 'Class',        dataIndex: 'className',    width: 110 },
    { title: 'Section',      dataIndex: 'sectionName',  width: 80 },
    { title: 'Route',        dataIndex: 'routeName',    width: 150 },
    { title: 'Bus',          dataIndex: 'busName',      width: 100 },
    { title: 'Trip',         dataIndex: 'busTrip',      width: 80 },
    { title: 'Father Name',  dataIndex: 'fatherName',   width: 160 },
    { title: 'Mobile',       dataIndex: 'fatherMobile', width: 120 },
  ]

  return (
    <div>
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Select
            style={{ width: 180 }}
            placeholder="Academic Year *"
            value={yearId}
            onChange={v => { setYearId(v); setRows([]); setLoaded(false) }}
            options={academicYears.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
          />
        </Col>
        <Col>
          <Select
            style={{ width: 180 }}
            placeholder="All Routes"
            value={busRouteId}
            onChange={v => { setBusRouteId(v); setRows([]); setLoaded(false) }}
            options={routes.map(r => ({ label: r.routeName, value: r.busRouteId }))}
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
              {rows.length} student{rows.length !== 1 ? 's' : ''}
            </Text>
          </div>
          <Table
            rowKey="admissionNo"
            dataSource={rows}
            columns={tableColumns}
            size="small"
            pagination={{ pageSize: 50, showTotal: t => `${t} students` }}
            loading={loading}
            scroll={{ x: 'max-content' }}
          />
        </>
      )}

      {loaded && rows.length === 0 && !loading && (
        <Text type="secondary">No transport students found for the selected filters.</Text>
      )}
    </div>
  )
}
