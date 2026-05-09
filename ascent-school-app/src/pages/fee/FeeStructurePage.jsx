import { useEffect, useState } from 'react'
import {
  Card, Form, Select, Button, Table, InputNumber, Typography, Alert, Row, Col, App as AntApp,
} from 'antd'
import { SaveOutlined, SearchOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Text } = Typography

export default function FeeStructurePage() {
  const { message } = AntApp.useApp()

  // Lookups
  const [academicYears,  setAcademicYears]  = useState([])
  const [classes,        setClasses]        = useState([])
  const [feeCategories,  setFeeCategories]  = useState([])
  const [feeTypes,       setFeeTypes]       = useState([])
  const [terms,          setTerms]          = useState([])

  // Filter state
  const [filters, setFilters] = useState({ academicYearId: null, classId: null, feeCategoryId: null })

  // Grid data: Map of "feeTypeId_termId" → amount
  const [amountMap, setAmountMap] = useState({})
  const [loaded,    setLoaded]    = useState(false)
  const [loading,   setLoading]   = useState(false)
  const [saving,    setSaving]    = useState(false)

  useEffect(() => {
    Promise.all([
      api.get('/school/master/academic-years'),
      api.get('/school/master/classes'),
      api.get('/school/master/fee-categories'),
      api.get('/school/master/fee-types'),
      api.get('/school/master/terms'),
    ]).then(([yr, cl, fc, ft, tm]) => {
      setAcademicYears(yr.data?.data || [])
      setClasses(cl.data?.data || [])
      setFeeCategories(fc.data?.data || [])
      setFeeTypes(ft.data?.data || [])
      setTerms(tm.data?.data || [])
    }).catch(() => {})
  }, [])

  const loadStructure = async () => {
    const { academicYearId, classId, feeCategoryId } = filters
    if (!academicYearId || !classId || !feeCategoryId) {
      message.warning('Select academic year, class and fee category first.')
      return
    }
    setLoading(true)
    setLoaded(false)
    try {
      const res = await api.get(
        `/school/fees/structure?classId=${classId}&feeCategoryId=${feeCategoryId}&academicYearId=${academicYearId}`
      )
      const items = res.data?.data || []
      const map   = {}
      items.forEach((item) => {
        const key = `${item.feeTypeId}_${item.termId ?? 0}`
        map[key] = item.amount
      })
      setAmountMap(map)
      setLoaded(true)
    } finally {
      setLoading(false)
    }
  }

  const handleAmountChange = (feeTypeId, termId, value) => {
    const key = `${feeTypeId}_${termId ?? 0}`
    setAmountMap((prev) => ({ ...prev, [key]: value || 0 }))
  }

  const handleSave = async () => {
    const { academicYearId, classId, feeCategoryId } = filters

    // Build items list from amountMap (only non-zero entries)
    const items = []
    for (const [key, amount] of Object.entries(amountMap)) {
      if ((amount || 0) <= 0) continue
      const [feeTypeId, termId] = key.split('_').map(Number)
      items.push({
        FeeTypeId: feeTypeId,
        TermId:    termId === 0 ? null : termId,
        Amount:    amount,
      })
    }

    setSaving(true)
    try {
      await api.post('/school/fees/structure', {
        ClassId:        classId,
        FeeCategoryId:  feeCategoryId,
        AcademicYearId: academicYearId,
        Items:          items,
      })
      message.success('Fee structure saved.')
    } catch {
      message.error('Failed to save fee structure.')
    } finally {
      setSaving(false)
    }
  }

  // Build columns: one per term (+ "No Term" column)
  const activeTerms = terms
    .filter((t) => t.status !== 'Inactive')
    .sort((a, b) => (a.orderNo ?? 999) - (b.orderNo ?? 999))

  const columns = [
    {
      title:     'Fee Type',
      dataIndex: 'feeTypeName',
      key:       'feeTypeName',
      width:     180,
      render:    (v) => <Text strong>{v}</Text>,
    },
    // One column per term
    ...activeTerms.map((term) => ({
      title: term.termName,
      key:   `term_${term.termId}`,
      width: 130,
      render: (_, row) => (
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          precision={2}
          value={amountMap[`${row.feeTypeId}_${term.termId}`] ?? 0}
          onChange={(v) => handleAmountChange(row.feeTypeId, term.termId, v)}
          disabled={!loaded}
        />
      ),
    })),
    // Column for fees with no term
    {
      title: '(No Term)',
      key:   'term_none',
      width: 130,
      render: (_, row) => (
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          precision={2}
          value={amountMap[`${row.feeTypeId}_0`] ?? 0}
          onChange={(v) => handleAmountChange(row.feeTypeId, null, v)}
          disabled={!loaded}
        />
      ),
    },
  ]

  const activeFeeTypes = feeTypes
    .filter((f) => f.status !== 'Inactive')
    .sort((a, b) => (a.sequenceNo ?? 999) - (b.sequenceNo ?? 999))

  return (
    <Card title="Fee Structure Setup">
      {/* Filters */}
      <Row gutter={12} style={{ marginBottom: 16 }}>
        <Col flex="200px">
          <Select
            style={{ width: '100%' }}
            placeholder="Academic Year"
            options={academicYears.map((y) => ({ value: y.academicYearId, label: y.academicYear }))}
            value={filters.academicYearId}
            onChange={(v) => setFilters((f) => ({ ...f, academicYearId: v }))}
          />
        </Col>
        <Col flex="200px">
          <Select
            style={{ width: '100%' }}
            placeholder="Class"
            options={classes.map((c) => ({ value: c.classId, label: c.className }))}
            value={filters.classId}
            onChange={(v) => setFilters((f) => ({ ...f, classId: v }))}
          />
        </Col>
        <Col flex="200px">
          <Select
            style={{ width: '100%' }}
            placeholder="Fee Category"
            options={feeCategories.map((c) => ({ value: c.feeCategoryId, label: c.categoryName }))}
            value={filters.feeCategoryId}
            onChange={(v) => setFilters((f) => ({ ...f, feeCategoryId: v }))}
          />
        </Col>
        <Col>
          <Button icon={<SearchOutlined />} onClick={loadStructure} loading={loading}>
            Load
          </Button>
        </Col>
      </Row>

      {!loaded && (
        <Alert
          message="Select academic year, class and fee category, then click Load to view or edit the fee structure."
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
            columns={columns}
            pagination={false}
            size="small"
            bordered
            scroll={{ x: 'max-content' }}
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
