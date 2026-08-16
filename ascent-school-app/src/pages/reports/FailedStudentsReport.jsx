import { useEffect, useState } from 'react'
import { Select, Button, Space, Row, Col, Table, Typography, Tag, Divider, App as AntApp } from 'antd'
import { FilePdfOutlined, FileTextOutlined, CloseCircleOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportCsv } from './reportUtils'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import api, { apiError } from '../../api/axiosInstance'

const { Text, Title } = Typography

const PASS_MARK_OPTIONS = [
  { value: 35, label: '35%' },
  { value: 40, label: '40%' },
  { value: 50, label: '50%' },
  { value: 60, label: '60%' },
]

export default function FailedStudentsReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [years,     setYears]     = useState([])
  const [classes,   setClasses]   = useState([])
  const [sections,  setSections]  = useState([])
  const [examTypes, setExamTypes] = useState([])

  const [yearId,      setYearId]      = useState(null)
  const [examTypeId,  setExamTypeId]  = useState(null)
  const [classId,     setClassId]     = useState(null)
  const [sectionId,   setSectionId]   = useState(null)
  const [passMarkPct, setPassMarkPct] = useState(35)

  const [data,    setData]    = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const years = r.data?.data || []
      setYears(years)
      const current = years.find(y => y.isCurrent)
      if (current) onYearChange(current.academicYearId)
    })
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
  }, [])

  const onYearChange = async (val) => {
    setYearId(val)
    setExamTypeId(null)
    setData(null)
    if (!val) { setExamTypes([]); return }
    try {
      const r = await api.get(`/school/marks/exam-types?academicYearId=${val}`)
      setExamTypes(r.data?.data || [])
    } catch { setExamTypes([]) }
  }

  const onClassChange = async (val) => {
    setClassId(val)
    setSectionId(null)
    setSections([])
    setData(null)
    if (!val) return
    try {
      const r = await api.get(`/school/master/sections?classId=${val}`)
      setSections(r.data?.data || [])
    } catch { setSections([]) }
  }

  const handleLoad = async () => {
    if (!yearId || !examTypeId) {
      message.warning('Select Academic Year and Examination.')
      return
    }
    setLoading(true)
    setData(null)
    try {
      const params = new URLSearchParams({ academicYearId: yearId, examTypeId, passMarkPct })
      if (classId)   params.append('classId', classId)
      if (sectionId) params.append('sectionId', sectionId)
      const r = await api.get(`/school/reports/failed-students?${params}`)
      setData(r.data?.data)
    } catch (e) { message.error(apiError(e, 'Failed to load failed students report.')) }
    finally { setLoading(false) }
  }

  // ── Export PDF ─────────────────────────────────────────────────────────────

  const handlePdf = () => {
    if (!data?.rows?.length) { message.warning('No data to export.'); return }

    const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' })
    const pw   = doc.internal.pageSize.getWidth()

    let y = 14
    doc.setFontSize(13).setFont(undefined, 'bold')
    doc.text(schoolName || 'School', pw / 2, y, { align: 'center' }); y += 6
    doc.setFontSize(10).setFont(undefined, 'normal')
    doc.text(`Failed Students — ${data.examName} (${data.academicYear})`, pw / 2, y, { align: 'center' }); y += 5
    doc.setFontSize(9)
    doc.text(`Pass Mark: ${data.passMarkPct}%  |  Total Failed: ${data.rows.length}`, pw / 2, y, { align: 'center' }); y += 4

    const subjectCols = (data.subjects || []).map(s => ({ header: s, dataKey: s }))
    const tableBody = data.rows.map((row, i) => {
      const obj = {
        '#':           i + 1,
        'Adm No':      row.admissionNo,
        'Name':        row.studentName,
        'Class':       `${row.className} ${row.sectionName}`,
        'Failed In':   row.failedSubjects?.join(', ') || '',
        'Failed':      row.failedCount,
        'Total':       `${row.totalObtained}/${row.totalMax}`,
        '%':           `${row.percentage}%`,
      }
      ;(data.subjects || []).forEach(s => { obj[s] = row.subjectMarks?.[s] ?? '—' })
      return obj
    })

    autoTable(doc, {
      startY: y,
      head: [[
        '#', 'Adm No', 'Name', 'Class',
        ...(data.subjects || []).map(s => s),
        'Failed', 'Total', '%', 'Failed In',
      ]],
      body: tableBody.map(r => [
        r['#'], r['Adm No'], r['Name'], r['Class'],
        ...(data.subjects || []).map(s => r[s]),
        r['Failed'], r['Total'], r['%'], r['Failed In'],
      ]),
      styles:       { fontSize: 7, cellPadding: 1.5 },
      headStyles:   { fillColor: [220, 53, 53], textColor: 255, fontStyle: 'bold' },
      columnStyles: { 0: { cellWidth: 8 }, 1: { cellWidth: 18 }, 2: { cellWidth: 36 } },
      alternateRowStyles: { fillColor: [255, 245, 245] },
    })

    doc.save(`failed_students_${data.examName.replace(/\s+/g, '_')}.pdf`)
  }

  // ── Export CSV ─────────────────────────────────────────────────────────────

  const handleCsv = () => {
    if (!data?.rows?.length) { message.warning('No data to export.'); return }
    const cols = [
      'Adm No', 'Name', 'Class', 'Section',
      ...(data.subjects || []),
      'Failed Count', 'Total Obtained', 'Total Max', '%', 'Failed Subjects',
    ]
    const rows = data.rows.map(r => [
      r.admissionNo,
      r.studentName,
      r.className,
      r.sectionName,
      ...(data.subjects || []).map(s => r.subjectMarks?.[s] ?? '—'),
      r.failedCount,
      r.totalObtained,
      r.totalMax,
      `${r.percentage}%`,
      r.failedSubjects?.join('; ') || '',
    ])
    exportCsv({ columns: cols, rows, fileName: 'failed_students.csv' })
  }

  // ── Table columns ──────────────────────────────────────────────────────────

  const buildColumns = (subjects, passMarkPct) => {
    const threshold = passMarkPct / 100
    return [
      { title: '#', width: 45, align: 'center', render: (_, __, i) => i + 1 },
      { title: 'Adm No',  dataIndex: 'admissionNo', width: 90 },
      { title: 'Name',    dataIndex: 'studentName', width: 180 },
      { title: 'Class',   width: 120, render: (_, r) => `${r.className} — ${r.sectionName}` },
      ...(subjects || []).map(subj => ({
        title: subj,
        key:   subj,
        width: 78,
        align: 'center',
        render: (_, row) => {
          const val = row.subjectMarks?.[subj] ?? '—'
          if (val === 'AB') return <Tag color="error" style={{ fontSize: 11 }}>AB</Tag>
          if (val === '—')  return <Text type="secondary">—</Text>
          const [obt, max] = val.split('/').map(Number)
          const passed = max > 0 && obt / max >= threshold
          const color  = passed ? '#52c41a' : '#ff4d4f'
          return (
            <span style={{ color, fontWeight: 600, fontSize: 12 }}>
              {obt}<Text type="secondary" style={{ fontSize: 10 }}>/{max}</Text>
            </span>
          )
        },
      })),
      {
        title: 'Total',
        width: 80,
        align: 'center',
        render: (_, r) => (
          <Text strong>
            {r.totalObtained}
            <Text type="secondary" style={{ fontSize: 10 }}>/{r.totalMax}</Text>
          </Text>
        ),
      },
      {
        title: '%',
        dataIndex: 'percentage',
        width: 60,
        align: 'center',
        render: v => {
          const color = v >= passMarkPct ? '#52c41a' : '#ff4d4f'
          return <Text strong style={{ color }}>{v}%</Text>
        },
      },
      {
        title: 'Failed In',
        dataIndex: 'failedCount',
        width: 80,
        align: 'center',
        render: (v) => (
          <Tag color="error" style={{ fontWeight: 700 }}>{v} subj</Tag>
        ),
      },
    ]
  }

  return (
    <div>
      {/* ── Filters ─────────────────────────────────────────────────────────── */}
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }} wrap>
        <Col>
          <Select
            style={{ width: 140 }}
            placeholder="Year *"
            value={yearId}
            onChange={onYearChange}
            options={years.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
            allowClear
          />
        </Col>
        <Col>
          <Select
            style={{ width: 190 }}
            placeholder="Examination *"
            value={examTypeId}
            onChange={v => { setExamTypeId(v); setData(null) }}
            options={examTypes.map(e => ({ label: e.examTypeName, value: e.examTypeId }))}
            disabled={!yearId || !examTypes.length}
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
            onChange={v => { setSectionId(v); setData(null) }}
            options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
            disabled={sections.length === 0}
            allowClear
          />
        </Col>
        <Col>
          <Select
            style={{ width: 90 }}
            value={passMarkPct}
            onChange={v => { setPassMarkPct(v); setData(null) }}
            options={PASS_MARK_OPTIONS}
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Load
          </Button>
        </Col>
        {data?.rows?.length > 0 && (
          <Col style={{ marginLeft: 'auto' }}>
            <Space>
              <Button icon={<FilePdfOutlined />} onClick={handlePdf}>PDF</Button>
              <Button icon={<FileTextOutlined />} onClick={handleCsv}>CSV</Button>
            </Space>
          </Col>
        )}
      </Row>

      {/* ── Results ─────────────────────────────────────────────────────────── */}
      {data && (
        <div>
          {data.rows?.length === 0 && !loading && (
            <Text type="secondary">
              No students failed below {data.passMarkPct}% in {data.examName}.
            </Text>
          )}

          {data.rows?.length > 0 && (
            <>
              <Row align="middle" style={{ marginBottom: 8 }}>
                <Col>
                  <CloseCircleOutlined style={{ color: '#ff4d4f', marginRight: 8, fontSize: 16 }} />
                  <Title level={5} style={{ display: 'inline', margin: 0 }}>
                    {data.examName}
                  </Title>
                  <Text type="secondary" style={{ marginLeft: 12, fontSize: 12 }}>
                    {data.academicYear} · Pass Mark: {data.passMarkPct}%
                  </Text>
                  <Tag color="error" style={{ marginLeft: 12, fontWeight: 700 }}>
                    {data.rows.length} failed
                  </Tag>
                </Col>
              </Row>

              <Table
                rowKey={(r, i) => `${r.admissionNo}-${i}`}
                dataSource={data.rows}
                columns={buildColumns(data.subjects, data.passMarkPct)}
                size="small"
                pagination={{ pageSize: 50, showSizeChanger: true, showTotal: t => `${t} students` }}
                scroll={{ x: 'max-content' }}
                rowClassName={() => 'failed-row'}
              />

              <Divider style={{ margin: '12px 0 8px' }} />
              <Text type="secondary" style={{ fontSize: 12 }}>
                {data.rows.length} student{data.rows.length !== 1 ? 's' : ''} failed in at least one subject
                (pass mark: {data.passMarkPct}%)
              </Text>
            </>
          )}
        </div>
      )}
    </div>
  )
}
