import { useEffect, useState } from 'react'
import {
  Tabs, Card, Table, Button, Modal, Form, Input, InputNumber,
  Select, Popconfirm, Space, Tag, Typography, App as AntApp,
  Row, Col,
} from 'antd'
import { PlusOutlined, EditOutlined, SaveOutlined, SearchOutlined, CarOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography

// ── Shared status options ─────────────────────────────────────────────────────
const STATUS_OPTIONS = [
  { label: 'Active',   value: 'Active' },
  { label: 'Inactive', value: 'I' },
]

// ─────────────────────────────────────────────────────────────────────────────
// Buses Tab
// ─────────────────────────────────────────────────────────────────────────────
function BusesTab() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()
  const [buses,   setBuses]   = useState([])
  const [loading, setLoading] = useState(false)
  const [modal,   setModal]   = useState({ open: false, editing: null })
  const [saving,  setSaving]  = useState(false)

  const load = () => {
    setLoading(true)
    api.get('/school/transport/buses')
       .then(r => setBuses(r.data.data || []))
       .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const openCreate = () => {
    form.resetFields()
    form.setFieldsValue({ status: 'Active' })
    setModal({ open: true, editing: null })
  }

  const openEdit = (rec) => {
    form.setFieldsValue(rec)
    setModal({ open: true, editing: rec })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      if (modal.editing) {
        await api.put(`/school/transport/buses/${modal.editing.busId}`, values)
        message.success('Bus updated.')
      } else {
        await api.post('/school/transport/buses', values)
        message.success('Bus created.')
      }
      setModal({ open: false, editing: null })
      load()
    } catch (e) {
      message.error(e.message || 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Bus Name',        dataIndex: 'busName',        key: 'busName',        render: n => <Text strong>{n}</Text> },
    { title: 'Reg. No',         dataIndex: 'registrationNo', key: 'registrationNo', width: 130 },
    { title: 'Model',           dataIndex: 'model',          key: 'model',          width: 120 },
    { title: 'Capacity',        dataIndex: 'capacity',       key: 'capacity',       width: 90, render: c => c ?? '—' },
    { title: 'Status',          dataIndex: 'status',         key: 'status',         width: 90,
      render: s => <Tag color={s === 'Active' ? 'success' : 'default'}>{s}</Tag> },
    {
      title: '', key: 'actions', width: 60,
      render: (_, r) => <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />,
    },
  ]

  return (
    <Card extra={<Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Bus</Button>}>
      <Table dataSource={buses} columns={columns} rowKey="busId" loading={loading} pagination={false} size="middle" />

      <Modal title={modal.editing ? 'Edit Bus' : 'New Bus'} open={modal.open}
             onOk={handleSave} onCancel={() => setModal({ open: false, editing: null })}
             confirmLoading={saving} width={480}>
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="busName" label="Bus Name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="registrationNo" label="Registration No">
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="model" label="Model">
                <Input />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="capacity" label="Capacity">
                <InputNumber style={{ width: '100%' }} min={1} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="status" label="Status">
                <Select options={STATUS_OPTIONS} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="description" label="Description">
            <Input />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Routes Tab
// ─────────────────────────────────────────────────────────────────────────────
function RoutesTab() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()
  const [routes,  setRoutes]  = useState([])
  const [loading, setLoading] = useState(false)
  const [modal,   setModal]   = useState({ open: false, editing: null })
  const [saving,  setSaving]  = useState(false)

  const load = () => {
    setLoading(true)
    api.get('/school/transport/routes')
       .then(r => setRoutes(r.data.data || []))
       .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const openCreate = () => {
    form.resetFields()
    form.setFieldsValue({ status: 'Active' })
    setModal({ open: true, editing: null })
  }

  const openEdit = (rec) => {
    form.setFieldsValue(rec)
    setModal({ open: true, editing: rec })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      if (modal.editing) {
        await api.put(`/school/transport/routes/${modal.editing.routeId}`, values)
        message.success('Route updated.')
      } else {
        await api.post('/school/transport/routes', values)
        message.success('Route created.')
      }
      setModal({ open: false, editing: null })
      load()
    } catch (e) {
      message.error(e.message || 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    { title: 'Route Name',     dataIndex: 'routeName',     key: 'routeName',     render: n => <Text strong>{n}</Text> },
    { title: 'Code',           dataIndex: 'routeCode',     key: 'routeCode',     width: 90 },
    { title: 'Category',       dataIndex: 'routeCategory', key: 'routeCategory', width: 120 },
    { title: 'Description',    dataIndex: 'description',   key: 'description' },
    { title: 'Status',         dataIndex: 'status',        key: 'status',        width: 90,
      render: s => <Tag color={s === 'Active' ? 'success' : 'default'}>{s}</Tag> },
    {
      title: '', key: 'actions', width: 60,
      render: (_, r) => <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />,
    },
  ]

  return (
    <Card extra={<Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Route</Button>}>
      <Table dataSource={routes} columns={columns} rowKey="routeId" loading={loading} pagination={false} size="middle" />

      <Modal title={modal.editing ? 'Edit Route' : 'New Route'} open={modal.open}
             onOk={handleSave} onCancel={() => setModal({ open: false, editing: null })}
             confirmLoading={saving} width={480}>
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="routeName" label="Route Name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="routeCode" label="Route Code">
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="routeCategory" label="Category">
                <Input placeholder="e.g. Morning, Evening" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="status" label="Status">
                <Select options={STATUS_OPTIONS} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="description" label="Description">
            <Input />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Bus Fee Structure Tab
// ─────────────────────────────────────────────────────────────────────────────
function BusFeeTab() {
  const { message } = AntApp.useApp()
  const [routes,       setRoutes]       = useState([])
  const [academicYears,setAcademicYears] = useState([])
  const [selectedRoute,setSelectedRoute] = useState(null)
  const [selectedYear, setSelectedYear]  = useState(null)
  const [structure,    setStructure]     = useState(null)
  const [amounts,      setAmounts]       = useState({})  // termId → amount
  const [loading,      setLoading]       = useState(false)
  const [saving,       setSaving]        = useState(false)

  useEffect(() => {
    api.get('/school/transport/routes').then(r => setRoutes(r.data.data || []))
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const years = r.data?.data || []
      setAcademicYears(years)
      const current = years.find(y => y.isCurrent)
      if (current) setSelectedYear(current.academicYearId)
    })
  }, [])

  const loadStructure = async () => {
    if (!selectedRoute || !selectedYear) {
      message.warning('Select route and academic year first.')
      return
    }
    setLoading(true)
    try {
      const r = await api.get(`/school/transport/fee-structure?routeId=${selectedRoute}&academicYearId=${selectedYear}`)
      const s = r.data.data
      setStructure(s)
      const init = {}
      s.terms.forEach(t => { init[t.termId] = t.amount ?? '' })
      setAmounts(init)
    } finally {
      setLoading(false)
    }
  }

  const handleSave = async () => {
    setSaving(true)
    try {
      await api.post('/school/transport/fee-structure', {
        routeId:        selectedRoute,
        academicYearId: selectedYear,
        items:          Object.entries(amounts)
                          .filter(([, v]) => v !== '' && Number(v) > 0)
                          .map(([termId, amount]) => ({ termId: Number(termId), amount: Number(amount) })),
      })
      message.success('Bus fee structure saved.')
      loadStructure()
    } catch (e) {
      message.error(e.message || 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card>
      <Row gutter={16} align="middle" style={{ marginBottom: 16 }} wrap>
        <Col>
          <Text strong>Route</Text>
          <Select
            style={{ display: 'block', width: 200, marginTop: 4 }}
            placeholder="Select route"
            value={selectedRoute}
            onChange={v => { setSelectedRoute(v); setStructure(null) }}
            options={routes.map(r => ({ label: r.routeName, value: r.routeId }))}
          />
        </Col>
        <Col>
          <Text strong>Academic Year</Text>
          <Select
            style={{ display: 'block', width: 160, marginTop: 4 }}
            placeholder="Select year"
            value={selectedYear}
            onChange={v => { setSelectedYear(v); setStructure(null) }}
            options={academicYears.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
          />
        </Col>
        <Col style={{ marginTop: 20 }}>
          <Button type="primary" icon={<SearchOutlined />} loading={loading} onClick={loadStructure}>Load</Button>
        </Col>
      </Row>

      {structure && (
        <>
          <Table
            dataSource={structure.terms}
            rowKey="termId"
            pagination={false}
            size="middle"
            columns={[
              { title: 'Term', dataIndex: 'termName', key: 'termName', width: 200 },
              {
                title: 'Amount (₹)',
                key: 'amount',
                render: (_, t) => (
                  <InputNumber
                    min={0}
                    value={amounts[t.termId] === '' ? null : amounts[t.termId]}
                    onChange={v => setAmounts(prev => ({ ...prev, [t.termId]: v ?? '' }))}
                    style={{ width: 140 }}
                    placeholder="0"
                    formatter={v => v ? `₹ ${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : ''}
                    parser={v => v?.replace(/₹\s?|(,*)/g, '')}
                  />
                ),
              },
            ]}
          />
          <div style={{ marginTop: 16, textAlign: 'right' }}>
            <Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={handleSave}>
              Save Fee Structure
            </Button>
          </div>
        </>
      )}
    </Card>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Student Transport Tab
// ─────────────────────────────────────────────────────────────────────────────
function StudentsTab() {
  const { message } = AntApp.useApp()
  const [form] = Form.useForm()

  // lookup data
  const [years,   setYears]   = useState([])
  const [classes, setClasses] = useState([])
  const [sections,setSections]= useState([])
  const [routes,  setRoutes]  = useState([])
  const [buses,   setBuses]   = useState([])

  // filters
  const [yearFilter,    setYearFilter]    = useState(null)
  const [classFilter,   setClassFilter]   = useState(null)
  const [sectionFilter, setSectionFilter] = useState(null)
  const [routeFilter,   setRouteFilter]   = useState(null)

  // data
  const [students,  setStudents]  = useState([])
  const [loaded,    setLoaded]    = useState(false)
  const [loading,   setLoading]   = useState(false)
  const [modal,     setModal]     = useState({ open: false, student: null })
  const [saving,    setSaving]    = useState(false)

  useEffect(() => {
    Promise.all([
      api.get('/school/master/academic-years?activeOnly=true'),
      api.get('/school/master/classes'),
      api.get('/school/transport/routes'),
      api.get('/school/transport/buses'),
    ]).then(([yr, cl, ro, bu]) => {
      const years = yr.data?.data || []
      setYears(years)
      setClasses(cl.data.data || [])
      setRoutes(ro.data.data  || [])
      setBuses(bu.data.data   || [])
      const current = years.find(y => y.isCurrent)
      if (current) setYearFilter(current.academicYearId)
    })
  }, [])

  // reload sections when class changes
  useEffect(() => {
    if (!classFilter) { setSections([]); setSectionFilter(null); return }
    api.get(`/school/master/sections?classId=${classFilter}`)
       .then(r => setSections(r.data.data || []))
       .catch(() => setSections([]))
    setSectionFilter(null)
  }, [classFilter])

  const loadData = async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (yearFilter)    params.set('academicYearId', yearFilter)
      if (classFilter)   params.set('classId',        classFilter)
      if (sectionFilter) params.set('sectionId',      sectionFilter)
      if (routeFilter)   params.set('routeId',        routeFilter)
      const r = await api.get(`/school/transport/students?${params}`)
      setStudents(r.data.data || [])
      setLoaded(true)
    } catch { message.error('Failed to load students.') }
    finally  { setLoading(false) }
  }

  const openEdit = (student) => {
    form.setFieldsValue({
      transportType: student.transportType,
      busRouteId:    student.busRouteId,
      busId:         student.busId,
      busTrip:       student.busTrip,
    })
    setModal({ open: true, student })
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      await api.put(`/school/transport/students/${modal.student.studentUniqueId}`, {
        ...values,
        academicYearId: modal.student.academicYearId,
      })
      message.success('Transport updated.')
      setModal({ open: false, student: null })
      loadData()
    } catch (e) {
      message.error(e.message || 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  const columns = [
    {
      title: 'Student',
      key: 'student',
      render: (_, s) => (
        <div>
          <div style={{ fontWeight: 500 }}>{s.studentName}</div>
          <Text type="secondary" style={{ fontSize: 12 }}>{s.admissionNo}</Text>
        </div>
      ),
    },
    { title: 'Class',          dataIndex: 'className',     key: 'className',     width: 110 },
    { title: 'Transport Type', dataIndex: 'transportType', key: 'transportType', width: 120,
      render: t => t ? <Tag color="blue">{t}</Tag> : <Text type="secondary">—</Text> },
    { title: 'Route',          dataIndex: 'routeName',     key: 'routeName',     width: 160,
      render: r => r || <Text type="secondary">—</Text> },
    { title: 'Bus',            dataIndex: 'busName',       key: 'busName',       width: 120,
      render: b => b || <Text type="secondary">—</Text> },
    { title: 'Trip',           dataIndex: 'busTrip',       key: 'busTrip',       width: 90,
      render: t => t || <Text type="secondary">—</Text> },
    {
      title: '', key: 'actions', width: 60,
      render: (_, r) => <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />,
    },
  ]

  return (
    <Card>
      {/* Filter bar */}
      <Row gutter={12} align="bottom" style={{ marginBottom: 16 }} wrap>
        <Col>
          <Text strong>Academic Year</Text>
          <Select
            style={{ display: 'block', width: 160, marginTop: 4 }}
            placeholder="All years"
            allowClear
            value={yearFilter}
            onChange={setYearFilter}
            options={years.map(y => ({ label: y.academicYear, value: y.academicYearId }))}
          />
        </Col>
        <Col>
          <Text strong>Class</Text>
          <Select
            style={{ display: 'block', width: 160, marginTop: 4 }}
            placeholder="All classes"
            allowClear
            value={classFilter}
            onChange={v => { setClassFilter(v); setSectionFilter(null) }}
            options={classes.map(c => ({ label: c.className, value: c.classId }))}
          />
        </Col>
        {sections.length > 0 && (
          <Col>
            <Text strong>Section</Text>
            <Select
              style={{ display: 'block', width: 120, marginTop: 4 }}
              placeholder="All sections"
              allowClear
              value={sectionFilter}
              onChange={setSectionFilter}
              options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
            />
          </Col>
        )}
        <Col>
          <Text strong>Route</Text>
          <Select
            style={{ display: 'block', width: 180, marginTop: 4 }}
            placeholder="All routes"
            allowClear
            value={routeFilter}
            onChange={setRouteFilter}
            options={routes.map(r => ({ label: r.routeName, value: r.routeId }))}
          />
        </Col>
        <Col style={{ marginTop: 20 }}>
          <Button type="primary" icon={<SearchOutlined />} loading={loading} onClick={loadData}>
            Load
          </Button>
        </Col>
      </Row>

      <Table
        dataSource={students}
        columns={columns}
        rowKey="studentId"
        loading={loading}
        pagination={{ pageSize: 30 }}
        size="middle"
        locale={{ emptyText: loaded ? 'No students found' : 'Select filters and click Load' }}
      />

      <Modal
        title={`Transport — ${modal.student?.studentName || ''}`}
        open={modal.open}
        onOk={handleSave}
        onCancel={() => setModal({ open: false, student: null })}
        confirmLoading={saving}
        width={440}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="transportType" label="Transport Type">
            <Select
              allowClear
              placeholder="Select type"
              options={[
                { label: 'School Bus', value: 'Bus' },
                { label: 'Own Vehicle', value: 'Own' },
                { label: 'Walking',    value: 'Walk' },
                { label: 'Other',      value: 'Other' },
              ]}
            />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="busRouteId" label="Route">
                <Select
                  allowClear
                  placeholder="Select route"
                  options={routes.map(r => ({ label: r.routeName, value: r.routeId }))}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="busId" label="Bus">
                <Select
                  allowClear
                  placeholder="Select bus"
                  options={buses.map(b => ({ label: b.busName, value: b.busId }))}
                />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="busTrip" label="Trip">
            <Input placeholder="e.g. Morning, Both" />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Main Transport Page
// ─────────────────────────────────────────────────────────────────────────────
export default function TransportPage() {
  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Transport</Title>
      <Tabs
        items={[
          { key: 'buses',       label: 'Buses',            children: <BusesTab /> },
          { key: 'routes',      label: 'Routes',           children: <RoutesTab /> },
          { key: 'fee',         label: 'Bus Fee Structure', children: <BusFeeTab /> },
          { key: 'students',    label: 'Student Assignment', children: <StudentsTab /> },
        ]}
      />
    </div>
  )
}
