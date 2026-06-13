import { useEffect, useState, useRef } from 'react'
import {
  Card, Select, InputNumber, Button, Table, Checkbox,
  Space, Typography, Spin, App as AntApp, Row, Col, Form,
} from 'antd'
import { SaveOutlined, SearchOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography

export default function MarksEntryPage() {
  const { message } = AntApp.useApp()

  const [academicYears, setAcademicYears] = useState([])
  const [examTypes,     setExamTypes]     = useState([])
  const [classes,       setClasses]       = useState([])
  const [sections,      setSections]      = useState([])

  const [selectedYear,     setSelectedYear]     = useState(null)
  const [selectedExamType, setSelectedExamType] = useState(null)
  const [selectedClass,    setSelectedClass]    = useState(null)
  const [selectedSection,  setSelectedSection]  = useState(null)
  const [maxMarks,         setMaxMarks]         = useState(100)

  const [grid,    setGrid]    = useState(null)   // { subjects: [], rows: [] }
  const [marks,   setMarks]   = useState({})     // key: `${studentId}_${subjectId}` → { marks, absent }
  const [loading, setLoading] = useState(false)
  const [saving,  setSaving]  = useState(false)

  // Load dropdowns on mount
  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const years = r.data?.data || []
      setAcademicYears(years)
      const current = years.find(y => y.isCurrent)
      if (current) setSelectedYear(current.academicYearId)
    })
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
  }, [])

  // Load exam types when year changes
  useEffect(() => {
    if (!selectedYear) { setExamTypes([]); setSelectedExamType(null); return }
    api.get(`/school/marks/exam-types?academicYearId=${selectedYear}`)
       .then(r => setExamTypes(r.data.data || []))
  }, [selectedYear])

  async function loadSections(classId) {
    if (!classId) { setSections([]); return }
    try {
      const r = await api.get(`/school/master/sections?classId=${classId}`)
      setSections(r.data?.data || [])
    } catch { setSections([]) }
  }

  const loadGrid = async () => {
    if (!selectedYear || !selectedExamType || !selectedClass || !selectedSection) {
      message.warning('Please select Academic Year, Exam Type, Class and Section.')
      return
    }
    setLoading(true)
    try {
      const r = await api.get(
        `/school/marks?classId=${selectedClass}&sectionId=${selectedSection}&examTypeId=${selectedExamType}&academicYearId=${selectedYear}`
      )
      const g = r.data.data
      setGrid(g)

      // Initialise marks state from existing data
      const initial = {}
      g.rows.forEach(row => {
        row.marks.forEach(cell => {
          initial[`${row.studentId}_${cell.subjectId}`] = {
            marks:  cell.marksObtained ?? '',
            absent: cell.isAbsent,
          }
        })
      })
      setMarks(initial)
      if (g.rows.length > 0 && g.rows[0].marks.length > 0) {
        const firstMax = g.rows[0].marks[0].maxMarks
        if (firstMax > 0) setMaxMarks(firstMax)
      }
    } catch (e) {
      message.error('Failed to load marks grid.')
    } finally {
      setLoading(false)
    }
  }

  const handleMarkChange = (studentId, subjectId, value) => {
    setMarks(prev => ({
      ...prev,
      [`${studentId}_${subjectId}`]: { ...prev[`${studentId}_${subjectId}`], marks: value },
    }))
  }

  const handleAbsentChange = (studentId, subjectId, checked) => {
    setMarks(prev => ({
      ...prev,
      [`${studentId}_${subjectId}`]: { ...prev[`${studentId}_${subjectId}`], marks: checked ? 0 : '', absent: checked },
    }))
  }

  const handleSave = async () => {
    if (!grid) return
    setSaving(true)
    try {
      const entries = []
      grid.rows.forEach(row => {
        grid.subjects.forEach(sub => {
          const cell = marks[`${row.studentId}_${sub.subjectId}`] || {}
          entries.push({
            studentId:     row.studentId,
            subjectId:     sub.subjectId,
            marksObtained: cell.absent ? 0 : (parseFloat(cell.marks) || 0),
            isAbsent:      cell.absent || false,
          })
        })
      })

      await api.post('/school/marks', {
        classId:        selectedClass,
        examTypeId:     selectedExamType,
        academicYearId: selectedYear,
        maxMarks,
        entries,
      })
      message.success('Marks saved successfully.')
    } catch (e) {
      message.error(e.message || 'Failed to save marks.')
    } finally {
      setSaving(false)
    }
  }

  // Build Ant Design Table columns dynamically
  const columns = grid
    ? [
        {
          title: 'Student',
          dataIndex: 'studentName',
          key: 'studentName',
          fixed: 'left',
          width: 200,
          render: (name, row) => (
            <div>
              <div style={{ fontWeight: 500 }}>{name}</div>
              <Text type="secondary" style={{ fontSize: 12 }}>{row.admissionNo}</Text>
            </div>
          ),
        },
        ...grid.subjects.map(sub => ({
          title: sub.subjectName,
          key:   `sub_${sub.subjectId}`,
          width: 140,
          align: 'center',
          render: (_, row) => {
            const key    = `${row.studentId}_${sub.subjectId}`
            const cell   = marks[key] || { marks: '', absent: false }
            return (
              <Space direction="vertical" size={2}>
                <InputNumber
                  size="small"
                  min={0}
                  max={maxMarks}
                  value={cell.absent ? null : cell.marks}
                  disabled={cell.absent}
                  onChange={v => handleMarkChange(row.studentId, sub.subjectId, v)}
                  style={{ width: 70 }}
                  placeholder="—"
                />
                <Checkbox
                  checked={cell.absent}
                  onChange={e => handleAbsentChange(row.studentId, sub.subjectId, e.target.checked)}
                >
                  <Text style={{ fontSize: 11 }}>Absent</Text>
                </Checkbox>
              </Space>
            )
          },
        })),
      ]
    : []

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Marks Entry</Title>

      <Card style={{ marginBottom: 16 }}>
        <Row gutter={16} align="middle" wrap>
          <Col>
            <Text strong>Academic Year</Text>
            <Select
              style={{ display: 'block', width: 160, marginTop: 4 }}
              placeholder="Select year"
              value={selectedYear}
              onChange={v => { setSelectedYear(v); setSelectedExamType(null); setGrid(null) }}
              options={academicYears.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
            />
          </Col>
          <Col>
            <Text strong>Exam Type</Text>
            <Select
              style={{ display: 'block', width: 160, marginTop: 4 }}
              placeholder="Select exam"
              value={selectedExamType}
              onChange={v => { setSelectedExamType(v); setGrid(null) }}
              disabled={!selectedYear}
              options={examTypes.map(e => ({ label: e.examTypeName, value: e.examTypeId }))}
            />
          </Col>
          <Col>
            <Text strong>Class</Text>
            <Select
              style={{ display: 'block', width: 140, marginTop: 4 }}
              placeholder="Select class"
              value={selectedClass}
              onChange={v => {
                setSelectedClass(v)
                setSelectedSection(null)
                setGrid(null)
                loadSections(v)
              }}
              options={classes.map(c => ({ label: c.className, value: c.classId }))}
            />
          </Col>
          <Col>
            <Text strong>Section</Text>
            <Select
              style={{ display: 'block', width: 120, marginTop: 4 }}
              placeholder="Select section"
              value={selectedSection}
              disabled={sections.length === 0}
              onChange={v => { setSelectedSection(v); setGrid(null) }}
              options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
            />
          </Col>
          <Col>
            <Text strong>Max Marks</Text>
            <InputNumber
              style={{ display: 'block', width: 100, marginTop: 4 }}
              min={1}
              value={maxMarks}
              onChange={setMaxMarks}
            />
          </Col>
          <Col style={{ marginTop: 20 }}>
            <Button
              type="primary"
              icon={<SearchOutlined />}
              onClick={loadGrid}
              loading={loading}
            >
              Load
            </Button>
          </Col>
        </Row>
      </Card>

      {loading && <Spin />}

      {grid && !loading && (
        <Card
          title={`${grid.rows.length} students · ${grid.subjects.length} subjects`}
          extra={
            <Button
              type="primary"
              icon={<SaveOutlined />}
              loading={saving}
              onClick={handleSave}
            >
              Save All
            </Button>
          }
        >
          <Table
            dataSource={grid.rows}
            columns={columns}
            rowKey="studentId"
            pagination={false}
            scroll={{ x: 'max-content' }}
            size="middle"
          />
        </Card>
      )}

      {grid && grid.rows.length === 0 && !loading && (
        <Card>
          <Text type="secondary">No active students found for this class and section.</Text>
        </Card>
      )}
    </div>
  )
}
