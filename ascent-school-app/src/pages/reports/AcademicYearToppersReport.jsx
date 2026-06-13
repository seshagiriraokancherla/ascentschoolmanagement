import { useEffect, useState } from 'react'
import { Select, Button, Space, Row, Col, Table, Typography, Tag, Divider, App as AntApp } from 'antd'
import { FilePdfOutlined, FileTextOutlined, TrophyOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportToppersPdf, exportCsv } from './reportUtils'
import api from '../../api/axiosInstance'

const { Text, Title } = Typography

const TOP_N_OPTIONS = [
  { value: 3,  label: 'Top 3'  },
  { value: 5,  label: 'Top 5'  },
  { value: 10, label: 'Top 10' },
  { value: 20, label: 'Top 20' },
  { value: 0,  label: 'All'    },
]

const RANK_COLORS = { 1: '#ffd700', 2: '#c0c0c0', 3: '#cd7f32' }

export default function AcademicYearToppersReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [years,    setYears]    = useState([])
  const [classes,  setClasses]  = useState([])
  const [sections, setSections] = useState([])

  const [yearId,    setYearId]    = useState(null)
  const [classId,   setClassId]   = useState(null)
  const [sectionId, setSectionId] = useState(null)
  const [topN,      setTopN]      = useState(10)

  const [data,    setData]    = useState(null)
  const [loading, setLoading] = useState(false)

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
    setData(null)
    if (!val) return
    try {
      const r = await api.get(`/school/master/sections?classId=${val}`)
      setSections(r.data?.data || [])
    } catch { setSections([]) }
  }

  const handleLoad = async () => {
    if (!yearId) {
      message.warning('Select an Academic Year.')
      return
    }
    setLoading(true)
    setData(null)
    try {
      const params = new URLSearchParams({ academicYearId: yearId, topN })
      if (classId)   params.append('classId', classId)
      if (sectionId) params.append('sectionId', sectionId)
      const r = await api.get(`/school/reports/academic-year-toppers?${params}`)
      setData(r.data?.data)
    } catch { message.error('Failed to load academic year toppers.') }
    finally { setLoading(false) }
  }

  // ── Export PDF ─────────────────────────────────────────────────────────────
  // Reuse exportToppersPdf: treat each exam total column as a "subject" column.
  // ExamTotals values are "obtained/max" strings — same format as subjectMarks.

  const handlePdf = () => {
    if (!data?.classes?.length) { message.warning('No data to export.'); return }
    exportToppersPdf({
      schoolName,
      subtitle:     `Academic Year Toppers — ${data.academicYear}`,
      examName:     '',
      academicYear: data.academicYear,
      subjects:     data.examNames,
      classes: data.classes.map(cls => ({
        className:   cls.className,
        sectionName: cls.sectionName,
        rows: cls.rows.map(r => ({
          rank:          r.rank,
          admissionNo:   r.admissionNo,
          studentName:   r.studentName,
          subjectMarks:  r.examTotals,
          totalObtained: r.totalObtained,
          totalMax:      r.totalMax,
          percentage:    r.percentage,
        })),
      })),
    })
  }

  // ── Export CSV ─────────────────────────────────────────────────────────────

  const handleCsv = () => {
    if (!data?.classes?.length) { message.warning('No data to export.'); return }
    const cols = [
      'Class', 'Section', 'Rank', 'Adm No', 'Name',
      ...(data.examNames || []),
      'Total Obtained', 'Total Max', '%',
    ]
    const rows = (data.classes || []).flatMap(cls =>
      cls.rows.map(r => [
        cls.className,
        cls.sectionName,
        r.rank,
        r.admissionNo,
        r.studentName,
        ...(data.examNames || []).map(e => r.examTotals?.[e] ?? '—'),
        r.totalObtained,
        r.totalMax,
        `${r.percentage}%`,
      ])
    )
    exportCsv({ columns: cols, rows, fileName: `year_toppers_${data.academicYear}.csv` })
  }

  // ── Table columns ──────────────────────────────────────────────────────────

  const buildColumns = (examNames) => [
    {
      title: 'Rank',
      dataIndex: 'rank',
      width: 52,
      align: 'center',
      render: v => (
        <Tag
          style={{
            background: RANK_COLORS[v] || '#f0f0f0',
            border: 'none',
            fontWeight: 700,
            color: v <= 3 ? '#333' : '#555',
            minWidth: 28,
            textAlign: 'center',
          }}
        >
          {v <= 3 ? ['🥇','🥈','🥉'][v - 1] : `#${v}`}
        </Tag>
      ),
    },
    { title: 'Adm No', dataIndex: 'admissionNo', width: 90 },
    { title: 'Name',   dataIndex: 'studentName', width: 180 },
    ...(examNames || []).map(exam => ({
      title: exam,
      key:   exam,
      width: 90,
      align: 'center',
      render: (_, row) => {
        const val = row.examTotals?.[exam] ?? '—'
        if (val === '—') return <Text type="secondary">—</Text>
        const [obt, max] = val.split('/').map(Number)
        const pct   = max > 0 ? obt / max : 0
        const color = pct >= 0.9 ? '#52c41a' : pct >= 0.6 ? '#1677ff' : pct >= 0.35 ? '#fa8c16' : '#ff4d4f'
        return (
          <span style={{ color, fontWeight: 600, fontSize: 12 }}>
            {obt}<Text type="secondary" style={{ fontSize: 10 }}>/{max}</Text>
          </span>
        )
      },
    })),
    {
      title: 'Grand Total',
      width: 90,
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
        const color = v >= 90 ? '#52c41a' : v >= 60 ? '#1677ff' : v >= 35 ? '#fa8c16' : '#ff4d4f'
        return <Text strong style={{ color }}>{v}%</Text>
      },
    },
  ]

  const totalStudents = (data?.classes || []).reduce((s, c) => s + c.rows.length, 0)

  return (
    <div>
      {/* ── Filters ─────────────────────────────────────────────────────────── */}
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }} wrap>
        <Col>
          <Select
            style={{ width: 140 }}
            placeholder="Year *"
            value={yearId}
            onChange={v => { setYearId(v); setData(null) }}
            options={years.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
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
            style={{ width: 100 }}
            value={topN}
            onChange={setTopN}
            options={TOP_N_OPTIONS}
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Load
          </Button>
        </Col>
        {data?.classes?.length > 0 && (
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
          {data.classes?.length === 0 && !loading && (
            <Text type="secondary">No marks data found for the selected academic year.</Text>
          )}

          {(data.classes || []).map((cls, ci) => (
            <div key={`${cls.className}-${cls.sectionName}`} style={{ marginBottom: 28 }}>
              {ci > 0 && <Divider style={{ margin: '0 0 16px' }} />}
              <Row align="middle" style={{ marginBottom: 8 }}>
                <Col>
                  <TrophyOutlined style={{ color: '#ffd700', marginRight: 8, fontSize: 16 }} />
                  <Title level={5} style={{ display: 'inline', margin: 0 }}>
                    {cls.className} — {cls.sectionName}
                  </Title>
                  <Text type="secondary" style={{ marginLeft: 12, fontSize: 12 }}>
                    {cls.rows.length} student{cls.rows.length !== 1 ? 's' : ''}
                  </Text>
                </Col>
              </Row>

              <Table
                rowKey="admissionNo"
                dataSource={cls.rows}
                columns={buildColumns(data.examNames)}
                size="small"
                pagination={false}
                scroll={{ x: 'max-content' }}
                rowClassName={row => row.rank === 1 ? 'ant-table-row-selected' : ''}
              />
            </div>
          ))}

          {totalStudents > 0 && (
            <Text type="secondary" style={{ fontSize: 12 }}>
              {totalStudents} student{totalStudents !== 1 ? 's' : ''} across{' '}
              {data.classes?.length} class-section{data.classes?.length !== 1 ? 's' : ''} ·{' '}
              {data.examNames?.length} exam{data.examNames?.length !== 1 ? 's' : ''} combined
            </Text>
          )}
        </div>
      )}
    </div>
  )
}
