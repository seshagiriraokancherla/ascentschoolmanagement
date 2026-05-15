import { useState, useEffect, useCallback } from 'react'
import {
  Card, Row, Col, Select, Button, Table, Input, InputNumber,
  Tabs, Form, Space, Typography, message, Divider, Tag, Popconfirm, Radio,
} from 'antd'
import { PrinterOutlined, SaveOutlined, SearchOutlined, DeleteOutlined } from '@ant-design/icons'
import { jsPDF } from 'jspdf'
import autoTable from 'jspdf-autotable'
import api from '../../api/axiosInstance'
import { useBrandingStore } from '../../store/brandingStore'

const { Title, Text } = Typography
const { Option }      = Select
const { TextArea }    = Input

const CONCESSION_TYPES = ['Poor', 'Not Applicable', 'Staff Child']

// ── Indian number-to-words ────────────────────────────────────────────────

function amountToWords(amount) {
  const num  = Math.floor(Number(amount) || 0)
  if (num === 0) return 'Zero Rupees Only'

  const ones = ['','One','Two','Three','Four','Five','Six','Seven','Eight','Nine',
                 'Ten','Eleven','Twelve','Thirteen','Fourteen','Fifteen','Sixteen',
                 'Seventeen','Eighteen','Nineteen']
  const tens = ['','','Twenty','Thirty','Forty','Fifty','Sixty','Seventy','Eighty','Ninety']

  const convert = (n) => {
    if (n === 0) return ''
    if (n < 20)  return ones[n] + ' '
    if (n < 100) return tens[Math.floor(n / 10)] + (n % 10 ? ' ' + ones[n % 10] : '') + ' '
    return ones[Math.floor(n / 100)] + ' Hundred ' + convert(n % 100)
  }

  let result = ''
  const crores    = Math.floor(num / 10000000)
  const lakhs     = Math.floor((num % 10000000) / 100000)
  const thousands = Math.floor((num % 100000)   / 1000)
  const rem       = num % 1000

  if (crores)    result += convert(crores).trim()    + ' Crore '
  if (lakhs)     result += convert(lakhs).trim()     + ' Lakh '
  if (thousands) result += convert(thousands).trim() + ' Thousand '
  if (rem)       result += convert(rem)

  return result.trim() + ' Rupees Only'
}

// ── Currency formatter ────────────────────────────────────────────────────

const fmt = (n) =>
  new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n || 0)

// ── PDF print ─────────────────────────────────────────────────────────────

function printConcessions(rows, schoolName) {
  if (!rows.length) return

  const doc   = new jsPDF()
  const today = new Date().toLocaleDateString('en-IN')

  rows.forEach((row, idx) => {
    if (idx > 0) doc.addPage()

    doc.setFontSize(15)
    doc.setFont('helvetica', 'bold')
    doc.text(schoolName || 'School', 105, 18, { align: 'center' })

    doc.setFontSize(12)
    doc.text('FEE CONCESSION RECEIPT', 105, 26, { align: 'center' })

    doc.setLineWidth(0.3)
    doc.line(14, 30, 196, 30)

    doc.setFontSize(10)
    doc.setFont('helvetica', 'normal')
    doc.text(`Receipt No : ${row.receiptNo}`,   14,  38)
    doc.text(`Date       : ${today}`,            130, 38)

    doc.text(`Student Name : ${row.studentName}`, 14, 46)
    doc.text(`Admission No : ${row.admissionNo}`, 130, 46)
    doc.text(`Class        : ${row.className || ''}`,  14, 53)
    doc.text(`Academic Year: ${row.academicYear || ''}`, 130, 53)

    doc.line(14, 57, 196, 57)

    const feeLabel  = row.feeTypeName || row.routeName || ''
    const periodCell = row.termName || row.periodLabel || '—'
    autoTable(doc, {
      startY: 61,
      head:   [['Fee Type / Route', 'Term / Period', 'Concession Type', 'Amount (₹)']],
      body:   [[
        feeLabel,
        periodCell,
        row.concessionType || '',
        fmt(row.amount),
      ]],
      styles:        { fontSize: 10, cellPadding: 4 },
      headStyles:    { fillColor: [41, 128, 185], textColor: 255 },
      columnStyles:  { 3: { halign: 'right' } },
      margin:        { left: 14, right: 14 },
    })

    const afterTable = doc.lastAutoTable.finalY + 8

    doc.setFont('helvetica', 'bold')
    doc.setFontSize(11)
    doc.text(`Total Concession Amount : ₹${fmt(row.amount)}`, 14, afterTable)

    doc.setFont('helvetica', 'italic')
    doc.setFontSize(9)
    doc.text(`(${amountToWords(row.amount)})`, 14, afterTable + 7)

    if (row.remarks) {
      doc.setFont('helvetica', 'normal')
      doc.setFontSize(10)
      doc.text(`Remarks : ${row.remarks}`, 14, afterTable + 16)
    }

    doc.setFont('helvetica', 'normal')
    doc.setFontSize(10)
    doc.text('Authorised Signatory', 150, 265)
    doc.line(140, 267, 196, 267)
  })

  doc.save('fee-concession-receipts.pdf')
}

