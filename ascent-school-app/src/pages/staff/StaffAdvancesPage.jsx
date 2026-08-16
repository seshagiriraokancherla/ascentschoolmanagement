import { useEffect, useState } from 'react'
import {
  Table, Button, Space, Select, DatePicker, Tag, Modal, Form,
  InputNumber, Input, Typography, Row, Col, Tabs, Statistic, Card,
  Descriptions, Popconfirm, App as AntApp,
} from 'antd'
import { PlusOutlined, DollarOutlined, UndoOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'

const { Text, Title } = Typography
const { RangePicker } = DatePicker

export default function StaffAdvancesPage() {
  const { message } = AntApp.useApp()

  // ── Master data ────────────────────────────────────────────────────────────
  const [staffList, setStaffList] = useState([])

  useEffect(() => {
    api.get('/school/staff?status=Active')
      .then(r => setStaffList(r.data?.data || []))
  }, [])

  // ── Advances list state ────────────────────────────────────────────────────
  const [advances,      setAdvances]      = useState([])
  const [loadingAdv,    setLoadingAdv]    = useState(false)
  const [filterStaff,   setFilterStaff]   = useState(null)
  const [filterDates,   setFilterDates]   = useState(null)
  const [filterStatus,  setFilterStatus]  = useState('Active')

  const loadAdvances = async () => {
    setLoadingAdv(true)
    try {
      const params = new URLSearchParams()
      if (filterStaff)      params.append('staffId', filterStaff)
      if (filterStatus)     params.append('status',  filterStatus)
      if (filterDates?.[0]) params.append('dateFrom', filterDates[0].format('YYYY-MM-DD'))
      if (filterDates?.[1]) params.append('dateTo',   filterDates[1].format('YYYY-MM-DD'))
      const r = await api.get(`/school/staff/advances?${params}`)
      setAdvances(r.data?.data || [])
    } catch (e) { message.error(apiError(e, 'Failed to load advances.')) }
    finally { setLoadingAdv(false) }
  }

  useEffect(() => { loadAdvances() }, [filterStaff, filterDates, filterStatus])

  // ── Summary state ──────────────────────────────────────────────────────────
  const [summary,     setSummary]     = useState([])
  const [loadingSum,  setLoadingSum]  = useState(false)

  const loadSummary = async () => {
    setLoadingSum(true)
    try {
      const r = await api.get('/school/staff/advances/summary')
      setSummary(r.data?.data || [])
    } catch (e) { message.error(apiError(e, 'Failed to load summary.')) }
    finally { setLoadingSum(false) }
  }

  // ── New Advance modal ──────────────────────────────────────────────────────
  const [advModal, setAdvModal] = useState(false)
  const [advSaving, setAdvSaving] = useState(false)
  const [advForm]  = Form.useForm()

  const handleCreateAdvance = async () => {
    let vals
    try { vals = await advForm.validateFields() } catch { return }
    setAdvSaving(true)
    try {
      await api.post('/school/staff/advances', {
        ...vals,
        advanceDate: vals.advanceDate.format('YYYY-MM-DD'),
      })
      message.success('Advance recorded.')
      setAdvModal(false)
      advForm.resetFields()
      loadAdvances()
      loadSummary()
    } catch (e) { message.error(apiError(e, 'Failed to record advance.')) }
    finally { setAdvSaving(false) }
  }

  // ── Repayment modal ────────────────────────────────────────────────────────
  const [repModal,    setRepModal]    = useState(false)
  const [repAdvance,  setRepAdvance]  = useState(null)   // the advance being repaid
  const [repayments,  setRepayments]  = useState([])
  const [repLoading,  setRepLoading]  = useState(false)
  const [repSaving,   setRepSaving]   = useState(false)
  const [repForm]     = Form.useForm()

  const openRepayment = async (record) => {
    setRepAdvance(record)
    setRepayments([])
    setRepModal(true)
    repForm.resetFields()
    repForm.setFieldsValue({ repaymentDate: dayjs() })
    setRepLoading(true)
    try {
      const r = await api.get(`/school/staff/advances/${record.advanceId}/repayments`)
      setRepayments(r.data?.data || [])
    } catch {}
    finally { setRepLoading(false) }
  }

  const handleAddRepayment = async () => {
    let vals
    try { vals = await repForm.validateFields() } catch { return }
    setRepSaving(true)
    try {
      await api.post(`/school/staff/advances/${repAdvance.advanceId}/repayments`, {
        ...vals,
        repaymentDate: vals.repaymentDate.format('YYYY-MM-DD'),
      })
      message.success('Repayment recorded.')
      repForm.resetFields()
      repForm.setFieldsValue({ repaymentDate: dayjs() })
      // Refresh repayments list
      const r = await api.get(`/school/staff/advances/${repAdvance.advanceId}/repayments`)
      setRepayments(r.data?.data || [])
      loadAdvances()
      loadSummary()
    } catch (e) {
      message.error(e?.message || 'Failed to record repayment.')
    }
    finally { setRepSaving(false) }
  }

  const handleCancel = async (record) => {
    try {
      await api.put(`/school/staff/advances/${record.advanceId}/cancel`)
      message.success('Advance cancelled.')
      loadAdvances()
      loadSummary()
    } catch (e) { message.error(apiError(e, 'Failed to cancel advance.')) }
  }

  // ── Advance table columns ──────────────────────────────────────────────────
  const advColumns = [
    { title: 'Date',    dataIndex: 'advanceDate',  width: 100 },
    { title: 'Emp Code', dataIndex: 'employeeCode', width: 95,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Staff',   dataIndex: 'staffName',    width: 180 },
    { title: 'Designation', dataIndex: 'designation', width: 130,
      render: v => v ? <Tag color="blue" style={{ fontSize: 11 }}>{v}</Tag> : null },
    { title: 'Purpose', dataIndex: 'purpose', ellipsis: true,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Amount',  dataIndex: 'amount',       width: 100, align: 'right',
      render: v => <Text strong>₹{Number(v).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text> },
    { title: 'Repaid',  dataIndex: 'totalRepaid',  width: 100, align: 'right',
      render: v => <Text style={{ color: '#52c41a' }}>₹{Number(v).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text> },
    { title: 'Outstanding', dataIndex: 'outstanding', width: 110, align: 'right',
      render: v => {
        const n = Number(v)
        return n > 0
          ? <Text style={{ color: '#ff4d4f', fontWeight: 700 }}>₹{n.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text>
          : <Tag color="success">Cleared</Tag>
      } },
    { title: 'Status',  dataIndex: 'status',       width: 90,
      render: v => <Tag color={v === 'Active' ? 'processing' : 'default'}>{v}</Tag> },
    {
      title: 'Actions', width: 160, align: 'center',
      render: (_, record) => (
        <Space size={4}>
          {record.status === 'Active' && Number(record.outstanding) > 0 && (
            <Button
              size="small" icon={<UndoOutlined />}
              onClick={() => openRepayment(record)}
            >
              Repay
            </Button>
          )}
          {record.status === 'Active' && (
            <Popconfirm
              title="Cancel this advance? This cannot be undone."
              onConfirm={() => handleCancel(record)}
              okText="Cancel Advance"
              okButtonProps={{ danger: true }}
            >
              <Button size="small" danger>Cancel</Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ]

  // ── Summary table columns ──────────────────────────────────────────────────
  const sumColumns = [
    { title: '#', width: 45, align: 'center', render: (_, __, i) => i + 1 },
    { title: 'Emp Code',    dataIndex: 'employeeCode', width: 100,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Staff',       dataIndex: 'staffName',    width: 200 },
    { title: 'Designation', dataIndex: 'designation',  width: 130,
      render: v => v ? <Tag color="blue" style={{ fontSize: 11 }}>{v}</Tag> : null },
    { title: 'Total Advanced', dataIndex: 'totalAdvanced', width: 130, align: 'right',
      render: v => `₹${Number(v).toLocaleString('en-IN', { minimumFractionDigits: 2 })}` },
    { title: 'Total Repaid',   dataIndex: 'totalRepaid',   width: 120, align: 'right',
      render: v => <Text style={{ color: '#52c41a' }}>₹{Number(v).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text> },
    { title: 'Outstanding',    dataIndex: 'outstanding',   width: 120, align: 'right',
      render: v => <Text style={{ color: '#ff4d4f', fontWeight: 700 }}>₹{Number(v).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text> },
  ]

  const totalOutstanding = summary.reduce((s, r) => s + Number(r.outstanding), 0)

  return (
    <div>
      <Tabs
        defaultActiveKey="advances"
        onChange={key => key === 'summary' && loadSummary()}
        items={[
          {
            key: 'advances',
            label: 'Advances',
            children: (
              <div>
                {/* Filters */}
                <Row gutter={12} align="middle" style={{ marginBottom: 16 }} wrap>
                  <Col>
                    <Select
                      style={{ width: 200 }}
                      placeholder="All Staff"
                      value={filterStaff}
                      onChange={setFilterStaff}
                      options={staffList.map(s => ({
                        label: `${s.employeeCode ? s.employeeCode + ' — ' : ''}${s.staffName}`,
                        value: s.staffId,
                      }))}
                      showSearch
                      filterOption={(input, opt) => opt.label.toLowerCase().includes(input.toLowerCase())}
                      allowClear
                    />
                  </Col>
                  <Col>
                    <RangePicker
                      format="DD-MM-YYYY"
                      value={filterDates}
                      onChange={setFilterDates}
                      placeholder={['From', 'To']}
                      allowClear
                    />
                  </Col>
                  <Col>
                    <Select
                      style={{ width: 120 }}
                      value={filterStatus}
                      onChange={setFilterStatus}
                      options={[
                        { value: '',          label: 'All Status'  },
                        { value: 'Active',    label: 'Active'      },
                        { value: 'Cancelled', label: 'Cancelled'   },
                      ]}
                    />
                  </Col>
                  <Col style={{ marginLeft: 'auto' }}>
                    <Button
                      type="primary" icon={<PlusOutlined />}
                      onClick={() => { advForm.resetFields(); advForm.setFieldsValue({ advanceDate: dayjs() }); setAdvModal(true) }}
                    >
                      New Advance
                    </Button>
                  </Col>
                </Row>

                <Table
                  rowKey="advanceId"
                  dataSource={advances}
                  columns={advColumns}
                  loading={loadingAdv}
                  size="small"
                  pagination={{ pageSize: 20, showTotal: t => `${t} advances` }}
                  scroll={{ x: 'max-content' }}
                />
              </div>
            ),
          },
          {
            key: 'summary',
            label: 'Outstanding Summary',
            children: (
              <div>
                {totalOutstanding > 0 && (
                  <Row gutter={16} style={{ marginBottom: 16 }}>
                    <Col>
                      <Card size="small">
                        <Statistic
                          title="Total Outstanding"
                          value={totalOutstanding}
                          prefix="₹"
                          precision={2}
                          valueStyle={{ color: '#ff4d4f' }}
                        />
                      </Card>
                    </Col>
                    <Col>
                      <Card size="small">
                        <Statistic
                          title="Staff with Dues"
                          value={summary.length}
                          valueStyle={{ color: '#fa8c16' }}
                        />
                      </Card>
                    </Col>
                  </Row>
                )}

                <Table
                  rowKey="staffId"
                  dataSource={summary}
                  columns={sumColumns}
                  loading={loadingSum}
                  size="small"
                  pagination={false}
                  scroll={{ x: 'max-content' }}
                  locale={{ emptyText: 'No outstanding advances.' }}
                />
              </div>
            ),
          },
        ]}
      />

      {/* ── New Advance modal ──────────────────────────────────────────────── */}
      <Modal
        title={<Space><DollarOutlined />Record Staff Advance</Space>}
        open={advModal}
        onOk={handleCreateAdvance}
        onCancel={() => setAdvModal(false)}
        confirmLoading={advSaving}
        okText="Record Advance"
        destroyOnClose
        width={500}
      >
        <Form form={advForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="staffId" label="Staff Member" rules={[{ required: true, message: 'Select a staff member' }]}>
            <Select
              showSearch
              placeholder="Select staff"
              options={staffList.map(s => ({
                label: `${s.employeeCode ? s.employeeCode + ' — ' : ''}${s.staffName}${s.designation ? ' (' + s.designation + ')' : ''}`,
                value: s.staffId,
              }))}
              filterOption={(input, opt) => opt.label.toLowerCase().includes(input.toLowerCase())}
            />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="advanceDate" label="Advance Date" rules={[{ required: true, message: 'Select a date' }]}>
                <DatePicker style={{ width: '100%' }} format="DD-MM-YYYY" disabledDate={d => d && d.isAfter(dayjs(), 'day')} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="amount" label="Amount (₹)" rules={[{ required: true, message: 'Enter amount' }]}>
                <InputNumber
                  style={{ width: '100%' }} min={1} precision={2}
                  formatter={v => `₹ ${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                  parser={v => v.replace(/₹\s?|(,*)/g, '')}
                />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="purpose" label="Purpose">
            <Input placeholder="e.g. Medical emergency, House rent" />
          </Form.Item>
          <Form.Item name="remarks" label="Remarks">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>

      {/* ── Repayment modal ───────────────────────────────────────────────── */}
      <Modal
        title={
          repAdvance && (
            <Space>
              <UndoOutlined />
              Repayment — {repAdvance.staffName}
            </Space>
          )
        }
        open={repModal}
        onOk={handleAddRepayment}
        onCancel={() => setRepModal(false)}
        confirmLoading={repSaving}
        okText="Record Repayment"
        destroyOnClose
        width={560}
      >
        {repAdvance && (
          <>
            <Descriptions size="small" bordered column={2} style={{ marginBottom: 16 }}>
              <Descriptions.Item label="Advance Date">{repAdvance.advanceDate}</Descriptions.Item>
              <Descriptions.Item label="Advance Amount">
                <Text strong>₹{Number(repAdvance.amount).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="Total Repaid">
                <Text style={{ color: '#52c41a' }}>₹{Number(repAdvance.totalRepaid).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="Outstanding">
                <Text style={{ color: '#ff4d4f', fontWeight: 700 }}>₹{Number(repAdvance.outstanding).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text>
              </Descriptions.Item>
            </Descriptions>

            {/* Previous repayments */}
            {repayments.length > 0 && (
              <Table
                rowKey="repaymentId"
                dataSource={repayments}
                loading={repLoading}
                size="small"
                pagination={false}
                style={{ marginBottom: 16 }}
                columns={[
                  { title: 'Date',   dataIndex: 'repaymentDate', width: 100 },
                  { title: 'Amount', dataIndex: 'amount',        width: 110, align: 'right',
                    render: v => `₹${Number(v).toLocaleString('en-IN', { minimumFractionDigits: 2 })}` },
                  { title: 'Remarks', dataIndex: 'remarks', ellipsis: true,
                    render: v => v || <Text type="secondary">—</Text> },
                  { title: 'By', dataIndex: 'createdBy', width: 110, ellipsis: true },
                ]}
              />
            )}

            <Form form={repForm} layout="vertical">
              <Row gutter={12}>
                <Col span={12}>
                  <Form.Item name="repaymentDate" label="Repayment Date" rules={[{ required: true, message: 'Select date' }]}>
                    <DatePicker style={{ width: '100%' }} format="DD-MM-YYYY" disabledDate={d => d && d.isAfter(dayjs(), 'day')} />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    name="amount"
                    label={`Amount (max ₹${Number(repAdvance.outstanding).toLocaleString('en-IN', { minimumFractionDigits: 2 })})`}
                    rules={[
                      { required: true, message: 'Enter amount' },
                      { type: 'number', max: Number(repAdvance.outstanding), message: 'Exceeds outstanding balance' },
                    ]}
                  >
                    <InputNumber
                      style={{ width: '100%' }} min={0.01}
                      max={Number(repAdvance.outstanding)} precision={2}
                      formatter={v => `₹ ${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                      parser={v => v.replace(/₹\s?|(,*)/g, '')}
                    />
                  </Form.Item>
                </Col>
              </Row>
              <Form.Item name="remarks" label="Remarks">
                <Input placeholder="e.g. Salary deduction — June 2025" />
              </Form.Item>
            </Form>
          </>
        )}
      </Modal>
    </div>
  )
}
