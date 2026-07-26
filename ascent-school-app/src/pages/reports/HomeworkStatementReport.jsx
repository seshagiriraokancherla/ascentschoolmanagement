import { useEffect, useState } from 'react'
import { DatePicker, Select, Button, Space, Row, Col, Table, Typography, Tag, App as AntApp } from 'antd'
import { FilePdfOutlined, FileTextOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportCsv } from './reportUtils'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { RangePicker } = DatePicker
const { Text } = Typography

export default function HomeworkStatementReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [classes, setClasses] = useState([])
  const [classId, setClassId] = useState(null)
  const [dates,   setDates]   = useState(null)   // [dayjs, dayjs]

  const [rows,    setRows]    = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
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
      if (classId) params.append('classId', classId)
      const r = await api.get(`/school/reports/homework-statement?${params}`)
      setRows(r.data?.data || [])
    } catch { message.error('Failed to load homework statement.') }
    finally { setLoading(false) }
  }

  // ── Group rows by AssignedDate for display ────────────────────────────────

  const grouped = rows
    ? Object.entries(
        rows.reduce((acc, r) => {
          acc[r.assignedDate] = acc[r.assignedDate] || []
          acc[r.assignedDate].push(r)
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
    doc.text(`Day wise Homework Statement — ${rangeLabel}`, pw / 2, y, { align: 'center' }); y += 5
    if (classId) {
      const cls = classes.find(c => c.classId === classId)
      if (cls) { doc.setFontSize(9); doc.text(`Class: ${cls.className}`, pw / 2, y, { align: 'center' }); y += 4 }
    }
    y += 2

    // One autoTable per date group
    for (const [date, group] of grouped) {
      // Date header
      doc.setFontSize(9).setFont(undefined, 'bold')
      doc.setFillColor(230, 230, 230)
      doc.rect(14, y - 3, pw - 28, 7, 'F')
      doc.text(`Date: ${date}  (${group.length} item${group.length !== 1 ? 's' : ''})`, 16, y + 1)
      doc.setFont(undefined, 'normal')
      y += 7

      autoTable(doc, {
        startY: y,
        head:   [['Class', 'Subject', 'Title', 'Description', 'Assigned Date', 'By']],
        body:   group.map(r => [
          r.className, r.subjectName, r.title,
          r.description || '',
          r.assignedDate, r.assignedBy,
        ]),
        styles:     { fontSize: 8, cellPadding: 2 },
        headStyles: { fillColor: [41, 128, 185], textColor: 255, fontStyle: 'bold' },
        columnStyles: {
          0: { cellWidth: 22 },
          1: { cellWidth: 26 },
          2: { cellWidth: 40 },
          3: { cellWidth: 55 },
          4: { cellWidth: 22 },
          5: { cellWidth: 20 },
        },
        margin: { left: 14, right: 14 },
        didDrawPage: (d) => { y = d.cursor.y },
      })

      y = doc.lastAutoTable.finalY + 6
      if (y > 260) { doc.addPage(); y = 14 }
    }

    doc.save(`homework_${dates[0].format('YYYYMMDD')}_${dates[1].format('YYYYMMDD')}.pdf`)
  }

  // ── Export CSV ─────────────────────────────────────────────────────────────

  const handleCsv = () => {
    if (!rows?.length) { message.warning('No data to export.'); return }
    exportCsv({
      columns: ['Assigned Date', 'Class', 'Subject', 'Title', 'Description', 'Assigned By'],
      rows:    rows.map(r => [
        r.assignedDate, r.className, r.subjectName,
        r.title, r.description || '', r.assignedBy,
      ]),
      fileName: 'homework_statement.csv',
    })
  }

  // ── Table columns ──────────────────────────────────────────────────────────

  const columns = [
    { title: 'Class',         dataIndex: 'className',   width: 100 },
    { title: 'Subject',       dataIndex: 'subjectName', width: 120,
      render: v => <Tag color="blue" style={{ fontSize: 11 }}>{v}</Tag> },
    { title: 'Title',         dataIndex: 'title',       width: 200, ellipsis: true },
    { title: 'Description',   dataIndex: 'description', ellipsis: true,
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
        <Text type="secondary">No homework found for the selected date range.</Text>
      )}

      {grouped.map(([date, group]) => (
        <div key={date} style={{ marginBottom: 24 }}>
          <div style={{
            background: '#f5f5f5', padding: '6px 12px', borderRadius: 4,
            marginBottom: 8, display: 'flex', alignItems: 'center', gap: 10,
          }}>
            <Text strong style={{ fontSize: 13 }}>Date: {date}</Text>
            <Tag style={{ marginLeft: 4 }}>{group.length} item{group.length !== 1 ? 's' : ''}</Tag>
          </div>

          <Table
            rowKey={(r, i) => `${date}-${i}`}
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
          {rows.length} homework item{rows.length !== 1 ? 's' : ''} across {grouped.length} day{grouped.length !== 1 ? 's' : ''}
        </Text>
      )}
    </div>
  )
}
