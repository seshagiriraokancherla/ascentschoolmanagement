import { useEffect, useState } from 'react'
import { Select, Button, Space, Row, Col, Table, Typography, Tag, App as AntApp } from 'antd'
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

export default function ExamToppersReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [years,     setYears]     = useState([])
  const [classes,   setClasses]   = useState([])
  const [examTypes, setExamTypes] = useState([])

  const [yearId,     setYearId]     = useState(null)
  const [examTypeId, setExamTypeId] = useState(null)
  const [classId,    setClassId]    = useState(null)
  const [topN,       setTopN]       = useState(10)

  const [data,    setData]    = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years').then(r => setYears(r.data?.data || []))
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

  const handleLoad = async () => {
    if (!yearId || !examTypeId) {
      message.warning('Select Academic Year and Examination.')
      return
    }
    setLoading(true)
    setData(null)
    try {
      const params = new URLSearchParams({ academicYearId: yearId, examTypeId, topN })
      if (classId) params.append('classId', classId)
      const r = await api.get(`/school/reports/exam-toppers?${params}`)
      setData(r.data?.data)
    } catch { message.error('Failed to load toppers report.') }
    finally { setLoading(false) }
  }

  // ── Export helpers ─────────────────────────────────────────────────────────

  const handlePdf = () => {
    if (!data?.classes?.length) { message.warning('No data to export.'); return }
    exportToppersPdf({
      schoolName,
      examName:     data.examName,
      academicYear: data.academicYear,
      subjects:     data.subjects,
      classes:      (data.classes).map(cls => ({
        ...cls,
        rows: cls.rows.map(r => ({
          rank:          r.rank,
          admissionNo:   r.admissionNo,
          studentName:   r.studentName,
          subjectMarks:  r.subjectMarks,
          totalObtained: r.totalObtained,
          totalMax:      r.totalMax,
          percentage:    r.percentage,
        })),
      })),
    })
  }

  const handleCsv = () => {
    if (!data?.classes?.length) { message.warning('No data to export.'); return }
    const cols = ['Class', 'Section', 'Rank', 'Adm No', 'Name', ...(data.subjects || []), 'Total Obtained', 'Total Max', '%']
    const rows = (data.classes || []).flatMap(cls =>
      cls.rows.map(r => [
        cls.className,
        cls.sectionName,
        r.rank,
        r.admissionNo,
        r.studentName,
        ...(data.subjects || []).map(s => r.subjectMarks?.[s] ?? '—'),
        r.totalObtained,
        r.totalMax,
        `${r.percentage}%`,
      ])
    )
    exportCsv({ columns: cols, rows, fileName: 'exam_toppers.csv' })
  }

  // ── Build per-class table columns ──────────────────────────────────────────

  const buildColumns = (subjects) => [
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
    ...(subjects || []).map(subj => ({
      title: subj,
      key:   subj,
      width: 80,
      align: 'center',
      render: (_, row) => {
        const val = row.subjectMarks?.[subj] ?? '—'
        if (val === 'AB') return <Tag color="error" style={{ fontSize: 11 }}>AB</Tag>
        if (val === '—')  return <Text type="secondary">—</Text>
        const [obt, max] = val.split('/').map(Number)
        const pct = max > 0 ? obt / max : 0
        const color = pct >= 0.9 ? '#52c41a' : pct >= 0.6 ? '#1677ff' : pct >= 0.35 ? '#fa8c16' : '#ff4d4f'
        return <span style={{ color, fontWeight: 600, fontSize: 12 }}>{obt}<Text type="secondary" style={{ fontSize: 10 }}>/{max}</Text></span>
      },
    })),
    {
      title: 'Total',
      width: 80,
      align: 'center',
      render: (_, r) => <Text strong>{r.totalObtained}<Text type="secondary" style={{ fontSize: 10 }}>/{r.totalMax}</Text></Text>,
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
            onChange={v => { setClassId(v); setData(null) }}
            options={classes.map(c => ({ label: c.className, value: c.classId }))}
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
            <Text type="secondary">No marks data found for the selected exam.</Text>
          )}

          {(data.classes || []).map(cls => (
            <div key={`${cls.className}-${cls.sectionName}`} style={{ marginBottom: 28 }}>
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
                columns={buildColumns(data.subjects)}
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
              {data.classes?.length} class-section{data.classes?.length !== 1 ? 's' : ''}
            </Text>
          )}
        </div>
      )}
    </div>
  )
}
