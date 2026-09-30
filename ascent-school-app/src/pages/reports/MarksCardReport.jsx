import { useEffect, useState } from 'react'
import { Select, Button, Table, Row, Col, Alert, Space, Tag, Typography, App as AntApp } from 'antd'
import { PrinterOutlined, SearchOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import api, { apiError } from '../../api/axiosInstance'
import { generateMarksCards } from './reportUtils'
import hsChar from '../../assets/hs_char.png'

const { Text } = Typography

// School-specific right-side header image (the founder's photo on Holy Spirit's cards).
const RIGHT_IMAGE = import.meta.env.VITE_SUBDOMAIN === 'holyspiritjm' ? hsChar : null

export default function MarksCardReport() {
  const { message } = AntApp.useApp()
  const branding    = useBrandingStore(s => s.branding)

  const [years,     setYears]     = useState([])
  const [examTypes, setExamTypes] = useState([])
  const [classes,   setClasses]   = useState([])
  const [sections,  setSections]  = useState([])
  const [profile,   setProfile]   = useState(null)

  const [yearId,     setYearId]     = useState(null)
  const [examTypeId, setExamTypeId] = useState(null)
  const [classId,    setClassId]    = useState(null)
  const [sectionId,  setSectionId]  = useState(null)

  const [data,     setData]     = useState(null)   // MarksCardDataDto
  const [loading,  setLoading]  = useState(false)
  const [printing, setPrinting] = useState(null)   // 'all' | studentId | null

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const list = r.data?.data || []
      setYears(list)
      const current = list.find(y => y.isCurrent)
      if (current) onYearChange(current.academicYearId)
    }).catch(() => {})
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || [])).catch(() => {})
    // Branch address for the letterhead — optional; the card falls back to the name only.
    api.get('/school/settings/profile').then(r => setProfile(r.data?.data || null)).catch(() => {})
  }, [])

  async function onYearChange(v) {
    setYearId(v); setExamTypeId(null); setData(null)
    if (!v) { setExamTypes([]); return }
    try {
      const r = await api.get(`/school/marks/exam-types?academicYearId=${v}`)
      setExamTypes(r.data?.data || [])
    } catch { setExamTypes([]) }
  }

  async function onClassChange(v) {
    setClassId(v); setSectionId(null); setSections([]); setData(null)
    if (!v) return
    try {
      const r = await api.get(`/school/master/sections?classId=${v}`)
      setSections((r.data?.data || []).filter(s => s.status !== 'Inactive'))
    } catch { setSections([]) }
  }

  async function load() {
    if (!yearId || !examTypeId || !classId || !sectionId) {
      message.warning('Select Academic Year, Exam, Class and Section.')
      return
    }
    setLoading(true)
    try {
      const r = await api.get(
        `/school/marks-card?academicYearId=${yearId}&examTypeId=${examTypeId}&classId=${classId}&sectionId=${sectionId}`)
      setData(r.data?.data || null)
    } catch (e) {
      message.error(apiError(e, 'Failed to load marks cards.'))
    } finally {
      setLoading(false)
    }
  }

  async function print(students, key, fileName) {
    // Open the tab now, while we still have the click — browsers block popups opened
    // after an await. The PDF is loaded into it once generated.
    const win = window.open('', '_blank')
    if (win) win.document.write('<p style="font-family:sans-serif">Preparing marks cards…</p>')
    setPrinting(key)
    try {
      const address = [profile?.address, profile?.city, profile?.district, profile?.state]
        .filter(Boolean).join(', ') + (profile?.pinCode ? ` - ${profile.pinCode}` : '')

      const doc = await generateMarksCards({
        schoolName:    profile?.schoolName || branding?.displayName || 'School',
        address:       address.trim(),
        logoUrl:       branding?.logoPath,
        rightImageUrl: RIGHT_IMAGE,
        examTitle:     `PROGRESS REPORT - ${data.examTypeName || ''} (${data.academicYear || ''})`,
        className:     data.className,
        sectionName:   data.sectionName,
        subjects:      data.subjects,
        students,
        attendanceMonths: data.attendanceMonths || [],
        totalWorkingDays: data.totalWorkingDays || 0,
      })
      if (win) {
        doc.autoPrint()
        win.location.href = doc.output('bloburl')
      } else {
        doc.save(fileName)
      }
    } catch (e) {
      win?.close()
      message.error(apiError(e, 'Failed to generate marks cards.'))
    } finally {
      setPrinting(null)
    }
  }

  const safe = (s) => String(s || '').replace(/[^A-Za-z0-9_-]+/g, '_')
  const printAll = () => print(data.students, 'all',
    `marks_cards_${safe(data.examTypeName)}_${safe(data.className)}_${safe(data.sectionName)}.pdf`)
  const printOne = (st) => print([st], st.studentId,
    `marks_card_${safe(st.admissionNo)}_${safe(data.examTypeName)}.pdf`)

  const columns = [
    { title: 'S.No', key: 'sno', width: 60, render: (_, __, i) => i + 1 },
    { title: 'Adm No', dataIndex: 'admissionNo', width: 110 },
    { title: 'Student', dataIndex: 'studentName' },
    { title: 'Father Name', dataIndex: 'fatherName', render: v => v || '—' },
    { title: 'Total', key: 'total', width: 110, align: 'center',
      render: (_, r) => r.hasMarks ? `${Number(r.total)} / ${Number(r.maxTotal)}` : <Text type="secondary">No marks</Text> },
    { title: 'Grade', dataIndex: 'totalGrade', width: 80, align: 'center', render: v => v || '—' },
    { title: 'Rank', dataIndex: 'rank', width: 70, align: 'center', render: v => v ?? '—' },
    { title: 'Attendance', key: 'attendance', width: 110, align: 'center',
      render: (_, r) => data?.totalWorkingDays
        ? `${Number(r.totalPresent)} / ${data.totalWorkingDays}` : <Text type="secondary">Not marked</Text> },
    {
      title: '', key: 'print', width: 90,
      render: (_, r) => (
        <Button size="small" icon={<PrinterOutlined />} loading={printing === r.studentId}
                disabled={printing !== null} onClick={() => printOne(r)}>
          Print
        </Button>
      ),
    },
  ]

  const noGradeSubjects = (data?.subjects || []).filter(s => !s.hasGradeType).map(s => s.subjectName)

  return (
    <div>
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }} wrap>
        <Col>
          <Select style={{ width: 140 }} placeholder="Year *" value={yearId} onChange={onYearChange}
            options={years.map(y => ({ label: y.academicYear, value: y.academicYearId }))} />
        </Col>
        <Col>
          <Select style={{ width: 180 }} placeholder="Exam *" value={examTypeId}
            onChange={v => { setExamTypeId(v); setData(null) }} disabled={!yearId}
            options={examTypes.map(e => ({ label: e.examTypeName, value: e.examTypeId }))} />
        </Col>
        <Col>
          <Select style={{ width: 160 }} placeholder="Class *" value={classId} onChange={onClassChange}
            showSearch optionFilterProp="label"
            options={classes.map(c => ({ label: c.className, value: c.classId }))} />
        </Col>
        <Col>
          <Select style={{ width: 130 }} placeholder="Section *" value={sectionId}
            onChange={v => { setSectionId(v); setData(null) }} disabled={sections.length === 0}
            options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))} />
        </Col>
        <Col>
          <Button type="primary" icon={<SearchOutlined />} onClick={load} loading={loading}>Load</Button>
        </Col>
      </Row>

      {data && (
        <>
          <Space direction="vertical" style={{ width: '100%', marginBottom: 12 }}>
            {data.subjects.length === 0 && (
              <Alert type="warning" showIcon
                message="No subjects are mapped to this class for the year — set them in Master Data → Class Subjects." />
            )}
            {!data.scaleName ? (
              <Alert type="warning" showIcon
                message="No Total Grade Master scale is set on this exam, so the total grade will be blank."
                description="Open Master Data → Exam Master, edit this exam's rows and pick a Total Grade Master." />
            ) : !data.scaleHasBands ? (
              <Alert type="warning" showIcon
                message={`The scale "${data.scaleName}" has no bands, so the total grade will be blank.`}
                description="Add its bands in Master Data → Total Grade Master." />
            ) : data.scaleConflict && (
              <Alert type="warning" showIcon
                message={`This exam's subjects point at different grading scales — "${data.scaleName}" (used by most of them) is applied.`}
                description="Set the same Total Grade Master on every subject row in Exam Master." />
            )}
            {data.subjects.length > 0 && noGradeSubjects.length > 0 && (
              <Alert type="info" showIcon
                message={`Subject grade will be blank for: ${noGradeSubjects.join(', ')}.`}
                description="These subjects have no Subject Grade Master (with bands) on their Exam Master row for this exam." />
            )}
          </Space>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12, flexWrap: 'wrap', gap: 8 }}>
            <Space wrap>
              <Tag color="blue">{data.students.length} student{data.students.length === 1 ? '' : 's'}</Tag>
              {data.scaleName && <Tag>Total grade scale: {data.scaleName}</Tag>}
            </Space>
            <Button type="primary" icon={<PrinterOutlined />} loading={printing === 'all'}
                    disabled={data.students.length === 0 || printing !== null} onClick={printAll}>
              Print All ({data.students.length})
            </Button>
          </div>

          <Table
            rowKey="studentId"
            dataSource={data.students}
            columns={columns}
            size="small"
            pagination={false}
            scroll={{ x: 'max-content' }}
            locale={{ emptyText: 'No active students in this section for the selected year.' }}
          />
        </>
      )}
    </div>
  )
}