// ── Main Component ─────────────────────────────────────────────────────────

export default function FeeConcessionPage() {
  const { branding }   = useBrandingStore()
  const [indvForm]     = Form.useForm()

  // mode: 'school' | 'transport'
  const [concessionMode, setConcessionMode] = useState('school')

  // shared lookup data
  const [years,      setYears]      = useState([])
  const [classes,    setClasses]    = useState([])
  const [sections,   setSections]   = useState([])
  const [feeTypes,   setFeeTypes]   = useState([])
  const [terms,      setTerms]      = useState([])
  const [feePeriods, setFeePeriods] = useState([])
  const [routes,     setRoutes]     = useState([])
  const [hostels,    setHostels]    = useState([])

  // filter bar (shared across tabs)
  const [filterYear,    setFilterYear]    = useState(null)
  const [filterClass,   setFilterClass]   = useState(null)
  const [filterSection, setFilterSection] = useState(null)

  // Individual tab
  const [searchTerm,      setSearchTerm]      = useState('')
  const [searchResults,   setSearchResults]   = useState([])
  const [searching,       setSearching]       = useState(false)
  const [selectedStudent, setSelectedStudent] = useState(null)
  const [indvSaving,      setIndvSaving]      = useState(false)

  // Bulk Apply tab
  const [bulkFeeTypeId,      setBulkFeeTypeId]      = useState(null)
  const [bulkBusRouteId,     setBulkBusRouteId]     = useState(null)
  const [bulkHostelId,       setBulkHostelId]       = useState(null)
  const [bulkTermId,         setBulkTermId]         = useState(null)
  const [bulkFeePeriodId,    setBulkFeePeriodId]    = useState(null)
  const [bulkConcessionType, setBulkConcessionType] = useState(null)
  const [bulkStudents,       setBulkStudents]       = useState([])
  const [loadingStudents,    setLoadingStudents]     = useState(false)
  const [globalAmount,       setGlobalAmount]        = useState(null)
  const [bulkSaving,         setBulkSaving]          = useState(false)

  // Concession List tab
  const [concessions,      setConcessions]      = useState([])
  const [loadingList,      setLoadingList]      = useState(false)
  const [selectedListKeys, setSelectedListKeys] = useState([])

  // ── Load lookups ──────────────────────────────────────────────────────

  useEffect(() => {
    Promise.all([
      api.get('/school/master/academic-years'),
      api.get('/school/master/classes'),
      api.get('/school/master/fee-types'),
      api.get('/school/transport/routes'),
      api.get('/school/hostel'),
    ]).then(([yr, cl, ft, rt, ht]) => {
      setYears(yr.data?.data    || [])
      setClasses(cl.data?.data  || [])
      setFeeTypes(ft.data?.data || [])
      setRoutes(rt.data?.data   || [])
      setHostels(ht.data?.data  || [])
    }).catch(() => {})
  }, [])

  // Load terms and fee periods whenever the selected academic year changes
  useEffect(() => {
    if (!filterYear) { setTerms([]); setFeePeriods([]); return }
    Promise.all([
      api.get(`/school/master/terms?academicYearId=${filterYear}`),
      api.get(`/school/master/fee-periods?academicYearId=${filterYear}`),
    ]).then(([t, fp]) => {
      setTerms(t.data?.data        || [])
      setFeePeriods(fp.data?.data  || [])
    }).catch(() => {})
  }, [filterYear])

  useEffect(() => {
    if (!filterClass) { setSections([]); setFilterSection(null); return }
    api.get(`/school/master/sections?classId=${filterClass}`)
      .then((r) => setSections(r.data?.data || []))
      .catch(() => setSections([]))
    setFilterSection(null)
  }, [filterClass])

  // Reset bulk state when mode changes
  useEffect(() => {
    setBulkStudents([])
    setBulkFeeTypeId(null)
    setBulkBusRouteId(null)
    setBulkHostelId(null)
    setBulkTermId(null)
    setBulkFeePeriodId(null)
    setBulkConcessionType(null)
    setGlobalAmount(null)
    setSelectedStudent(null)
    indvForm.resetFields()
  }, [concessionMode])

  // ── Individual tab ────────────────────────────────────────────────────

  const handleStudentSearch = useCallback(async () => {
    if (!searchTerm.trim()) return
    setSearching(true)
    try {
      const params = new URLSearchParams({ search: searchTerm })
      if (filterYear) params.set('academicYearId', filterYear)
      // Restrict search to students on the selected route/hostel
      const indvRouteId  = indvForm.getFieldValue('busRouteId')
      const indvHostelId = indvForm.getFieldValue('hostelId')
      if (concessionMode === 'transport' && indvRouteId) params.set('busRouteId', indvRouteId)
      if (concessionMode === 'hostel'    && indvHostelId) params.set('hostelId', indvHostelId)
      const r = await api.get(`/school/students?${params}`)
      setSearchResults((r.data?.data || []).slice(0, 20))
    } catch { message.error('Student search failed') }
    finally  { setSearching(false) }
  }, [searchTerm, filterYear, concessionMode, indvForm])

  const selectStudent = (s) => {
    setSelectedStudent(s)
    setSearchResults([])
    setSearchTerm('')
  }

  const handleIndvSave = async (values) => {
    if (!selectedStudent) { message.warning('Please select a student first'); return }
    if (!filterYear)      { message.warning('Please select an academic year'); return }
    setIndvSaving(true)
    try {
      await api.post('/school/fees/concessions', {
        studentId:      selectedStudent.studentId,
        feeTypeId:      concessionMode === 'school'  ? (values.feeTypeId  || null) : null,
        busRouteId:     concessionMode === 'transport' ? (values.busRouteId || null) : null,
        hostelId:       concessionMode === 'hostel'  ? (values.hostelId   || null) : null,
        termId:         values.termId         || null,
        feePeriodId:    concessionMode !== 'transport' ? (values.feePeriodId || null) : null,
        academicYearId: filterYear,
        concessionType: values.concessionType,
        amount:         values.amount,
        remarks:        values.remarks || '',
      })
      message.success('Concession saved successfully')
      indvForm.resetFields()
      setSelectedStudent(null)
    } catch (e) { message.error(e.message || 'Save failed') }
    finally     { setIndvSaving(false) }
  }

  // ── Bulk Apply tab ────────────────────────────────────────────────────

  const handleLoadStudents = async () => {
    if (!filterYear)  { message.warning('Please select an academic year'); return }
    if (concessionMode === 'school' && !filterClass) {
      message.warning('Please select a class'); return
    }
    if (concessionMode === 'transport' && !bulkBusRouteId) {
      message.warning('Please select a bus route'); return
    }
    if (concessionMode === 'hostel' && !bulkHostelId) {
      message.warning('Please select a hostel'); return
    }
    setLoadingStudents(true)
    try {
      const params = new URLSearchParams({ academicYearId: filterYear })
      if (concessionMode === 'school') {
        params.set('classId', filterClass)
        if (filterSection) params.set('sectionId', filterSection)
      } else if (concessionMode === 'transport') {
        params.set('busRouteId', bulkBusRouteId)
      } else {
        params.set('hostelId', bulkHostelId)
      }
      const r = await api.get(`/school/students?${params}`)
      const students = (r.data?.data || []).map((s) => ({
        studentId:   s.studentId,
        studentName: s.studentName,
        admissionNo: s.admissionNo,
        className:   s.className,
        amount:      null,
        remarks:     '',
        key:         s.studentId,
      }))
      setBulkStudents(students)
    } catch { message.error('Failed to load students') }
    finally  { setLoadingStudents(false) }
  }

  const applyGlobalAmount = () => {
    if (!globalAmount) return
    setBulkStudents((prev) => prev.map((s) => ({ ...s, amount: globalAmount })))
  }

  const updateBulkRow = (studentId, field, value) => {
    setBulkStudents((prev) =>
      prev.map((s) => s.studentId === studentId ? { ...s, [field]: value } : s)
    )
  }

  const handleBulkSave = async () => {
    if (concessionMode === 'school' && !bulkFeeTypeId) {
      message.warning('Please select a fee type'); return
    }
    if (concessionMode === 'transport' && !bulkBusRouteId) {
      message.warning('Please select a bus route'); return
    }
    if (concessionMode === 'transport' && !bulkTermId) {
      message.warning('Please select a term'); return
    }
    if (concessionMode === 'hostel' && !bulkHostelId) {
      message.warning('Please select a hostel'); return
    }
    if (!bulkConcessionType) { message.warning('Please select a concession type'); return }
    if (!filterYear)         { message.warning('Please select an academic year'); return }

    const rows = bulkStudents.filter((s) => s.amount > 0).map((s) => ({
      studentId: s.studentId,
      amount:    s.amount,
      remarks:   s.remarks || '',
    }))
    if (!rows.length) { message.warning('No students have an amount entered'); return }

    setBulkSaving(true)
    try {
      const r = await api.post('/school/fees/concessions/bulk', {
        academicYearId: filterYear,
        feeTypeId:      concessionMode === 'school'     ? (bulkFeeTypeId  || null) : null,
        busRouteId:     concessionMode === 'transport'  ? (bulkBusRouteId || null) : null,
        hostelId:       concessionMode === 'hostel'     ? (bulkHostelId   || null) : null,
        termId:         bulkTermId       || null,
        feePeriodId:    concessionMode !== 'transport'  ? (bulkFeePeriodId || null) : null,
        concessionType: bulkConcessionType,
        rows,
      })
      message.success(r.data?.message || `${rows.length} concession(s) saved`)
      setBulkStudents([])
      setBulkFeeTypeId(null)
      setBulkBusRouteId(null)
      setBulkHostelId(null)
      setBulkTermId(null)
      setBulkFeePeriodId(null)
      setBulkConcessionType(null)
      setGlobalAmount(null)
    } catch (e) { message.error(e.message || 'Save failed') }
    finally     { setBulkSaving(false) }
  }

  // ── Concession List tab ───────────────────────────────────────────────

  const loadConcessions = async () => {
    if (!filterYear) { message.warning('Please select an academic year'); return }
    setLoadingList(true)
    setSelectedListKeys([])
    try {
      const params = new URLSearchParams({ academicYearId: filterYear })
      if (filterClass)   params.set('classId',   filterClass)
      if (filterSection) params.set('sectionId', filterSection)
      const r = await api.get(`/school/fees/concessions?${params}`)
      setConcessions(r.data?.data || [])
    } catch { message.error('Failed to load concessions') }
    finally  { setLoadingList(false) }
  }

  const handleDeleteConcession = async (concessionId) => {
    try {
      await api.delete(`/school/fees/concessions/${concessionId}`)
      message.success('Concession cancelled')
      setConcessions((prev) => prev.filter((c) => c.concessionId !== concessionId))
      setSelectedListKeys((prev) => prev.filter((k) => k !== concessionId))
    } catch (e) { message.error(e.message || 'Delete failed') }
  }

  const handlePrint = () => {
    const rows = concessions.filter((c) => selectedListKeys.includes(c.concessionId))
    if (!rows.length) { message.warning('Select at least one record to print'); return }
    printConcessions(rows, branding.displayName)
  }

  // ── Tables ────────────────────────────────────────────────────────────

  const searchColumns = [
    { title: 'Name',         dataIndex: 'studentName', key: 'name' },
    { title: 'Admission No', dataIndex: 'admissionNo', key: 'admno' },
    { title: 'Class',        dataIndex: 'className',   key: 'class' },
    {
      title: '',
      key: 'select',
      render: (_, s) => (
        <Button size="small" type="primary" onClick={() => selectStudent(s)}>Select</Button>
      ),
    },
  ]

  const bulkColumns = [
    { title: 'Student Name', dataIndex: 'studentName', key: 'name', width: 200 },
    { title: 'Admission No', dataIndex: 'admissionNo', key: 'admno', width: 130 },
    { title: 'Class',        dataIndex: 'className',   key: 'class', width: 100 },
    {
      title: 'Amount (₹)',
      key:   'amount',
      width: 140,
      render: (_, row) => (
        <InputNumber
          min={0}
          precision={2}
          style={{ width: '100%' }}
          value={row.amount}
          onChange={(v) => updateBulkRow(row.studentId, 'amount', v)}
        />
      ),
    },
    {
      title: 'Remarks',
      key:   'remarks',
      render: (_, row) => (
        <Input
          value={row.remarks}
          onChange={(e) => updateBulkRow(row.studentId, 'remarks', e.target.value)}
          placeholder="Optional"
        />
      ),
    },
  ]

  const listColumns = [
    { title: 'Student Name',    dataIndex: 'studentName',    key: 'name',  sorter: (a, b) => a.studentName.localeCompare(b.studentName) },
    { title: 'Admission No',    dataIndex: 'admissionNo',    key: 'admno' },
    { title: 'Class',           dataIndex: 'className',      key: 'class' },
    {
      title:  'Fee Type / Route / Hostel',
      key:    'feetype',
      render: (_, r) => r.feeTypeName || r.routeName || r.hostelName || <span style={{ color: '#bbb' }}>—</span>,
    },
    {
      title:     'Term / Period',
      key:       'period',
      render:    (_, r) => r.termName || r.periodLabel || <span style={{ color: '#bbb' }}>—</span>,
    },
    {
      title:     'Concession Type',
      dataIndex: 'concessionType',
      key:       'ctype',
      render:    (v) => <Tag color={v === 'Poor' ? 'blue' : v === 'Staff Child' ? 'green' : 'default'}>{v}</Tag>,
    },
    {
      title:     'Amount (₹)',
      dataIndex: 'amount',
      key:       'amount',
      align:     'right',
      render:    (v) => fmt(v),
    },
    { title: 'Receipt No', dataIndex: 'receiptNo', key: 'receipt' },
    {
      title: '',
      key:   'actions',
      width: 80,
      render: (_, row) => (
        <Popconfirm
          title="Cancel this concession?"
          description="Outstanding will be recalculated on next load."
          onConfirm={() => handleDeleteConcession(row.concessionId)}
          okText="Yes, Cancel"
          okType="danger"
          cancelText="No"
        >
          <Button
            size="small"
            danger
            icon={<DeleteOutlined />}
          />
        </Popconfirm>
      ),
    },
  ]

  const isTransport = concessionMode === 'transport'
  const isHostel    = concessionMode === 'hostel'

  // ── Render ────────────────────────────────────────────────────────────

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Fee Concession</Title>

      {/* ── Filter bar ──────────────────────────────────────────────── */}
      <Card style={{ marginBottom: 16 }}>
        <Row gutter={16} align="middle">
          <Col>
            <Text strong>Fee Category</Text>
            <br />
            <Radio.Group
              style={{ marginTop: 4 }}
              value={concessionMode}
              onChange={(e) => setConcessionMode(e.target.value)}
              buttonStyle="solid"
            >
              <Radio.Button value="school">School Fee</Radio.Button>
              <Radio.Button value="transport">Transport</Radio.Button>
              <Radio.Button value="hostel">Hostel</Radio.Button>
            </Radio.Group>
          </Col>
          <Col>
            <Text strong>Academic Year</Text>
            <br />
            <Select
              placeholder="Select year"
              style={{ width: 160, marginTop: 4 }}
              value={filterYear}
              onChange={setFilterYear}
              allowClear
            >
              {years.map((y) => (
                <Option key={y.academicYearId} value={y.academicYearId}>{y.academicYear}</Option>
              ))}
            </Select>
          </Col>
          {!isTransport && !isHostel && (
            <Col>
              <Text strong>Class</Text>
              <br />
              <Select
                placeholder="All classes"
                style={{ width: 160, marginTop: 4 }}
                value={filterClass}
                onChange={(v) => { setFilterClass(v); setFilterSection(null) }}
                allowClear
              >
                {classes.map((c) => (
                  <Option key={c.classId} value={c.classId}>{c.className}</Option>
                ))}
              </Select>
            </Col>
          )}
          {!isTransport && !isHostel && sections.length > 0 && (
            <Col>
              <Text strong>Section</Text>
              <br />
              <Select
                placeholder="All sections"
                style={{ width: 120, marginTop: 4 }}
                value={filterSection}
                onChange={setFilterSection}
                allowClear
              >
                {sections.map((s) => (
                  <Option key={s.sectionId} value={s.sectionId}>{s.sectionName}</Option>
                ))}
              </Select>
            </Col>
          )}
        </Row>
      </Card>

      <Tabs defaultActiveKey="individual">

        {/* ── Individual tab ─────────────────────────────────────────── */}
        <Tabs.TabPane tab="Individual Entry" key="individual">
          <Card>
            <Row gutter={24}>
              {/* Left: student search */}
              <Col span={12}>
                <Text strong>Search Student</Text>
                <Input.Search
                  style={{ marginTop: 8, marginBottom: 12 }}
                  placeholder="Name or admission number"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  onSearch={handleStudentSearch}
                  enterButton={<SearchOutlined />}
                  loading={searching}
                  onPressEnter={handleStudentSearch}
                />

                {searchResults.length > 0 && (
                  <Table
                    size="small"
                    rowKey="studentId"
                    columns={searchColumns}
                    dataSource={searchResults}
                    pagination={false}
                    style={{ marginBottom: 16 }}
                  />
                )}

                {selectedStudent && (
                  <Card
                    size="small"
                    style={{ background: '#f6ffed', border: '1px solid #b7eb8f', marginTop: 8 }}
                  >
                    <Text strong>{selectedStudent.studentName}</Text>
                    <br />
                    <Text type="secondary">
                      {selectedStudent.admissionNo} · {selectedStudent.className}
                    </Text>
                    <br />
                    <Button
                      size="small"
                      style={{ marginTop: 8 }}
                      onClick={() => { setSelectedStudent(null); indvForm.resetFields() }}
                    >
                      Clear
                    </Button>
                  </Card>
                )}
              </Col>

              {/* Right: concession form */}
              <Col span={12}>
                <Text strong>Concession Details</Text>
                <Form
                  form={indvForm}
                  layout="vertical"
                  style={{ marginTop: 8 }}
                  onFinish={handleIndvSave}
                >
                  {isTransport ? (
                    <>
                      <Form.Item
                        name="busRouteId"
                        label="Bus Route"
                        rules={[{ required: true, message: 'Select bus route' }]}
                      >
                        <Select placeholder="Select route">
                          {routes.map((r) => (
                            <Option key={r.routeId} value={r.routeId}>{r.routeName}</Option>
                          ))}
                        </Select>
                      </Form.Item>

                      <Form.Item
                        name="termId"
                        label="Term"
                        rules={[{ required: true, message: 'Select term' }]}
                      >
                        <Select placeholder="Select term">
                          {terms.map((t) => (
                            <Option key={t.termId} value={t.termId}>{t.termName}</Option>
                          ))}
                        </Select>
                      </Form.Item>
                    </>
                  ) : isHostel ? (
                    <>
                      <Form.Item
                        name="hostelId"
                        label="Hostel"
                        rules={[{ required: true, message: 'Select hostel' }]}
                      >
                        <Select placeholder="Select hostel">
                          {hostels.map((h) => (
                            <Option key={h.hostelId} value={h.hostelId}>{h.hostelName}</Option>
                          ))}
                        </Select>
                      </Form.Item>

                      {terms.length > 0 && (
                        <Form.Item name="termId" label="Term">
                          <Select placeholder="Select term (if term-based)" allowClear>
                            {terms.map((t) => (
                              <Option key={t.termId} value={t.termId}>{t.termName}</Option>
                            ))}
                          </Select>
                        </Form.Item>
                      )}

                      {feePeriods.length > 0 && (
                        <Form.Item name="feePeriodId" label="Fee Period">
                          <Select placeholder="Select period (if monthly)" allowClear>
                            {feePeriods.map((p) => (
                              <Option key={p.feePeriodId} value={p.feePeriodId}>{p.periodLabel}</Option>
                            ))}
                          </Select>
                        </Form.Item>
                      )}
                    </>
                  ) : (
                    <>
                      <Form.Item
                        name="feeTypeId"
                        label="Fee Type"
                        rules={[{ required: true, message: 'Select fee type' }]}
                      >
                        <Select placeholder="Select fee type">
                          {feeTypes.map((f) => (
                            <Option key={f.feeTypeId} value={f.feeTypeId}>{f.feeTypeName}</Option>
                          ))}
                        </Select>
                      </Form.Item>

                      {terms.length > 0 && (
                        <Form.Item name="termId" label="Term">
                          <Select placeholder="All terms / select specific" allowClear>
                            {terms.map((t) => (
                              <Option key={t.termId} value={t.termId}>{t.termName}</Option>
                            ))}
                          </Select>
                        </Form.Item>
                      )}

                      {feePeriods.length > 0 && (
                        <Form.Item name="feePeriodId" label="Fee Period">
                          <Select placeholder="All periods / select specific" allowClear>
                            {feePeriods.map((p) => (
                              <Option key={p.feePeriodId} value={p.feePeriodId}>{p.periodLabel}</Option>
                            ))}
                          </Select>
                        </Form.Item>
                      )}
                    </>
                  )}

                  <Form.Item
                    name="concessionType"
                    label="Concession Type"
                    rules={[{ required: true, message: 'Select concession type' }]}
                  >
                    <Select placeholder="Select type">
                      {CONCESSION_TYPES.map((t) => <Option key={t} value={t}>{t}</Option>)}
                    </Select>
                  </Form.Item>

                  <Form.Item
                    name="amount"
                    label="Concession Amount (₹)"
                    rules={[{ required: true, message: 'Enter amount' }]}
                  >
                    <InputNumber min={0.01} precision={2} style={{ width: '100%' }} />
                  </Form.Item>

                  <Form.Item name="remarks" label="Remarks">
                    <TextArea rows={2} placeholder="Optional" />
                  </Form.Item>

                  <Button
                    type="primary"
                    htmlType="submit"
                    icon={<SaveOutlined />}
                    loading={indvSaving}
                    disabled={!selectedStudent}
                  >
                    Save Concession
                  </Button>
                </Form>
              </Col>
            </Row>
          </Card>
        </Tabs.TabPane>

        {/* ── Bulk Apply tab ─────────────────────────────────────────── */}
        <Tabs.TabPane tab="Bulk Apply" key="bulk">
          <Card>
            <Row gutter={16} align="bottom" style={{ marginBottom: 16 }}>
              {isTransport ? (
                <>
                  <Col>
                    <Text strong>Bus Route</Text>
                    <br />
                    <Select
                      placeholder="Select route"
                      style={{ width: 220, marginTop: 4 }}
                      value={bulkBusRouteId}
                      onChange={(v) => { setBulkBusRouteId(v); setBulkStudents([]) }}
                    >
                      {routes.map((r) => (
                        <Option key={r.routeId} value={r.routeId}>{r.routeName}</Option>
                      ))}
                    </Select>
                  </Col>
                  <Col>
                    <Text strong>Term</Text>
                    <br />
                    <Select
                      placeholder="Select term"
                      style={{ width: 160, marginTop: 4 }}
                      value={bulkTermId}
                      onChange={setBulkTermId}
                    >
                      {terms.map((t) => (
                        <Option key={t.termId} value={t.termId}>{t.termName}</Option>
                      ))}
                    </Select>
                  </Col>
                </>
              ) : isHostel ? (
                <>
                  <Col>
                    <Text strong>Hostel</Text>
                    <br />
                    <Select
                      placeholder="Select hostel"
                      style={{ width: 220, marginTop: 4 }}
                      value={bulkHostelId}
                      onChange={(v) => { setBulkHostelId(v); setBulkStudents([]) }}
                    >
                      {hostels.map((h) => (
                        <Option key={h.hostelId} value={h.hostelId}>{h.hostelName}</Option>
                      ))}
                    </Select>
                  </Col>
                  {terms.length > 0 && (
                    <Col>
                      <Text strong>Term</Text>
                      <br />
                      <Select
                        placeholder="Select term (if term-based)"
                        style={{ width: 180, marginTop: 4 }}
                        value={bulkTermId}
                        onChange={setBulkTermId}
                        allowClear
                      >
                        {terms.map((t) => (
                          <Option key={t.termId} value={t.termId}>{t.termName}</Option>
                        ))}
                      </Select>
                    </Col>
                  )}
                  {feePeriods.length > 0 && (
                    <Col>
                      <Text strong>Fee Period</Text>
                      <br />
                      <Select
                        placeholder="Select period (if monthly)"
                        style={{ width: 180, marginTop: 4 }}
                        value={bulkFeePeriodId}
                        onChange={setBulkFeePeriodId}
                        allowClear
                      >
                        {feePeriods.map((p) => (
                          <Option key={p.feePeriodId} value={p.feePeriodId}>{p.periodLabel}</Option>
                        ))}
                      </Select>
                    </Col>
                  )}
                </>
              ) : (
                <>
                  <Col>
                    <Text strong>Fee Type</Text>
                    <br />
                    <Select
                      placeholder="Select fee type"
                      style={{ width: 220, marginTop: 4 }}
                      value={bulkFeeTypeId}
                      onChange={setBulkFeeTypeId}
                    >
                      {feeTypes.map((f) => (
                        <Option key={f.feeTypeId} value={f.feeTypeId}>{f.feeTypeName}</Option>
                      ))}
                    </Select>
                  </Col>
                  {terms.length > 0 && (
                    <Col>
                      <Text strong>Term</Text>
                      <br />
                      <Select
                        placeholder="All terms"
                        style={{ width: 160, marginTop: 4 }}
                        value={bulkTermId}
                        onChange={setBulkTermId}
                        allowClear
                      >
                        {terms.map((t) => (
                          <Option key={t.termId} value={t.termId}>{t.termName}</Option>
                        ))}
                      </Select>
                    </Col>
                  )}
                  {feePeriods.length > 0 && (
                    <Col>
                      <Text strong>Fee Period</Text>
                      <br />
                      <Select
                        placeholder="All periods"
                        style={{ width: 160, marginTop: 4 }}
                        value={bulkFeePeriodId}
                        onChange={setBulkFeePeriodId}
                        allowClear
                      >
                        {feePeriods.map((p) => (
                          <Option key={p.feePeriodId} value={p.feePeriodId}>{p.periodLabel}</Option>
                        ))}
                      </Select>
                    </Col>
                  )}
                </>
              )}
              <Col>
                <Text strong>Concession Type</Text>
                <br />
                <Select
                  placeholder="Select type"
                  style={{ width: 180, marginTop: 4 }}
                  value={bulkConcessionType}
                  onChange={setBulkConcessionType}
                >
                  {CONCESSION_TYPES.map((t) => <Option key={t} value={t}>{t}</Option>)}
                </Select>
              </Col>
              <Col>
                <Button
                  type="default"
                  style={{ marginTop: 24 }}
                  loading={loadingStudents}
                  onClick={handleLoadStudents}
                >
                  Load Students
                </Button>
              </Col>
            </Row>

            {bulkStudents.length > 0 && (
              <>
                <Divider orientation="left" plain>
                  Apply to All
                </Divider>
                <Space style={{ marginBottom: 16 }}>
                  <InputNumber
                    min={0.01}
                    precision={2}
                    placeholder="Amount"
                    value={globalAmount}
                    onChange={setGlobalAmount}
                    style={{ width: 160 }}
                  />
                  <Button onClick={applyGlobalAmount}>Apply to All Rows</Button>
                </Space>

                <Table
                  size="small"
                  rowKey="studentId"
                  columns={bulkColumns}
                  dataSource={bulkStudents}
                  pagination={{ pageSize: 20 }}
                  scroll={{ x: 700 }}
                />

                <div style={{ marginTop: 16, textAlign: 'right' }}>
                  <Button
                    type="primary"
                    icon={<SaveOutlined />}
                    loading={bulkSaving}
                    onClick={handleBulkSave}
                  >
                    Save All
                  </Button>
                </div>
              </>
            )}
          </Card>
        </Tabs.TabPane>

        {/* ── Concession List tab ────────────────────────────────────── */}
        <Tabs.TabPane tab="Concession List" key="list">
          <Card>
            <Space style={{ marginBottom: 16 }}>
              <Button type="primary" onClick={loadConcessions} loading={loadingList}>
                Load Concessions
              </Button>
              <Button
                icon={<PrinterOutlined />}
                disabled={!selectedListKeys.length}
                onClick={handlePrint}
              >
                Print Selected ({selectedListKeys.length})
              </Button>
            </Space>

            <Table
              size="small"
              rowKey="concessionId"
              columns={listColumns}
              dataSource={concessions}
              loading={loadingList}
              rowSelection={{
                selectedRowKeys: selectedListKeys,
                onChange: (keys) => setSelectedListKeys(keys),
              }}
              pagination={{ pageSize: 20 }}
              scroll={{ x: 900 }}
              summary={(rows) => {
                const total = rows.reduce((s, r) => s + (r.amount || 0), 0)
                return (
                  <Table.Summary.Row>
                    <Table.Summary.Cell index={0} colSpan={6}>
                      <Text strong>Total</Text>
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={6} align="right">
                      <Text strong>₹{fmt(total)}</Text>
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={7} colSpan={2} />
                  </Table.Summary.Row>
                )
              }}
            />
          </Card>
        </Tabs.TabPane>
      </Tabs>
    </div>
  )
}
