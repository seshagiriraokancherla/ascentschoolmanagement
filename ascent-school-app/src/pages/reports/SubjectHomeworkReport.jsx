import { useEffect, useState } from 'react'
import { DatePicker, Select, Button, Space, Row, Col, Table, Typography, Tag, App as AntApp } from 'antd'
import { FilePdfOutlined, FileTextOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportCsv } from './reportUtils'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import api from '../../api/axiosInstance'

const { RangePicker } = DatePicker
const { Text } = Typography

const SUBJECT_COLORS = [
  '#1677ff','#52c41a','#fa8c16','#eb2f96','#722ed1',
  '#13c2c2','#f5222d','#a0d911','#096dd9','#d4380d',
]

export default function SubjectHomeworkReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [classes,  setClasses]  = useState([])
  const [subjects, setSubjects] = useState([])
  const [classId,   setClassId]   = useState(null)
  const [subjectId, setSubjectId] = useState(null)
  const [dates,     setDates]     = useState(null)   // [dayjs, dayjs]

  const [rows,    setRows]    = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
    api.get('/school/master/subjects').then(r => setSubjects(r.data?.data || []))
  }, [])

  const handleLoad = async () => {
    if (!dates?.[0] || !dates?.[1]) {
      message.warning('Select a date range.')
      return
    }
    setLoading(true)
    setRows(null)
    try {
      const params = new URLSearchParams({
        dateFrom: dates[0].format('YYYY-MM-DD'),
        dateTo:   dates[1].format('YYYY-MM-DD'),
      })
      if (classId)   params.append('classId',   classId)
      if (subjectId) params.append('subjectId', subjectId)
      const r = await api.get(`/school/reports/homework-statement?${params}`)
      setRows(r.data?.data || [])
    } catch { message.error('Failed to load subject homework report.') }
    finally { setLoading(false) }
  }

  // ── Group rows by SubjectName ──────────────────────────────────────────────

  const grouped = rows
    ? Object.entries(
        rows.reduce((acc, r) => {
          acc[r.subjectName] = acc[r.subjectName] || []
          acc[r.subjectName].push(r)
          return acc
        }, {})
      ).sort(([a], [b]) => a.localeCompare(b))
    : []

  // ── Export PDF ─────────────────────────────────────────────────────────────

  const handlePdf = () => {
    if (!rows?.length) { message.warning('No data to export.'); return }

    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' })
    const pw  = doc.internal.pageSize.getWidth()

    let y = 14
    doc.setFontSize(13).setFont(undefined, 'bold')
    doc.text(schoolName || 'School', pw / 2, y, { align: 'center' }); y += 6
    doc.setFontSize(10).setFont(undefined, 'normal')
    const rangeLabel = `${dates[0].format('DD-MM-YYYY')} to ${dates[1].format('DD-MM-YYYY')}`
    doc.text(`Subject wise Homework Report — ${rangeLabel}`, pw / 2, y, { align: 'center' }); y += 7

    for (const [subject, group] of grouped) {
      // Subject header band
      doc.setFontSize(9).setFont(undefined, 'bold')
      doc.setFillColor(41, 128, 185)
      doc.setTextColor(255, 255, 255)
      doc.rect(14, y - 3, pw - 28, 7, 'F')
      doc.text(`${subject}  (${group.length} item${group.length !== 1 ? 's' : ''})`, 16, y + 1)
      doc.setTextColor(0, 0, 0).setFont(undefined, 'normal')
      y += 8

      autoTable(doc, {
        startY: y,
        head:   [['Class', 'Title', 'Description', 'Assigned Date', 'By']],
        body:   group.map(r => [
          r.className, r.title,
          r.description || '',
          r.assignedDate, r.assignedBy,
        ]),
        styles:     { fontSize: 8, cellPadding: 2 },
        headStyles: { fillColor: [230, 240, 255], textColor: 0, fontStyle: 'bold' },
        columnStyles: {
          0: { cellWidth: 24 },
          1: { cellWidth: 44 },
          2: { cellWidth: 62 },
          3: { cellWidth: 24 },
          4: { cellWidth: 24 },
        },
        margin: { left: 14, right: 14 },
      })

      y = doc.lastAutoTable.finalY + 8
      if (y > 260) { doc.addPage(); y = 14 }
    }

    doc.save(`subject_homework_${dates[0].format('YYYYMMDD')}_${dates[1].format('YYYYMMDD')}.pdf`)
  }

  // ── Export CSV ─────────────────────────────────────────────────────────────

  const handleCsv = () => {
    if (!rows?.length) { message.warning('No data to export.'); return }
    exportCsv({
      columns: ['Subject', 'Assigned Date', 'Class', 'Title', 'Description', 'Assigned By'],
      rows:    rows.map(r => [
        r.subjectName, r.assignedDate,
        r.className, r.title, r.description || '', r.assignedBy,
      ]),
      fileName: 'subject_homework_report.csv',
    })
  }

  // ── Table columns (inside each subject group) ──────────────────────────────

  const columns = [
    { title: 'Class',        dataIndex: 'className',   width: 100 },
    { title: 'Title',        dataIndex: 'title',       width: 200, ellipsis: true },
    { title: 'Description',  dataIndex: 'description', ellipsis: true,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Assigned Date', dataIndex: 'assignedDate', width: 110 },
    { title: 'Assigned By',   dataIndex: 'assignedBy',   width: 130, ellipsis: true },
  ]

  return (
    <div>
      {/* ── Filters ─────────────────────────────────────────────────────────── */}
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }} wrap>
        <Col>
          <RangePicker
            format="DD-MM-YYYY"
            value={dates}
            onChange={v => { setDates(v); setRows(null) }}
            placeholder={['From *', 'To *']}
            allowClear
          />
        </Col>
        <Col>
          <Select
            style={{ width: 150 }}
            placeholder="All Classes"
            value={classId}
            onChange={v => { setClassId(v); setRows(null) }}
            options={classes.map(c => ({ label: c.className, value: c.classId }))}
            allowClear
          />
        </Col>
        <Col>
          <Select
            style={{ width: 160 }}
            placeholder="All Subjects"
            value={subjectId}
            onChange={v => { setSubjectId(v); setRows(null) }}
            options={subjects.map(s => ({ label: s.subjectName, value: s.subjectId }))}
            showSearch
            filterOption={(input, opt) => opt.label.toLowerCase().includes(input.toLowerCase())}
            allowClear
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Load
          </Button>
        </Col>
        {rows?.length > 0 && (
          <Col style={{ marginLeft: 'auto' }}>
            <Space>
              <Button icon={<FilePdfOutlined />} onClick={handlePdf}>PDF</Button>
              <Button icon={<FileTextOutlined />} onClick={handleCsv}>CSV</Button>
            </Space>
          </Col>
        )}
      </Row>

      {/* ── Results ─────────────────────────────────────────────────────────── */}
      {rows !== null && rows.length === 0 && !loading && (
        <Text type="secondary">No homework found for the selected filters.</Text>
      )}

      {grouped.map(([subject, group], idx) => (
        <div key={subject} style={{ marginBottom: 24 }}>
          <div style={{
            background: '#e6f4ff', padding: '6px 12px', borderRadius: 4,
            marginBottom: 8, display: 'flex', alignItems: 'center', gap: 10,
            borderLeft: `4px solid ${SUBJECT_COLORS[idx % SUBJECT_COLORS.length]}`,
          }}>
            <Tag
              color={SUBJECT_COLORS[idx % SUBJECT_COLORS.length]}
              style={{ fontWeight: 700, fontSize: 12, margin: 0 }}
            >
              {subject}
            </Tag>
            <Text type="secondary" style={{ fontSize: 12 }}>
              {group.length} item{group.length !== 1 ? 's' : ''}
            </Text>
          </div>

          <Table
            rowKey={(r, i) => `${subject}-${i}`}
            dataSource={group}
            columns={columns}
            size="small"
            pagination={false}
            scroll={{ x: 'max-content' }}
          />
        </div>
      ))}

      {rows?.length > 0 && (
        <Text type="secondary" style={{ fontSize: 12 }}>
          {rows.length} homework item{rows.length !== 1 ? 's' : ''} across {grouped.length} subject{grouped.length !== 1 ? 's' : ''}
        </Text>
      )}
    </div>
  )
}
