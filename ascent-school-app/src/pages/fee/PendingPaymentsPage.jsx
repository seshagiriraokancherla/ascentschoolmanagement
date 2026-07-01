import { useState } from 'react'
import {
  Card, Table, Button, Tag, Space, Typography, Tooltip, DatePicker, Alert, Empty, App as AntApp,
} from 'antd'
import { ReloadOutlined, SyncOutlined, CheckCircleOutlined, ThunderboltOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Text } = Typography

// Maps a gateway payment status → an AntD tag colour.
const STATUS_COLOR = {
  captured:   'green',
  authorized: 'orange',
  failed:     'red',
  created:    'default',
}

export default function PendingPaymentsPage() {
  const { message, modal } = AntApp.useApp()

  const [orders,       setOrders]       = useState([])
  const [loading,      setLoading]      = useState(false)
  const [scanning,     setScanning]     = useState(false)
  const [scanned,      setScanned]      = useState(false)   // showing scan results (captured only)
  const [started,      setStarted]      = useState(false)   // user has clicked Scan or Show-all yet
  const [createdAfter, setCreatedAfter] = useState(null)
  // Per-row live status: { [gatewayOrderId]: { loading, data } }
  const [statusMap,    setStatusMap]    = useState({})
  const [reconciling,  setReconciling]  = useState({})      // { [id]: true }
  const [reconcilingAll, setReconcilingAll] = useState(false)

  const qStr = () => {
    const q = new URLSearchParams()
    if (createdAfter) q.set('createdAfter', createdAfter.format('YYYY-MM-DDTHH:mm:ss'))
    return q
  }

  // Full pending list (every Pending order, no gateway calls).
  const load = async () => {
    setLoading(true)
    setScanned(false)
    setStarted(true)
    setStatusMap({})
    try {
      const res = await api.get(`/school/fees/payment-orders/pending?${qStr()}`)
      setOrders(res.data?.data || [])
    } catch {
      message.error('Failed to load pending payments.')
    } finally {
      setLoading(false)
    }
  }

  // Bulk scan — checks every pending order at the gateway, returns only captured/reconcilable ones.
  const scan = async () => {
    setScanning(true)
    setStarted(true)
    try {
      const res = await api.get(`/school/fees/payment-orders/scan?onlyReconcilable=true&${qStr()}`)
      const rows = res.data?.data || []
      setOrders(rows)
      // Seed the status map so the tags + Reconcile buttons work without re-checking each row.
      const seed = {}
      rows.forEach(r => {
        seed[r.gatewayOrderId] = {
          loading: false,
          data: {
            found:         r.found,
            gatewayStatus: r.gatewayStatus,
            paymentId:     r.paymentId,
            amount:        r.gatewayAmount,
            reconcilable:  r.reconcilable,
            note:          r.note,
          },
        }
      })
      setStatusMap(seed)
      setScanned(true)
      message.success(rows.length
        ? `Found ${rows.length} captured payment(s) ready to reconcile.`
        : 'No captured-but-unreconciled payments found.')
    } catch (e) {
      message.error(e?.response?.data?.message || 'Scan failed.')
    } finally {
      setScanning(false)
    }
  }

  const checkStatus = async (id) => {
    setStatusMap(m => ({ ...m, [id]: { loading: true } }))
    try {
      const res = await api.get(`/school/fees/payment-orders/${id}/status`)
      setStatusMap(m => ({ ...m, [id]: { loading: false, data: res.data?.data } }))
    } catch (e) {
      setStatusMap(m => ({ ...m, [id]: { loading: false } }))
      message.error(e?.response?.data?.message || 'Could not fetch status from the gateway.')
    }
  }

  const reconcileOne = async (id) => {
    const res = await api.post(`/school/fees/payment-orders/${id}/reconcile`)
    setOrders(list => list.filter(o => o.gatewayOrderId !== id))
    return res.data?.data
  }

  const doReconcile = async (record) => {
    const id = record.gatewayOrderId
    setReconciling(r => ({ ...r, [id]: true }))
    try {
      const receipt = await reconcileOne(id)
      message.success(
        receipt?.receiptNo
          ? `Reconciled. Receipt ${receipt.receiptNo} generated.`
          : 'Payment reconciled. Receipt generated.'
      )
    } catch (e) {
      message.error(e?.response?.data?.message || 'Reconcile failed.')
    } finally {
      setReconciling(r => ({ ...r, [id]: false }))
    }
  }

  const confirmReconcile = (record) => {
    const s = statusMap[record.gatewayOrderId]?.data
    modal.confirm({
      title: 'Reconcile this payment?',
      content: (
        <div>
          <p>A fee receipt will be created for <b>{record.studentName || record.admissionNo}</b>.</p>
          <p style={{ marginBottom: 0 }}>
            Gateway payment: <b>{s?.paymentId}</b> · Amount: <b>₹{Number(s?.amount ?? record.amount).toFixed(2)}</b>
          </p>
        </div>
      ),
      okText: 'Reconcile',
      onOk: () => doReconcile(record),
    })
  }

  const reconcilableRows = orders.filter(o => statusMap[o.gatewayOrderId]?.data?.reconcilable)

  const reconcileAll = () => {
    const targets = reconcilableRows
    if (!targets.length) return
    modal.confirm({
      title: `Reconcile ${targets.length} captured payment(s)?`,
      content: 'A fee receipt will be created for each. This cannot be undone.',
      okText: `Reconcile ${targets.length}`,
      onOk: async () => {
        setReconcilingAll(true)
        let ok = 0, fail = 0
        // Sequential — clearer per-row progress and avoids hammering the gateway.
        for (const r of targets) {
          try { await reconcileOne(r.gatewayOrderId); ok++ }
          catch { fail++ }
        }
        setReconcilingAll(false)
        message[fail ? 'warning' : 'success'](
          `Reconciled ${ok}${fail ? `, ${fail} failed` : ''}.`)
      },
    })
  }

  const renderStatus = (record) => {
    const entry = statusMap[record.gatewayOrderId]
    if (!entry) return <Text type="secondary">—</Text>
    if (entry.loading) return <Tag icon={<SyncOutlined spin />} color="processing">Checking…</Tag>
    const d = entry.data
    if (!d) return <Text type="danger">Error</Text>
    if (!d.found) return <Tooltip title={d.note}><Tag color="default">Not attempted</Tag></Tooltip>
    return (
      <Tooltip title={d.note}>
        <Tag color={STATUS_COLOR[d.gatewayStatus] || 'default'} style={{ textTransform: 'capitalize' }}>
          {d.gatewayStatus}
        </Tag>
      </Tooltip>
    )
  }

  const columns = [
    {
      title: 'Created (UTC)', dataIndex: 'createdAt', width: 140,
      render: v => <Text style={{ whiteSpace: 'nowrap' }}>{v}</Text>,
    },
    {
      title: 'Student', key: 'student',
      render: (_, r) => (
        <div>
          <div>{r.studentName || <Text type="secondary">(unknown)</Text>}</div>
          <Text type="secondary" style={{ fontSize: 12 }}>{r.admissionNo}</Text>
        </div>
      ),
    },
    { title: 'Category', dataIndex: 'feeTypeCategory', width: 110, render: v => v || '—' },
    {
      title: 'Amount', dataIndex: 'amount', width: 110, align: 'right',
      render: v => <b>₹{Number(v).toFixed(2)}</b>,
    },
    { title: 'Created By', dataIndex: 'createdBy', width: 130, render: v => v || '—' },
    { title: 'Gateway Status', key: 'status', width: 140, render: (_, r) => renderStatus(r) },
    {
      title: 'Action', key: 'action', width: 230,
      render: (_, r) => {
        const entry = statusMap[r.gatewayOrderId]
        const reconcilable = entry?.data?.reconcilable
        return (
          <Space>
            <Button
              size="small"
              icon={<SyncOutlined />}
              loading={entry?.loading}
              onClick={() => checkStatus(r.gatewayOrderId)}
            >
              Check status
            </Button>
            <Tooltip title={!reconcilable ? 'Check status first — only captured payments can be reconciled.' : ''}>
              <Button
                size="small"
                type="primary"
                icon={<CheckCircleOutlined />}
                disabled={!reconcilable || reconcilingAll}
                loading={reconciling[r.gatewayOrderId]}
                onClick={() => confirmReconcile(r)}
              >
                Reconcile
              </Button>
            </Tooltip>
          </Space>
        )
      },
    },
  ]

  return (
    <Card
      title="Pending Online Payments"
      extra={
        <Space>
          <DatePicker
            showTime
            placeholder="Created after"
            value={createdAfter}
            onChange={setCreatedAfter}
            format="YYYY-MM-DD HH:mm"
          />
          <Button
            type="primary"
            icon={<ThunderboltOutlined />}
            loading={scanning}
            onClick={scan}
          >
            Scan for captured payments
          </Button>
          <Button icon={<ReloadOutlined />} onClick={load} disabled={scanning}>
            Show all pending
          </Button>
        </Space>
      }
    >
      <Text type="secondary">
        Online orders that never completed (the parent left the app before the success screen).
        Most are <b>Not attempted</b> (never paid). Click <b>Scan for captured payments</b> to check
        them all against Razorpay at once and list only the ones that were actually paid — then
        reconcile to generate the receipts.
      </Text>

      {scanned && (
        <Alert
          style={{ marginTop: 12 }}
          type={reconcilableRows.length ? 'success' : 'info'}
          showIcon
          message={
            reconcilableRows.length
              ? `Showing ${reconcilableRows.length} captured payment(s) ready to reconcile.`
              : 'No captured-but-unreconciled payments found.'
          }
          action={
            reconcilableRows.length > 0 && (
              <Button
                size="small"
                type="primary"
                icon={<CheckCircleOutlined />}
                loading={reconcilingAll}
                onClick={reconcileAll}
              >
                Reconcile all ({reconcilableRows.length})
              </Button>
            )
          }
        />
      )}

      {!started && !loading && !scanning ? (
        <Empty
          style={{ marginTop: 48, marginBottom: 32 }}
          description={
            <span>
              Click <b>Scan for captured payments</b> to find paid-but-unreconciled orders,
              <br />or <b>Show all pending</b> to browse every pending order.
            </span>
          }
        />
      ) : (
        <Table
          style={{ marginTop: 16 }}
          rowKey="gatewayOrderId"
          columns={columns}
          dataSource={orders}
          loading={loading || scanning}
          size="small"
          scroll={{ x: 'max-content' }}
          pagination={{ pageSize: 20, showSizeChanger: false }}
        />
      )}
    </Card>
  )
}
