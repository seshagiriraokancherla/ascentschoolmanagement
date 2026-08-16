import { useState } from 'react'
import {
  Card, Input, Button, Table, Select, DatePicker, Row, Col,
  Divider, Tag, Avatar, Typography, Space, Statistic, App as AntApp,
  Tabs, Checkbox, Alert, Badge,
} from 'antd'
import {
  SearchOutlined, UserOutlined, DollarOutlined, WifiOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'
import { useBrandingStore } from '../../store/brandingStore'

const { Text, Title } = Typography

function loadRazorpayScript() {
  return new Promise((resolve) => {
    if (window.Razorpay) { resolve(true); return }
    const script   = document.createElement('script')
    script.src     = 'https://checkout.razorpay.com/v1/checkout.js'
    script.onload  = () => resolve(true)
    script.onerror = () => resolve(false)
    document.body.appendChild(script)
  })
}

/**
 * Shared fee collection component.
 * @param {string}  title            - Page title shown in card header
 * @param {string}  feeTypeCategory  - 'Admission'|'School'|'Transport'|'Hostel'|'Other'
 * @param {string|null} joinTypeFilter - 'New' for Admission screen; null for others
 */
export default function FeeCollectionBase({ title, feeTypeCategory, joinTypeFilter }) {
  const { message, modal } = AntApp.useApp()
  const { branding }       = useBrandingStore()

  const [searchVal,   setSearchVal]   = useState('')
  const [searching,   setSearching]   = useState(false)
  const [students,    setStudents]    = useState([])

  const [summary,     setSummary]     = useState(null)   // CrossYearFeeSummaryDto
  const [loadingDues, setLoadingDues] = useState(false)
  const [selectedYear, setSelectedYear] = useState(null) // AcademicYearId

  const [paymentModes,  setPaymentModes]  = useState([])
  const [paymentModeId, setPaymentModeId] = useState(null)
  const [paymentDate,   setPaymentDate]   = useState(dayjs())
  const [chequeNo,      setChequeNo]      = useState('')
  const [chequeDate,    setChequeDate]    = useState(null)
  const [bankName,      setBankName]      = useState('')
  const [remarks,       setRemarks]       = useState('')
  const [selectedKeys,     setSelectedKeys]     = useState([])
  const [selectedTermKeys, setSelectedTermKeys] = useState([])
  const [collecting,       setCollecting]       = useState(false)

  const selectedMode = paymentModes.find((m) => m.paymentModeId === paymentModeId)
  const isOnlineMode = selectedMode?.isOnline === true

  // Current year data being collected
  const yearData = summary?.years?.find((y) => y.academicYearId === selectedYear) || null

  // Only items with outstanding > 0 are selectable
  const collectibleItems = (yearData?.lineItems || []).filter((li) => li.outstanding > 0)

  // Key uniquely identifies a fee line item (works for both Term and Monthly)
  const lineKey = (li) => li.feePeriodId
    ? `${li.feeTypeId}_P_${li.feePeriodId}`
    : `${li.feeTypeId}_T_${li.termId ?? 0}`

  // Term-level key — identifies the term/period column (not the fee type)
  const termKey = (li) => li.feePeriodId
    ? `P_${li.feePeriodId}`
    : `T_${li.termId ?? 0}`

  // Distinct terms/periods from ALL line items for the selected year (incl. paid ones)
  const uniqueTerms = (() => {
    const seen = new Set()
    const result = []
    for (const li of (yearData?.lineItems || [])) {
      const k = termKey(li)
      if (!seen.has(k)) {
        seen.add(k)
        result.push({ key: k, label: li.periodLabel || li.termName || '(No Term)', orderNo: li.orderNo ?? li.periodSequenceNo ?? 999 })
      }
    }
    return result.sort((a, b) => a.orderNo - b.orderNo)
  })()

  // Items visible in the table — filtered by which terms are selected
  const visibleItems = (yearData?.lineItems || []).filter((li) => selectedTermKeys.includes(termKey(li)))

  const totalToPay = collectibleItems
    .filter((li) => selectedKeys.includes(lineKey(li)))
    .reduce((acc, li) => acc + li.outstanding, 0)

  // Initialise term + item selection from a line-items array
  const initSelection = (lineItems) => {
    const termKeys = [...new Set(
      lineItems
        .filter((li) => li.outstanding > 0)
        .map((li) => li.feePeriodId ? `P_${li.feePeriodId}` : `T_${li.termId ?? 0}`)
    )]
    setSelectedTermKeys(termKeys)
    const itemKeys = lineItems
      .filter((li) => li.outstanding > 0)
      .map((li) => li.feePeriodId
        ? `${li.feeTypeId}_P_${li.feePeriodId}`
        : `${li.feeTypeId}_T_${li.termId ?? 0}`)
    setSelectedKeys(itemKeys)
  }

  const handleTermToggle = (tKey, checked) => {
    if (checked) {
      setSelectedTermKeys((prev) => [...prev, tKey])
      // Auto-select outstanding items for this term
      const newItemKeys = (yearData?.lineItems || [])
        .filter((li) => (li.feePeriodId ? `P_${li.feePeriodId}` : `T_${li.termId ?? 0}`) === tKey && li.outstanding > 0)
        .map((li) => li.feePeriodId ? `${li.feeTypeId}_P_${li.feePeriodId}` : `${li.feeTypeId}_T_${li.termId ?? 0}`)
      setSelectedKeys((prev) => [...new Set([...prev, ...newItemKeys])])
    } else {
      setSelectedTermKeys((prev) => prev.filter((k) => k !== tKey))
      // Deselect items for this term
      const removeItemKeys = new Set(
        (yearData?.lineItems || [])
          .filter((li) => (li.feePeriodId ? `P_${li.feePeriodId}` : `T_${li.termId ?? 0}`) === tKey)
          .map((li) => li.feePeriodId ? `${li.feeTypeId}_P_${li.feePeriodId}` : `${li.feeTypeId}_T_${li.termId ?? 0}`)
      )
      setSelectedKeys((prev) => prev.filter((k) => !removeItemKeys.has(k)))
    }
  }

  const searchStudents = async () => {
    if (!searchVal.trim()) return
    setSearching(true)
    try {
      const params = new URLSearchParams({ search: searchVal, latestOnly: 'true' })
      if (joinTypeFilter) params.set('joinType', joinTypeFilter)
      const res = await api.get(`/school/students?${params}`)
      setStudents(res.data?.data || [])
    } finally {
      setSearching(false)
    }
  }

  const selectStudent = async (student) => {
    setStudents([])
    setSearchVal(student.studentName)
    setSummary(null)
    setSelectedKeys([])
    setSelectedTermKeys([])
    setSelectedYear(null)
    if (!student.studentUniqueId) {
      message.error('This student has no unique ID assigned. Run the student_unique_id_migration.sql first.')
      return
    }
    setLoadingDues(true)
    try {
      if (paymentModes.length === 0) {
        const pmRes = await api.get('/school/master/payment-modes')
        setPaymentModes(pmRes.data?.data || [])
      }
      const res = await api.get(
        `/school/fees/student-unique/${student.studentUniqueId}?feeTypeCategory=${feeTypeCategory}`
      )
      const data = res.data?.data
      setSummary(data)
      const cur = data?.years?.find((y) => y.isCurrent)
      if (cur) {
        setSelectedYear(cur.academicYearId)
        initSelection(cur.lineItems || [])
      }
    } catch (e) {
      message.error(apiError(e, 'Failed to load student fee details.'))
    } finally {
      setLoadingDues(false)
    }
  }

  const handleYearChange = (yearId) => {
    setSelectedYear(yearId)
    const yr = summary?.years?.find((y) => y.academicYearId === yearId)
    initSelection(yr?.lineItems || [])
  }

  const buildItems = () =>
    collectibleItems
      .filter((li) => selectedKeys.includes(lineKey(li)))
      .map((li) => ({
        FeeTypeId:        li.feeTypeId   || null,
        TermId:           li.termId      || null,
        FeePeriodId:      li.feePeriodId || null,
        BusRouteId:       li.busRouteId  || null,
        HostelId:         li.hostelId    || null,
        // li.outstanding already nets out the concession (structure − paid − concession).
        // Send Amount as the gross (outstanding + concession) and ConcessionAmount as the
        // concession, so the server stores net_amount = Amount − Concession = outstanding
        // (correct paid total) while the receipt still shows the concession line.
        Amount:           (li.outstanding || 0) + (li.concessionAmount || 0),
        ConcessionAmount: li.concessionAmount || 0,
      }))

  const doCollect = async (isOnline) => {
    const items = buildItems()
    if (items.length === 0) { message.warning('No items selected.'); return }
    if (!paymentModeId)     { message.warning('Select a payment mode.'); return }

    const payload = {
      StudentId:       yearData.studentId,
      StudentUniqueId: summary.studentUniqueId,
      FeeTypeCategory: feeTypeCategory,
      AcademicYearId:  yearData.academicYearId,
      PaymentModeId:   paymentModeId,
      PaymentDate:     paymentDate?.toISOString() || null,
      ChequeNo:        chequeNo || null,
      ChequeDate:      chequeDate?.toISOString() || null,
      BankName:        bankName || null,
      Remarks:         remarks || null,
      Items:           items,
    }

    setCollecting(true)
    try {
      if (!isOnline) {
        const res = await api.post('/school/fees/collect', payload)
        const receipt = res.data?.data
        modal.success({
          title:   `Receipt ${receipt.receiptNo} generated`,
          content: `Total collected: ₹${receipt.totalAmount.toFixed(2)}`,
        })
        await reloadSummary()
        resetPaymentForm()
        return
      }

      // Online (Razorpay)
      const orderRes = await api.post('/school/fees/payment-orders', payload)
      const orderData = orderRes.data?.data
      const scriptLoaded = await loadRazorpayScript()
      if (!scriptLoaded) { message.error('Could not load Razorpay checkout.'); return }

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
            ondismiss: () => { message.warning('Payment cancelled.'); reject(new Error('dismissed')) },
          },
          handler: async (response) => {
            try {
              const verifyRes = await api.post(
                `/school/fees/payment-orders/${orderData.gatewayOrderId}/verify`,
                { PaymentId: response.razorpay_payment_id, OrderId: response.razorpay_order_id, Signature: response.razorpay_signature }
              )
              const receipt = verifyRes.data?.data
              modal.success({
                title:   `Receipt ${receipt.receiptNo} generated`,
                content: `Online payment of ₹${receipt.totalAmount.toFixed(2)} confirmed.`,
              })
              await reloadSummary()
              resetPaymentForm()
              resolve()
            } catch (e) {
              message.error(apiError(e, 'Payment received but receipt creation failed. Contact support.'))
              reject(new Error('verify_failed'))
            }
          },
        })
        rzp.open()
      })
    } catch (err) {
      if (err?.message !== 'dismissed' && err?.message !== 'verify_failed')
        message.error(apiError(err, 'Failed to process payment.'))
    } finally {
      setCollecting(false)
    }
  }

  const handleCollect = () => {
    if (!yearData) return
    doCollect(isOnlineMode)
  }

  const reloadSummary = async () => {
    const res = await api.get(
      `/school/fees/student-unique/${summary.studentUniqueId}?feeTypeCategory=${feeTypeCategory}`
    )
    const data = res.data?.data
    setSummary(data)
    const yr = data?.years?.find((y) => y.academicYearId === selectedYear)
    initSelection(yr?.lineItems || [])
  }

  const resetPaymentForm = () => {
    setPaymentModeId(null)
    setPaymentDate(dayjs())
    setChequeNo(''); setChequeDate(null); setBankName(''); setRemarks('')
  }

  // ── Columns ──────────────────────────────────────────────────────────────

  const outstandingCols = [
    { title: 'Year',        dataIndex: 'academicYear',      key: 'year',    width: 100 },
    { title: 'Class',       dataIndex: 'className',         key: 'class',   width: 120 },
    { title: 'Fee Category', dataIndex: 'feeCategoryName',  key: 'cat',     width: 130 },
    {
      title: 'Total (₹)',   dataIndex: 'totalStructure',   key: 'total',   width: 110,
      render: (v) => v.toFixed(2),
    },
    {
      title: 'Paid (₹)',    dataIndex: 'totalPaid',         key: 'paid',    width: 100,
      render: (v) => v.toFixed(2),
    },
    {
      title: 'Outstanding (₹)', dataIndex: 'totalOutstanding', key: 'out', width: 130,
      render: (v) => (
        <Text strong style={{ color: v > 0 ? '#cf1322' : '#52c41a' }}>{v.toFixed(2)}</Text>
      ),
    },
    {
      title: '', key: 'action', width: 80,
      render: (_, row) => (
        row.totalOutstanding > 0
          ? <Button size="small" type="link" onClick={() => handleYearChange(row.academicYearId)}>
              Collect
            </Button>
          : <Tag color="success">Cleared</Tag>
      ),
    },
  ]

  const itemCols = [
    {
      title: '',
      key: 'check',
      width: 36,
      render: (_, li) => {
        const k = lineKey(li)
        return (
          <Checkbox
            checked={selectedKeys.includes(k)}
            disabled={li.outstanding <= 0}
            onChange={(e) => {
              setSelectedKeys((prev) =>
                e.target.checked ? [...prev, k] : prev.filter((x) => x !== k)
              )
            }}
          />
        )
      },
    },
    { title: 'Fee Type', dataIndex: 'feeTypeName', key: 'ft', width: 160 },
    {
      title: 'Term / Period', key: 'term', width: 130,
      render: (_, li) => li.periodLabel || li.termName || <Text type="secondary">—</Text>,
    },
    { title: 'Fee (₹)', dataIndex: 'structureAmount', key: 'sa', width: 100, render: (v) => v.toFixed(2) },
    { title: 'Paid (₹)', dataIndex: 'paidAmount',    key: 'pa', width: 100, render: (v) => v.toFixed(2) },
    {
      title: 'Outstanding (₹)', dataIndex: 'outstanding', key: 'out', width: 130,
      render: (v) => <Text strong style={{ color: v > 0 ? '#cf1322' : '#52c41a' }}>{v.toFixed(2)}</Text>,
    },
    {
      title: 'Pay Now (₹)', dataIndex: 'outstanding', key: 'pay', width: 110,
      render: (v, li) =>
        selectedKeys.includes(lineKey(li)) && v > 0
          ? <Text strong style={{ color: '#1677ff' }}>₹{v.toFixed(2)}</Text>
          : <Text type="secondary">—</Text>,
    },
    {
      title: 'Concession (₹)', dataIndex: 'concessionAmount', key: 'con', width: 120,
      render: (v) => v > 0
        ? <Text style={{ color: '#52c41a' }}>{(v || 0).toFixed(2)}</Text>
        : <Text type="secondary">0.00</Text>,
    },
  ]

  return (
    <div>
      {/* Student Search */}
      <Card title={title} style={{ marginBottom: 16 }}>
        <Row gutter={12}>
          <Col flex="auto">
            <Input
              placeholder="Search by student name or admission no"
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

        {students.length > 0 && (
          <div style={{ border: '1px solid #d9d9d9', borderRadius: 6, marginTop: 8, maxHeight: 240, overflow: 'auto' }}>
            {students.map((s) => (
              <div
                key={s.studentId}
                style={{ padding: '8px 12px', cursor: 'pointer', borderBottom: '1px solid #f0f0f0' }}
                onClick={() => selectStudent(s)}
                onMouseEnter={(e) => (e.currentTarget.style.background = '#f5f5f5')}
                onMouseLeave={(e) => (e.currentTarget.style.background = 'white')}
              >
                <Space>
                  <Avatar icon={<UserOutlined />} size={28} />
                  <span>
                    <strong>{s.studentName}</strong> — {s.admissionNo} — {s.className}
                    {s.joinType && <Tag style={{ marginLeft: 6 }} color="blue">{s.joinType}</Tag>}
                  </span>
                  <Tag color={s.status === 'Active' ? 'green' : 'default'}>{s.status}</Tag>
                </Space>
              </div>
            ))}
          </div>
        )}
      </Card>

      {/* Student info + dues */}
      {summary && (
        <>
          {/* Student header */}
          <Card style={{ marginBottom: 16 }}>
            <Row gutter={24} align="middle">
              <Col>
                <Avatar icon={<UserOutlined />} size={48} />
              </Col>
              <Col>
                <Title level={5} style={{ margin: 0 }}>{summary.studentName}</Title>
                <Text type="secondary">{summary.admissionNo}</Text>
              </Col>
              {summary.joinType && (
                <Col>
                  <Tag color="blue">{summary.joinType}</Tag>
                </Col>
              )}
            </Row>
          </Card>

          {/* All-years outstanding summary */}
          <Card
            title="All Years Outstanding"
            size="small"
            style={{ marginBottom: 16 }}
            loading={loadingDues}
          >
            <Table
              rowKey="academicYearId"
              dataSource={summary.years || []}
              columns={outstandingCols}
              pagination={false}
              size="small"
              scroll={{ x: 'max-content' }}
              rowClassName={(r) => r.academicYearId === selectedYear ? 'ant-table-row-selected' : ''}
            />
          </Card>

          {/* Collect for selected year */}
          {yearData && (
            <Card
              title={
                <Space>
                  <span>Collect Fee —</span>
                  <Select
                    size="small"
                    style={{ width: 130 }}
                    value={selectedYear}
                    onChange={handleYearChange}
                    options={(summary.years || []).map((y) => ({
                      value: y.academicYearId,
                      label: y.academicYear,
                    }))}
                  />
                  <Text type="secondary" style={{ fontWeight: 400 }}>
                    {yearData.className} · {yearData.feeCategoryName}
                  </Text>
                </Space>
              }
              style={{ marginBottom: 16 }}
            >
              {collectibleItems.length === 0 ? (
                <Alert type="success" showIcon message="All fees cleared for this year." />
              ) : (
                <>
                  {/* Term filter — only shown when there are multiple terms/periods */}
                  {uniqueTerms.length > 1 && (
                    <div style={{ marginBottom: 12, padding: '8px 14px', background: '#fafafa', border: '1px solid #f0f0f0', borderRadius: 6, display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'center' }}>
                      <Text strong style={{ marginRight: 4 }}>Terms:</Text>
                      {uniqueTerms.map((t) => (
                        <Checkbox
                          key={t.key}
                          checked={selectedTermKeys.includes(t.key)}
                          onChange={(e) => handleTermToggle(t.key, e.target.checked)}
                        >
                          {t.label}
                        </Checkbox>
                      ))}
                    </div>
                  )}

                  <Table
                    rowKey={lineKey}
                    dataSource={visibleItems}
                    columns={itemCols}
                    pagination={false}
                    size="small"
                    scroll={{ x: 'max-content' }}
                    style={{ marginBottom: 16 }}
                  />

                  {/* Payment details */}
                  <Divider orientation="left">Payment Details</Divider>
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
                    {!isOnlineMode && (
                      <>
                        <Col xs={24} md={6}>
                          <div style={{ marginBottom: 8 }}><label>Reference No</label></div>
                          <Input value={chequeNo} onChange={(e) => setChequeNo(e.target.value)} />
                        </Col>
                        <Col xs={24} md={6}>
                          <div style={{ marginBottom: 8 }}><label>Reference Date</label></div>
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

                  {isOnlineMode && (
                    <Alert
                      type="info" showIcon style={{ marginTop: 16 }}
                      message="Online payment"
                      description={`After clicking Collect, a ${selectedMode?.modeName} checkout will open.`}
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
                        disabled={totalToPay <= 0 || selectedKeys.length === 0}
                      >
                        {isOnlineMode ? 'Collect Online & Issue Receipt' : 'Collect & Issue Receipt'}
                      </Button>
                    </Col>
                  </Row>
                </>
              )}
            </Card>
          )}
        </>
      )}
    </div>
  )
}
