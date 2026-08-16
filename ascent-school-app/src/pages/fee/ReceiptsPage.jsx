import { useEffect, useState } from 'react'
import {
  Card, Table, Button, Input, Select, DatePicker, Tag, Drawer,
  Descriptions, Row, Col, Divider, Typography, Space, Modal, Form, App as AntApp,
} from 'antd'
import { SearchOutlined, EyeOutlined, StopOutlined, DownloadOutlined, PrinterOutlined } from '@ant-design/icons'
import Papa from 'papaparse'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'
import { useBrandingStore } from '../../store/brandingStore'

const { Text, Title } = Typography
const { RangePicker } = DatePicker

const STATUS_OPTIONS = [
  { value: '',          label: 'All Status' },
  { value: 'Active',    label: 'Active' },
  { value: 'Cancelled', label: 'Cancelled' },
]

export default function ReceiptsPage() {
  const { message }  = AntApp.useApp()
  const { branding } = useBrandingStore()

  const [receipts,  setReceipts]  = useState([])
  const [loading,   setLoading]   = useState(false)
  const [printReceipt, setPrintReceipt] = useState(null)
  const [search,       setSearch]       = useState('')
  const [dateRange,    setDateRange]    = useState(null)
  const [status,       setStatus]       = useState('')
  const [createdAfter, setCreatedAfter] = useState(null)
  const [paymentModeId, setPaymentModeId] = useState(null)
  const [paymentModes,  setPaymentModes]  = useState([])
  const [page,         setPage]         = useState(1)
  const [pageSize,     setPageSize]     = useState(20)

  // Receipt detail drawer
  const [drawerOpen,  setDrawerOpen]  = useState(false)
  const [detailReceipt, setDetailReceipt] = useState(null)
  const [loadingDetail, setLoadingDetail] = useState(false)

  // Cancel modal
  const [cancelModalOpen, setCancelModalOpen] = useState(false)
  const [cancellingId,    setCancellingId]    = useState(null)
  const [cancelling,      setCancelling]      = useState(false)
  const [cancelForm] = Form.useForm()

  const load = async () => {
    setLoading(true)
    try {
      const q = new URLSearchParams()
      if (search)           q.set('search',       search)
      if (dateRange?.[0])   q.set('dateFrom',     dateRange[0].format('YYYY-MM-DD'))
      if (dateRange?.[1])   q.set('dateTo',       dateRange[1].format('YYYY-MM-DD'))
      if (status)           q.set('status',       status)
      if (createdAfter)     q.set('createdAfter', createdAfter.format('YYYY-MM-DDTHH:mm:ss'))
      if (paymentModeId)    q.set('paymentModeId', paymentModeId)
      const res = await api.get(`/school/fees/receipts?${q}`)
      setReceipts(res.data?.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  // Load Active payment modes for the filter dropdown.
  useEffect(() => {
    api.get('/school/master/payment-modes')
      .then(r => setPaymentModes((r.data?.data || []).filter(m => m.status === 'Active')))
      .catch(() => {})
  }, [])

  const viewReceipt = async (receiptId) => {
    setDrawerOpen(true)
    setLoadingDetail(true)
    try {
      const res = await api.get(`/school/fees/receipts/${receiptId}`)
      setDetailReceipt(res.data?.data)
    } catch (e) {
      message.error(apiError(e, 'Failed to load receipt.'))
    } finally {
      setLoadingDetail(false)
    }
  }

  // Render the receipt in the hidden print area, then open the browser print dialog.
  const handlePrint = async (receiptId, existing) => {
    let rec = existing
    if (!rec) {
      try {
        const res = await api.get(`/school/fees/receipts/${receiptId}`)
        rec = res.data?.data
      } catch (e) {
        message.error(apiError(e, 'Failed to load receipt for printing.'))
        return
      }
    }
    setPrintReceipt(rec)
    // Let React paint the print area before invoking print.
    setTimeout(() => window.print(), 150)
  }

  const openCancelModal = (receiptId) => {
    setCancellingId(receiptId)
    cancelForm.resetFields()
    setCancelModalOpen(true)
  }

  const handleCancel = async () => {
    const values = await cancelForm.validateFields()
    setCancelling(true)
    try {
      await api.put(`/school/fees/receipts/${cancellingId}/cancel`, {
        CancelReason: values.cancelReason,
      })
      message.success('Receipt cancelled.')
      setCancelModalOpen(false)
      load()
      if (detailReceipt?.receiptId === cancellingId) setDrawerOpen(false)
    } catch (e) {
      message.error(apiError(e, 'Failed to cancel receipt.'))
    } finally {
      setCancelling(false)
    }
  }

  const exportCsv = () => {
    if (receipts.length === 0) { message.warning('No receipts to export.'); return }
    const rows = receipts.map((r) => ({
      ReceiptNo:       r.receiptNo,
      StudentName:     r.studentName,
      AdmissionNo:     r.admissionNo,
      Class:           r.className,
      PaymentDate:     r.paymentDate ? dayjs(r.paymentDate).format('DD-MM-YYYY') : '',
      Amount:          Number(r.totalAmount).toFixed(2),
      PaymentMode:     r.paymentModeName,
      Status:          r.status,
    }))
    const csv = Papa.unparse(rows)
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
    const a   = Object.assign(document.createElement('a'), { href: url, download: 'receipts.csv' })
    a.click(); URL.revokeObjectURL(url)
  }

  const columns = [
    { title: 'S.No', key: 'serialNo', width: 60, render: (_, __, index) => (page - 1) * pageSize + index + 1 },
    { title: 'Receipt No',  dataIndex: 'receiptNo',       key: 'receiptNo',       width: 110 },
    { title: 'Student',     dataIndex: 'studentName',     key: 'studentName' },
    { title: 'Adm No',      dataIndex: 'admissionNo',     key: 'admissionNo',     width: 100 },
    { title: 'Class',       dataIndex: 'className',       key: 'className',       width: 110 },
    {
      title: 'Date',
      dataIndex: 'paymentDate',
      key:  'paymentDate',
      width: 100,
      render: (v) => v ? dayjs(v).format('DD-MM-YYYY') : '—',
    },
    {
      title: 'Amount (₹)',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      width: 110,
      render: (v) => <Text strong>{Number(v).toFixed(2)}</Text>,
    },
    { title: 'Mode',  dataIndex: 'paymentModeName', key: 'paymentModeName', width: 90 },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (v) => <Tag color={v === 'Active' ? 'green' : 'red'}>{v}</Tag>,
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 140,
      render: (_, record) => (
        <Space size="small">
          <Button size="small" icon={<EyeOutlined />} onClick={() => viewReceipt(record.receiptId)}>
            View
          </Button>
          <Button size="small" icon={<PrinterOutlined />} onClick={() => handlePrint(record.receiptId)}>
            Print
          </Button>
          {record.status === 'Active' && (
            <Button
              size="small"
              danger
              icon={<StopOutlined />}
              onClick={() => openCancelModal(record.receiptId)}
            >
              Cancel
            </Button>
          )}
        </Space>
      ),
    },
  ]

  return (
    <>
      <Card title="Fee Receipts">
        {/* Filters */}
        <Row gutter={12} style={{ marginBottom: 16 }}>
          <Col flex="auto">
            <Input
              placeholder="Search by student name, admission no or receipt no"
              prefix={<SearchOutlined />}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onPressEnter={load}
              allowClear
            />
          </Col>
          <Col>
            <RangePicker
              format="DD-MM-YYYY"
              value={dateRange}
              onChange={setDateRange}
            />
          </Col>
          <Col>
            <Select
              style={{ width: 130 }}
              options={STATUS_OPTIONS}
              value={status}
              onChange={setStatus}
            />
          </Col>
          <Col>
            <Select
              style={{ width: 150 }}
              placeholder="All Payment Modes"
              allowClear
              value={paymentModeId}
              onChange={(v) => setPaymentModeId(v ?? null)}
              options={paymentModes.map(m => ({ label: m.modeName, value: m.paymentModeId }))}
            />
          </Col>
          <Col>
            <DatePicker
              placeholder="Created after (sync filter)"
              showTime={{ format: 'HH:mm' }}
              format="DD-MM-YYYY HH:mm"
              value={createdAfter}
              onChange={setCreatedAfter}
              allowClear
            />
          </Col>
          <Col>
            <Button icon={<SearchOutlined />} onClick={load}>Search</Button>
          </Col>
          <Col>
            <Button icon={<DownloadOutlined />} onClick={exportCsv}>Export CSV</Button>
          </Col>
        </Row>

        <Table
          rowKey="receiptId"
          dataSource={receipts}
          columns={columns}
          loading={loading}
          size="small"
          pagination={{
            current: page,
            pageSize,
            showTotal: (t) => `${t} receipts`,
            onChange: (p, ps) => { setPage(p); setPageSize(ps) },
          }}
        />
      </Card>

      {/* Receipt Detail Drawer */}
      <Drawer
        title={`Receipt — ${detailReceipt?.receiptNo || ''}`}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        width={520}
        loading={loadingDetail}
        extra={
          detailReceipt && (
            <Space>
              <Button
                icon={<PrinterOutlined />}
                size="small"
                onClick={() => handlePrint(detailReceipt.receiptId, detailReceipt)}
              >
                Print
              </Button>
              {detailReceipt.status === 'Active' && (
                <Button
                  danger
                  icon={<StopOutlined />}
                  size="small"
                  onClick={() => openCancelModal(detailReceipt.receiptId)}
                >
                  Cancel Receipt
                </Button>
              )}
            </Space>
          )
        }
      >
        {detailReceipt && (
          <>
            <Tag
              color={detailReceipt.status === 'Active' ? 'green' : 'red'}
              style={{ marginBottom: 12 }}
            >
              {detailReceipt.status}
            </Tag>

            <Descriptions bordered size="small" column={1} style={{ marginBottom: 16 }}>
              <Descriptions.Item label="Receipt No">{detailReceipt.receiptNo}</Descriptions.Item>
              <Descriptions.Item label="Student">{detailReceipt.studentName}</Descriptions.Item>
              <Descriptions.Item label="Admission No">{detailReceipt.admissionNo}</Descriptions.Item>
              <Descriptions.Item label="Father Name">{detailReceipt.fatherName || '—'}</Descriptions.Item>
              <Descriptions.Item label="Class">{detailReceipt.className || '—'}</Descriptions.Item>
              <Descriptions.Item label="Academic Year">{detailReceipt.academicYear || '—'}</Descriptions.Item>
              <Descriptions.Item label="Payment Date">
                {dayjs(detailReceipt.paymentDate).format('DD-MM-YYYY')}
              </Descriptions.Item>
              <Descriptions.Item label="Payment Mode">{detailReceipt.paymentModeName || '—'}</Descriptions.Item>
              {detailReceipt.chequeNo && (
                <Descriptions.Item label="Reference No">{detailReceipt.chequeNo}</Descriptions.Item>
              )}
              {detailReceipt.bankName && (
                <Descriptions.Item label="Bank">{detailReceipt.bankName}</Descriptions.Item>
              )}
              {detailReceipt.remarks && (
                <Descriptions.Item label="Remarks">{detailReceipt.remarks}</Descriptions.Item>
              )}
              {detailReceipt.status === 'Cancelled' && (
                <Descriptions.Item label="Cancel Reason">
                  <Text type="danger">{detailReceipt.cancelReason}</Text>
                </Descriptions.Item>
              )}
              <Descriptions.Item label="Collected By">{detailReceipt.createdBy || '—'}</Descriptions.Item>
            </Descriptions>

            <Divider orientation="left" plain>Fee Items</Divider>

            <Table
              rowKey="itemId"
              size="small"
              pagination={false}
              dataSource={detailReceipt.items || []}
              columns={[
                { title: 'Fee Type', dataIndex: 'feeTypeName', key: 'feeTypeName' },
                { title: 'Term',     dataIndex: 'termName',    key: 'termName', render: (v) => v || '—' },
                { title: 'Amount',   dataIndex: 'amount',      key: 'amount',   render: (v) => `₹${Number(v).toFixed(2)}` },
                { title: 'Concession', dataIndex: 'concessionAmount', key: 'concessionAmount',
                  render: (v) => v > 0 ? <Text type="warning">₹{Number(v).toFixed(2)}</Text> : '—' },
                { title: 'Net',      dataIndex: 'netAmount',   key: 'netAmount',
                  render: (v) => <Text strong>₹{Number(v).toFixed(2)}</Text> },
              ]}
            />

            <Divider />
            <div style={{ textAlign: 'right' }}>
              <Title level={4}>Total: ₹{Number(detailReceipt.totalAmount).toFixed(2)}</Title>
            </div>
          </>
        )}
      </Drawer>

      {/* Cancel Modal */}
      <Modal
        title="Cancel Receipt"
        open={cancelModalOpen}
        onOk={handleCancel}
        onCancel={() => setCancelModalOpen(false)}
        confirmLoading={cancelling}
        okText="Confirm Cancel"
        okButtonProps={{ danger: true }}
      >
        <Form form={cancelForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="cancelReason"
            label="Reason for Cancellation"
            rules={[{ required: true, message: 'Please enter a reason.' }]}
          >
            <Input.TextArea rows={3} placeholder="Enter cancellation reason..." />
          </Form.Item>
        </Form>
      </Modal>

      {/* Hidden print area — visible only on print */}
      <ReceiptPrintArea receipt={printReceipt} branding={branding} />
      <style>{`
        .receipt-print-area { display: none; }
        @media print {
          body * { visibility: hidden; }
          .receipt-print-area { display: block; }
          .receipt-print-area, .receipt-print-area * { visibility: visible; }
          .receipt-print-area { position: absolute; left: 0; top: 0; width: 100%; }
        }
      `}</style>
    </>
  )
}

const printCellHead = { border: '1px solid #000', padding: '5px 8px', textAlign: 'left', background: '#f0f0f0' }
const printCell     = { border: '1px solid #000', padding: '5px 8px' }

function ReceiptPrintArea({ receipt, branding }) {
  if (!receipt) return null
  const fmt = (n) => `₹${Number(n || 0).toFixed(2)}`

  return (
    <div className="receipt-print-area">
      <div style={{ padding: 24, fontFamily: 'Arial, sans-serif', color: '#000' }}>
        {/* Header */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, borderBottom: '2px solid #000', paddingBottom: 8 }}>
          {branding?.logoPath && <img src={branding.logoPath} alt="logo" style={{ height: 56 }} />}
          <div style={{ flex: 1 }}>
            <div style={{ fontSize: 20, fontWeight: 'bold' }}>{branding?.displayName || 'School'}</div>
            <div style={{ fontSize: 13 }}>Fee Receipt</div>
          </div>
          <div style={{ textAlign: 'right', fontSize: 12 }}>
            <div><strong>Receipt No:</strong> {receipt.receiptNo}</div>
            <div><strong>Date:</strong> {dayjs(receipt.paymentDate).format('DD-MM-YYYY')}</div>
            {receipt.status === 'Cancelled' && (
              <div style={{ color: 'red', fontWeight: 'bold' }}>CANCELLED</div>
            )}
          </div>
        </div>

        {/* Student / payment details */}
        <table style={{ width: '100%', fontSize: 13, margin: '12px 0' }}>
          <tbody>
            <tr>
              <td style={{ padding: '2px 0' }}><strong>Student:</strong> {receipt.studentName}</td>
              <td style={{ padding: '2px 0' }}><strong>Admission No:</strong> {receipt.admissionNo}</td>
            </tr>
            <tr>
              <td style={{ padding: '2px 0' }}><strong>Class:</strong> {receipt.className || '—'}</td>
              <td style={{ padding: '2px 0' }}><strong>Academic Year:</strong> {receipt.academicYear || '—'}</td>
            </tr>
            <tr>
              <td style={{ padding: '2px 0' }}><strong>Father Name:</strong> {receipt.fatherName || '—'}</td>
              <td style={{ padding: '2px 0' }}><strong>Payment Mode:</strong> {receipt.paymentModeName || '—'}</td>
            </tr>
            {(receipt.chequeNo || receipt.bankName) && (
              <tr>
                <td style={{ padding: '2px 0' }}>{receipt.chequeNo && <><strong>Reference No:</strong> {receipt.chequeNo}</>}</td>
                <td style={{ padding: '2px 0' }}>{receipt.bankName && <><strong>Bank:</strong> {receipt.bankName}</>}</td>
              </tr>
            )}
          </tbody>
        </table>

        {/* Items */}
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
          <thead>
            <tr>
              <th style={printCellHead}>Fee Type</th>
              <th style={printCellHead}>Term</th>
              <th style={{ ...printCellHead, textAlign: 'right' }}>Amount</th>
              <th style={{ ...printCellHead, textAlign: 'right' }}>Concession</th>
              <th style={{ ...printCellHead, textAlign: 'right' }}>Net</th>
            </tr>
          </thead>
          <tbody>
            {(receipt.items || []).map((it, i) => (
              <tr key={i}>
                <td style={printCell}>{it.feeTypeName || it.routeName || it.hostelName || '—'}</td>
                <td style={printCell}>{it.termName || '—'}</td>
                <td style={{ ...printCell, textAlign: 'right' }}>{fmt(it.amount)}</td>
                <td style={{ ...printCell, textAlign: 'right' }}>{it.concessionAmount > 0 ? fmt(it.concessionAmount) : '—'}</td>
                <td style={{ ...printCell, textAlign: 'right' }}>{fmt(it.netAmount)}</td>
              </tr>
            ))}
            <tr>
              <td style={{ ...printCell, textAlign: 'right', fontWeight: 'bold' }} colSpan={4}>Total</td>
              <td style={{ ...printCell, textAlign: 'right', fontWeight: 'bold' }}>{fmt(receipt.totalAmount)}</td>
            </tr>
          </tbody>
        </table>

        {receipt.status === 'Cancelled' && receipt.cancelReason && (
          <div style={{ marginTop: 12, color: 'red', fontSize: 12 }}>
            <strong>Cancel Reason:</strong> {receipt.cancelReason}
          </div>
        )}

        {/* Footer */}
        <div style={{ marginTop: 28, display: 'flex', justifyContent: 'space-between', fontSize: 12 }}>
          <div>{branding?.receiptFooterText || ''}</div>
          <div>Collected by: {receipt.createdBy || '—'}</div>
        </div>
      </div>
    </div>
  )
}
