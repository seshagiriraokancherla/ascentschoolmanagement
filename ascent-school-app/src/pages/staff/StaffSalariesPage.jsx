import { useState, useEffect, useCallback } from 'react'
import {
  Card, Tabs, Select, Button, Table, Tag, Space, Modal, Form,
  InputNumber, Input, Popconfirm, Divider, Row, Col, Statistic,
  message, DatePicker, Spin, Typography,
} from 'antd'
import {
  PlusOutlined, DeleteOutlined, SaveOutlined,
  CheckCircleOutlined, StopOutlined, PrinterOutlined, EditOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import api from '../../api/axiosInstance'
import { useBrandingStore } from '../../store/brandingStore'

const { Option } = Select
const { Text } = Typography

const MONTHS = [
  { value: 1,  label: 'January'   },
  { value: 2,  label: 'February'  },
  { value: 3,  label: 'March'     },
  { value: 4,  label: 'April'     },
  { value: 5,  label: 'May'       },
  { value: 6,  label: 'June'      },
  { value: 7,  label: 'July'      },
  { value: 8,  label: 'August'    },
  { value: 9,  label: 'September' },
  { value: 10, label: 'October'   },
  { value: 11, label: 'November'  },
  { value: 12, label: 'December'  },
]

const currentYear  = new Date().getFullYear()
const YEARS = Array.from({ length: 10 }, (_, i) => currentYear - 2 + i)

// ─────────────────────────────────────────────────────────────────────────────
// Salary Slip PDF (portrait A4, browser-generated)
// ─────────────────────────────────────────────────────────────────────────────
function generateSalarySlipPdf(slip, schoolName) {
  const doc   = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' })
  const pageW = doc.internal.pageSize.getWidth()
  const mL    = 15, mR = 15

  const monthName = MONTHS.find(m => m.value === slip.month)?.label || slip.month
  const now = new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })

  // Header
  doc.setFont('helvetica', 'bold')
  doc.setFontSize(16)
  doc.setTextColor(22, 119, 255)
  doc.text(schoolName || 'School', pageW / 2, 18, { align: 'center' })

  doc.setFontSize(13)
  doc.setTextColor(40, 40, 40)
  doc.text('SALARY SLIP', pageW / 2, 27, { align: 'center' })

  doc.setFont('helvetica', 'normal')
  doc.setFontSize(10)
  doc.setTextColor(80, 80, 80)
  doc.text(`${monthName} ${slip.year}`, pageW / 2, 34, { align: 'center' })

  doc.setFontSize(8)
  doc.text(`Generated: ${now}`, pageW - mR, 10, { align: 'right' })

  doc.setDrawColor(22, 119, 255)
  doc.setLineWidth(0.5)
  doc.line(mL, 38, pageW - mR, 38)

  // Staff info table
  autoTable(doc, {
    body: [
      ['Employee Name', slip.staffName,    'Employee Code', slip.employeeCode],
      ['Designation',   slip.designation,  'Status',        slip.status],
    ],
    startY: 42,
    theme: 'grid',
    styles:     { fontSize: 9, cellPadding: 2.5, textColor: [30, 30, 30] },
    columnStyles: {
      0: { fontStyle: 'bold', fillColor: [240, 245, 255], cellWidth: 40 },
      1: { cellWidth: 55 },
      2: { fontStyle: 'bold', fillColor: [240, 245, 255], cellWidth: 35 },
      3: { cellWidth: 45 },
    },
    margin: { left: mL, right: mR },
  })

  const afterInfo = doc.lastAutoTable.finalY + 8

  // Earnings & Deductions side-by-side
  const earnings   = (slip.items || []).filter(i => i.componentType === 'Earning')
  const deductions = (slip.items || []).filter(i => i.componentType === 'Deduction')
  const halfW      = (pageW - mL - mR - 6) / 2

  // Earnings table (left half)
  autoTable(doc, {
    head: [['Earnings', 'Amount (₹)']],
    body: earnings.map(e => [e.componentName, Number(e.amount).toFixed(2)]),
    startY: afterInfo,
    margin: { left: mL, right: pageW / 2 + 3 },
    tableWidth: halfW,
    styles:     { fontSize: 9, cellPadding: 2 },
    headStyles: { fillColor: [82, 196, 26], textColor: 255, fontStyle: 'bold' },
    alternateRowStyles: { fillColor: [246, 255, 237] },
    columnStyles: { 1: { halign: 'right' } },
  })
  const earningsY = doc.lastAutoTable.finalY

  // Deductions table (right half)
  autoTable(doc, {
    head: [['Deductions', 'Amount (₹)']],
    body: deductions.map(d => [d.componentName, Number(d.amount).toFixed(2)]),
    startY: afterInfo,
    margin: { left: pageW / 2 + 3, right: mR },
    tableWidth: halfW,
    styles:     { fontSize: 9, cellPadding: 2 },
    headStyles: { fillColor: [255, 77, 79], textColor: 255, fontStyle: 'bold' },
    alternateRowStyles: { fillColor: [255, 241, 240] },
    columnStyles: { 1: { halign: 'right' } },
  })
  const deductionsY = doc.lastAutoTable.finalY

  const summaryY = Math.max(earningsY, deductionsY) + 8

  // Summary
  autoTable(doc, {
    body: [
      ['Gross Earnings',    `₹ ${Number(slip.grossEarnings).toFixed(2)}`],
      ['Total Deductions',  `₹ ${Number(slip.totalDeductions).toFixed(2)}`],
      ['Advance Deducted',  `₹ ${Number(slip.advanceDeducted).toFixed(2)}`],
      ['Net Salary',        `₹ ${Number(slip.netSalary).toFixed(2)}`],
    ],
    startY: summaryY,
    margin: { left: pageW / 2 - 10, right: mR },
    tableWidth: pageW / 2 - mR + 10,
    theme: 'grid',
    styles:     { fontSize: 10, cellPadding: 2.5 },
    columnStyles: {
      0: { fontStyle: 'bold', fillColor: [245, 245, 245] },
      1: { halign: 'right' },
    },
    didParseCell: (data) => {
      if (data.row.index === 3) {
        data.cell.styles.fontStyle    = 'bold'
        data.cell.styles.fontSize     = 11
        data.cell.styles.fillColor    = [22, 119, 255]
        data.cell.styles.textColor    = [255, 255, 255]
      }
    },
  })

  const sigY = doc.lastAutoTable.finalY + 18

  // Footer
  doc.setFont('helvetica', 'normal')
  doc.setFontSize(9)
  doc.setTextColor(80, 80, 80)
  doc.text(`Processed by: ${slip.processedBy}`, mL, sigY)

  doc.setDrawColor(100)
  doc.setLineWidth(0.3)
  doc.line(mL, sigY + 16, mL + 55, sigY + 16)
  doc.line(pageW - mR - 55, sigY + 16, pageW - mR, sigY + 16)

  doc.setFontSize(8)
  doc.setTextColor(100)
  doc.text('Employee Signature', mL + 4, sigY + 20)
  doc.text('Authorized Signature', pageW - mR - 51, sigY + 20)

  doc.save(`salary_slip_${slip.employeeCode || slip.staffId}_${monthName}_${slip.year}.pdf`)
}

