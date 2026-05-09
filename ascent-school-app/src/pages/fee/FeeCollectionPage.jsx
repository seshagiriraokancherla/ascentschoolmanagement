import { useState } from 'react'
import {
  Card, Input, Button, Table, Select, DatePicker, InputNumber,
  Row, Col, Divider, Tag, Avatar, Typography, Space, Statistic, Alert, App as AntApp,
} from 'antd'
import { SearchOutlined, UserOutlined, DollarOutlined, WifiOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'
import { useBrandingStore } from '../../store/brandingStore'

const { Text, Title } = Typography

// Dynamically load the Razorpay checkout script once.
function loadRazorpayScript() {
  return new Promise((resolve) => {
    if (window.Razorpay) { resolve(true); return }
    const script    = document.createElement('script')
    script.src      = 'https://checkout.razorpay.com/v1/checkout.js'
    script.onload   = () => resolve(true)
    script.onerror  = () => resolve(false)
    document.body.appendChild(script)
  })
}

export default function FeeCollectionPage() {
  const { message, modal } = AntApp.useApp()
  const { branding }       = useBrandingStore()

  // Student search
  const [searchVal,   setSearchVal]   = useState('')
  const [searching,   setSearching]   = useState(false)
  const [students,    setStudents]    = useState([])

  // Selected student
  const [summary,     setSummary]     = useState(null)
  const [loadingDues, setLoadingDues] = useState(false)

  // Payment form
  const [paymentModes,   setPaymentModes]   = useState([])
  const [paymentModeId,  setPaymentModeId]  = useState(null)
  const [paymentDate,    setPaymentDate]    = useState(dayjs())
  const [chequeNo,       setChequeNo]       = useState('')
  const [chequeDate,     setChequeDate]     = useState(null)
  const [bankName,       setBankName]       = useState('')
  const [remarks,        setRemarks]        = useState('')

  // Line item amounts (maps "feeTypeId_termId" → { amount, concession })
  const [lineAmounts, setLineAmounts] = useState({})
  const [collecting,  setCollecting]  = useState(false)

  // Derived: is the selected mode an online/gateway mode?
  const selectedMode = paymentModes.find((m) => m.paymentModeId === paymentModeId)
  const isOnlineMode = selectedMode?.isOnline === true

  const searchStudents = async () => {
    if (!searchVal.trim()) return
    setSearching(true)
    try {
      const res = await api.get(`/school/students?search=${encodeURIComponent(searchVal)}`)
      setStudents(res.data?.data || [])
    } finally {
      setSearching(false)
    }
  }

  const selectStudent = async (student) => {
    setStudents([])
    setSearchVal(student.studentName)
    setLoadingDues(true)
    setSummary(null)
    setLineAmounts({})
    try {
      if (paymentModes.length === 0) {
        const pmRes = await api.get('/school/master/payment-modes')
        setPaymentModes(pmRes.data?.data || [])
      }
      const res = await api.get(`/school/fees/student/${student.studentId}`)
      const sum = res.data?.data
      setSummary(sum)
      const initAmounts = {}
      ;(sum.lineItems || []).forEach((li) => {
        const key = `${li.feeTypeId}_${li.termId ?? 0}`
        initAmounts[key] = { amount: Math.max(0, li.outstanding), concession: 0 }
      })
      setLineAmounts(initAmounts)
    } catch {
      message.error('Failed to load student fee details.')
    } finally {
      setLoadingDues(false)
    }
  }

  const lineKey = (li) => `${li.feeTypeId}_${li.termId ?? 0}`

  const updateLine = (li, field, val) => {
    const key = lineKey(li)
    setLineAmounts((prev) => ({
      ...prev,
      [key]: { ...(prev[key] || { amount: 0, concession: 0 }), [field]: val ?? 0 },
    }))
  }

  const totalToPay = summary
    ? (summary.lineItems || []).reduce((acc, li) => {
        const { amount = 0, concession = 0 } = lineAmounts[lineKey(li)] || {}
        return acc + Math.max(0, amount - concession)
      }, 0)
    : 0

  // Build the items array used by both offline and online flows
  const buildItems = () =>
    (summary.lineItems || [])
      .map((li) => {
        const { amount = 0, concession = 0 } = lineAmounts[lineKey(li)] || {}
        return {
          FeeTypeId:        li.feeTypeId,
          TermId:           li.termId,
          Amount:           amount,
          ConcessionAmount: concession,
        }
      })
      .filter((i) => i.Amount > 0)

  // ── Offline collection (cash / cheque) ─────────────────────────────────
  const handleOfflineCollect = async () => {
    const items = buildItems()
    if (items.length === 0) { message.warning('No amounts entered.'); return }

    setCollecting(true)
    try {
      const res = await api.post('/school/fees/collect', {
        StudentId:      summary.studentId,
        AcademicYearId: summary.academicYearId,
        PaymentModeId:  paymentModeId,
        PaymentDate:    paymentDate?.toISOString() || null,
        ChequeNo:       chequeNo || null,
        ChequeDate:     chequeDate?.toISOString() || null,
        BankName:       bankName || null,
        Remarks:        remarks || null,
        Items:          items,
      })
      const receipt = res.data?.data
      modal.success({
        title:   `Receipt ${receipt.receiptNo} generated`,
        content: `Total collected: ₹${receipt.totalAmount.toFixed(2)}`,
      })
      reloadDues()
      resetPaymentForm()
    } catch {
      message.error('Failed to collect fee.')
    } finally {
      setCollecting(false)
    }
  }

  // ── Online collection (gateway checkout) ───────────────────────────────
  const handleOnlineCollect = async () => {
    const items = buildItems()
    if (items.length === 0) { message.warning('No amounts entered.'); return }

    setCollecting(true)
    try {
      // Step 1: Create order on our server (which calls Razorpay API)
      const orderRes = await api.post('/school/fees/payment-orders', {
        StudentId:      summary.studentId,
        AcademicYearId: summary.academicYearId,
        PaymentModeId:  paymentModeId,
        PaymentDate:    paymentDate?.toISOString() || null,
        Remarks:        remarks || null,
        Items:          items,
      })
      const orderData = orderRes.data?.data

      // Step 2: Load Razorpay checkout script
      const scriptLoaded = await loadRazorpayScript()
      if (!scriptLoaded) {
        message.error('Could not load Razorpay checkout. Check your internet connection.')
        return
      }

      // Step 3: Open Razorpay checkout modal
      await new Promise((resolve, reject) => {
        const rzp = new window.Razorpay({
          key:         orderData.keyId,
          amount:      orderData.amountInPaise,
          currency:    orderData.currency,
          order_id:    orderData.externalOrderId,
          name:        'School Fee Payment',
          description: `Fee for ${summary.studentName}`,
          theme:       { color: branding.primaryColor || '#1677ff' },
          prefill:     { name: summary.studentName },
          modal: {
            ondismiss: () => {
              message.warning('Payment cancelled.')
              reject(new Error('dismissed'))
            },
          },
          handler: async (response) => {
            try {
              // Step 4: Verify on server — creates receipt
              const verifyRes = await api.post(
                `/school/fees/payment-orders/${orderData.gatewayOrderId}/verify`,
                {
                  PaymentId: response.razorpay_payment_id,
                  OrderId:   response.razorpay_order_id,
                  Signature: response.razorpay_signature,
                }
              )
              const receipt = verifyRes.data?.data
              modal.success({
                title:   `Receipt ${receipt.receiptNo} generated`,
                content: `Online payment of ₹${receipt.totalAmount.toFixed(2)} confirmed. Ref: ${response.razorpay_payment_id}`,
              })
              reloadDues()
              resetPaymentForm()
              resolve()
            } catch {
              message.error('Payment received but receipt creation failed. Contact support with payment ID: ' + response.razorpay_payment_id)
              reject(new Error('verify_failed'))
            }
          },
        })
        rzp.open()
      })
    } catch (err) {
      // 'dismissed' is user-initiated, not an error
      if (err?.message !== 'dismissed' && err?.message !== 'verify_failed') {
        message.error('Failed to initiate online payment.')
      }
    } finally {
      setCollecting(false)
    }
  }

  const handleCollect = () => {
    if (!summary)            return
    if (!paymentModeId)      { message.warning('Select a payment mode.'); return }
    isOnlineMode ? handleOnlineCollect() : handleOfflineCollect()
  }

  const reloadDues = async () => {
    const sumRes = await api.get(`/school/fees/student/${summary.studentId}`)
    const newSum = sumRes.data?.data
    setSummary(newSum)
    const initAmounts = {}
    ;(newSum.lineItems || []).forEach((li) => {
      const key = lineKey(li)
      initAmounts[key] = { amount: Math.max(0, li.outstanding), concession: 0 }
    })
    setLineAmounts(initAmounts)
  }

  const resetPaymentForm = () => {
    setPaymentModeId(null)
    setPaymentDate(dayjs())
    setChequeNo(''); setChequeDate(null); setBankName(''); setRemarks('')
  }

  const dueColumns = [
    { title: 'Fee Type', dataIndex: 'feeTypeName', key: 'feeTypeName', width: 160 },
    { title: 'Term',     dataIndex: 'termName',    key: 'termName',    width: 120,
      render: (v) => v || <Text type="secondary">—</Text> },
    {
      title: 'Structure (₹)', dataIndex: 'structureAmount', key: 'structureAmount', width: 110,
      render: (v) => v.toFixed(2),
    },
    {
      title: 'Paid (₹)', dataIndex: 'paidAmount', key: 'paidAmount', width: 100,
      render: (v) => v.toFixed(2),
    },
    {
      title: 'Outstanding (₹)', dataIndex: 'outstanding', key: 'outstanding', width: 120,
      render: (v) => (
        <Text strong style={{ color: v > 0 ? '#cf1322' : '#52c41a' }}>{v.toFixed(2)}</Text>
      ),
    },
    {
      title: 'Concession (₹)',
      key: 'concession',
      width: 120,
      render: (_, li) => (
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          precision={2}
          value={lineAmounts[lineKey(li)]?.concession ?? 0}
          onChange={(v) => updateLine(li, 'concession', v)}
        />
      ),
    },
    {
      title: 'Pay Now (₹)',
      key: 'amount',
      width: 120,
      render: (_, li) => (
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          precision={2}
          value={lineAmounts[lineKey(li)]?.amount ?? 0}
          onChange={(v) => updateLine(li, 'amount', v)}
        />
      ),
    },
  ]

  return (
    <div>
      {/* Student Search */}
      <Card title="Fee Collection" style={{ marginBottom: 16 }}>
        <Row gutter={12}>
          <Col flex="auto">
            <Input
              placeholder="Search student by name or admission no"
              prefix={<SearchOutlined />}
              value={searchVal}
              onChange={(e) => setSearchVal(e.target.value)}
              onPressEnter={searchStudents}
            />
          </Col>
          <Col>
            <Button icon={<SearchOutlined />} onClick={searchStudents} loading={searching}>
              Search
            </Button>
          </Col>
        </Row>

        {/* Search results dropdown */}
        {students.length > 0 && (
          <div style={{ border: '1px solid #d9d9d9', borderRadius: 6, marginTop: 8, maxHeight: 240, overflow: 'auto' }}>
            {students.map((s) => (
              <div
                key={s.studentId}
                style={{ padding: '8px 12px', cursor: 'pointer', borderBottom: '1px solid #f0f0f0' }}
                onClick={() => selectStudent(s)}
                onMouseEnter={(e) => e.currentTarget.style.background = '#f5f5f5'}
                onMouseLeave={(e) => e.currentTarget.style.background = 'white'}
              >
                <Space>
                  <Avatar icon={<UserOutlined />} size={28} />
                  <span><strong>{s.studentName}</strong> — {s.admissionNo} — {s.className}</span>
                  <Tag color={s.status === 'Active' ? 'green' : 'default'}>{s.status}</Tag>
                </Space>
              </div>
            ))}
          </div>
        )}
      </Card>

      {/* Student + Dues */}
      {summary && (
        <>
          {/* Student Info */}
          <Card style={{ marginBottom: 16 }}>
            <Row gutter={24}>
              <Col>
                <Text type="secondary">Student</Text>
                <Title level={5} style={{ margin: 0 }}>{summary.studentName}</Title>
                <Text type="secondary">{summary.admissionNo}</Text>
              </Col>
              <Col>
                <Text type="secondary">Class</Text>
                <div><Text strong>{summary.className || '—'}</Text></div>
              </Col>
              <Col>
                <Text type="secondary">Fee Category</Text>
                <div><Text strong>{summary.feeCategoryName || '—'}</Text></div>
              </Col>
              <Col>
                <Text type="secondary">Academic Year</Text>
                <div><Text strong>{summary.academicYear || '—'}</Text></div>
              </Col>
            </Row>
          </Card>

          {/* Dues Table */}
          <Card title="Fee Dues" style={{ marginBottom: 16 }}>
            <Table
              rowKey={(r) => lineKey(r)}
              dataSource={summary.lineItems || []}
              columns={dueColumns}
              pagination={false}
              size="small"
              loading={loadingDues}
              scroll={{ x: 'max-content' }}
            />
          </Card>

          {/* Payment Details */}
          <Card title="Payment Details">
            <Row gutter={16}>
              <Col xs={24} md={6}>
                <div style={{ marginBottom: 8 }}><label>Payment Mode *</label></div>
                <Select
                  style={{ width: '100%' }}
                  placeholder="Select mode"
                  options={paymentModes.map((m) => ({
                    value: m.paymentModeId,
                    label: m.isOnline
                      ? <span><WifiOutlined style={{ color: '#1677ff', marginRight: 6 }} />{m.modeName}</span>
                      : m.modeName,
                  }))}
                  value={paymentModeId}
                  onChange={setPaymentModeId}
                />
              </Col>
              <Col xs={24} md={6}>
                <div style={{ marginBottom: 8 }}><label>Payment Date</label></div>
                <DatePicker
                  style={{ width: '100%' }}
                  value={paymentDate}
                  onChange={setPaymentDate}
                  format="DD-MM-YYYY"
                />
              </Col>

              {/* Cheque fields — hidden for online modes */}
              {!isOnlineMode && (
                <>
                  <Col xs={24} md={6}>
                    <div style={{ marginBottom: 8 }}><label>Cheque No</label></div>
                    <Input value={chequeNo} onChange={(e) => setChequeNo(e.target.value)} />
                  </Col>
                  <Col xs={24} md={6}>
                    <div style={{ marginBottom: 8 }}><label>Cheque Date</label></div>
                    <DatePicker
                      style={{ width: '100%' }}
                      value={chequeDate}
                      onChange={setChequeDate}
                      format="DD-MM-YYYY"
                    />
                  </Col>
                  <Col xs={24} md={6}>
                    <div style={{ marginBottom: 8 }}><label>Bank Name</label></div>
                    <Input value={bankName} onChange={(e) => setBankName(e.target.value)} />
                  </Col>
                </>
              )}

              <Col xs={24} md={12}>
                <div style={{ marginBottom: 8 }}><label>Remarks</label></div>
                <Input value={remarks} onChange={(e) => setRemarks(e.target.value)} />
              </Col>
            </Row>

            {/* Online mode info banner */}
            {isOnlineMode && (
              <Alert
                type="info"
                showIcon
                style={{ marginTop: 16 }}
                message="Online payment"
                description={`After clicking Collect, a ${selectedMode?.modeName} checkout will open. The parent/student can pay via UPI, credit card, debit card, or net banking.`}
              />
            )}

            <Divider />

            <Row justify="end" align="middle" gutter={24}>
              <Col>
                <Statistic
                  title="Total to Collect"
                  value={totalToPay}
                  prefix="₹"
                  precision={2}
                  valueStyle={{ color: '#1677ff', fontSize: 24 }}
                />
              </Col>
              <Col>
                <Button
                  type="primary"
                  size="large"
                  icon={isOnlineMode ? <WifiOutlined /> : <DollarOutlined />}
                  loading={collecting}
                  onClick={handleCollect}
                  disabled={totalToPay <= 0}
                >
                  {isOnlineMode ? 'Collect Online & Issue Receipt' : 'Collect & Issue Receipt'}
                </Button>
              </Col>
            </Row>
          </Card>
        </>
      )}
    </div>
  )
}
