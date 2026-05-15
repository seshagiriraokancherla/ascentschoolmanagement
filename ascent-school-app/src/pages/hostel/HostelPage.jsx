import { useState, useEffect, useRef } from 'react'
import {
  Card, Tabs, Table, Button, Modal, Form, Input, InputNumber,
  Select, Space, Typography, Row, Col, Tag, Alert, Upload,
  message as antMessage, App as AntApp,
} from 'antd'
import {
  PlusOutlined, EditOutlined, SaveOutlined, UploadOutlined,
} from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography
const { Option }      = Select
const { TextArea }    = Input

// ── Hostels CRUD ─────────────────────────────────────────────────────────────

function HostelsTab({ onHostelsChange }) {
  const { message, modal } = AntApp.useApp()
  const [hostels,  setHostels]  = useState([])
  const [loading,  setLoading]  = useState(false)
  const [showForm, setShowForm] = useState(false)
  const [editing,  setEditing]  = useState(null)
  const [saving,   setSaving]   = useState(false)
  const [form]                  = Form.useForm()

  const load = async () => {
    setLoading(true)
    try {
      const r = await api.get('/school/hostel')
      const list = r.data?.data || []
      setHostels(list)
      onHostelsChange(list)
    } finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  const openCreate = () => { setEditing(null); form.resetFields(); setShowForm(true) }
  const openEdit   = (h)  => { setEditing(h); form.setFieldsValue(h); setShowForm(true) }

  const handleSave = async (values) => {
    setSaving(true)
    try {
      if (editing) {
        await api.put(`/school/hostel/${editing.hostelId}`, values)
        message.success('Hostel updated')
      } else {
        await api.post('/school/hostel', values)
        message.success('Hostel created')
      }
      setShowForm(false)
      await load()
    } catch (e) { message.error(e.message || 'Save failed') }
    finally     { setSaving(false) }
  }

  const cols = [
    { title: 'Hostel Name', dataIndex: 'hostelName', key: 'name', sorter: (a, b) => a.hostelName.localeCompare(b.hostelName) },
    { title: 'Description', dataIndex: 'description', key: 'desc' },
    { title: 'Capacity',    dataIndex: 'capacity',    key: 'cap',  width: 90 },
    { title: 'No. of Rooms', dataIndex: 'noOfRooms',  key: 'rooms', width: 110 },
    { title: 'Contact No',  dataIndex: 'contactNo',   key: 'contact' },
    { title: 'Address',     dataIndex: 'address',     key: 'addr' },
    {
      title: '', key: 'action', width: 60,
      render: (_, h) => (
        <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(h)} />
      ),
    },
  ]

  return (
    <>
      <div style={{ marginBottom: 12, textAlign: 'right' }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
          Add Hostel
        </Button>
      </div>
      <Table
        rowKey="hostelId"
        dataSource={hostels}
        columns={cols}
        loading={loading}
        size="small"
        pagination={false}
        scroll={{ x: 'max-content' }}
      />

      <Modal
        open={showForm}
        title={editing ? 'Edit Hostel' : 'Add Hostel'}
        onCancel={() => setShowForm(false)}
        footer={null}
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={handleSave} style={{ marginTop: 8 }}>
          <Form.Item name="hostelName" label="Hostel Name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <TextArea rows={2} />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="capacity" label="Capacity">
                <InputNumber style={{ width: '100%' }} min={0} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="noOfRooms" label="No. of Rooms">
                <InputNumber style={{ width: '100%' }} min={0} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="contactNo" label="Contact No">
            <Input />
          </Form.Item>
          <Form.Item name="address" label="Address">
            <TextArea rows={2} />
          </Form.Item>
          <div style={{ textAlign: 'right' }}>
            <Space>
              <Button onClick={() => setShowForm(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit" icon={<SaveOutlined />} loading={saving}>
                Save
              </Button>
            </Space>
          </div>
        </Form>
      </Modal>
    </>
  )
}

// ── Fee Structure ─────────────────────────────────────────────────────────────

function FeeStructureTab({ hostels }) {
  const { message } = AntApp.useApp()
  const [years,      setYears]      = useState([])
  const [terms,      setTerms]      = useState([])
  const [feePeriods, setFeePeriods] = useState([])
  const [selHostel,  setSelHostel]  = useState(null)
  const [selYear,    setSelYear]    = useState(null)
  const [payType,    setPayType]    = useState('Term')  // Term | Monthly
  const [structure,  setStructure]  = useState([])
  const [amounts,    setAmounts]    = useState({})      // key → amount
  const [loading,    setLoading]    = useState(false)
  const [saving,     setSaving]     = useState(false)

  useEffect(() => {
    api.get('/school/master/academic-years').then((r) => setYears(r.data?.data || [])).catch(() => {})
  }, [])

  useEffect(() => {
    if (!selYear) { setTerms([]); setFeePeriods([]); return }
    Promise.all([
      api.get(`/school/master/terms?academicYearId=${selYear}`),
      api.get(`/school/master/fee-periods?academicYearId=${selYear}`),
    ]).then(([t, fp]) => {
      setTerms(t.data?.data       || [])
      setFeePeriods(fp.data?.data || [])
    }).catch(() => {})
  }, [selYear])

  const loadStructure = async () => {
    if (!selHostel || !selYear) { message.warning('Select hostel and academic year'); return }
    setLoading(true)
    try {
      const r = await api.get(`/school/hostel/fee-structure?hostelId=${selHostel}&academicYearId=${selYear}`)
      const rows = r.data?.data || []
      setStructure(rows)
      const cols = payType === 'Monthly' ? feePeriods : terms
      const map  = {}
      for (const row of rows) {
        const k = payType === 'Monthly'
          ? `P_${row.feePeriodId}`
          : `T_${row.termId}`
        map[k] = row.amount
      }
      setAmounts(map)
    } finally { setLoading(false) }
  }

  const handleSave = async () => {
    if (!selHostel || !selYear) { message.warning('Select hostel and academic year'); return }
    const cols = payType === 'Monthly' ? feePeriods : terms
    const items = cols.map((col) => {
      const k = payType === 'Monthly' ? `P_${col.feePeriodId}` : `T_${col.termId}`
      return payType === 'Monthly'
        ? { FeePeriodId: col.feePeriodId, TermId: null, Amount: amounts[k] || 0 }
        : { TermId: col.termId, FeePeriodId: null, Amount: amounts[k] || 0 }
    })
    setSaving(true)
    try {
      await api.post('/school/hostel/fee-structure', {
        HostelId:      selHostel,
        AcademicYearId: selYear,
        PaymentType:   payType,
        Items:         items,
      })
      message.success('Fee structure saved')
      await loadStructure()
    } catch (e) { message.error(e.message || 'Save failed') }
    finally     { setSaving(false) }
  }

  const cols = payType === 'Monthly' ? feePeriods : terms
  const colLabel = payType === 'Monthly'
    ? (c) => c.periodLabel
    : (c) => c.termName
  const colKey = payType === 'Monthly'
    ? (c) => `P_${c.feePeriodId}`
    : (c) => `T_${c.termId}`

  const tableCols = [
    { title: 'Hostel', key: 'hostel', render: () => hostels.find((h) => h.hostelId === selHostel)?.hostelName || '' },
    ...cols.map((col) => ({
      title: colLabel(col),
      key:   colKey(col),
      width: 120,
      render: () => (
        <InputNumber
          min={0}
          precision={2}
          style={{ width: '100%' }}
          value={amounts[colKey(col)] || 0}
          onChange={(v) => setAmounts((prev) => ({ ...prev, [colKey(col)]: v || 0 }))}
        />
      ),
    })),
  ]

  return (
    <div>
      <Row gutter={12} style={{ marginBottom: 16 }} align="bottom">
        <Col>
          <div style={{ marginBottom: 4 }}><Text strong>Hostel</Text></div>
          <Select
            placeholder="Select hostel"
            style={{ width: 200 }}
            value={selHostel}
            onChange={setSelHostel}
            allowClear
          >
            {hostels.map((h) => <Option key={h.hostelId} value={h.hostelId}>{h.hostelName}</Option>)}
          </Select>
        </Col>
        <Col>
          <div style={{ marginBottom: 4 }}><Text strong>Academic Year</Text></div>
          <Select
            placeholder="Select year"
            style={{ width: 160 }}
            value={selYear}
            onChange={setSelYear}
            allowClear
          >
            {years.map((y) => <Option key={y.academicYearId} value={y.academicYearId}>{y.academicYear}</Option>)}
          </Select>
        </Col>
        <Col>
          <div style={{ marginBottom: 4 }}><Text strong>Payment Type</Text></div>
          <Select
            style={{ width: 120 }}
            value={payType}
            onChange={(v) => { setPayType(v); setAmounts({}) }}
          >
            <Option value="Term">Term</Option>
            <Option value="Monthly">Monthly</Option>
          </Select>
        </Col>
        <Col>
          <Button onClick={loadStructure} loading={loading}>Load</Button>
        </Col>
      </Row>

      {selHostel && selYear && cols.length > 0 ? (
        <>
          <Table
            rowKey={() => 'hostel-row'}
            dataSource={[{}]}
            columns={tableCols}
            pagination={false}
            size="small"
            scroll={{ x: 'max-content' }}
            style={{ marginBottom: 16 }}
          />
          <div style={{ textAlign: 'right' }}>
            <Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={handleSave}>
              Save Fee Structure
            </Button>
          </div>
        </>
      ) : selHostel && selYear ? (
        <Alert
          type="warning"
          showIcon
          message={`No ${payType === 'Monthly' ? 'fee periods' : 'terms'} found for this academic year. Add them in Master Data first.`}
        />
      ) : null}
    </div>
  )
}

// ── Student Assignment ────────────────────────────────────────────────────────

function StudentAssignmentTab({ hostels }) {
  const { message } = AntApp.useApp()
  const [years,     setYears]     = useState([])
  const [classes,   setClasses]   = useState([])
  const [sections,  setSections]  = useState([])
  const [selYear,   setSelYear]   = useState(null)
  const [selClass,  setSelClass]  = useState(null)
  const [selSect,   setSelSect]   = useState(null)
  const [selHostel, setSelHostel] = useState(null)
  const [students,  setStudents]  = useState([])
  const [loading,   setLoading]   = useState(false)
  const [editRec,   setEditRec]   = useState(null)
  const [newHostel, setNewHostel] = useState(null)
  const [saving,    setSaving]    = useState(false)

  useEffect(() => {
    Promise.all([
      api.get('/school/master/academic-years'),
      api.get('/school/master/classes'),
    ]).then(([yr, cl]) => {
      setYears(yr.data?.data  || [])
      setClasses(cl.data?.data || [])
    }).catch(() => {})
  }, [])

  useEffect(() => {
    if (!selClass) { setSections([]); setSelSect(null); return }
    api.get(`/school/master/sections?classId=${selClass}`)
      .then((r) => setSections(r.data?.data || []))
      .catch(() => setSections([]))
    setSelSect(null)
  }, [selClass])

  const handleLoad = async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (selYear)   params.set('academicYearId', selYear)
      if (selClass)  params.set('classId',        selClass)
      if (selSect)   params.set('sectionId',      selSect)
      if (selHostel) params.set('hostelId',        selHostel)
      const r = await api.get(`/school/hostel/students?${params}`)
      setStudents(r.data?.data || [])
    } finally { setLoading(false) }
  }

  const openEdit = (s) => { setEditRec(s); setNewHostel(s.hostelId || null) }

  const handleUpdate = async () => {
    if (!editRec) return
    setSaving(true)
    try {
      await api.put(`/school/hostel/students/${editRec.studentUniqueId}`, {
        HostelId:      newHostel || null,
        AcademicYearId: editRec.academicYearId,
      })
      message.success('Hostel assignment updated')
      setEditRec(null)
      await handleLoad()
    } catch (e) { message.error(e.message || 'Update failed') }
    finally     { setSaving(false) }
  }

  const cols = [
    { title: 'Student Name', dataIndex: 'studentName', key: 'name', sorter: (a, b) => a.studentName.localeCompare(b.studentName) },
    { title: 'Admission No', dataIndex: 'admissionNo', key: 'admno' },
    { title: 'Class',        dataIndex: 'className',   key: 'class' },
    { title: 'Section',      dataIndex: 'sectionName', key: 'sect' },
    {
      title: 'Hostel',
      key:   'hostel',
      render: (_, s) => s.hostelName
        ? <Tag color="blue">{s.hostelName}</Tag>
        : <Text type="secondary">Not assigned</Text>,
    },
    {
      title: '', key: 'action', width: 70,
      render: (_, s) => (
        <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(s)}>Edit</Button>
      ),
    },
  ]

  return (
    <>
      <Row gutter={12} style={{ marginBottom: 16 }} align="bottom">
        <Col>
          <div style={{ marginBottom: 4 }}><Text strong>Academic Year</Text></div>
          <Select placeholder="Select year" style={{ width: 160 }} value={selYear} onChange={setSelYear} allowClear>
            {years.map((y) => <Option key={y.academicYearId} value={y.academicYearId}>{y.academicYear}</Option>)}
          </Select>
        </Col>
        <Col>
          <div style={{ marginBottom: 4 }}><Text strong>Class</Text></div>
          <Select placeholder="All classes" style={{ width: 160 }} value={selClass} onChange={setSelClass} allowClear>
            {classes.map((c) => <Option key={c.classId} value={c.classId}>{c.className}</Option>)}
          </Select>
        </Col>
        {sections.length > 0 && (
          <Col>
            <div style={{ marginBottom: 4 }}><Text strong>Section</Text></div>
            <Select placeholder="All sections" style={{ width: 120 }} value={selSect} onChange={setSelSect} allowClear>
              {sections.map((s) => <Option key={s.sectionId} value={s.sectionId}>{s.sectionName}</Option>)}
            </Select>
          </Col>
        )}
        <Col>
          <div style={{ marginBottom: 4 }}><Text strong>Hostel Filter</Text></div>
          <Select placeholder="All hostels" style={{ width: 180 }} value={selHostel} onChange={setSelHostel} allowClear>
            {hostels.map((h) => <Option key={h.hostelId} value={h.hostelId}>{h.hostelName}</Option>)}
          </Select>
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>Load Students</Button>
        </Col>
      </Row>

      <Table
        rowKey="studentId"
        dataSource={students}
        columns={cols}
        loading={loading}
        size="small"
        pagination={{ pageSize: 20 }}
        scroll={{ x: 'max-content' }}
      />

      <Modal
        open={!!editRec}
        title={`Assign Hostel — ${editRec?.studentName}`}
        onCancel={() => setEditRec(null)}
        onOk={handleUpdate}
        okText="Save"
        confirmLoading={saving}
      >
        <div style={{ marginTop: 16 }}>
          <Text strong>Hostel</Text>
          <Select
            allowClear
            placeholder="Select hostel (clear to remove)"
            style={{ width: '100%', marginTop: 8 }}
            value={newHostel}
            onChange={setNewHostel}
          >
            {hostels.map((h) => <Option key={h.hostelId} value={h.hostelId}>{h.hostelName}</Option>)}
          </Select>
        </div>
      </Modal>
    </>
  )
}

