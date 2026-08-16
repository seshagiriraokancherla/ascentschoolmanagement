import { useEffect, useState } from 'react'
import {
  Table, Button, Modal, Form, Select, Input, InputNumber, DatePicker,
  Tag, Popconfirm, Alert, Row, Col, Upload, Space, Typography, message as antMessage,
} from 'antd'
import {
  PlusOutlined, EditOutlined, DeleteOutlined,
  UploadOutlined, DownloadOutlined, ImportOutlined,
} from '@ant-design/icons'
import Papa from 'papaparse'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Text } = Typography

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

const IMPORT_HEADERS = [
  'AcademicYear', 'ExamType', 'Class', 'Subject', 'ExamName', 'Category', 'ExamDate',
  'TotalMarks', 'ExamMinMarks', 'SubjectMax', 'SubjectMin', 'ActivityMax', 'GradeType', 'Remarks', 'Status',
]
const IMPORT_EXAMPLE = [
  ['2026-27', 'FA-1', '1 Class', 'Mathematics', 'First Formative', 'Theory', '2026-08-10', '20', '7', '20', '7', '', '', '', 'Active'],
  ['2026-27', 'FA-1', '1 Class', 'English',     'First Formative', 'Theory', '2026-08-11', '20', '7', '20', '7', '', '', '', 'Active'],
]

