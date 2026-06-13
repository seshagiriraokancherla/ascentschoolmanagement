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
  { value: 0,  label: 'All'    },
]

const RANK_COLORS = { 1: '#ffd700', 2: '#c0c0c0', 3: '#cd7f32' }

export default function ClassToppersReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [years,    setYears]    = useState([])
  const [classes,  setClasses]  = useState([])
  const [sections, setSections] = useState([])

  const [yearId,    setYearId]    = useState(null)
  const [classId,   setClassId]   = useState(null)
  const [sectionId, setSectionId] = useState(null)
  const [topN,      setTopN]      = useState(5)

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
    if (!yearId || !classId || !sectionId) {
      message.warning('Select Academic Year, Class and Section.')
      return
    }
    setLoading(true)
    setData(null)
    try {
      const params = new URLSearchParams({ academicYearId: yearId, classId, sectionId, topN })
      const r = await api.get(`/school/reports/class-toppers?${params}`)
      setData(r.data?.data)
    } catch { message.error('Failed to load class toppers.') }
    finally { setLoading(false) }
  }

  // ── Export ─────────────────────────────────────────────────────────────────

  const handlePdf = () => {
    if (!data?.exams?.length) { message.warning('No data to export.'); return }
    exportToppersPdf({
      schoolName,
      subtitle:     `Class Toppers — ${data.className} — ${data.sectionName} (${data.academicYear})`,
      examName:     '',
      academicYear: data.academicYear,
      subjects:     data.subjects,
      // reuse "classes" structure: each "class group" is an exam group
      classes: data.exams.map(e => ({
        className:   e.examName,
        sectionName: '',
        rows:        e.rows.map(r => ({
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
    if (!data?.exams?.length) { message.warning('No data to export.'); return }
    const cols = ['Exam', 'Rank', 'Adm No', 'Name', ...(data.subjects || []), 'Total Obtained', 'Total Max', '%']
    const rows = (data.exams || []).flatMap(exam =>
      exam.rows.map(r => [
        exam.examName,
        r.rank,
        r.admissionNo,
        r.studentName,
        ...(data.subjects || []).map(s => r.subjectMarks?.[s] ?? '—'),
        r.totalObtained,
        r.totalMax,
        `${r.percentage}%`,
      ])
    )
    exportCsv({ columns: cols, rows, fileName: 'class_toppers.csv' })
  }

  // ── Table columns (shared across all exam sections) ───────────────────────

  const buildColumns = (subjects) => [
    {
      title: 'Rank',
      dataIndex: 'rank',
      width: 52,
      align: 'center',
      render: v => (
        <Tag style={{ background: RANK_COLORS[v] || '#f0f0f0', border: 'none', fontWeight: 700, color: v <= 3 ? '#333' : '#555', minWidth: 28, textAlign: 'center' }}>
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
        const pct   = max > 0 ? obt / max : 0
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

  const totalStudents = (data?.exams || []).reduce((s, e) => s + e.rows.length, 0)

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
            placeholder="Section *"
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
        {data?.exams?.length > 0 && (
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
          {!data.exams?.length && !loading && (
            <Text type="secondary">No marks data found for this class and section.</Text>
          )}

          {(data.exams || []).map((exam, ei) => (
            <div key={exam.examName} style={{ marginBottom: 28 }}>
              {ei > 0 && <Divider style={{ margin: '0 0 16px' }} />}
              <Row align="middle" style={{ marginBottom: 8 }}>
                <Col>
                  <TrophyOutlined style={{ color: '#ffd700', marginRight: 8, fontSize: 16 }} />
                  <Title level={5} style={{ display: 'inline', margin: 0 }}>
                    {exam.examName}
                  </Title>
                  <Text type="secondary" style={{ marginLeft: 12, fontSize: 12 }}>
                    {exam.rows.length} student{exam.rows.length !== 1 ? 's' : ''}
                  </Text>
                </Col>
              </Row>

              <Table
                rowKey={(r, i) => `${exam.examName}-${r.admissionNo}-${i}`}
                dataSource={exam.rows}
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
              {data.exams?.length} exam{data.exams?.length !== 1 ? 's' : ''} · {totalStudents} entries total
            </Text>
          )}
        </div>
      )}
    </div>
  )
}
