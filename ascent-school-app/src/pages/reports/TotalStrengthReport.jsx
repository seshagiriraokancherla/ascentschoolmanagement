import { useEffect, useState } from 'react'
import { Table, Select, Button, Space, Row, Col, Typography, Divider, App as AntApp } from 'antd'
import { FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api, { apiError } from '../../api/axiosInstance'

const { Text } = Typography

const COLUMNS = [
  { label: 'Class',   key: 'className' },
  { label: 'Section', key: 'sectionName' },
  { label: 'Boys',    key: 'boys' },
  { label: 'Girls',   key: 'girls' },
  { label: 'Others',  key: 'others' },
  { label: 'Total',   key: 'total' },
]

export default function TotalStrengthReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [academicYears, setAcademicYears] = useState([])
  const [yearId,        setYearId]        = useState(null)
  const [rows,          setRows]          = useState([])
  const [loading,       setLoading]       = useState(false)
  const [loaded,        setLoaded]        = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const years = r.data?.data || []
      setAcademicYears(years)
      const current = years.find(y => y.isCurrent)
      if (current) setYearId(current.academicYearId)
    })
  }, [])

  const handleLoad = async () => {
    if (!yearId) { message.warning('Select an Academic Year.'); return }
    setLoading(true)
    try {
      const r = await api.get(`/school/reports/strength?academicYearId=${yearId}`)
      setRows(r.data?.data || [])
      setLoaded(true)
    } catch (e) { message.error(apiError(e, 'Failed to load strength report.')) }
    finally { setLoading(false) }
  }

  const yearLabel = () =>
    academicYears.find(y => y.academicYearId === yearId)?.academicYear || ''

  // Summary totals row
  const totals = rows.reduce(
    (acc, r) => ({
      boys:   acc.boys   + r.boys,
      girls:  acc.girls  + r.girls,
      others: acc.others + r.others,
      total:  acc.total  + r.total,
    }),
    { boys: 0, girls: 0, others: 0, total: 0 }
  )

  const toExportRows = () => [
    ...rows.map(r => COLUMNS.map(c => r[c.key] ?? '')),
    ['', 'TOTAL', totals.boys, totals.girls, totals.others, totals.total],
  ]

  const handlePdf = () => {
    if (!rows.length) { message.warning('No data to export.'); return }
    exportPdf({
      schoolName,
      title:    `Total Strength — ${yearLabel()}`,
      columns:  COLUMNS.map(c => c.label),
      rows:     toExportRows(),
      fileName: `total_strength_${yearId}.pdf`,
    })
  }

  const handleCsv = () => {
    if (!rows.length) { message.warning('No data to export.'); return }
    exportCsv({
      columns:  COLUMNS.map(c => c.label),
      rows:     toExportRows(),
      fileName: `total_strength_${yearId}.csv`,
    })
  }

  const tableColumns = [
    { title: 'Class',   dataIndex: 'className',   width: 160 },
    { title: 'Section', dataIndex: 'sectionName', width: 100 },
    { title: 'Boys',    dataIndex: 'boys',         width: 80,  align: 'center' },
    { title: 'Girls',   dataIndex: 'girls',        width: 80,  align: 'center' },
    { title: 'Others',  dataIndex: 'others',       width: 80,  align: 'center' },
    {
      title: 'Total', dataIndex: 'total', width: 80, align: 'center',
      render: v => <Text strong>{v}</Text>,
    },
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
            <Text type="secondary" style={{ marginLeft: 8 }}>
              Total Strength — {yearLabel()}
            </Text>
          </div>
          <Table
            rowKey={(r, i) => i}
            dataSource={rows}
            columns={tableColumns}
            size="small"
            pagination={false}
            loading={loading}
            summary={() => (
              <Table.Summary.Row style={{ fontWeight: 600, background: '#fafafa' }}>
                <Table.Summary.Cell index={0} colSpan={2}>Grand Total</Table.Summary.Cell>
                <Table.Summary.Cell index={2} align="center">{totals.boys}</Table.Summary.Cell>
                <Table.Summary.Cell index={3} align="center">{totals.girls}</Table.Summary.Cell>
                <Table.Summary.Cell index={4} align="center">{totals.others}</Table.Summary.Cell>
                <Table.Summary.Cell index={5} align="center">
                  <Text strong>{totals.total}</Text>
                </Table.Summary.Cell>
              </Table.Summary.Row>
            )}
          />
        </>
      )}

      {loaded && rows.length === 0 && !loading && (
        <Text type="secondary">No active students found for the selected year.</Text>
      )}
    </div>
  )
}
