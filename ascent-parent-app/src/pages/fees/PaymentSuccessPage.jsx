import { useLocation, useNavigate } from 'react-router-dom'
import { Result, Button, Descriptions, Table, Typography, Card, Divider } from 'antd'
import { CheckCircleOutlined, PrinterOutlined, ArrowLeftOutlined } from '@ant-design/icons'

const { Text } = Typography
const fmt = (n) => `₹${Number(n || 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`

export default function PaymentSuccessPage() {
  const navigate  = useNavigate()
  const { state } = useLocation()
  const receipt   = state?.receipt

  if (!receipt) {
    return (
      <Result
        status="info"
        title="No receipt to display"
        extra={<Button type="primary" onClick={() => navigate('/fees')}>Back to Fees</Button>}
      />
    )
  }

  const itemCols = [
    {
      title: 'Description',
      key: 'desc',
      render: (_, r) => r.feeTypeName || r.routeName || r.hostelName || '—',
    },
    { title: 'Term / Period', dataIndex: 'termName', key: 'term', render: (v) => v || '—' },
    { title: 'Amount',        dataIndex: 'amount',   key: 'amt',  align: 'right', render: fmt },
    { title: 'Concession',    dataIndex: 'concessionAmount', key: 'conc', align: 'right', render: (v) => v > 0 ? fmt(v) : '—' },
    { title: 'Net',           dataIndex: 'netAmount', key: 'net', align: 'right', render: (v) => <Text strong>{fmt(v)}</Text> },
  ]

  return (
    <div style={{ maxWidth: 700, margin: '0 auto' }} className="print-area">
      <Result
        icon={<CheckCircleOutlined style={{ color: '#52c41a' }} />}
        status="success"
        title="Payment Successful!"
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
          dataSource={receipt.items || []}
          columns={itemCols}
          rowKey="itemId"
          size="small"
          pagination={false}
          summary={() => (
            <Table.Summary.Row>
              <Table.Summary.Cell colSpan={4} align="right">
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
