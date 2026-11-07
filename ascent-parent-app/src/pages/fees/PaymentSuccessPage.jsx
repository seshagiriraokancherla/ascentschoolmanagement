import { useLocation, useNavigate } from 'react-router-dom'
import { Result, Button, Descriptions, Table, Typography, Card, Divider } from 'antd'
import { CheckCircleOutlined, PrinterOutlined, ArrowLeftOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'

const { Text, Title } = Typography
const fmt = (n) => `₹${Number(n || 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`

export default function PaymentSuccessPage() {
  const navigate  = useNavigate()
  const { state } = useLocation()
  const { branding } = useBrandingStore()
  const receipt   = state?.receipt
  const reprint   = state?.reprint    // true when reprinting an earlier receipt from the fee list

  if (!receipt) {
    return (
      <Result
        status="info"
        title="No receipt to display"
        extra={<Button type="primary" onClick={() => navigate('/fees')}>Back to Fees</Button>}
      />
    )
  }

  // One row per term / month instead of one per fee head: "Term 1 Fee — 100", showing
  // only the amount paid (net of concession). Transport / hostel lines keep their own
  // row per term. A line with no term or month keeps its fee head name.
  const rows = []
  for (const it of receipt.items || []) {
    const kind  = it.routeName ? ' Transport' : it.hostelName ? ' Hostel' : ''
    const term  = (it.termName || '').trim()
    const label = term ? `${term}${kind} Fee` : (it.feeTypeName || it.routeName || it.hostelName || 'Fee')
    const row   = rows.find(r => r.label === label)
    if (row) row.amount += Number(it.netAmount || 0)
    else rows.push({ label, amount: Number(it.netAmount || 0) })
  }

  const itemCols = [
    { title: 'Description', dataIndex: 'label',  key: 'desc' },
    { title: 'Amount',      dataIndex: 'amount', key: 'amt', align: 'right', render: (v) => <Text strong>{fmt(v)}</Text> },
  ]

  return (
    <div style={{ maxWidth: 700, margin: '0 auto' }} className="print-area">
      {/* School header */}
      <div style={{ textAlign: 'center', marginBottom: 8 }}>
        {branding?.logoPath && (
          <img src={branding.logoPath} alt="logo" style={{ height: 56, marginBottom: 8 }} />
        )}
        <Title level={3} style={{ margin: 0 }}>{branding?.displayName || 'School'}</Title>
        <Text type="secondary">Fee Receipt</Text>
      </div>

      <Result
        icon={<CheckCircleOutlined style={{ color: '#52c41a' }} />}
        status="success"
        title={reprint ? 'Fee Receipt' : 'Payment Successful!'}
        subTitle={`Receipt No: ${receipt.receiptNo}`}
      />

      <Card bordered={false}>
        <Descriptions column={2} size="small" bordered>
          <Descriptions.Item label="Receipt No">{receipt.receiptNo}</Descriptions.Item>
          <Descriptions.Item label="Date">
            {new Date(receipt.paymentDate).toLocaleDateString('en-IN')}
          </Descriptions.Item>
          <Descriptions.Item label="Student">{receipt.studentName}</Descriptions.Item>
          <Descriptions.Item label="Class">{receipt.className}</Descriptions.Item>
          <Descriptions.Item label="Admission No">{receipt.admissionNo}</Descriptions.Item>
          <Descriptions.Item label="Academic Year">{receipt.academicYear}</Descriptions.Item>
          <Descriptions.Item label="Payment Mode">{receipt.paymentModeName}</Descriptions.Item>
          {receipt.remarks && (
            <Descriptions.Item label="Remarks" span={2}>{receipt.remarks}</Descriptions.Item>
          )}
        </Descriptions>

        <Divider />

        <Table
          dataSource={rows}
          columns={itemCols}
          rowKey="label"
          size="small"
          pagination={false}
          summary={() => (
            <Table.Summary.Row>
              <Table.Summary.Cell align="right">
                <Text strong>Total Paid</Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell align="right">
                <Text strong style={{ fontSize: 16, color: '#52c41a' }}>
                  {fmt(receipt.totalAmount)}
                </Text>
              </Table.Summary.Cell>
            </Table.Summary.Row>
          )}
        />
      </Card>

      <div style={{ textAlign: 'center', marginTop: 24, display: 'flex', justifyContent: 'center', gap: 12 }}
           className="no-print">
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/fees')}>
          Back to Fees
        </Button>
        <Button type="primary" icon={<PrinterOutlined />} onClick={() => window.print()}>
          Print Receipt
        </Button>
      </div>

      <style>{`
        @media print {
          .no-print { display: none !important; }
          .print-area { padding: 0; }
        }
      `}</style>
    </div>
  )
}
