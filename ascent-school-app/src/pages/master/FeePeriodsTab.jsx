import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Input, InputNumber, Select, Tag, Space, message } from 'antd'
import { PlusOutlined, EditOutlined, ThunderboltOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

const MONTH_OPTIONS = [
  { value: 1,  label: 'January' },
  { value: 2,  label: 'February' },
  { value: 3,  label: 'March' },
  { value: 4,  label: 'April' },
  { value: 5,  label: 'May' },
  { value: 6,  label: 'June' },
  { value: 7,  label: 'July' },
  { value: 8,  label: 'August' },
  { value: 9,  label: 'September' },
  { value: 10, label: 'October' },
  { value: 11, label: 'November' },
  { value: 12, label: 'December' },
]

const MONTH_NAMES = ['', 'January', 'February', 'March', 'April', 'May', 'June',
                     'July', 'August', 'September', 'October', 'November', 'December']

// Apr-Feb of an academic year (11 months): Apr-Dec in startYear, Jan-Feb in startYear+1
function buildAprFebMonths(startYear) {
  const months = []
  for (let m = 4; m <= 12; m++) months.push({ monthNo: m, yearNo: startYear })
  for (let m = 1; m <= 2; m++)  months.push({ monthNo: m, yearNo: startYear + 1 })
  return months
}

export default function FeePeriodsTab() {
  const [rows,         setRows]         = useState([])
  const [academicYears, setAcademicYears] = useState([])
  const [yearId,       setYearId]       = useState(null)
  const [loading,      setLoading]      = useState(false)
  const [open,         setOpen]         = useState(false)
  const [editing,      setEditing]      = useState(null)
  const [saving,       setSaving]       = useState(false)
  const [generating,   setGenerating]   = useState(false)
  const [form] = Form.useForm()

  const yearOptions = academicYears.map((y) => ({
    value: y.academicYearId,
    label: y.academicYear,
  }))

  async function load(aYearId) {
    setLoading(true)
    try {
      const res = await api.get(`/school/master/fee-periods${aYearId ? `?academicYearId=${aYearId}` : ''}`)
      setRows(res.data?.data || [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    api.get('/school/master/academic-years')
       .then((r) => setAcademicYears(r.data?.data || []))
       .catch(() => {})
    load(null)
  }, [])

  function onYearChange(val) {
    setYearId(val)
    load(val)
  }

  function openCreate() {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ academicYearId: yearId, status: 'Active' })
    setOpen(true)
  }

  function openEdit(record) {
    setEditing(record)
    form.setFieldsValue({
      academicYearId: record.academicYearId,
      monthNo:        record.monthNo,
      yearNo:         record.yearNo,
      periodLabel:    record.periodLabel,
      sequenceNo:     record.sequenceNo,
      status:         record.status,
    })
    setOpen(true)
  }

  function onMonthOrYearChange() {
    const { monthNo, yearNo } = form.getFieldsValue(['monthNo', 'yearNo'])
    if (monthNo && yearNo) {
      form.setFieldValue('periodLabel', `${MONTH_NAMES[monthNo]} ${yearNo}`)
    }
  }

  async function handleSave() {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        AcademicYearId: values.academicYearId || null,
        MonthNo:        values.monthNo,
        YearNo:         values.yearNo,
        PeriodLabel:    values.periodLabel,
        SequenceNo:     values.sequenceNo || null,
        Status:         values.status,
      }
      let res
      if (editing) {
        res = await api.put(`/school/master/fee-periods/${editing.feePeriodId}`, body)
      } else {
        res = await api.post('/school/master/fee-periods', body)
      }
      setRows(res.data?.data || [])
      setOpen(false)
    } finally {
      setSaving(false)
    }
  }

  async function generateAprFeb() {
    if (!yearId) {
      message.warning('Select an academic year first.')
      return
    }
    const selectedYear = academicYears.find((y) => y.academicYearId === yearId)
    if (!selectedYear) return

    // Parse start year from "2024-25" → 2024
    const startYear = parseInt(selectedYear.academicYear.substring(0, 4), 10)
    if (isNaN(startYear)) {
      message.error('Cannot parse academic year format. Expected "YYYY-YY".')
      return
    }

    const months = buildAprFebMonths(startYear)
    const existingLabels = new Set(rows.map((r) => r.periodLabel))
    const toCreate = months.filter(
      ({ monthNo, yearNo }) => !existingLabels.has(`${MONTH_NAMES[monthNo]} ${yearNo}`)
    )

    if (toCreate.length === 0) {
      message.info('All Apr–Feb periods already exist for this academic year.')
      return
    }

    setGenerating(true)
    try {
      let seq = rows.length + 1
      for (const { monthNo, yearNo } of toCreate) {
        await api.post('/school/master/fee-periods', {
          AcademicYearId: yearId,
          MonthNo:        monthNo,
          YearNo:         yearNo,
          PeriodLabel:    `${MONTH_NAMES[monthNo]} ${yearNo}`,
          SequenceNo:     seq++,
          Status:         'Active',
        })
      }
      await load(yearId)
      message.success(`Generated ${toCreate.length} fee period(s).`)
    } finally {
      setGenerating(false)
    }
  }

  const columns = [
    { title: 'Seq',    dataIndex: 'sequenceNo',  key: 'sequenceNo',  width: 60 },
    { title: 'Period', dataIndex: 'periodLabel', key: 'periodLabel' },
    { title: 'Month',  dataIndex: 'monthNo',     key: 'monthNo',  width: 100,
      render: (v) => MONTH_NAMES[v] },
    { title: 'Year',   dataIndex: 'yearNo',      key: 'yearNo',   width: 80 },
    {
      title: 'Status', dataIndex: 'status', key: 'status', width: 100,
      render: (v) => <Tag color={v === 'Active' ? 'green' : 'default'}>{v}</Tag>,
    },
    {
      title: '', key: 'actions', width: 60,
      render: (_, record) => (
        <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(record)} />
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'center' }}>
        <Select
          placeholder="Filter by academic year"
          options={yearOptions}
          style={{ width: 200 }}
          value={yearId}
          onChange={onYearChange}
          allowClear
          onClear={() => onYearChange(null)}
        />
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
          Add Period
        </Button>
        <Button
          icon={<ThunderboltOutlined />}
          onClick={generateAprFeb}
          loading={generating}
          disabled={!yearId}
          title="Generate Apr–Feb periods for selected academic year"
        >
          Generate Apr–Feb
        </Button>
      </div>

      <Table
        rowKey="feePeriodId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        size="small"
        pagination={false}
        locale={{ emptyText: yearId ? 'No periods yet for this year.' : 'Select a year or add periods.' }}
      />

      <Modal
        title={editing ? 'Edit Fee Period' : 'Add Fee Period'}
        open={open}
        onOk={handleSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="academicYearId" label="Academic Year">
            <Select options={yearOptions} placeholder="Select year" allowClear />
          </Form.Item>
          <Form.Item name="monthNo" label="Month" rules={[{ required: true, message: 'Month is required.' }]}>
            <Select options={MONTH_OPTIONS} placeholder="Select month" onChange={onMonthOrYearChange} />
          </Form.Item>
          <Form.Item name="yearNo" label="Calendar Year" rules={[{ required: true, message: 'Year is required.' }]}>
            <InputNumber min={2000} max={2100} style={{ width: '100%' }} onChange={onMonthOrYearChange} />
          </Form.Item>
          <Form.Item name="periodLabel" label="Period Label" rules={[{ required: true, message: 'Label is required.' }]}>
            <Input placeholder="e.g. April 2024" maxLength={20} />
          </Form.Item>
          <Form.Item name="sequenceNo" label="Sequence No">
            <InputNumber min={1} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="status" label="Status" rules={[{ required: true }]}>
            <Select options={STATUS_OPTIONS} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
