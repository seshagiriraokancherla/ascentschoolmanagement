import { useEffect, useState } from 'react'
import {
  Select, Button, Table, Input, Checkbox,
  Row, Col, Typography, Divider, App as AntApp, Tag,
} from 'antd'
import { FilePdfOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { generateHallTickets } from './reportUtils'
import api, { apiError } from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { Text } = Typography

export default function ExamHallTicketReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [years,     setYears]     = useState([])
  const [classes,   setClasses]   = useState([])
  const [sections,  setSections]  = useState([])
  const [examTypes, setExamTypes] = useState([])

  const [yearId,     setYearId]     = useState(null)
  const [examTypeId, setExamTypeId] = useState(null)
  const [classId,    setClassId]    = useState(null)
  const [sectionId,  setSectionId]  = useState(null)

  const [students,       setStudents]       = useState([])
  const [schedule,       setSchedule]       = useState([])
  const [loaded,         setLoaded]         = useState(false)
  const [loading,        setLoading]        = useState(false)
  const [ticketsPerPage, setTicketsPerPage] = useState(2)

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
    setLoaded(false)
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
    setLoaded(false)
    if (!val) return
    try {
      const r = await api.get(`/school/master/sections?classId=${val}`)
      setSections(r.data?.data || [])
    } catch { setSections([]) }
  }

  const handleLoad = async () => {
    if (!yearId || !examTypeId || !classId || !sectionId) {
      message.warning('Select Academic Year, Examination, Class and Section.')
      return
    }
    setLoading(true)
    setLoaded(false)
    try {
      const [sRes, subRes] = await Promise.all([
        api.get(`/school/students?academicYearId=${yearId}&classId=${classId}&sectionId=${sectionId}&status=Active`),
        api.get('/school/master/subjects'),
      ])
      const studentList = sRes.data?.data  || []
      const subjectList = subRes.data?.data || []

      setStudents(studentList)
      setSchedule(subjectList.map(s => ({
        subjectId:   s.subjectId,
        subjectName: s.subjectName,
        included:    true,
        examDate:    '',
        time:        '',
        maxMarks:    '',
      })))
      setLoaded(true)
      if (!studentList.length) message.warning('No active students found for this selection.')
    } catch (e) { message.error(apiError(e, 'Failed to load data.')) }
    finally { setLoading(false) }
  }

  const updateSchedule = (subjectId, field, value) =>
    setSchedule(prev => prev.map(s => s.subjectId === subjectId ? { ...s, [field]: value } : s))

  const handleGenerate = () => {
    const included = schedule.filter(s => s.included)
    if (!students.length)   { message.warning('No students loaded.'); return }
    if (!included.length)   { message.warning('Select at least one subject.'); return }

    const year    = years.find(y => y.academicYearId === yearId)
    const exam    = examTypes.find(e => e.examTypeId === examTypeId)
    const cls     = classes.find(c => c.classId === classId)
    const sec     = sections.find(s => s.sectionId === sectionId)

    generateHallTickets({
      schoolName,
      className:      cls?.className    || '',
      sectionName:    sec?.sectionName  || '',
      academicYear:   year?.academicYear || '',
      examName:       exam?.examTypeName || exam?.name || '',
      schedule:       included,
      ticketsPerPage,
      students:       students.map(s => ({
        studentName: s.studentName,
        admissionNo: s.admissionNo,
        dateOfBirth: s.dateOfBirth ? dayjs(s.dateOfBirth).format('DD MMM YYYY') : '—',
        gender:      s.gender || '—',
      })),
    })
  }

  const scheduleColumns = [
    {
      title:    '',
      key:      'chk',
      width:    36,
      align:    'center',
      render:   (_, row) => (
        <Checkbox
          checked={row.included}
          onChange={e => updateSchedule(row.subjectId, 'included', e.target.checked)}
        />
      ),
    },
    { title: 'Subject', dataIndex: 'subjectName', width: 160 },
    {
      title:  'Exam Date',
      key:    'examDate',
      width:  150,
      render: (_, row) => (
        <Input
          size="small"
          placeholder="e.g. 10/06/2025"
          value={row.examDate}
          onChange={e => updateSchedule(row.subjectId, 'examDate', e.target.value)}
          disabled={!row.included}
        />
      ),
    },
    {
      title:  'Time',
      key:    'time',
      width:  180,
      render: (_, row) => (
        <Input
          size="small"
          placeholder="e.g. 10:00 AM – 12:00 PM"
          value={row.time}
          onChange={e => updateSchedule(row.subjectId, 'time', e.target.value)}
          disabled={!row.included}
        />
      ),
    },
    {
      title:  'Max Marks',
      key:    'maxMarks',
      width:  110,
      render: (_, row) => (
        <Input
          size="small"
          placeholder="100"
          value={row.maxMarks}
          onChange={e => updateSchedule(row.subjectId, 'maxMarks', e.target.value)}
          disabled={!row.included}
        />
      ),
    },
  ]

  const includedCount = schedule.filter(s => s.included).length
  const pageCount     = Math.ceil(students.length / ticketsPerPage)

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
            onChange={v => { setExamTypeId(v); setLoaded(false) }}
            options={examTypes.map(e => ({ label: e.examTypeName || e.name, value: e.examTypeId }))}
            disabled={!yearId || !examTypes.length}
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
            onChange={v => { setSectionId(v); setLoaded(false) }}
            options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
            disabled={sections.length === 0}
            allowClear
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Load
          </Button>
        </Col>
        <Col style={{ marginLeft: 8 }}>
          <Select
            style={{ width: 148 }}
            value={ticketsPerPage}
            onChange={setTicketsPerPage}
            options={[
              { value: 1, label: '1 ticket / page' },
              { value: 2, label: '2 tickets / page' },
              { value: 3, label: '3 tickets / page' },
            ]}
          />
        </Col>
      </Row>

      {/* ── Schedule editor ──────────────────────────────────────────────────── */}
      {loaded && (
        <>
          <Divider style={{ margin: '8px 0 12px' }}>
            <Text type="secondary" style={{ fontSize: 12 }}>
              {students.length} student{students.length !== 1 ? 's' : ''} loaded
              &nbsp;·&nbsp;
              Fill in the exam schedule below, then generate
            </Text>
          </Divider>

          <Table
            rowKey="subjectId"
            dataSource={schedule}
            columns={scheduleColumns}
            size="small"
            pagination={false}
            style={{ marginBottom: 20 }}
            rowClassName={row => !row.included ? 'ant-table-row-selected' : ''}
          />

          <Row justify="center" gutter={12} align="middle">
            <Col>
              <Text type="secondary" style={{ fontSize: 12 }}>
                {includedCount} subject{includedCount !== 1 ? 's' : ''} selected
                &nbsp;·&nbsp;
                <Tag color="blue">{students.length} tickets, {pageCount} page{pageCount !== 1 ? 's' : ''}</Tag>
                {ticketsPerPage === 3 && includedCount > 6 && (
                  <Tag color="warning" style={{ marginLeft: 4 }}>3/page: best with ≤6 subjects</Tag>
                )}
              </Text>
            </Col>
            <Col>
              <Button
                type="primary"
                size="large"
                icon={<FilePdfOutlined />}
                onClick={handleGenerate}
                disabled={!students.length || !includedCount}
              >
                Generate PDF ({students.length} tickets, {pageCount} page{pageCount !== 1 ? 's' : ''})
              </Button>
            </Col>
          </Row>
        </>
      )}
    </div>
  )
}