// ─────────────────────────────────────────────────────────────────────────────
// Tab 1 — Salary Structure
// ─────────────────────────────────────────────────────────────────────────────
function SalaryStructureTab({ staffList }) {
  const [selectedStaffId, setSelectedStaffId] = useState(null)
  const [components, setComponents]           = useState([])
  const [loading, setLoading]                 = useState(false)
  const [saving, setSaving]                   = useState(false)

  // modal state
  const [modalOpen, setModalOpen] = useState(false)
  const [modalType, setModalType] = useState('Earning') // 'Earning' | 'Deduction'
  const [form]                    = Form.useForm()

  const loadComponents = useCallback(async (staffId) => {
    if (!staffId) return
    setLoading(true)
    try {
      const res = await api.get(`/school/staff/salary-components?staffId=${staffId}`)
      setComponents(res.data.data || [])
    } catch {
      message.error('Failed to load salary structure.')
    } finally {
      setLoading(false)
    }
  }, [])

  const handleStaffChange = (id) => {
    setSelectedStaffId(id)
    loadComponents(id)
  }

  const handleAdd = (type) => {
    setModalType(type)
    form.resetFields()
    setModalOpen(true)
  }

  const handleModalOk = () => {
    form.validateFields().then(values => {
      const newItem = {
        componentName: values.componentName.trim(),
        componentType: modalType,
        amount:        values.amount,
        displayOrder:  components.filter(c => c.componentType === modalType).length,
      }
      setComponents(prev => [...prev, newItem])
      setModalOpen(false)
    })
  }

  const handleDelete = (idx) => {
    setComponents(prev => prev.filter((_, i) => i !== idx))
  }

  const handleSave = async () => {
    if (!selectedStaffId) return
    setSaving(true)
    try {
      await api.post('/school/staff/salary-components', {
        staffId:    selectedStaffId,
        components: components.map((c, i) => ({
          componentName: c.componentName || c.ComponentName,
          componentType: c.componentType || c.ComponentType,
          amount:        c.amount        || c.Amount,
          displayOrder:  i,
        })),
      })
      message.success('Salary structure saved.')
      loadComponents(selectedStaffId)
    } catch {
      message.error('Failed to save salary structure.')
    } finally {
      setSaving(false)
    }
  }

  const earnings   = components.filter(c => (c.componentType || c.ComponentType) === 'Earning')
  const deductions = components.filter(c => (c.componentType || c.ComponentType) === 'Deduction')
  const totalGross = earnings.reduce((s, c) => s + Number(c.amount || c.Amount || 0), 0)
  const totalDed   = deductions.reduce((s, c) => s + Number(c.amount || c.Amount || 0), 0)

  const makeColumns = (typeLabel) => [
    { title: 'Component', dataIndex: 'componentName', key: 'name',
      render: (v, r) => v || r.ComponentName },
    { title: 'Amount (₹)', dataIndex: 'amount', key: 'amount', align: 'right',
      render: (v, r) => Number(v || r.Amount || 0).toFixed(2) },
    {
      title: '', key: 'del', width: 48, align: 'center',
      render: (_, __, idx) => {
        const allOfType = components
          .map((c, i) => ({ c, i }))
          .filter(x => (x.c.componentType || x.c.ComponentType) === typeLabel)
        const realIdx = allOfType[idx]?.i
        return (
          <Popconfirm title="Remove this component?" onConfirm={() => handleDelete(realIdx)}>
            <Button type="text" danger icon={<DeleteOutlined />} size="small" />
          </Popconfirm>
        )
      },
    },
  ]

  return (
    <div>
      <Row gutter={16} style={{ marginBottom: 16 }}>
        <Col span={10}>
          <Select
            showSearch
            placeholder="Select staff member"
            style={{ width: '100%' }}
            onChange={handleStaffChange}
            filterOption={(input, opt) =>
              opt.label.toLowerCase().includes(input.toLowerCase())
            }
            options={staffList.map(s => ({
              value: s.staffId || s.StaffId,
              label: `${s.employeeCode || s.EmployeeCode || ''} — ${s.staffName || s.StaffName}`,
            }))}
          />
        </Col>
      </Row>

      {selectedStaffId && (
        <Spin spinning={loading}>
          <Row gutter={16}>
            {/* Earnings */}
            <Col xs={24} md={12}>
              <Card
                size="small"
                title={<Text strong style={{ color: '#52c41a' }}>Earnings</Text>}
                extra={
                  <Button
                    type="dashed" size="small" icon={<PlusOutlined />}
                    onClick={() => handleAdd('Earning')}
                  >
                    Add Earning
                  </Button>
                }
              >
                <Table
                  rowKey={(_, i) => i}
                  dataSource={earnings}
                  columns={makeColumns('Earning')}
                  pagination={false}
                  size="small"
                  locale={{ emptyText: 'No earnings defined' }}
                  summary={() => (
                    <Table.Summary.Row>
                      <Table.Summary.Cell index={0}><Text strong>Total</Text></Table.Summary.Cell>
                      <Table.Summary.Cell index={1} align="right">
                        <Text strong>₹ {totalGross.toFixed(2)}</Text>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={2} />
                    </Table.Summary.Row>
                  )}
                />
              </Card>
            </Col>

            {/* Deductions */}
            <Col xs={24} md={12}>
              <Card
                size="small"
                title={<Text strong style={{ color: '#ff4d4f' }}>Deductions</Text>}
                extra={
                  <Button
                    type="dashed" size="small" icon={<PlusOutlined />}
                    onClick={() => handleAdd('Deduction')}
                  >
                    Add Deduction
                  </Button>
                }
              >
                <Table
                  rowKey={(_, i) => i}
                  dataSource={deductions}
                  columns={makeColumns('Deduction')}
                  pagination={false}
                  size="small"
                  locale={{ emptyText: 'No deductions defined' }}
                  summary={() => (
                    <Table.Summary.Row>
                      <Table.Summary.Cell index={0}><Text strong>Total</Text></Table.Summary.Cell>
                      <Table.Summary.Cell index={1} align="right">
                        <Text strong>₹ {totalDed.toFixed(2)}</Text>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={2} />
                    </Table.Summary.Row>
                  )}
                />
              </Card>
            </Col>
          </Row>

          <Divider />

          <Row gutter={32} style={{ marginBottom: 16 }}>
            <Col>
              <Statistic title="Gross Earnings" value={totalGross} precision={2} prefix="₹"
                valueStyle={{ color: '#52c41a' }} />
            </Col>
            <Col>
              <Statistic title="Total Deductions" value={totalDed} precision={2} prefix="₹"
                valueStyle={{ color: '#ff4d4f' }} />
            </Col>
            <Col>
              <Statistic title="Net Salary" value={totalGross - totalDed} precision={2} prefix="₹"
                valueStyle={{ color: '#1677ff', fontWeight: 700 }} />
            </Col>
          </Row>

          <Button
            type="primary" icon={<SaveOutlined />}
            loading={saving} onClick={handleSave}
          >
            Save Structure
          </Button>
        </Spin>
      )}

      {!selectedStaffId && (
        <div style={{ textAlign: 'center', color: '#bfbfbf', padding: '60px 0' }}>
          Select a staff member to view or edit their salary structure.
        </div>
      )}

      {/* Add component modal */}
      <Modal
        title={`Add ${modalType}`}
        open={modalOpen}
        onOk={handleModalOk}
        onCancel={() => setModalOpen(false)}
        okText="Add"
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="componentName"
            label="Component Name"
            rules={[{ required: true, message: 'Component name is required.' }]}
          >
            <Input placeholder={modalType === 'Earning' ? 'e.g. Basic Pay' : 'e.g. PF'} />
          </Form.Item>
          <Form.Item
            name="amount"
            label="Amount (₹)"
            rules={[{ required: true, message: 'Amount is required.' }]}
          >
            <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} prefix="₹" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Tab 2 — Monthly Salaries
// ─────────────────────────────────────────────────────────────────────────────
function MonthlySalariesTab({ staffList }) {
  const schoolName = useBrandingStore(s => s.branding.displayName)

  const now = new Date()
  const [month,    setMonth]    = useState(now.getMonth() + 1)
  const [year,     setYear]     = useState(now.getFullYear())
  const [status,   setStatus]   = useState(null)
  const [salaries, setSalaries] = useState([])
  const [loading,  setLoading]  = useState(false)

  // Process modal
  const [processOpen,   setProcessOpen]   = useState(false)
  const [processing,    setProcessing]    = useState(false)

  // Edit modal
  const [editRecord, setEditRecord] = useState(null)
  const [editForm]                  = Form.useForm()
  const [editSaving, setEditSaving] = useState(false)

  // Mark paid modal
  const [paidRecord, setPaidRecord] = useState(null)
  const [paidDate,   setPaidDate]   = useState(dayjs())
  const [paidSaving, setPaidSaving] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (month)  params.set('month',  month)
      if (year)   params.set('year',   year)
      if (status) params.set('status', status)
      const res = await api.get(`/school/staff/salaries?${params}`)
      setSalaries(res.data.data || [])
    } catch {
      message.error('Failed to load salaries.')
    } finally {
      setLoading(false)
    }
  }, [month, year, status])

  useEffect(() => { load() }, [load])

  const handleProcess = async () => {
    setProcessing(true)
    try {
      const res = await api.post('/school/staff/salaries/process', { month, year })
      message.success(res.data.message || 'Salaries processed.')
      setProcessOpen(false)
      load()
    } catch (e) {
      message.error(e?.response?.data?.message || 'Failed to process salaries.')
    } finally {
      setProcessing(false)
    }
  }

  const handleEditOk = async () => {
    const values = await editForm.validateFields()
    setEditSaving(true)
    try {
      await api.put(`/school/staff/salaries/${editRecord.salaryId}`, {
        advanceDeducted: values.advanceDeducted,
        remarks:         values.remarks || '',
      })
      message.success('Salary updated.')
      setEditRecord(null)
      load()
    } catch {
      message.error('Failed to update salary.')
    } finally {
      setEditSaving(false)
    }
  }

  const handleMarkPaid = async () => {
    setPaidSaving(true)
    try {
      const dateStr = paidDate ? paidDate.format('YYYY-MM-DD') : ''
      await api.put(`/school/staff/salaries/${paidRecord.salaryId}/mark-paid?paidDate=${dateStr}`)
      message.success('Salary marked as Paid.')
      setPaidRecord(null)
      load()
    } catch {
      message.error('Failed to mark salary as Paid.')
    } finally {
      setPaidSaving(false)
    }
  }

  const handleCancel = async (record) => {
    try {
      await api.put(`/school/staff/salaries/${record.salaryId}/cancel`)
      message.success('Salary cancelled.')
      load()
    } catch (e) {
      message.error(e?.response?.data?.message || 'Failed to cancel salary.')
    }
  }

  const handlePrintSlip = async (record) => {
    try {
      const res = await api.get(`/school/staff/salaries/${record.salaryId}/slip`)
      generateSalarySlipPdf(res.data.data, schoolName)
    } catch {
      message.error('Failed to load salary slip.')
    }
  }

  const statusTag = (s) => {
    const colors = { Draft: 'warning', Paid: 'success', Cancelled: 'default' }
    return <Tag color={colors[s] || 'default'}>{s}</Tag>
  }

  const columns = [
    { title: 'Emp Code',    dataIndex: 'employeeCode',    key: 'ec',    width: 100 },
    { title: 'Name',        dataIndex: 'staffName',       key: 'name',  width: 160 },
    { title: 'Designation', dataIndex: 'designation',     key: 'desig', width: 130 },
    { title: 'Gross (₹)',   dataIndex: 'grossEarnings',   key: 'gross', align: 'right',
      render: v => Number(v).toFixed(2) },
    { title: 'Deductions (₹)', dataIndex: 'totalDeductions', key: 'ded', align: 'right',
      render: v => Number(v).toFixed(2) },
    { title: 'Advance (₹)', dataIndex: 'advanceDeducted', key: 'adv',  align: 'right',
      render: v => Number(v).toFixed(2) },
    { title: 'Net Salary (₹)', dataIndex: 'netSalary',   key: 'net',  align: 'right',
      render: v => <Text strong>₹ {Number(v).toFixed(2)}</Text> },
    { title: 'Status',      dataIndex: 'status',          key: 'st',   width: 100,
      render: statusTag },
    {
      title: 'Actions', key: 'actions', width: 200,
      render: (_, record) => (
        <Space>
          {record.status === 'Draft' && (
            <>
              <Button
                size="small" icon={<EditOutlined />}
                onClick={() => {
                  setEditRecord(record)
                  editForm.setFieldsValue({
                    advanceDeducted: record.advanceDeducted,
                    remarks:         record.remarks,
                  })
                }}
              >
                Edit
              </Button>
              <Button
                size="small" type="primary" icon={<CheckCircleOutlined />}
                onClick={() => { setPaidRecord(record); setPaidDate(dayjs()) }}
              >
                Mark Paid
              </Button>
            </>
          )}
          <Button
            size="small" icon={<PrinterOutlined />}
            onClick={() => handlePrintSlip(record)}
          >
            Print Slip
          </Button>
          {record.status === 'Draft' && (
            <Popconfirm
              title="Cancel this salary record?"
              description="This action cannot be undone."
              onConfirm={() => handleCancel(record)}
            >
              <Button size="small" danger icon={<StopOutlined />}>Cancel</Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ]

  const monthLabel = MONTHS.find(m => m.value === month)?.label || ''

  return (
    <div>
      <Row gutter={12} style={{ marginBottom: 16 }} align="middle" wrap>
        <Col>
          <Select value={month} onChange={setMonth} style={{ width: 130 }}>
            {MONTHS.map(m => <Option key={m.value} value={m.value}>{m.label}</Option>)}
          </Select>
        </Col>
        <Col>
          <Select value={year} onChange={setYear} style={{ width: 100 }}>
            {YEARS.map(y => <Option key={y} value={y}>{y}</Option>)}
          </Select>
        </Col>
        <Col>
          <Select
            allowClear placeholder="All statuses"
            value={status} onChange={setStatus} style={{ width: 140 }}
          >
            <Option value="Draft">Draft</Option>
            <Option value="Paid">Paid</Option>
            <Option value="Cancelled">Cancelled</Option>
          </Select>
        </Col>
        <Col>
          <Button type="primary" onClick={() => setProcessOpen(true)}>
            Process Salaries
          </Button>
        </Col>
      </Row>

      <Table
        rowKey="salaryId"
        dataSource={salaries}
        columns={columns}
        loading={loading}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} records` }}
        size="small"
        scroll={{ x: 900 }}
        locale={{ emptyText: 'No salary records found. Click "Process Salaries" to generate.' }}
      />

      {/* Process confirm modal */}
      <Modal
        title="Process Salaries"
        open={processOpen}
        onOk={handleProcess}
        onCancel={() => setProcessOpen(false)}
        okText="Process"
        confirmLoading={processing}
      >
        <p>
          This will generate salary records for all active staff with a salary structure
          defined for <strong>{monthLabel} {year}</strong>.
        </p>
        <p>Staff already processed for this month will be skipped automatically.</p>
      </Modal>

      {/* Edit modal */}
      <Modal
        title={`Edit Salary — ${editRecord?.staffName}`}
        open={!!editRecord}
        onOk={handleEditOk}
        onCancel={() => setEditRecord(null)}
        okText="Save"
        confirmLoading={editSaving}
        destroyOnClose
      >
        <Form form={editForm} layout="vertical">
          <Form.Item name="advanceDeducted" label="Advance Deducted (₹)">
            <InputNumber min={0} step={0.01} style={{ width: '100%' }} prefix="₹" />
          </Form.Item>
          <Form.Item name="remarks" label="Remarks">
            <Input.TextArea rows={2} maxLength={200} />
          </Form.Item>
        </Form>
      </Modal>

      {/* Mark Paid modal */}
      <Modal
        title={`Mark as Paid — ${paidRecord?.staffName}`}
        open={!!paidRecord}
        onOk={handleMarkPaid}
        onCancel={() => setPaidRecord(null)}
        okText="Mark Paid"
        confirmLoading={paidSaving}
        destroyOnClose
      >
        <Form layout="vertical">
          <Form.Item label="Paid Date">
            <DatePicker
              value={paidDate}
              onChange={setPaidDate}
              style={{ width: '100%' }}
              format="DD-MM-YYYY"
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Main page
// ─────────────────────────────────────────────────────────────────────────────
export default function StaffSalariesPage() {
  const [staffList, setStaffList] = useState([])

  useEffect(() => {
    api.get('/school/staff?status=Active')
      .then(res => setStaffList(res.data.data || []))
      .catch(() => {})
  }, [])

  const items = [
    {
      key:      'structure',
      label:    'Salary Structure',
      children: <SalaryStructureTab staffList={staffList} />,
    },
    {
      key:      'monthly',
      label:    'Monthly Salaries',
      children: <MonthlySalariesTab staffList={staffList} />,
    },
  ]

  return (
    <Card title="Staff Salaries">
      <Tabs items={items} />
    </Card>
  )
}