// ── Bulk Fee Structure Import ─────────────────────────────────────────────────

const EXAMPLE_ROWS = [
  ['Sunrise Hostel', '2024-25', 'Term', 'Term 1', '', '5000'],
  ['Sunrise Hostel', '2024-25', 'Term', 'Term 2', '', '5000'],
  ['Sunrise Hostel', '2024-25', 'Monthly', '', 'Apr 2024', '2000'],
]

function BulkImportTab() {
  const { message } = AntApp.useApp()
  const [file,      setFile]      = useState(null)
  const [preview,   setPreview]   = useState([])
  const [result,    setResult]    = useState(null)
  const [importing, setImporting] = useState(false)
  const fileRef = useRef()

  const parseCSV = (text) => {
    const lines = text.trim().split('\n')
    if (lines.length < 2) { message.error('CSV has no data rows'); return }
    const rows = lines.slice(1).map((line, i) => {
      const cols = line.split(',').map((c) => c.trim().replace(/^"|"$/g, ''))
      return {
        RowNumber:   i + 2,
        HostelName:  cols[0] || '',
        AcademicYear: cols[1] || '',
        PaymentType: cols[2] || '',
        TermName:    cols[3] || '',
        PeriodLabel: cols[4] || '',
        Amount:      cols[5] || '',
      }
    }).filter((r) => r.HostelName || r.Amount)
    setPreview(rows)
    setResult(null)
  }

  const handleFileChange = (info) => {
    const f = info.file?.originFileObj || info.file
    if (!f) return
    setFile(f)
    const reader = new FileReader()
    reader.onload = (e) => parseCSV(e.target.result)
    reader.readAsText(f)
  }

  const handleImport = async () => {
    if (!preview.length) { message.warning('No rows to import'); return }
    setImporting(true)
    try {
      const r = await api.post('/school/hostel/fee-structure/bulk', { Rows: preview })
      setResult(r.data?.data || {})
      setPreview([])
      setFile(null)
    } catch (e) { message.error(e.message || 'Import failed') }
    finally     { setImporting(false) }
  }

  const downloadTemplate = () => {
    const header = 'HostelName,AcademicYear,PaymentType,TermName,PeriodLabel,Amount'
    const rows   = EXAMPLE_ROWS.map((r) => r.join(',')).join('\n')
    const blob   = new Blob([header + '\n' + rows], { type: 'text/csv' })
    const url    = URL.createObjectURL(blob)
    const a      = document.createElement('a')
    a.href = url; a.download = 'hostel_fee_structure_template.csv'; a.click()
    URL.revokeObjectURL(url)
  }

  const previewCols = [
    { title: '#',             dataIndex: 'RowNumber',    key: 'row',   width: 50 },
    { title: 'Hostel Name',   dataIndex: 'HostelName',   key: 'hostel' },
    { title: 'Academic Year', dataIndex: 'AcademicYear', key: 'year' },
    { title: 'Payment Type',  dataIndex: 'PaymentType',  key: 'pt' },
    { title: 'Term',          dataIndex: 'TermName',     key: 'term' },
    { title: 'Period',        dataIndex: 'PeriodLabel',  key: 'period' },
    { title: 'Amount',        dataIndex: 'Amount',       key: 'amount' },
  ]

  return (
    <div>
      <Card
        title="Column Reference"
        size="small"
        style={{ marginBottom: 16 }}
        extra={<Button size="small" onClick={downloadTemplate}>Download Template</Button>}
      >
        <Table
          size="small"
          pagination={false}
          dataSource={[
            { col: 'HostelName',   req: '✓', desc: 'Hostel name (must exist in master)' },
            { col: 'AcademicYear', req: '✓', desc: 'e.g. 2024-25' },
            { col: 'PaymentType',  req: '✓', desc: 'Term or Monthly' },
            { col: 'TermName',     req: '',  desc: 'Required when PaymentType = Term' },
            { col: 'PeriodLabel',  req: '',  desc: 'Required when PaymentType = Monthly' },
            { col: 'Amount',       req: '✓', desc: 'Numeric fee amount' },
          ]}
          rowKey="col"
          columns={[
            { title: 'Column',        dataIndex: 'col',  key: 'col' },
            { title: 'Required',      dataIndex: 'req',  key: 'req', width: 80, align: 'center' },
            { title: 'Description',   dataIndex: 'desc', key: 'desc' },
          ]}
        />
      </Card>

      <Card style={{ marginBottom: 16 }}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <Upload
            accept=".csv"
            showUploadList={false}
            beforeUpload={() => false}
            onChange={handleFileChange}
          >
            <Button icon={<UploadOutlined />}>Select CSV File</Button>
          </Upload>
          {file && <Text type="secondary">Selected: {file.name} — {preview.length} rows</Text>}
        </Space>
      </Card>

      {preview.length > 0 && (
        <Card title={`Preview — ${preview.length} rows`} style={{ marginBottom: 16 }}>
          <Table
            rowKey="RowNumber"
            dataSource={preview.slice(0, 10)}
            columns={previewCols}
            pagination={false}
            size="small"
            scroll={{ x: 'max-content' }}
          />
          {preview.length > 10 && (
            <Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
              Showing first 10 of {preview.length} rows
            </Text>
          )}
          <div style={{ marginTop: 16, textAlign: 'right' }}>
            <Button type="primary" loading={importing} onClick={handleImport}>
              Import {preview.length} Rows
            </Button>
          </div>
        </Card>
      )}

      {result && (
        <Alert
          type={result.failed > 0 ? 'warning' : 'success'}
          showIcon
          message={`Imported ${result.imported} / ${result.total} rows${result.failed > 0 ? ` (${result.failed} failed)` : ''}`}
          description={
            result.errors?.length > 0 && (
              <ul style={{ margin: 0, paddingLeft: 16 }}>
                {result.errors.slice(0, 10).map((e, i) => (
                  <li key={i}>Row {e.row}: {e.reason}</li>
                ))}
                {result.errors.length > 10 && <li>…and {result.errors.length - 10} more</li>}
              </ul>
            )
          }
        />
      )}
    </div>
  )
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export default function HostelPage() {
  const [hostels, setHostels] = useState([])

  return (
    <AntApp>
      <div>
        <Title level={4} style={{ marginBottom: 16 }}>Hostel Management</Title>
        <Card>
          <Tabs defaultActiveKey="hostels">
            <Tabs.TabPane tab="Hostels" key="hostels">
              <HostelsTab onHostelsChange={setHostels} />
            </Tabs.TabPane>
            <Tabs.TabPane tab="Fee Structure" key="fee">
              <FeeStructureTab hostels={hostels} />
            </Tabs.TabPane>
            <Tabs.TabPane tab="Student Assignment" key="students">
              <StudentAssignmentTab hostels={hostels} />
            </Tabs.TabPane>
            <Tabs.TabPane tab="Bulk Import Structure" key="bulk">
              <BulkImportTab />
            </Tabs.TabPane>
          </Tabs>
        </Card>
      </div>
    </AntApp>
  )
}