function downloadCsv(filename, rows) {
  const csv = rows.map(r => r.map(c => {
    const v = (c ?? '').toString()
    return /[",\n]/.test(v) ? `"${v.replace(/"/g, '""')}"` : v
  }).join(',')).join('\n')
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = filename; a.click()
  URL.revokeObjectURL(url)
}

export default function ExamMasterTab() {
  const [years,      setYears]      = useState([])
  const [yearId,     setYearId]     = useState(null)
  const [examTypes,  setExamTypes]  = useState([])
  const [examTypeId, setExamTypeId] = useState(null)
  const [classes,    setClasses]    = useState([])
  const [classId,    setClassId]    = useState(null)
  const [gradeTypes, setGradeTypes] = useState([])
  const [rows,       setRows]       = useState([])
  const [loading,    setLoading]    = useState(false)

  const [open,     setOpen]     = useState(false)
  const [editing,  setEditing]  = useState(null)     // row being edited, or null for create
  const [saving,   setSaving]   = useState(false)
  const [modalSubjects, setModalSubjects] = useState([])
  const [form] = Form.useForm()

  // Bulk import
  const [importOpen,   setImportOpen]   = useState(false)
  const [parsedRows,   setParsedRows]   = useState([])
  const [importing,    setImporting]    = useState(false)
  const [importResult, setImportResult] = useState(null)

  const classOptions     = classes.map((c)   => ({ value: c.classId, label: c.className }))
  const examTypeOptions  = examTypes.map((e)  => ({ value: e.examTypeId, label: e.examTypeName }))
  const gradeTypeOptions = gradeTypes.map((g) => ({
    value: g.id,
    label: g.grade ? `${g.gradeName} (${g.grade})` : g.gradeName,
  }))

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true')
       .then((r) => {
         const list = r.data?.data || []
         setYears(list)
         const current = list.find((y) => y.isCurrent)
         if (current) {
           setYearId(current.academicYearId)
           loadExamTypes(current.academicYearId)
         }
       })
       .catch(() => {})
    api.get('/school/master/classes').then((r) => setClasses(r.data?.data || [])).catch(() => {})
    api.get('/school/grade-types').then((r) => setGradeTypes(r.data?.data || [])).catch(() => {})
  }, [])

  async function loadExamTypes(yId) {
    if (!yId) { setExamTypes([]); return }
    const { data } = await api.get(`/school/marks/exam-types?academicYearId=${yId}`)
    setExamTypes(data.data || [])
  }

  async function load(yId, etId, cId) {
    if (!yId) { setRows([]); return }
    setLoading(true)
    try {
      let qs = `academicYearId=${yId}`
      if (etId) qs += `&examTypeId=${etId}`
      if (cId)  qs += `&classId=${cId}`
      const { data } = await api.get(`/school/exam-master?${qs}`)
      setRows(data.data || [])
    } finally {
      setLoading(false)
    }
  }

  function onYearChange(val) {
    setYearId(val)
    setExamTypeId(null)
    loadExamTypes(val)
    load(val, null, classId)
  }
  function onExamTypeChange(val) { setExamTypeId(val); load(yearId, val, classId) }
  function onClassChange(val)    { setClassId(val);    load(yearId, examTypeId, val) }

  // Subjects mapped to the class — no academic-year filter (exam setup).
  async function loadModalSubjects(cId) {
    if (!cId) { setModalSubjects([]); return }
    const { data } = await api.get(`/school/class-subjects/for-class?classId=${cId}`)
    setModalSubjects(data.data || [])
  }

  async function openCreate() {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({
      examTypeId: examTypeId || undefined,
      classId:    classId || undefined,
      status:     'Active',
    })
    await loadModalSubjects(classId)
    setOpen(true)
  }

  async function openEdit(record) {
    setEditing(record)
    await loadModalSubjects(record.classId)
    form.setFieldsValue({
      examTypeId:      record.examTypeId,
      classId:         record.classId,
      subjectId:       record.subjectId,
      examName:        record.examName,
      examCategory:    record.examCategory,
      examDate:        record.examDate ? dayjs(record.examDate) : null,
      examTotalMarks:  record.examTotalMarks,
      examMinMarks:    record.examMinMarks,
      subMaxMarks:     record.subMaxMarks,
      subjectMinMarks: record.subjectMinMarks,
      activityMaxMarks: record.activityMaxMarks,
      gradeTypeId:     record.gradeTypeId,
      examRemarks:     record.examRemarks,
      status:          record.examStatus || 'Active',
    })
    setOpen(true)
  }

  async function onModalClassChange(cId) {
    form.setFieldsValue({ subjectId: undefined, subjectIds: undefined })
    await loadModalSubjects(cId)
  }

  async function onSave() {
    const v = await form.validateFields()
    setSaving(true)
    try {
      const common = {
        examName:        v.examName || null,
        examCategory:    v.examCategory || null,
        examDate:        v.examDate ? v.examDate.format('YYYY-MM-DD') : null,
        examTotalMarks:  v.examTotalMarks ?? null,
        examMinMarks:    v.examMinMarks ?? null,
        subMaxMarks:     v.subMaxMarks ?? null,
        subjectMinMarks: v.subjectMinMarks ?? null,
        activityMaxMarks: v.activityMaxMarks ?? null,
        gradeTypeId:     v.gradeTypeId ?? null,
        examRemarks:     v.examRemarks || null,
        examStatus:      v.status,
      }
      if (editing) {
        await api.put(`/school/exam-master/${editing.id}`, {
          examTypeId: v.examTypeId, classId: v.classId, academicYearId: yearId,
          subjectId: v.subjectId, ...common,
        })
      } else {
        await api.post('/school/exam-master', {
          examTypeId: v.examTypeId, classId: v.classId, academicYearId: yearId,
          subjectIds: v.subjectIds, ...common,
        })
      }
      setOpen(false)
      load(yearId, examTypeId, classId)
    } finally {
      setSaving(false)
    }
  }

  async function onDelete(record) {
    await api.delete(`/school/exam-master/${record.id}`)
    load(yearId, examTypeId, classId)
  }

  // ── Bulk import ────────────────────────────────────────────────────────────

  function openImport() {
    setParsedRows([])
    setImportResult(null)
    setImportOpen(true)
  }

  function downloadTemplate() {
    downloadCsv('exam_master_template.csv', [IMPORT_HEADERS, ...IMPORT_EXAMPLE])
  }

  const numOrNull = (v) => (v === '' || v == null ? null : Number(v))

  function beforeUpload(file) {
    Papa.parse(file, {
      header: true,
      skipEmptyLines: true,
      transformHeader: h => h.trim(),
      complete: (res) => {
        const rows = (res.data || []).map(r => ({
          academicYear: (r.AcademicYear ?? '').trim(),
          examType:     (r.ExamType ?? '').trim(),
          class:        (r.Class ?? '').trim(),
          subject:      (r.Subject ?? '').trim(),
          examName:     (r.ExamName ?? '').trim(),
          category:     (r.Category ?? '').trim(),
          examDate:     (r.ExamDate ?? '').trim(),
          totalMarks:   numOrNull(r.TotalMarks),
          examMinMarks: numOrNull(r.ExamMinMarks),
          subjectMax:   numOrNull(r.SubjectMax),
          subjectMin:   numOrNull(r.SubjectMin),
          activityMax:  numOrNull(r.ActivityMax),
          gradeType:    (r.GradeType ?? '').trim(),
          remarks:      (r.Remarks ?? '').trim(),
          status:       (r.Status ?? '').trim() || 'Active',
        })).filter(r => r.academicYear || r.examType || r.class || r.subject)
        setParsedRows(rows)
        setImportResult(null)
        if (rows.length === 0) antMessage.warning('No data rows found in the file.')
      },
      error: () => antMessage.error('Could not read the CSV file.'),
    })
    return false
  }

  async function runImport() {
    if (parsedRows.length === 0) { antMessage.warning('Upload a CSV first.'); return }
    setImporting(true)
    try {
      const { data } = await api.post('/school/exam-master/bulk', { rows: parsedRows })
      setImportResult(data.data)
      load(yearId, examTypeId, classId)
    } catch (e) {
      antMessage.error(e.message || 'Import failed.')
    } finally {
      setImporting(false)
    }
  }

  function downloadErrors() {
    const rows = [['Row', 'Exam/Class/Subject', 'Reason'],
      ...importResult.errors.map(e => [e.row, e.identifier, e.reason])]
    downloadCsv('exam_master_import_errors.csv', rows)
  }

  const columns = [
    { title: 'Exam Name', dataIndex: 'examName', key: 'examName', render: (v) => v || '—' },
    { title: 'Exam Type', dataIndex: 'examTypeName', key: 'examTypeName' },
    { title: 'Class',     dataIndex: 'className', key: 'className' },
    { title: 'Subject',   dataIndex: 'subjectName', key: 'subjectName' },
    { title: 'Date', dataIndex: 'examDate', key: 'examDate',
      render: (v) => (v ? dayjs(v).format('DD-MM-YYYY') : '—') },
    { title: 'Total', dataIndex: 'examTotalMarks', key: 'examTotalMarks', width: 70, render: (v) => v ?? '—' },
    { title: 'Sub Max', dataIndex: 'subMaxMarks', key: 'subMaxMarks', width: 80, render: (v) => v ?? '—' },
    { title: 'Act Max', dataIndex: 'activityMaxMarks', key: 'activityMaxMarks', width: 80, render: (v) => v ?? '—' },
    { title: 'Grade', dataIndex: 'gradeName', key: 'gradeName', render: (v) => v || '—' },
    { title: 'Status', dataIndex: 'examStatus', key: 'examStatus',
      render: (v) => <Tag color={v === 'Active' ? 'green' : 'default'}>{v}</Tag> },
    {
      title: '', key: 'actions', width: 100,
      render: (_, record) => (
        <>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(record)} style={{ marginRight: 8 }} />
          <Popconfirm title="Delete this exam?" okText="Delete" okButtonProps={{ danger: true }}
            onConfirm={() => onDelete(record)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </>
      ),
    },
  ]

  return (
    <div>
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Define exams per class and subject."
        description="Pick an academic year, exam type and class, then add the exam for one or more of that class's subjects. Subjects come from the Class Subjects mapping."
      />

      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'center', flexWrap: 'wrap' }}>
        <Select placeholder="Academic year" style={{ width: 170 }} value={yearId} onChange={onYearChange}
          options={years.map((y) => ({ value: y.academicYearId, label: y.academicYear }))} />
        <Select placeholder="All exam types" style={{ width: 190 }} value={examTypeId} onChange={onExamTypeChange}
          options={examTypeOptions} allowClear />
        <Select placeholder="All classes" style={{ width: 200 }} value={classId} onChange={onClassChange}
          options={classOptions} allowClear showSearch optionFilterProp="label" />
        <Button type="primary" icon={<PlusOutlined />} disabled={!yearId} onClick={openCreate}>Add Exam</Button>
        <Button icon={<ImportOutlined />} onClick={openImport}>Bulk Import</Button>
      </div>

      <Table
        rowKey="id"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 15 }}
        locale={{ emptyText: yearId ? 'No exams defined yet.' : 'Select an academic year.' }}
      />

      <Modal
        title={editing ? 'Edit Exam' : 'Add Exam'}
        open={open}
        onOk={onSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        okText={editing ? 'Save' : 'Add'}
        width={720}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="examTypeId" label="Exam Type" rules={[{ required: true, message: 'Exam type is required.' }]}>
                <Select options={examTypeOptions} placeholder="Select exam type" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="classId" label="Class" rules={[{ required: true, message: 'Class is required.' }]}>
                <Select options={classOptions} placeholder="Select class" showSearch optionFilterProp="label"
                  onChange={onModalClassChange} />
              </Form.Item>
            </Col>
          </Row>

          {editing ? (
            <Form.Item name="subjectId" label="Subject" rules={[{ required: true, message: 'Subject is required.' }]}>
              <Select
                placeholder="Select subject"
                options={modalSubjects.map((s) => ({ value: s.subjectId, label: s.subjectName }))}
                showSearch optionFilterProp="label"
              />
            </Form.Item>
          ) : (
            <Form.Item name="subjectIds" label="Subjects" rules={[{ required: true, message: 'Select at least one subject.' }]}
              extra="One exam row is created for each selected subject.">
              <Select
                mode="multiple"
                placeholder={modalSubjects.length ? 'Select subjects' : 'No subjects mapped to this class'}
                options={modalSubjects.map((s) => ({ value: s.subjectId, label: s.subjectName }))}
                showSearch optionFilterProp="label"
              />
            </Form.Item>
          )}

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="examName" label="Exam Name">
                <Input placeholder="e.g. First Formative Assessment" maxLength={200} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="examCategory" label="Category">
                <Input placeholder="e.g. Theory" maxLength={200} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="examDate" label="Exam Date">
                <DatePicker style={{ width: '100%' }} format="DD-MM-YYYY" />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={6}>
              <Form.Item name="examTotalMarks" label="Total Marks">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="examMinMarks" label="Exam Min Marks">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="subMaxMarks" label="Subject Max">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="subjectMinMarks" label="Subject Pass">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="gradeTypeId" label="Grade Type">
                <Select options={gradeTypeOptions} placeholder="Select grade type" allowClear showSearch
                  optionFilterProp="label" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="activityMaxMarks" label="Activity Max"
                extra="Set to enable an activity-marks column for this subject.">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="status" label="Status">
                <Select options={STATUS_OPTIONS} />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item name="examRemarks" label="Remarks">
            <Input.TextArea rows={2} maxLength={300} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Bulk Import Exams"
        open={importOpen}
        onCancel={() => setImportOpen(false)}
        width={760}
        footer={[
          <Button key="close" onClick={() => setImportOpen(false)}>Close</Button>,
          <Button key="import" type="primary" icon={<ImportOutlined />} loading={importing}
            disabled={parsedRows.length === 0} onClick={runImport}>
            Import {parsedRows.length > 0 ? `(${parsedRows.length})` : ''}
          </Button>,
        ]}
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="How it works"
          description={
            <div>
              One row per subject. <b>AcademicYear, ExamType, Class, Subject</b> are matched by name and
              required (the exam type must exist for that year, subjects/classes must exist).
              <b> GradeType</b> is optional (blank = none). A row whose year+exam type+class+subject already
              exists is <b>skipped</b>. Marks/date are optional. Columns: {IMPORT_HEADERS.join(', ')}.
            </div>
          }
        />

        <Space style={{ marginBottom: 12 }}>
          <Button icon={<DownloadOutlined />} onClick={downloadTemplate}>Download Template</Button>
          <Upload accept=".csv" showUploadList={false} beforeUpload={beforeUpload} maxCount={1}>
            <Button icon={<UploadOutlined />}>Upload CSV</Button>
          </Upload>
          {parsedRows.length > 0 && <Text type="secondary">{parsedRows.length} rows ready</Text>}
        </Space>

        {parsedRows.length > 0 && (
          <Table
            size="small"
            rowKey={(_, i) => i}
            dataSource={parsedRows.slice(0, 10)}
            pagination={false}
            scroll={{ x: 'max-content' }}
            style={{ marginBottom: 12 }}
            columns={[
              { title: 'Year', dataIndex: 'academicYear' },
              { title: 'Exam', dataIndex: 'examType' },
              { title: 'Class', dataIndex: 'class' },
              { title: 'Subject', dataIndex: 'subject' },
              { title: 'Exam Name', dataIndex: 'examName' },
              { title: 'Date', dataIndex: 'examDate', width: 100 },
              { title: 'Sub Max', dataIndex: 'subjectMax', width: 70 },
              { title: 'Act Max', dataIndex: 'activityMax', width: 70 },
            ]}
            footer={() => parsedRows.length > 10 ? `Showing first 10 of ${parsedRows.length} rows` : null}
          />
        )}

        {importResult && (
          <Alert
            type={importResult.failed > 0 ? 'warning' : 'success'}
            showIcon
            message={`Imported ${importResult.imported} of ${importResult.total}` +
              (importResult.skipped > 0 ? ` · ${importResult.skipped} skipped (already exist)` : '') +
              (importResult.failed > 0 ? ` · ${importResult.failed} failed` : '')}
            description={importResult.failed > 0 && (
              <div>
                <Button size="small" icon={<DownloadOutlined />} onClick={downloadErrors} style={{ marginTop: 8 }}>
                  Download errors
                </Button>
                <Table
                  size="small"
                  rowKey={(_, i) => i}
                  style={{ marginTop: 8 }}
                  pagination={{ pageSize: 5 }}
                  dataSource={importResult.errors}
                  columns={[
                    { title: 'Row', dataIndex: 'row', width: 60 },
                    { title: 'Exam/Class/Subject', dataIndex: 'identifier' },
                    { title: 'Reason', dataIndex: 'reason' },
                  ]}
                />
              </div>
            )}
          />
        )}
      </Modal>
    </div>
  )
}
