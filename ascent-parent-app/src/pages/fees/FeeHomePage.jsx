import { useState, useEffect, useCallback } from 'react'
import {
  Card, Tabs, Collapse, Table, Checkbox, Button, Tag, Typography,
  Statistic, Row, Col, Empty, Spin, Alert, Avatar, App as AntApp,
} from 'antd'
import { CreditCardOutlined, ReloadOutlined, UserOutlined, PrinterOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import api from '../../api/axiosInstance'
import { useAuthStore } from '../../store/authStore'

const { Text } = Typography
const fmt = (n) => `₹${Number(n || 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`

const CATEGORIES = [
  { key: 'School',    label: 'School Fee'    },
  { key: 'Transport', label: 'Transport Fee' },
  { key: 'Hostel',    label: 'Hostel Fee'    },
]

function itemKey(li) {
  return [li.feeTypeId, li.busRouteId, li.hostelId, li.termId, li.feePeriodId]
    .map((v) => v ?? 'n').join('_')
}

// Group key by term (or fee period for monthly structures).
function termKey(li) {
  return li.feePeriodId ? `P_${li.feePeriodId}` : `T_${li.termId ?? 0}`
}

// Collapse line items into one entry per term: summed amounts + the underlying
// per-head line items (kept so payment can split the term total back into heads).
function groupByTermFn(lineItems) {
  const map = new Map()
  for (const li of lineItems) {
    const key = termKey(li)
    if (!map.has(key)) {
      map.set(key, {
        key,
        label:           li.termName || li.periodLabel || '—',
        structureAmount: 0,
        concessionAmount: 0,
        outstanding:     0,
        receiptId:       null,
        createdBy:       null,
        items:           [],
      })
    }
    const g = map.get(key)
    g.structureAmount  += li.structureAmount  || 0
    g.concessionAmount += li.concessionAmount || 0
    g.outstanding      += li.outstanding      || 0
    // Carry the receipt that paid this term (from the first paid head) for reprint.
    if (li.receiptId && g.receiptId == null) { g.receiptId = li.receiptId; g.createdBy = li.createdBy }
    g.items.push(li)
  }
  return Array.from(map.values())
}

function YearSection({ year, paying, onPay, groupByTerm = false }) {
  const [yearSelected, setYearSelected] = useState(new Set())
  const [printingId, setPrintingId]     = useState(null)
  const navigate    = useNavigate()
  const { message } = AntApp.useApp()

  // Reprint a paid row's receipt: fetch detail → reuse the success page (it renders + prints).
  const handlePrint = async (receiptId) => {
    setPrintingId(receiptId)
    try {
      const r = await api.get(`/mobile/fees/receipts/${receiptId}`)
      const receipt = r.data?.data
      if (receipt) navigate('/fees/success', { state: { receipt, reprint: true } })
      else message.error('Receipt not found.')
    } catch (e) {
      message.error(e.message || 'Failed to load receipt')
    } finally {
      setPrintingId(null)
    }
  }

  // Rows are either term groups (School) or raw line items (Transport/Hostel).
  const rows    = groupByTerm ? groupByTermFn(year.lineItems) : year.lineItems
  const rowKey  = groupByTerm ? (r) => r.key : itemKey

  const outstandingRows = rows.filter((r) => r.outstanding > 0)
  const selectedRows    = outstandingRows.filter((r) => yearSelected.has(rowKey(r)))
  const selectedTotal   = selectedRows.reduce((s, r) => s + r.outstanding, 0)
  const allChecked      = outstandingRows.length > 0 && selectedRows.length === outstandingRows.length
  const someChecked     = selectedRows.length > 0 && !allChecked

  const toggle = (r, checked) => {
    setYearSelected((prev) => {
      const s = new Set(prev)
      if (checked) s.add(rowKey(r)); else s.delete(rowKey(r))
      return s
    })
  }

  const toggleAll = () => {
    if (allChecked) setYearSelected(new Set())
    else setYearSelected(new Set(outstandingRows.map(rowKey)))
  }

  // Expand selected rows to the underlying per-head line items with outstanding > 0.
  // For grouped (School) rows this splits the term total back into its fee heads;
  // for ungrouped rows each row is already a single line item.
  const expandToLineItems = () =>
    selectedRows
      .flatMap((r) => (groupByTerm ? r.items : [r]))
      .filter((li) => li.outstanding > 0)

  const handlePayClick = () =>
    onPay(year, expandToLineItems(), selectedTotal, () => setYearSelected(new Set()))

  const checkCol = {
    title: () => outstandingRows.length > 0
      ? <Checkbox indeterminate={someChecked} checked={allChecked} onChange={toggleAll} />
      : null,
    key: '_check', width: 48,
    render: (_, r) => r.outstanding > 0
      ? <Checkbox checked={yearSelected.has(rowKey(r))} onChange={(e) => toggle(r, e.target.checked)} />
      : <Tag color="success" style={{ fontSize: 10 }}>Paid</Tag>,
  }

  const outstandingCol = {
    title: 'Outstanding', key: 'due', align: 'right',
    render: (_, r) => (
      <Text strong style={{ color: r.outstanding > 0 ? '#cf1322' : '#52c41a' }}>
        {fmt(r.outstanding)}
      </Text>
    ),
  }

  // Print Receipt — only on paid rows whose payment was made via the parent portal.
  const printCol = {
    title: 'Receipt', key: '_print', width: 90, align: 'center',
    render: (_, r) => (r.outstanding <= 0 && r.receiptId && r.createdBy === 'Parent Portal')
      ? <Button size="small" type="link" icon={<PrinterOutlined />}
          loading={printingId === r.receiptId} onClick={() => handlePrint(r.receiptId)}>
          Print
        </Button>
      : null,
  }

  // Grouped (School): Term | Total | Outstanding. Term total only — no head breakdown.
  const groupedCols = [
    checkCol,
    { title: 'Term / Period', key: 'term', render: (_, r) => r.label },
    { title: 'Total', key: 'amt', align: 'right', render: (_, r) => fmt(r.structureAmount) },
    outstandingCol,
    printCol,
  ]

  // Ungrouped (Transport/Hostel): full head-wise detail (unchanged).
  const detailCols = [
    checkCol,
    { title: 'Fee / Route / Hostel', key: 'name', render: (_, li) => li.feeTypeName || '—' },
    { title: 'Term / Period', key: 'term', render: (_, li) => li.termName || li.periodLabel || '—' },
    { title: 'Total', key: 'amt', align: 'right', render: (_, li) => fmt(li.structureAmount) },
    {
      title: 'Concession', key: 'conc', align: 'right',
      render: (_, li) => li.concessionAmount > 0 ? <Tag color="green">-{fmt(li.concessionAmount)}</Tag> : '—',
    },
    outstandingCol,
    printCol,
  ]

  return (
    <div>
      <Row gutter={16} style={{ marginBottom: 16 }}>
        <Col span={6}><Statistic title="Total Fee"   value={year.totalStructure}   prefix="₹" precision={2} /></Col>
        <Col span={6}><Statistic title="Paid"        value={year.totalPaid}        prefix="₹" precision={2} valueStyle={{ color: '#52c41a' }} /></Col>
        <Col span={6}><Statistic title="Outstanding" value={year.totalOutstanding} prefix="₹" precision={2} valueStyle={{ color: year.totalOutstanding > 0 ? '#cf1322' : '#52c41a' }} /></Col>
        <Col span={6} style={{ display: 'flex', alignItems: 'flex-end' }}>
          {selectedRows.length > 0 && (
            <Button
              type="primary" icon={<CreditCardOutlined />}
              loading={paying} block
              onClick={handlePayClick}
            >
              Pay {fmt(selectedTotal)}
            </Button>
          )}
        </Col>
      </Row>
      <Table
        dataSource={rows} columns={groupByTerm ? groupedCols : detailCols} rowKey={rowKey}
        size="small" pagination={false}
        rowClassName={(r) => r.outstanding <= 0 ? 'paid-row' : ''}
      />
    </div>
  )
}

// Each tab manages its own data independently
function FeeTab({ category, paying, onPay: onPayOuter }) {
  // Bind category into every onPay call so handlePay doesn't need DOM inspection
  const onPay = useCallback(
    (year, items, total, clear) => onPayOuter(year, items, total, clear, category),
    [onPayOuter, category]
  )
  const { message }              = AntApp.useApp()
  const [data,    setData]       = useState(null)
  const [loading, setLoading]    = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const r = await api.get(`/mobile/fees/outstanding?feeTypeCategory=${category}`)
      setData(r.data?.data || null)
    } catch (e) {
      message.error(e.message || `Failed to load ${category} fee data`)
    } finally {
      setLoading(false)
    }
  }, [category])

  useEffect(() => { load() }, [load])

  if (loading) return <div style={{ textAlign: 'center', padding: 48 }}><Spin size="large" /></div>
  if (!data)   return <Empty description="No fee data found" />

  const hasOutstanding = data.years?.some((y) => y.totalOutstanding > 0)
  const defaultOpen    = data.years?.filter((y) => y.totalOutstanding > 0).map((y) => String(y.academicYearId)) || []

  const collapseItems = (data.years || []).map((year) => ({
    key:   String(year.academicYearId),
    label: (
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <Text strong>{year.academicYear}</Text>
        {year.isCurrent && <Tag color="blue">Current</Tag>}
        {year.totalOutstanding > 0
          ? <Tag color="red">Due: {fmt(year.totalOutstanding)}</Tag>
          : <Tag color="green">Fully Paid</Tag>
        }
      </div>
    ),
    children: <YearSection year={year} paying={paying} onPay={onPay} groupByTerm={category === 'School'} />,
  }))

  return (
    <div style={{ paddingTop: 8 }}>
      {hasOutstanding && (
        <Alert type="warning" showIcon
          message="Pending dues"
          description="Select fee items and click Pay to complete payment online."
          style={{ marginBottom: 16 }}
        />
      )}
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
        <Button size="small" icon={<ReloadOutlined />} onClick={load}>Refresh</Button>
      </div>
      {collapseItems.length === 0
        ? <Empty description="No fee structure found for this category" />
        : <Collapse items={collapseItems} defaultActiveKey={defaultOpen} bordered={false} style={{ background: 'transparent' }} />
      }
    </div>
  )
}

