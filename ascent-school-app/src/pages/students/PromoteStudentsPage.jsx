import { useEffect, useState } from 'react'
import {
  Card, Select, Button, Table, Typography, Alert,
  Row, Col, App as AntApp, Tag, Statistic,
} from 'antd'
import { ArrowRightOutlined, CheckCircleOutlined } from '@ant-design/icons'
import api, { apiError } from '../../api/axiosInstance'

const { Title, Text } = Typography

export default function PromoteStudentsPage() {
  const { message, modal } = AntApp.useApp()

  const [academicYears,   setAcademicYears]   = useState([])
  const [classes,         setClasses]         = useState([])

  // Source
  const [fromSections,   setFromSections]   = useState([])
  const [fromYearId,     setFromYearId]     = useState(null)
  const [fromClassId,    setFromClassId]    = useState(null)
  const [fromSectionId,  setFromSectionId]  = useState(null)

  // Target
  const [toSections,     setToSections]     = useState([])
  const [toYearId,       setToYearId]       = useState(null)
  const [toClassId,      setToClassId]      = useState(null)
  const [toSectionId,    setToSectionId]    = useState(null)

  const [preview,   setPreview]   = useState(null)
  const [result,    setResult]    = useState(null)
  const [loading,   setLoading]   = useState(false)
  const [promoting, setPromoting] = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then((r) => {
      const years = r.data?.data || []
      setAcademicYears(years)
      const current = years.find(y => y.isCurrent)
      if (current) setFromYearId(current.academicYearId)
    })
    api.get('/school/master/classes').then((r) => setClasses(r.data?.data || []))
  }, [])

  const loadFromSections = async (classId) => {
    setFromSections([]); setFromSectionId(null)
    if (!classId) return
    try {
      const r = await api.get(`/school/master/sections?classId=${classId}`)
      setFromSections(r.data?.data || [])
    } catch { setFromSections([]) }
  }

  const loadToSections = async (classId) => {
    setToSections([]); setToSectionId(null)
    if (!classId) return
    try {
      const r = await api.get(`/school/master/sections?classId=${classId}`)
      setToSections(r.data?.data || [])
    } catch { setToSections([]) }
  }

  const loadPreview = async () => {
    if (!fromYearId || !fromClassId) {
      message.warning('Select source Academic Year and Class.')
      return
    }
    setLoading(true); setPreview(null); setResult(null)
    try {
      const params = new URLSearchParams({ fromYearId, fromClassId })
      if (fromSectionId) params.set('fromSectionId', fromSectionId)
      const r    = await api.get(`/school/students/promote/preview?${params}`)
      const body = r.data?.data || {}
      setPreview({ students: body.students || [], detainedCount: body.detainedCount || 0 })
    } catch (e) {
      message.error(apiError(e, 'Failed to load student list.'))
    } finally {
      setLoading(false)
    }
  }

  const handlePromote = () => {
    if (!toYearId || !toClassId || !toSectionId) {
      message.warning('Select target Academic Year, Class and Section.')
      return
    }

    const fromYearLabel    = academicYears.find((y) => y.academicYearId === fromYearId)?.academicYear
    const toYearLabel      = academicYears.find((y) => y.academicYearId === toYearId)?.academicYear
    const fromClassName    = classes.find((c) => c.classId === fromClassId)?.className
    const toClassName      = classes.find((c) => c.classId === toClassId)?.className
    const fromSectionLabel = fromSections.find((s) => s.sectionId === fromSectionId)?.sectionName
    const toSectionLabel   = toSections.find((s) => s.sectionId === toSectionId)?.sectionName

    const fromLabel = fromSectionLabel
      ? `${fromYearLabel} · ${fromClassName} — Section ${fromSectionLabel}`
      : `${fromYearLabel} · ${fromClassName} (all sections)`

    const toLabel = `${toYearLabel} · ${toClassName} — Section ${toSectionLabel}`

    modal.confirm({
      title:   'Confirm Promotion',
      content: (
        <div>
          <p>Promote <strong>{preview.students.length} students</strong>:</p>
          <p style={{ margin: '6px 0' }}>
            <Tag color="blue">{fromLabel}</Tag>
          </p>
          <p style={{ margin: '0 0 6px', textAlign: 'center' }}>
            <ArrowRightOutlined />
          </p>
          <p style={{ margin: '0 0 10px' }}>
            <Tag color="green">{toLabel}</Tag>
          </p>
          <p style={{ color: '#888', fontSize: 12 }}>
            Students already promoted to the target year will be skipped.
          </p>
        </div>
      ),
      okText:    'Promote',
      okType:    'primary',
      cancelText: 'Cancel',
      onOk:      doPromote,
    })
  }

  const doPromote = async () => {
    setPromoting(true)
    try {
      const r = await api.post('/school/students/promote', {
        fromAcademicYearId: fromYearId,
        fromClassId,
        fromSectionId:    fromSectionId || null,
        toAcademicYearId: toYearId,
        toClassId,
        toSectionId:      toSectionId || null,
      })
      setResult(r.data?.data)
      setPreview(null)
    } catch (e) {
      message.error(e.message || 'Promotion failed.')
    } finally {
      setPromoting(false)
    }
  }

  const reset = () => {
    setFromYearId(null); setFromClassId(null); setFromSectionId(null); setFromSections([])
    setToYearId(null);   setToClassId(null);   setToSectionId(null);   setToSections([])
    setPreview(null); setResult(null)
  }

  const yearOptions  = academicYears.map((y) => ({ value: y.academicYearId, label: y.academicYear }))
  const classOptions = classes.map((c) => ({ value: c.classId, label: c.className }))

  const fromSectionOptions = [
    { value: null, label: 'All Sections (entire class)' },
    ...fromSections.map((s) => ({ value: s.sectionId, label: `Section ${s.sectionName}` })),
  ]

  const toSectionOptions = toSections.map((s) => ({ value: s.sectionId, label: `Section ${s.sectionName}` }))

  const previewColumns = [
    { title: 'Admission No', dataIndex: 'admissionNo', key: 'admissionNo', width: 130 },
    { title: 'Student Name', dataIndex: 'studentName', key: 'studentName' },
    { title: 'Class',        dataIndex: 'className',   key: 'className',   width: 120 },
    {
      title: 'Current Section', dataIndex: 'sectionName', key: 'sectionName', width: 130,
      render: (v) => v
        ? <Tag>{v}</Tag>
        : <Text type="secondary">—</Text>,
    },
  ]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Promote Students</Title>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Promotion creates new student records for the next academic year, copying all personal details. Select the source (From) and target (To) class and section, then confirm."
      />

      {/* ── Step 1: Source ───────────────────────────────────────────────────── */}
      <Card title="Step 1 — Select Students to Promote" style={{ marginBottom: 16 }}>
        <Row gutter={16} align="bottom" wrap>
          <Col>
            <Text strong>From Year</Text>
            <Select
              style={{ display: 'block', width: 150, marginTop: 4 }}
              placeholder="Select year"
              value={fromYearId}
              options={yearOptions}
              onChange={(v) => { setFromYearId(v); setPreview(null); setResult(null) }}
            />
          </Col>
          <Col>
            <Text strong>From Class</Text>
            <Select
              style={{ display: 'block', width: 150, marginTop: 4 }}
              placeholder="Select class"
              value={fromClassId}
              options={classOptions}
              onChange={(v) => {
                setFromClassId(v); setPreview(null); setResult(null)
                loadFromSections(v)
              }}
            />
          </Col>
          <Col>
            <Text strong>From Section</Text>
            <Select
              style={{ display: 'block', width: 200, marginTop: 4 }}
              value={fromSectionId}
              options={fromSectionOptions}
              disabled={fromSections.length === 0}
              onChange={(v) => { setFromSectionId(v); setPreview(null); setResult(null) }}
            />
          </Col>
          <Col style={{ marginTop: 20 }}>
            <Button type="primary" loading={loading} onClick={loadPreview}>
              Preview Students
            </Button>
          </Col>
        </Row>
      </Card>

      {/* ── Preview table ────────────────────────────────────────────────────── */}
      {preview && (
        <>
          {preview.detainedCount > 0 && (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message={`${preview.detainedCount} detained student${preview.detainedCount !== 1 ? 's' : ''} excluded`}
              description="Detained students are not eligible for promotion and have been removed from this list. Release the detention from the Students page first if you want to include them."
            />
          )}

          <Card
            title={`${preview.students.length} eligible students`}
            style={{ marginBottom: 16 }}
          >
            {preview.students.length === 0
              ? <Text type="secondary">No eligible students found for the selected criteria.</Text>
              : (
                <Table
                  dataSource={preview.students}
                  columns={previewColumns}
                  rowKey="studentId"
                  size="small"
                  pagination={{ pageSize: 15, size: 'small' }}
                />
              )
            }
          </Card>

          {/* ── Step 2: Target ─────────────────────────────────────────────────── */}
          {preview.students.length > 0 && (
            <Card title="Step 2 — Select Promotion Target" style={{ marginBottom: 16 }}>
              {/* Row 1: dropdowns */}
              <Row gutter={16} style={{ marginBottom: 12 }}>
                <Col>
                  <Text strong>To Year</Text>
                  <Select
                    style={{ display: 'block', width: 150, marginTop: 4 }}
                    placeholder="Select year"
                    value={toYearId}
                    options={yearOptions}
                    onChange={setToYearId}
                  />
                </Col>
                <Col>
                  <Text strong>To Class</Text>
                  <Select
                    style={{ display: 'block', width: 150, marginTop: 4 }}
                    placeholder="Select class"
                    value={toClassId}
                    options={classOptions}
                    onChange={(v) => { setToClassId(v); loadToSections(v) }}
                  />
                </Col>
                <Col>
                  <Text strong>To Section</Text>
                  <Select
                    style={{ display: 'block', width: 200, marginTop: 4 }}
                    placeholder={!toClassId ? 'Select class first' : 'Select section'}
                    value={toSectionId}
                    options={toSectionOptions}
                    disabled={!toClassId}
                    onChange={setToSectionId}
                  />
                </Col>
              </Row>

              {/* Row 2: hint + buttons */}
              {toSectionId && (
                <Alert
                  type="info"
                  showIcon
                  style={{ marginBottom: 12 }}
                  message={`All ${preview.students.length} students will be promoted to ${toSections.find((s) => s.sectionId === toSectionId)?.sectionName} of the selected class.`}
                />
              )}

              <Row gutter={8}>
                <Col>
                  <Button
                    type="primary"
                    loading={promoting}
                    disabled={!toYearId || !toClassId || !toSectionId}
                    onClick={handlePromote}
                  >
                    Promote {preview.students.length} Students
                  </Button>
                </Col>
                <Col>
                  <Button onClick={reset}>Reset</Button>
                </Col>
              </Row>
            </Card>
          )}
        </>
      )}

      {/* ── Result ──────────────────────────────────────────────────────────── */}
      {result && (
        <Card
          title={<span><CheckCircleOutlined style={{ color: '#52c41a', marginRight: 8 }} />Promotion Complete</span>}
          extra={<Button onClick={reset}>Promote Another Batch</Button>}
        >
          <Row gutter={32}>
            <Col><Statistic title="Total Selected" value={result.total} /></Col>
            <Col><Statistic title="Promoted" value={result.promoted} valueStyle={{ color: '#52c41a' }} /></Col>
            <Col><Statistic title="Skipped (already existed)" value={result.skipped} valueStyle={{ color: '#faad14' }} /></Col>
          </Row>
          {result.skipped > 0 && (
            <Alert
              type="warning"
              showIcon
              style={{ marginTop: 16 }}
              message={`${result.skipped} students were skipped because a record already exists for the target academic year.`}
            />
          )}
        </Card>
      )}
    </div>
  )
}
