import { useEffect, useState } from 'react'
import {
  Table, Button, Modal, Form, Input, InputNumber, Select, Tag, Popconfirm, Row, Col,
  Upload, Alert, Space, Typography, message as antMessage,
} from 'antd'
import {
  PlusOutlined, EditOutlined, DeleteOutlined, UploadOutlined, DownloadOutlined, ImportOutlined,
} from '@ant-design/icons'
import Papa from 'papaparse'
import api from '../../api/axiosInstance'

const { Text } = Typography

const TEMPLATE_HEADERS = ['GradeName', 'SubjectName', 'MinMarks', 'MaxMarks', 'Grade', 'Remarks', 'Status']
const TEMPLATE_EXAMPLE = [
  ['Distinction', '', '90', '100', 'A+', 'Excellent', 'Active'],
  ['First Class', '', '75', '89',  'A',  'Very Good', 'Active'],
  ['Second Class', '', '60', '74', 'B',  'Good',      'Active'],
  ['Pass', '', '35', '59', 'C', 'Pass', 'Active'],
  ['Fail', '', '0', '34', 'F', 'Needs Improvement', 'Active'],
]

// Download a CSV built entirely client-side.
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

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

export default function GradeTypesTab() {
  const [rows,        setRows]        = useState([])
  const [subjects,    setSubjects]    = useState([])
  const [filterSubject, setFilterSubject] = useState(null)
  const [loading,     setLoading]     = useState(false)
  const [open,        setOpen]        = useState(false)
  const [editing,     setEditing]     = useState(null)
  const [saving,      setSaving]      = useState(false)
  const [form] = Form.useForm()

  // Bulk import
  const [importOpen, setImportOpen]   = useState(false)
  const [parsedRows, setParsedRows]   = useState([])
  const [importing,  setImporting]    = useState(false)
  const [importResult, setImportResult] = useState(null)

  const subjectOptions = subjects.map((s) => ({ value: s.subjectId, label: s.subjectName }))

  useEffect(() => {
    api.get('/school/master/subjects').then((r) => setSubjects(r.data?.data || [])).catch(() => {})
    load(null)
  }, [])

  async function load(subjectId) {
    setLoading(true)
    try {
      const qs = subjectId ? `?subjectId=${subjectId}` : ''
      const { data } = await api.get(`/school/grade-types${qs}`)
      setRows(data.data || [])
    } finally {
      setLoading(false)
    }
  }

  function onFilterSubject(val) {
    setFilterSubject(val)
    load(val)
  }

  function openCreate() {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ status: 'Active' })
    setOpen(true)
  }

  function openEdit(record) {
    setEditing(record)
    form.setFieldsValue({
      gradeName: record.gradeName,
      subjectId: record.subjectId,
      minMarks:  record.minMarks,
      maxMarks:  record.maxMarks,
      grade:     record.grade,
      remarks:   record.remarks,
      status:    record.status || 'Active',
    })
    setOpen(true)
  }

  async function handleSave() {
    const v = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        gradeName: v.gradeName,
        subjectId: v.subjectId ?? null,
        minMarks:  v.minMarks ?? null,
        maxMarks:  v.maxMarks ?? null,
        grade:     v.grade || null,
        remarks:   v.remarks || null,
        status:    v.status,
      }
      let res
      if (editing) res = await api.put(`/school/grade-types/${editing.id}`, body)
      else         res = await api.post('/school/grade-types', body)
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  async function onDelete(record) {
    await api.delete(`/school/grade-types/${record.id}`)
    load(filterSubject)
  }

  // ── Bulk import ────────────────────────────────────────────────────────────

  function openImport() {
    setParsedRows([])
    setImportResult(null)
    setImportOpen(true)
  }

  function downloadTemplate() {
    downloadCsv('grade_types_template.csv', [TEMPLATE_HEADERS, ...TEMPLATE_EXAMPLE])
  }

  function beforeUpload(file) {
    Papa.parse(file, {
      header: true,
      skipEmptyLines: true,
      transformHeader: h => h.trim(),
      complete: (res) => {
        const rows = (res.data || []).map(r => ({
          gradeName:   (r.GradeName ?? '').trim(),
          subjectName: (r.SubjectName ?? '').trim(),
          minMarks:    r.MinMarks === '' || r.MinMarks == null ? null : Number(r.MinMarks),
          maxMarks:    r.MaxMarks === '' || r.MaxMarks == null ? null : Number(r.MaxMarks),
          grade:       (r.Grade ?? '').trim(),
          remarks:     (r.Remarks ?? '').trim(),
          status:      (r.Status ?? '').trim() || 'Active',
        })).filter(r => r.gradeName || r.grade || r.minMarks != null || r.maxMarks != null)
        setParsedRows(rows)
        setImportResult(null)
        if (rows.length === 0) antMessage.warning('No data rows found in the file.')
      },
      error: () => antMessage.error('Could not read the CSV file.'),
    })
    return false   // prevent AntD auto-upload
  }

  async function runImport() {
    if (parsedRows.length === 0) { antMessage.warning('Upload a CSV first.'); return }
    setImporting(true)
    try {
      const { data } = await api.post('/school/grade-types/bulk', { rows: parsedRows })
      setImportResult(data.data)
      load(filterSubject)
    } catch (e) {
      antMessage.error(e.message || 'Import failed.')
    } finally {
      setImporting(false)
    }
  }

  function downloadErrors() {
    const rows = [['Row', 'GradeName', 'Reason'],
      ...importResult.errors.map(e => [e.row, e.identifier, e.reason])]
    downloadCsv('grade_types_import_errors.csv', rows)
  }

  const columns = [
    { title: 'Grade Name', dataIndex: 'gradeName', key: 'gradeName' },
    { title: 'Subject', dataIndex: 'subjectName', key: 'subjectName',
      render: (v) => v || <Tag color="blue">All subjects</Tag> },
    { title: 'Min', dataIndex: 'minMarks', key: 'minMarks', width: 70, render: (v) => v ?? '—' },
    { title: 'Max', dataIndex: 'maxMarks', key: 'maxMarks', width: 70, render: (v) => v ?? '—' },
    { title: 'Grade', dataIndex: 'grade', key: 'grade', render: (v) => v || '—' },
    { title: 'Status', dataIndex: 'status', key: 'status',
      render: (v) => <Tag color={v === 'Active' ? 'green' : 'default'}>{v}</Tag> },
    {
      title: '', key: 'actions', width: 100,
      render: (_, record) => (
        <>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(record)} style={{ marginRight: 8 }} />
          <Popconfirm title="Delete this grade?" okText="Delete" okButtonProps={{ danger: true }}
            onConfirm={() => onDelete(record)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </>
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'center', flexWrap: 'wrap' }}>
        <Select
          placeholder="All subjects"
          style={{ width: 220 }}
          value={filterSubject}
          onChange={onFilterSubject}
          options={subjectOptions}
          allowClear
          showSearch
          optionFilterProp="label"
        />
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Grade</Button>
        <Button icon={<ImportOutlined />} onClick={openImport}>Bulk Import</Button>
      </div>

      <Table
        rowKey="id"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 15 }}
      />

      <Modal
        title={editing ? 'Edit Grade' : 'Add Grade'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        okText={editing ? 'Save' : 'Add'}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="gradeName" label="Grade Name" rules={[{ required: true, message: 'Grade name is required.' }]}>
            <Input placeholder="e.g. Distinction" maxLength={200} />
          </Form.Item>
          <Form.Item name="subjectId" label="Subject" extra="Leave empty to apply to all subjects.">
            <Select options={subjectOptions} placeholder="All subjects" allowClear showSearch optionFilterProp="label" />
          </Form.Item>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="minMarks" label="Min Marks">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="maxMarks" label="Max Marks">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="grade" label="Grade">
                <Input placeholder="e.g. A+" maxLength={100} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="remarks" label="Remarks">
            <Input.TextArea rows={2} maxLength={200} />
          </Form.Item>
          <Form.Item name="status" label="Status">
            <Select options={STATUS_OPTIONS} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Bulk Import Grades"
        open={importOpen}
        onCancel={() => setImportOpen(false)}
        width={720}
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
              Download the template, fill one row per grade band, and upload it.
              <b> SubjectName</b> is optional — leave it blank to apply the grade to all subjects
              (or type an exact subject name to scope it). Columns: {TEMPLATE_HEADERS.join(', ')}.
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
            style={{ marginBottom: 12 }}
            columns={[
              { title: 'Grade Name', dataIndex: 'gradeName' },
              { title: 'Subject', dataIndex: 'subjectName', render: v => v || <Tag color="blue">All</Tag> },
              { title: 'Min', dataIndex: 'minMarks', width: 60 },
              { title: 'Max', dataIndex: 'maxMarks', width: 60 },
              { title: 'Grade', dataIndex: 'grade', width: 70 },
              { title: 'Status', dataIndex: 'status', width: 80 },
            ]}
            footer={() => parsedRows.length > 10 ? `Showing first 10 of ${parsedRows.length} rows` : null}
          />
        )}

        {importResult && (
          <Alert
            type={importResult.failed > 0 ? 'warning' : 'success'}
            showIcon
            message={`Imported ${importResult.imported} of ${importResult.total}` +
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
                    { title: 'Grade Name', dataIndex: 'identifier' },
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
