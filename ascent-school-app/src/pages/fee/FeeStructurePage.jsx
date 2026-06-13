import { useEffect, useState } from 'react'
import {
  Card, Form, Select, Button, Table, InputNumber, Typography,
  Alert, Row, Col, App as AntApp, Radio, Tag,
} from 'antd'
import { SaveOutlined, SearchOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Text } = Typography

const ADMISSION_TYPE_OPTIONS = [
  { value: '',    label: 'All / Not Set' },
  { value: 'New', label: 'New Students' },
  { value: 'Old', label: 'Old Students' },
]

export default function FeeStructurePage() {
  const { message } = AntApp.useApp()

  // Lookups
  const [academicYears,  setAcademicYears]  = useState([])
  const [classes,        setClasses]        = useState([])
  const [feeCategories,  setFeeCategories]  = useState([])
  const [feeTypes,       setFeeTypes]       = useState([])
  const [terms,          setTerms]          = useState([])
  const [feePeriods,     setFeePeriods]     = useState([])

  // Filter state
  const [filters, setFilters] = useState({
    academicYearId: null,
    classId:        null,
    feeCategoryId:  null,
    admissionType:  '',
  })

  // Payment type — detected from loaded data or set by user
  const [paymentType, setPaymentType] = useState('Term')

  // Grid state: "feeTypeId_T_termId" or "feeTypeId_P_periodId" → amount
  const [amountMap, setAmountMap] = useState({})
  const [loaded,    setLoaded]    = useState(false)
  const [loading,   setLoading]   = useState(false)
  const [saving,    setSaving]    = useState(false)

  useEffect(() => {
    Promise.all([
      api.get('/school/master/academic-years?activeOnly=true'),
      api.get('/school/master/classes'),
      api.get('/school/master/fee-categories'),
      api.get('/school/master/fee-types'),
    ]).then(([yr, cl, fc, ft]) => {
      const years = yr.data?.data || []
      setAcademicYears(years)
      setClasses(cl.data?.data || [])
      setFeeCategories(fc.data?.data || [])
      setFeeTypes(ft.data?.data || [])
      const current = years.find(y => y.isCurrent)
      if (current) {
        setFilters(f => ({ ...f, academicYearId: current.academicYearId }))
        loadTerms(current.academicYearId)
        loadFeePeriods(current.academicYearId)
      }
    }).catch(() => {})
  }, [])

  // Load terms and fee periods when academic year changes
  async function loadTerms(yearId) {
    if (!yearId) { setTerms([]); return }
    try {
      const res = await api.get(`/school/master/terms?academicYearId=${yearId}`)
      setTerms(res.data?.data || [])
    } catch {
      setTerms([])
    }
  }

  async function loadFeePeriods(yearId) {
    if (!yearId) { setFeePeriods([]); return }
    try {
      const res = await api.get(`/school/master/fee-periods?academicYearId=${yearId}`)
      setFeePeriods(res.data?.data || [])
    } catch {
      setFeePeriods([])
    }
  }

  function onYearChange(val) {
    setFilters((f) => ({ ...f, academicYearId: val }))
    loadTerms(val)
    loadFeePeriods(val)
    setLoaded(false)
    setAmountMap({})
  }

  const loadStructure = async () => {
    const { academicYearId, classId, feeCategoryId, admissionType } = filters
    if (!academicYearId || !classId || !feeCategoryId) {
      message.warning('Select academic year, class and fee category first.')
      return
    }
    setLoading(true)
    setLoaded(false)
    try {
      const params = new URLSearchParams({
        classId, feeCategoryId, academicYearId,
        ...(admissionType ? { admissionType } : {}),
      })
      const res  = await api.get(`/school/fees/structure?${params}`)
      const items = res.data?.data || []

      // Detect payment type from loaded data
      const detectedType = items.find((i) => i.paymentType === 'Monthly')
        ? 'Monthly' : 'Term'
      setPaymentType(detectedType)

      // Build amount map
      const map = {}
      items.forEach((item) => {
        const key = item.feePeriodId
          ? `${item.feeTypeId}_P_${item.feePeriodId}`
          : `${item.feeTypeId}_T_${item.termId ?? 0}`
        map[key] = item.amount
      })
      setAmountMap(map)
      setLoaded(true)
    } finally {
      setLoading(false)
    }
  }

  function handleAmountChange(feeTypeId, colKey, value) {
    const key = `${feeTypeId}_${colKey}`
    setAmountMap((prev) => ({ ...prev, [key]: value || 0 }))
  }

  function onPaymentTypeChange(val) {
    setPaymentType(val)
    // Reset amounts when switching type to avoid stale data confusion
    setAmountMap({})
  }

  const handleSave = async () => {
    const { academicYearId, classId, feeCategoryId, admissionType } = filters

    const items = []
    for (const [key, amount] of Object.entries(amountMap)) {
      if ((amount || 0) <= 0) continue
      // key format: "feeTypeId_T_termId" or "feeTypeId_P_periodId"
      const parts     = key.split('_')
      const feeTypeId = Number(parts[0])
      const kind      = parts[1]      // 'T' or 'P'
      const colId     = Number(parts[2])

      items.push({
        FeeTypeId:     feeTypeId,
        TermId:        kind === 'T' && colId !== 0 ? colId : null,
        FeePeriodId:   kind === 'P' ? colId : null,
        PaymentType:   paymentType,
        AdmissionType: admissionType || null,
        Amount:        amount,
      })
    }

    setSaving(true)
    try {
      await api.post('/school/fees/structure', {
        ClassId:        classId,
        FeeCategoryId:  feeCategoryId,
        AcademicYearId: academicYearId,
        AdmissionType:  admissionType || null,
        PaymentType:    paymentType,
        Items:          items,
      })
      message.success('Fee structure saved.')
    } catch {
      message.error('Failed to save fee structure.')
    } finally {
      setSaving(false)
    }
  }

  // Columns based on payment type
  const activeFeeTypes = feeTypes
    .filter((f) => f.status !== 'Inactive')
    .sort((a, b) => (a.sequenceNo ?? 999) - (b.sequenceNo ?? 999))

  const periodColumns = paymentType === 'Monthly'
    ? feePeriods
        .filter((p) => p.status !== 'Inactive')
        .sort((a, b) => {
          if (a.yearNo !== b.yearNo) return a.yearNo - b.yearNo
          return a.monthNo - b.monthNo
        })
    : null

  const termColumns = paymentType === 'Term'
    ? terms
        .filter((t) => t.status !== 'Inactive')
        .sort((a, b) => (a.orderNo ?? 999) - (b.orderNo ?? 999))
    : null

  const buildColumns = () => {
    const base = [{
      title:     'Fee Type',
      dataIndex: 'feeTypeName',
      key:       'feeTypeName',
      width:     180,
      fixed:     'left',
      render:    (v) => <Text strong>{v}</Text>,
    }]

    if (paymentType === 'Term') {
      const tCols = (termColumns || []).map((term) => ({
        title: term.termName,
        key:   `term_${term.termId}`,
        width: 130,
        render: (_, row) => (
          <InputNumber
            style={{ width: '100%' }}
            min={0}
            precision={2}
            value={amountMap[`${row.feeTypeId}_T_${term.termId}`] ?? 0}
            onChange={(v) => handleAmountChange(row.feeTypeId, `T_${term.termId}`, v)}
            disabled={!loaded}
          />
        ),
      }))
      // Legacy no-term column
      tCols.push({
        title: '(No Term)',
        key:   'term_none',
        width: 130,
        render: (_, row) => (
          <InputNumber
            style={{ width: '100%' }}
            min={0}
            precision={2}
            value={amountMap[`${row.feeTypeId}_T_0`] ?? 0}
            onChange={(v) => handleAmountChange(row.feeTypeId, 'T_0', v)}
            disabled={!loaded}
          />
        ),
      })
      return [...base, ...tCols]
    }

    // Monthly
    const pCols = (periodColumns || []).map((period) => ({
      title: period.periodLabel,
      key:   `period_${period.feePeriodId}`,
      width: 130,
      render: (_, row) => (
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          precision={2}
          value={amountMap[`${row.feeTypeId}_P_${period.feePeriodId}`] ?? 0}
          onChange={(v) => handleAmountChange(row.feeTypeId, `P_${period.feePeriodId}`, v)}
          disabled={!loaded}
        />
      ),
    }))

    if (pCols.length === 0) {
      pCols.push({
        title: 'No periods defined',
        key:   'no_periods',
        render: () => <Text type="secondary">Add Fee Periods in Master Data first</Text>,
      })
    }
    return [...base, ...pCols]
  }

  return (
    <Card title="Fee Structure Setup">
      {/* Filters */}
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }} align="middle">
        <Col flex="200px">
          <Select
            style={{ width: '100%' }}
            placeholder="Academic Year"
            options={academicYears.map((y) => ({ value: y.academicYearId, label: y.academicYear }))}
            value={filters.academicYearId}
            onChange={onYearChange}
          />
        </Col>
        <Col flex="200px">
          <Select
            style={{ width: '100%' }}
            placeholder="Class"
            options={classes.map((c) => ({ value: c.classId, label: c.className }))}
            value={filters.classId}
            onChange={(v) => { setFilters((f) => ({ ...f, classId: v })); setLoaded(false); setAmountMap({}) }}
          />
        </Col>
        <Col flex="200px">
          <Select
            style={{ width: '100%' }}
            placeholder="Fee Category"
            options={feeCategories.map((c) => ({ value: c.feeCategoryId, label: c.categoryName }))}
            value={filters.feeCategoryId}
            onChange={(v) => { setFilters((f) => ({ ...f, feeCategoryId: v })); setLoaded(false); setAmountMap({}) }}
          />
        </Col>
        <Col flex="160px">
          <Select
            style={{ width: '100%' }}
            placeholder="Admission Type"
            options={ADMISSION_TYPE_OPTIONS}
            value={filters.admissionType}
            onChange={(v) => { setFilters((f) => ({ ...f, admissionType: v })); setLoaded(false); setAmountMap({}) }}
          />
        </Col>
        <Col>
          <Button icon={<SearchOutlined />} onClick={loadStructure} loading={loading}>
            Load
          </Button>
        </Col>
      </Row>

      {loaded && (
        <Row style={{ marginBottom: 12 }} align="middle" gutter={16}>
          <Col>
            <Text strong>Payment Type:</Text>
          </Col>
          <Col>
            <Radio.Group
              value={paymentType}
              onChange={(e) => onPaymentTypeChange(e.target.value)}
              optionType="button"
              buttonStyle="solid"
              size="small"
            >
              <Radio.Button value="Term">Term</Radio.Button>
              <Radio.Button value="Monthly">Monthly</Radio.Button>
            </Radio.Group>
          </Col>
          {paymentType === 'Monthly' && feePeriods.length === 0 && (
            <Col>
              <Tag color="warning">No fee periods defined for this year — add them in Master Data → Fee Periods</Tag>
            </Col>
          )}
        </Row>
      )}

      {!loaded && (
        <Alert
          message="Select academic year, class, fee category and admission type, then click Load."
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      {loaded && (
        <>
          <Table
            rowKey="feeTypeId"
            dataSource={activeFeeTypes}
            columns={buildColumns()}
            pagination={false}
            size="small"
            bordered
            scroll={{ x: 'max-content', y: 'calc(100vh - 320px)' }}
          />
          <div style={{ marginTop: 16, textAlign: 'right' }}>
            <Button type="primary" icon={<SaveOutlined />} onClick={handleSave} loading={saving}>
              Save Fee Structure
            </Button>
          </div>
        </>
      )}
    </Card>
  )
}
