import { useEffect, useState } from 'react'
import {
  Card, Table, Button, Input, Select, DatePicker, Tag, Drawer,
  Descriptions, Row, Col, Divider, Typography, Space, Modal, Form, App as AntApp,
} from 'antd'
import { SearchOutlined, EyeOutlined, StopOutlined, DownloadOutlined } from '@ant-design/icons'
import Papa from 'papaparse'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Text, Title } = Typography
const { RangePicker } = DatePicker

const STATUS_OPTIONS = [
  { value: '',          label: 'All Status' },
  { value: 'Active',    label: 'Active' },
  { value: 'Cancelled', label: 'Cancelled' },
]

export default function ReceiptsPage() {
  const { message } = AntApp.useApp()

  const [receipts,  setReceipts]  = useState([])
  const [loading,   setLoading]   = useState(false)
  const [search,       setSearch]       = useState('')
  const [dateRange,    setDateRange]    = useState(null)
  const [status,       setStatus]       = useState('')
  const [createdAfter, setCreatedAfter] = useState(null)

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
      const res = await api.get(`/school/fees/receipts?${q}`)
      setReceipts(res.data?.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const viewReceipt = async (receiptId) => {
    setDrawerOpen(true)
    setLoadingDetail(true)
    try {
      const res = await api.get(`/school/fees/receipts/${receiptId}`)
      setDetailReceipt(res.data?.data)
    } catch {
      message.error('Failed to load receipt.')
    } finally {
      setLoadingDetail(false)
    }
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
    } catch {
      message.error('Failed to cancel receipt.')
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
          pagination={{ pageSize: 20, showTotal: (t) => `${t} receipts` }}
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
          detailReceipt?.status === 'Active' && (
            <Button
              danger
              icon={<StopOutlined />}
              size="small"
              onClick={() => openCancelModal(detailReceipt.receiptId)}
            >
              Cancel Receipt
            </Button>
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
    </>
  )
}