export default function FeeHomePage() {
  const navigate    = useNavigate()
  const { message } = AntApp.useApp()
  const { child }   = useAuthStore()
  const [paying, setPaying] = useState(false)

  const handlePay = async (year, selectedItems, total, clearSelection, category) => {
    setPaying(true)
    try {
      const items = selectedItems.map((li) => ({
        FeeTypeId:        li.feeTypeId   || null,
        TermId:           li.termId      || null,
        FeePeriodId:      li.feePeriodId || null,
        BusRouteId:       li.busRouteId  || null,
        HostelId:         li.hostelId    || null,
        // outstanding already nets out the concession; send gross + concession so the
        // server records net_amount = outstanding (correct paid total) while the receipt
        // still shows the concession line.
        Amount:           (li.outstanding || 0) + (li.concessionAmount || 0),
        ConcessionAmount: li.concessionAmount || 0,
      }))

      const orderRes = await api.post('/mobile/fees/payment-orders', {
        AcademicYearId:  year.academicYearId,
        FeeTypeCategory: category,
        Items:           items,
      })
      const order = orderRes.data?.data

      await loadRazorpayScript()

      await new Promise((resolve, reject) => {
        const rzp = new window.Razorpay({
          key:      order.keyId,
          amount:   order.amountInPaise,
          currency: order.currency || 'INR',
          order_id: order.externalOrderId,
          name:     document.title,
          description: `Fee — ${year.academicYear}`,
          handler: async (response) => {
            try {
              const vRes = await api.post(
                `/mobile/fees/payment-orders/${order.gatewayOrderId}/verify`,
                {
                  PaymentId: response.razorpay_payment_id,
                  OrderId:   response.razorpay_order_id,
                  Signature: response.razorpay_signature,
                }
              )
              clearSelection()
              navigate('/fees/success', { state: { receipt: vRes.data?.data } })
              resolve()
            } catch (e) { reject(e) }
          },
          modal: { ondismiss: () => reject(new Error('cancelled')) },
          theme: { color: '#1677ff' },
        })
        rzp.open()
      })
    } catch (e) {
      if (e.message !== 'cancelled') message.error(e.message || 'Payment failed. Please try again.')
    } finally {
      setPaying(false)
    }
  }

  return (
    <div>
      {child && (
        <Card bordered={false} style={{ marginBottom: 16 }} bodyStyle={{ padding: 16 }}>
          <Row align="middle" gutter={12}>
            <Col flex="none">
              <Avatar size={48} icon={<UserOutlined />} style={{ background: '#1677ff' }} />
            </Col>
            <Col flex="auto">
              <Text strong style={{ fontSize: 16, display: 'block' }}>{child.studentName || '—'}</Text>
              <span>
                {child.className && <Tag color="blue">{child.className}</Tag>}
                {child.sectionName && <Tag color="geekblue">Section {child.sectionName}</Tag>}
                {child.admissionNo && <Tag>Adm No: {child.admissionNo}</Tag>}
              </span>
            </Col>
          </Row>
        </Card>
      )}
      <Card bordered={false}>
        <Tabs
          defaultActiveKey="School"
          items={CATEGORIES.map(({ key, label }) => ({
            key,
            label,
            children: <FeeTab category={key} paying={paying} onPay={handlePay} />,
          }))}
        />
      </Card>
      <style>{`.paid-row { opacity: 0.55; }`}</style>
    </div>
  )
}

function loadRazorpayScript() {
  return new Promise((resolve, reject) => {
    if (window.Razorpay) { resolve(); return }
    const s = document.createElement('script')
    s.src = 'https://checkout.razorpay.com/v1/checkout.js'
    s.onload  = resolve
    s.onerror = () => reject(new Error('Failed to load payment gateway. Check your internet connection.'))
    document.body.appendChild(s)
  })
}
