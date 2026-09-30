import { useEffect, useState } from 'react'
import {
  Card, Select, InputNumber, Button, Table, Checkbox,
  Space, Typography, Spin, App as AntApp, Row, Col, Tag,
} from 'antd'
import { SaveOutlined, SearchOutlined, FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons'
import api, { apiError } from '../../api/axiosInstance'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from '../reports/reportUtils'

const { Title, Text } = Typography

// 45.50 → "45.5", 45 → "45"
const num = v => String(Math.round(Number(v) * 100) / 100)

/**
 * Build the export table from a SAVED marks grid (server response, not the on-screen edits).
 * Subjects with an activity component get two columns (Act, Marks); others get one (Marks).
 * Total = Σ(marks + activity) over all subjects, out of Σ(max + activity max) — absent and
 * not-entered subjects count 0 but still carry their max.
 */
function buildMarksExport(grid) {
  const subjects = grid.subjects
  const grandMax = subjects.reduce(
    (s, sub) => s + Number(sub.maxMarks || 0) + (sub.hasActivity ? Number(sub.activityMaxMarks || 0) : 0), 0)

  const csvColumns = ['S.No', 'Adm No', 'Student Name']
  subjects.forEach(sub => {
    if (sub.hasActivity) csvColumns.push(`${sub.subjectName} Act (/${num(sub.activityMaxMarks)})`)
    csvColumns.push(`${sub.subjectName} Marks (/${num(sub.maxMarks)})`)
  })
  csvColumns.push(`Total (/${num(grandMax)})`, '%')

  const rows = grid.rows.map((row, i) => {
    const bySubject = Object.fromEntries(row.marks.map(c => [c.subjectId, c]))
    let total = 0
    let anySaved = false
    const cells = []
    subjects.forEach(sub => {
      const c = bySubject[sub.subjectId] || {}
      const saved = c.isAbsent || c.marksObtained != null || c.activityMarks != null
      if (saved) anySaved = true
      if (c.isAbsent) {
        if (sub.hasActivity) cells.push('AB')
        cells.push('AB')
        return
      }
      if (sub.hasActivity) cells.push(c.activityMarks != null ? num(c.activityMarks) : '-')
      cells.push(c.marksObtained != null ? num(c.marksObtained) : '-')
      total += Number(c.marksObtained || 0) + (sub.hasActivity ? Number(c.activityMarks || 0) : 0)
    })
    const pct = anySaved && grandMax > 0 ? ((total / grandMax) * 100).toFixed(2) : '-'
    return [i + 1, row.admissionNo || '', row.studentName || '', ...cells,
            anySaved ? num(total) : '-', pct]
  })

  // Two-row PDF header: subject name spans its Act/Marks sub-columns.
  const center = { halign: 'center', valign: 'middle' }
  const top = [
    { content: 'S.No',         rowSpan: 2, styles: center },
    { content: 'Adm No',       rowSpan: 2, styles: center },
    { content: 'Student Name', rowSpan: 2, styles: { valign: 'middle' } },
  ]
  const sub2 = []
  subjects.forEach(sub => {
    top.push({ content: sub.subjectName, colSpan: sub.hasActivity ? 2 : 1, styles: center })
    if (sub.hasActivity) sub2.push({ content: `Act /${num(sub.activityMaxMarks)}`, styles: center })
    sub2.push({ content: `Marks /${num(sub.maxMarks)}`, styles: center })
  })
  top.push({ content: `Total\n/${num(grandMax)}`, rowSpan: 2, styles: center },
           { content: '%',                        rowSpan: 2, styles: center })

  return { csvColumns, pdfHead: [top, sub2], rows, markColCount: sub2.length }
}

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

  const [grid,    setGrid]    = useState(null)   // { subjects: [], rows: [] }
  // key: `${studentId}_${subjectId}` → { marks, activity, absent }
  const [marks,   setMarks]   = useState({})
  const [loading, setLoading] = useState(false)
  const [saving,  setSaving]  = useState(false)
  const [dirty,   setDirty]   = useState(false)   // edits on screen not yet saved
  const [exporting, setExporting] = useState(null) // 'pdf' | 'csv' | null

  const schoolName = useBrandingStore(s => s.branding.displayName)

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const years = r.data?.data || []
      setAcademicYears(years)
      const current = years.find(y => y.isCurrent)
      if (current) setSelectedYear(current.academicYearId)
    })
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
  }, [])

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
      message.warning('Please select Academic Year, Exam, Class and Section.')
      return
    }
    setLoading(true)
    try {
      const r = await api.get(
        `/school/marks?classId=${selectedClass}&sectionId=${selectedSection}&examTypeId=${selectedExamType}&academicYearId=${selectedYear}`
      )
      const g = r.data.data
      setGrid(g)

      const initial = {}
      g.rows.forEach(row => {
        row.marks.forEach(cell => {
          initial[`${row.studentId}_${cell.subjectId}`] = {
            marks:    cell.marksObtained ?? '',
            activity: cell.activityMarks ?? '',
            absent:   cell.isAbsent,
          }
        })
      })
      setMarks(initial)
      setDirty(false)
    } catch (e) {
      message.error(apiError(e, 'Failed to load marks grid.'))
    } finally {
      setLoading(false)
    }
  }

  const patchCell = (studentId, subjectId, patch) => {
    const key = `${studentId}_${subjectId}`
    setMarks(prev => ({ ...prev, [key]: { ...prev[key], ...patch } }))
    setDirty(true)
  }

  // Exports only SAVED marks: re-fetches the grid from the server rather than reading the
  // on-screen edits (the loaded `grid` is also stale after a Save All).
  const handleExport = async (kind) => {
    if (dirty) message.warning('You have unsaved changes — they are not included. Click Save All first to include them.')
    setExporting(kind)
    try {
      const r = await api.get(
        `/school/marks?classId=${selectedClass}&sectionId=${selectedSection}&examTypeId=${selectedExamType}&academicYearId=${selectedYear}`
      )
      const saved = r.data.data
      if (!saved?.rows?.length || !saved?.subjects?.length) {
        message.info('Nothing to export.')
        return
      }
      const { csvColumns, pdfHead, rows, markColCount } = buildMarksExport(saved)

      const yearLabel    = academicYears.find(y => y.academicYearId === selectedYear)?.academicYear || ''
      const examLabel    = examTypes.find(e => e.examTypeId === selectedExamType)?.examTypeName || ''
      const classLabel   = classes.find(c => c.classId === selectedClass)?.className || ''
      const sectionLabel = sections.find(s => s.sectionId === selectedSection)?.sectionName || ''
      const baseName = `marks_${examLabel}_${classLabel}_${sectionLabel}_${yearLabel}`
        .replace(/[^A-Za-z0-9_-]+/g, '_')

      if (kind === 'csv') {
        exportCsv({ columns: csvColumns, rows, fileName: `${baseName}.csv` })
        return
      }

      // Shrink the font as the number of mark columns grows so the sheet fits A4 landscape.
      const fontSize = markColCount <= 10 ? 8 : markColCount <= 16 ? 7 : 6
      const colStyles = { 0: { halign: 'center', cellWidth: 9 }, 1: { cellWidth: 18 } }
      for (let c = 3; c < 3 + markColCount + 2; c++) colStyles[c] = { halign: 'center' }
      colStyles[3 + markColCount]     = { halign: 'center', fontStyle: 'bold' }  // Total
      colStyles[3 + markColCount + 1] = { halign: 'center', fontStyle: 'bold' }  // %

      exportPdf({
        schoolName: schoolName || 'School',
        title:  `Marks Statement — ${examLabel} (${yearLabel}) · Class ${classLabel} - ${sectionLabel}`,
        head:   pdfHead,
        rows,
        fileName: `${baseName}.pdf`,
        tableOptions: {
          margin: { left: 8, right: 8 },
          styles: { fontSize, cellPadding: 1.5, lineWidth: 0.1, lineColor: [200, 200, 200] },
          headStyles: { fillColor: [22, 119, 255], textColor: 255, fontStyle: 'bold',
                        lineWidth: 0.1, lineColor: [255, 255, 255] },
          columnStyles: colStyles,
        },
      })
    } catch (e) {
      message.error(apiError(e, 'Failed to export marks.'))
    } finally {
      setExporting(null)
    }
  }

  const handleAbsent = (studentId, subjectId, checked) => {
    patchCell(studentId, subjectId,
      checked ? { absent: true, marks: '', activity: '' } : { absent: false })
  }

  const handleSave = async () => {
    if (!grid) return
    setSaving(true)
    try {
      const entries = []
      grid.rows.forEach(row => {
        grid.subjects.forEach(sub => {
          const cell = marks[`${row.studentId}_${sub.subjectId}`] || {}
          const hasMark     = cell.marks    !== '' && cell.marks    != null
          const hasActivity = cell.activity !== '' && cell.activity != null
          // Skip cells with nothing entered (and not marked absent) — no junk 0 rows.
          if (!cell.absent && !hasMark && !hasActivity) return
          entries.push({
            studentId:        row.studentId,
            subjectId:        sub.subjectId,
            examId:           sub.examId ?? null,
            marksObtained:    cell.absent ? 0 : (parseFloat(cell.marks) || 0),
            maxMarks:         sub.maxMarks,
            activityMarks:    (sub.hasActivity && !cell.absent && hasActivity)
                                ? parseFloat(cell.activity) : null,
            activityMaxMarks: sub.hasActivity ? sub.activityMaxMarks : null,
            isAbsent:         cell.absent || false,
          })
        })
      })

      if (entries.length === 0) {
        message.warning('Enter at least one mark before saving.')
        setSaving(false)
        return
      }

      await api.post('/school/marks', {
        classId:        selectedClass,
        examTypeId:     selectedExamType,
        academicYearId: selectedYear,
        entries,
      })
      message.success('Marks saved successfully.')
      setDirty(false)
    } catch (e) {
      message.error(e.message || 'Failed to save marks.')
    } finally {
      setSaving(false)
    }
  }

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
          title: (
            <div style={{ textAlign: 'center' }}>
              <div>{sub.subjectName}</div>
              <Text type="secondary" style={{ fontSize: 11, fontWeight: 400 }}>
                Marks /{sub.maxMarks}{sub.hasActivity ? ` · Act /${sub.activityMaxMarks}` : ''}
              </Text>
            </div>
          ),
          key:   `sub_${sub.subjectId}`,
          width: sub.hasActivity ? 200 : 130,
          align: 'center',
          render: (_, row) => {
            const key  = `${row.studentId}_${sub.subjectId}`
            const cell = marks[key] || { marks: '', activity: '', absent: false }
            return (
              <Space direction="vertical" size={2} style={{ width: '100%' }}>
                <Space size={4} style={{ justifyContent: 'center' }}>
                  {sub.hasActivity && (
                    <InputNumber
                      size="small"
                      min={0}
                      max={sub.activityMaxMarks}
                      value={cell.absent ? null : cell.activity}
                      disabled={cell.absent}
                      onChange={v => patchCell(row.studentId, sub.subjectId, { activity: v })}
                      style={{ width: 64 }}
                      placeholder="Act"
                    />
                  )}
                  <InputNumber
                    size="small"
                    min={0}
                    max={sub.maxMarks}
                    value={cell.absent ? null : cell.marks}
                    disabled={cell.absent}
                    onChange={v => patchCell(row.studentId, sub.subjectId, { marks: v })}
                    style={{ width: 70 }}
                    placeholder="Marks"
                  />
                </Space>
                <Checkbox
                  checked={cell.absent}
                  onChange={e => handleAbsent(row.studentId, sub.subjectId, e.target.checked)}
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
            <Text strong>Exam</Text>
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
          <Col style={{ marginTop: 20 }}>
            <Button type="primary" icon={<SearchOutlined />} onClick={loadGrid} loading={loading}>
              Load
            </Button>
          </Col>
        </Row>
      </Card>

      {loading && <Spin />}

      {grid && !loading && grid.subjects.length > 0 && (
        <Card
          title={`${grid.rows.length} students · ${grid.subjects.length} subjects`}
          extra={
            <Space wrap>
              <Button icon={<FilePdfOutlined />} loading={exporting === 'pdf'}
                      disabled={grid.rows.length === 0 || exporting !== null}
                      onClick={() => handleExport('pdf')}>
                Export PDF
              </Button>
              <Button icon={<FileExcelOutlined />} loading={exporting === 'csv'}
                      disabled={grid.rows.length === 0 || exporting !== null}
                      onClick={() => handleExport('csv')}>
                Export CSV
              </Button>
              <Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={handleSave}>
                Save All
              </Button>
            </Space>
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

      {grid && !loading && grid.subjects.length === 0 && (
        <Card>
          <Text type="secondary">
            No subjects mapped to this class. Set them in <Tag>Master Data → Class Subjects</Tag> first.
          </Text>
        </Card>
      )}

      {grid && grid.rows.length === 0 && grid.subjects.length > 0 && !loading && (
        <Card>
          <Text type="secondary">No active students found for this class and section.</Text>
        </Card>
      )}
    </div>
  )
}
