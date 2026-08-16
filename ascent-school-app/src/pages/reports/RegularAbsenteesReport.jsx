import { useEffect, useState } from 'react'
import { Table, Select, Button, Space, Row, Col, Typography, App as AntApp, DatePicker, InputNumber, Tag, Tooltip } from 'antd'
import { FilePdfOutlined, FileExcelOutlined, SearchOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'
import api, { apiError } from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { Text } = Typography
const { RangePicker } = DatePicker

const CSV_COLUMNS = [
  { label: 'Adm No',        key: 'admissionNo'    },
  { label: 'Student Name',  key: 'studentName'    },
  { label: 'Class',         key: 'className'      },
  { label: 'Section',       key: 'sectionName'    },
  { label: 'Father Name',   key: 'fatherName'     },
  { label: 'Mobile',        key: 'fatherMobile'   },
  { label: 'Absent Days',   key: 'absentDays'     },
  { label: 'Last Absent',   key: 'lastAbsentDate' },
  { label: 'Status',        key: 'status'         },
]

export default function RegularAbsenteesReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [classes,   setClasses]   = useState([])
  const [sections,  setSections]  = useState([])
  const [dateRange, setDateRange] = useState([dayjs().startOf('month'), dayjs()])
  const [classId,   setClassId]   = useState(null)
  const [sectionId, setSectionId] = useState(null)
  const [minDays,   setMinDays]   = useState(3)
  const [data,      setData]      = useState([])
  const [loading,   setLoading]   = useState(false)

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
  }, [])

  const loadSections = async (cid) => {
    setSectionId(null)
    setSections([])
    if (!cid) return
    try {
      const r = await api.get(`/school/master/sections?classId=${cid}`)
      setSections(r.data?.data || [])
    } catch { /* ignore */ }
  }

  const load = async () => {
    if (!dateRange?.[0] || !dateRange?.[1]) { message.warning('Select a date range.'); return }
    setLoading(true)
    try {
      const params = new URLSearchParams({
        dateFrom: dateRange[0].format('YYYY-MM-DD'),
        dateTo:   dateRange[1].format('YYYY-MM-DD'),
        minDays:  minDays ?? 3,
      })
      if (classId)   params.append('classId',   classId)
      if (sectionId) params.append('sectionId', sectionId)
      const r = await api.get(`/school/reports/regular-absentees?${params}`)
      setData(r.data?.data || [])
    } catch (e) {
      message.error(apiError(e, 'Failed to load report.'))
    } finally {
      setLoading(false)
    }
  }

  const columns = [
    { title: '#',            key: 'idx',   width: 50,  render: (_, __, i) => i + 1 },
    { title: 'Adm No',       dataIndex: 'admissionNo',  width: 100 },
    { title: 'Student Name', dataIndex: 'studentName',  ellipsis: true },
    { title: 'Class',        dataIndex: 'className',    width: 90 },
    { title: 'Section',      dataIndex: 'sectionName',  width: 80 },
    { title: 'Father Name',  dataIndex: 'fatherName',   ellipsis: true },
    { title: 'Mobile',       dataIndex: 'fatherMobile', width: 125 },
    {
      title: 'Absent Days', dataIndex: 'absentDays', width: 110, align: 'center',
      sorter: (a, b) => b.absentDays - a.absentDays,
      render: v => (
        <Tag color={v >= 10 ? 'red' : v >= 5 ? 'orange' : 'gold'}>
          {v} {v === 1 ? 'day' : 'days'}
        </Tag>
      ),
    },
    { title: 'Last Absent',  dataIndex: 'lastAbsentDate', width: 110, align: 'center' },
    {
      title: 'Status', dataIndex: 'status', width: 90,
      render: v => (
        <Tag color={v === 'Active' ? 'green' : v === 'Blocked' ? 'red' : 'default'}>{v}</Tag>
      ),
    },
  ]

  const exportToPdf = () => {
    if (!data.length) return
    const from = dateRange[0].format('DD-MM-YYYY')
    const to   = dateRange[1].format('DD-MM-YYYY')
    exportPdf({
      title:      'Regular Absentees Report',
      subtitle:   `Date: ${from} to ${to}  |  Min Absent Days: ${minDays}`,
      schoolName,
      columns:    CSV_COLUMNS.map(c => c.label),
      rows:       data.map(r => CSV_COLUMNS.map(c => r[c.key] ?? '')),
      landscape:  true,
    })
  }

  const exportToCsv = () => {
    if (!data.length) return
    exportCsv({ columns: CSV_COLUMNS, rows: data, filename: 'regular_absentees' })
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <Row gutter={[12, 8]} align="bottom">
        <Col xs={24} sm={12} md={7}>
          <div style={{ marginBottom: 4 }}><Text strong>Date Range <span style={{ color: 'red' }}>*</span></Text></div>
          <RangePicker
            style={{ width: '100%' }}
            value={dateRange}
            onChange={v => setDateRange(v || [null, null])}
          />
        </Col>
        <Col xs={12} sm={6} md={3}>
          <div style={{ marginBottom: 4 }}>
            <Tooltip title="Show students absent at least this many days">
              <Text strong>Min Days</Text>
            </Tooltip>
          </div>
          <InputNumber
            min={1} max={365}
            style={{ width: '100%' }}
            value={minDays}
            onChange={v => setMinDays(v ?? 3)}
          />
        </Col>
        <Col xs={12} sm={6} md={4}>
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
        <Col xs={12} sm={6} md={3}>
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
        <Col xs={12} sm={6} md={4}>
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
        scroll={{ x: 'max-content' }}
        pagination={{ pageSize: 50, showTotal: t => `${t} students` }}
        rowClassName={r => r.status === 'Blocked' ? 'ant-table-row-blocked' : ''}
      />
    </div>
  )
}
