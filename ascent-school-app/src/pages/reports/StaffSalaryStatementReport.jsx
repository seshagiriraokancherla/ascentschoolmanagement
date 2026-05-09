import { useState, useCallback } from 'react'
import { Row, Col, Select, Button, Table, Tag, Space, Typography } from 'antd'
import { FilePdfOutlined, FileTextOutlined, SearchOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from './reportUtils'

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

const currentYear = new Date().getFullYear()
const YEARS = Array.from({ length: 10 }, (_, i) => currentYear - 2 + i)

export default function StaffSalaryStatementReport() {
  const schoolName = useBrandingStore(s => s.branding.displayName)

  const now = new Date()
  const [month,    setMonth]    = useState(now.getMonth() + 1)
  const [year,     setYear]     = useState(now.getFullYear())
  const [status,   setStatus]   = useState(null)
  const [data,     setData]     = useState([])
  const [loading,  setLoading]  = useState(false)
  const [searched, setSearched] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (month)  params.set('month',  month)
      if (year)   params.set('year',   year)
      if (status) params.set('status', status)
      const res = await api.get(`/school/staff/salaries?${params}`)
      setData(res.data.data || [])
      setSearched(true)
    } catch {
      setData([])
    } finally {
      setLoading(false)
    }
  }, [month, year, status])

  const monthLabel = MONTHS.find(m => m.value === month)?.label || ''
  const title      = `Staff Salary Statement — ${monthLabel} ${year}`

  const COLUMNS = [
    'Emp Code', 'Name', 'Designation',
    'Gross (₹)', 'Deductions (₹)', 'Advance (₹)', 'Net Salary (₹)',
    'Status', 'Paid Date',
  ]

  const toRow = r => [
    r.employeeCode,
    r.staffName,
    r.designation,
    Number(r.grossEarnings).toFixed(2),
    Number(r.totalDeductions).toFixed(2),
    Number(r.advanceDeducted).toFixed(2),
    Number(r.netSalary).toFixed(2),
    r.status,
    r.paidDate || '',
  ]

  const totals = data.reduce(
    (acc, r) => ({
      gross: acc.gross + Number(r.grossEarnings   || 0),
      ded:   acc.ded   + Number(r.totalDeductions || 0),
      adv:   acc.adv   + Number(r.advanceDeducted || 0),
      net:   acc.net   + Number(r.netSalary       || 0),
    }),
    { gross: 0, ded: 0, adv: 0, net: 0 }
  )

  const handlePdf = () => {
    const rows = data.map(toRow)
    rows.push([
      '', 'TOTAL', '',
      totals.gross.toFixed(2),
      totals.ded.toFixed(2),
      totals.adv.toFixed(2),
      totals.net.toFixed(2),
      '', '',
    ])
    exportPdf({
      schoolName: schoolName || 'School',
      title,
      columns:     COLUMNS,
      rows,
      fileName:    `staff_salary_statement_${monthLabel}_${year}.pdf`,
      tableOptions: {
        columnStyles: {
          3: { halign: 'right' },
          4: { halign: 'right' },
          5: { halign: 'right' },
          6: { halign: 'right' },
        },
        didParseCell: (cellData) => {
          if (cellData.row.index === rows.length - 1) {
            cellData.cell.styles.fontStyle  = 'bold'
            cellData.cell.styles.fillColor  = [240, 240, 240]
          }
        },
      },
    })
  }

  const handleCsv = () => {
    exportCsv({
      columns:  COLUMNS,
      rows:     data.map(toRow),
      fileName: `staff_salary_statement_${monthLabel}_${year}.csv`,
    })
  }

  const statusTag = (s) => {
    const colors = { Draft: 'warning', Paid: 'success', Cancelled: 'default' }
    return <Tag color={colors[s] || 'default'}>{s}</Tag>
  }

  const columns = [
    { title: 'Emp Code',       dataIndex: 'employeeCode',    key: 'ec',    width: 100 },
    { title: 'Name',           dataIndex: 'staffName',       key: 'name',  width: 160 },
    { title: 'Designation',    dataIndex: 'designation',     key: 'desig', width: 130 },
    { title: 'Gross (₹)',      dataIndex: 'grossEarnings',   key: 'gross', align: 'right',
      render: v => Number(v).toFixed(2) },
    { title: 'Deductions (₹)', dataIndex: 'totalDeductions', key: 'ded',   align: 'right',
      render: v => Number(v).toFixed(2) },
    { title: 'Advance (₹)',    dataIndex: 'advanceDeducted', key: 'adv',   align: 'right',
      render: v => Number(v).toFixed(2) },
    { title: 'Net Salary (₹)', dataIndex: 'netSalary',       key: 'net',   align: 'right',
      render: v => <Text strong>₹ {Number(v).toFixed(2)}</Text> },
    { title: 'Status',         dataIndex: 'status',          key: 'st',    width: 100,
      render: statusTag },
    { title: 'Paid Date',      dataIndex: 'paidDate',        key: 'pd',    width: 110,
      render: v => v || '—' },
  ]

  return (
    <div>
      <Row gutter={12} style={{ marginBottom: 16 }} wrap align="middle">
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
          </Select>
        </Col>
        <Col>
          <Button
            type="primary" icon={<SearchOutlined />}
            loading={loading} onClick={load}
          >
            Load
          </Button>
        </Col>
        {searched && data.length > 0 && (
          <Col>
            <Space>
              <Button icon={<FilePdfOutlined />} onClick={handlePdf}>PDF</Button>
              <Button icon={<FileTextOutlined />} onClick={handleCsv}>CSV</Button>
            </Space>
          </Col>
        )}
      </Row>

      {searched && (
        <Table
          rowKey="salaryId"
          dataSource={data}
          columns={columns}
          loading={loading}
          pagination={{ pageSize: 25, showTotal: t => `${t} records` }}
          size="small"
          scroll={{ x: 900 }}
          locale={{ emptyText: 'No salary records found for the selected period.' }}
          summary={() =>
            data.length > 0 ? (
              <Table.Summary.Row>
                <Table.Summary.Cell index={0} colSpan={3}>
                  <Text strong>Totals</Text>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={3} align="right">
                  <Text strong>{totals.gross.toFixed(2)}</Text>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={4} align="right">
                  <Text strong>{totals.ded.toFixed(2)}</Text>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={5} align="right">
                  <Text strong>{totals.adv.toFixed(2)}</Text>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={6} align="right">
                  <Text strong style={{ color: '#1677ff' }}>
                    ₹ {totals.net.toFixed(2)}
                  </Text>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={7} colSpan={2} />
              </Table.Summary.Row>
            ) : null
          }
        />
      )}
    </div>
  )
}
