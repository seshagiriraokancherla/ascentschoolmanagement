import { useEffect, useState } from 'react'
import {
  Table, Button, Input, Select, Space, Avatar, Tag, Card, Row, Col,
  Modal, Form, Typography, App as AntApp, Tooltip, Popconfirm,
} from 'antd'
import { PlusOutlined, SearchOutlined, UserOutlined, EditOutlined, SwapOutlined, StopOutlined, CheckCircleOutlined, PauseCircleOutlined, PlayCircleOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import api from '../../api/axiosInstance'

const { Text } = Typography

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'

const STATUS_OPTIONS = [
  { value: '',         label: 'All Status' },
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
  { value: 'Blocked',  label: 'Blocked' },
  { value: 'TC',       label: 'TC Issued' },
]

export default function StudentsPage() {
  const navigate = useNavigate()
  const { message } = AntApp.useApp()
  const [sectionForm] = Form.useForm()

  const [students,      setStudents]      = useState([])
  const [loading,       setLoading]       = useState(false)
  const [classes,       setClasses]       = useState([])
  const [sections,      setSections]      = useState([])
  const [academicYears, setAcademicYears] = useState([])

  // Change-section modal state
  const [sectionModal,     setSectionModal]     = useState({ open: false, student: null })
  const [modalSections,    setModalSections]    = useState([])
  const [modalClassId,     setModalClassId]     = useState(null)
  const [savingSection,    setSavingSection]    = useState(false)

  // Block student modal state
  const [blockModal,  setBlockModal]  = useState({ open: false, student: null })
  const [blockReason, setBlockReason] = useState('')
  const [savingBlock, setSavingBlock] = useState(false)

  // Detain student modal state
  const [detainModal,  setDetainModal]  = useState({ open: false, student: null })
  const [detainReason, setDetainReason] = useState('')
  const [savingDetain, setSavingDetain] = useState(false)

  // Filter state
  const [search,         setSearch]         = useState('')
  const [classId,        setClassId]        = useState(null)
  const [sectionId,      setSectionId]      = useState(null)
  const [academicYearId, setAcademicYearId] = useState(null)
  const [status,         setStatus]         = useState('')

  const load = async (params = {}) => {
    setLoading(true)
    try {
      const q = new URLSearchParams()
      if (params.search)         q.set('search',         params.search)
      if (params.classId)        q.set('classId',        params.classId)
      if (params.sectionId)      q.set('sectionId',      params.sectionId)
      if (params.academicYearId) q.set('academicYearId', params.academicYearId)
      if (params.status)         q.set('status',         params.status)
      const res = await api.get(`/school/students?${q}`)
      setStudents(res.data?.data || [])
    } finally {
      setLoading(false)
    }
  }

  async function loadSections(cId) {
    if (!cId) { setSections([]); return }
    try {
      const res = await api.get(`/school/master/sections?classId=${cId}`)
      setSections(res.data?.data || [])
    } catch { setSections([]) }
  }

  useEffect(() => {
    Promise.all([
      api.get('/school/master/academic-years?activeOnly=true'),
      api.get('/school/master/classes'),
    ]).then(([yr, cl]) => {
      const years = yr.data?.data || []
      setAcademicYears(years)
      setClasses(cl.data?.data || [])
      const current = years.find(y => y.isCurrent)
      const curYearId = current?.academicYearId ?? null
      if (curYearId) setAcademicYearId(curYearId)
      load({ search, classId, sectionId, academicYearId: curYearId, status })
    }).catch(() => {
      load({ search, classId, sectionId, academicYearId: null, status })
    })
  }, [])

  const handleSearch = () => load({ search, classId, sectionId, academicYearId, status })

  function onClassFilterChange(val) {
    setClassId(val)
    setSectionId(null)
    loadSections(val)
    load({ search, classId: val, sectionId: null, academicYearId, status })
  }

  const openSectionModal = async (student) => {
    setModalClassId(student.classId)
    sectionForm.setFieldsValue({ sectionId: student.sectionId || null })
    setSectionModal({ open: true, student })
    try {
      const r = await api.get(`/school/master/sections?classId=${student.classId}`)
      setModalSections(r.data?.data || [])
    } catch { setModalSections([]) }
  }

  const handleSectionModalClassChange = async (cId) => {
    setModalClassId(cId)
    sectionForm.setFieldsValue({ sectionId: null })
    try {
      const r = await api.get(`/school/master/sections?classId=${cId}`)
      setModalSections(r.data?.data || [])
    } catch { setModalSections([]) }
  }

  const handleSaveSection = async () => {
    const values = await sectionForm.validateFields()
    setSavingSection(true)
    try {
      await api.put(`/school/students/${sectionModal.student.studentId}/section`, { sectionId: values.sectionId })
      message.success('Section updated.')
      setSectionModal({ open: false, student: null })
      load({ search, classId, sectionId, academicYearId, status })
    } catch (e) {
      message.error(e.message || 'Failed to update section.')
    } finally {
      setSavingSection(false)
    }
  }

  const openBlockModal = (student) => {
    setBlockReason('')
    setBlockModal({ open: true, student })
  }

  const handleBlock = async () => {
    if (!blockReason.trim()) { message.warning('Enter a block reason.'); return }
    setSavingBlock(true)
    try {
      await api.put(`/school/students/${blockModal.student.studentId}/block`, { reason: blockReason.trim() })
      message.success('Student blocked.')
      setBlockModal({ open: false, student: null })
      load({ search, classId, sectionId, academicYearId, status })
    } catch (e) {
      message.error(e.message || 'Failed to block student.')
    } finally {
      setSavingBlock(false)
    }
  }

  const handleUnblock = async (student) => {
    try {
      await api.put(`/school/students/${student.studentId}/unblock`)
      message.success('Student unblocked.')
      load({ search, classId, sectionId, academicYearId, status })
    } catch (e) {
      message.error(e.message || 'Failed to unblock student.')
    }
  }

  const openDetainModal = (student) => {
    setDetainReason('')
    setDetainModal({ open: true, student })
  }

  const handleDetain = async () => {
    if (!detainReason.trim()) { message.warning('Enter a detention reason.'); return }
    setSavingDetain(true)
    try {
      await api.put(`/school/students/${detainModal.student.studentId}/detain`, { reason: detainReason.trim() })
      message.success('Student marked as detained.')
      setDetainModal({ open: false, student: null })
      load({ search, classId, sectionId, academicYearId, status })
    } catch (e) {
      message.error(e.message || 'Failed to detain student.')
    } finally {
      setSavingDetain(false)
    }
  }

  const handleRelease = async (student) => {
    try {
      await api.put(`/school/students/${student.studentId}/release`)
      message.success('Detention cleared.')
      load({ search, classId, sectionId, academicYearId, status })
    } catch (e) {
      message.error(e.message || 'Failed to release student.')
    }
  }

  const classOptions        = [{ value: null, label: 'All Classes' },   ...classes.map((c) => ({ value: c.classId,        label: c.className }))]
  const sectionOptions      = [{ value: null, label: 'All Sections' },  ...sections.map((s) => ({ value: s.sectionId,     label: s.sectionName }))]
  const academicYearOptions = academicYears.map((y) => ({ value: y.academicYearId, label: y.academicYear }))

  const columns = [
    {
      title:     'Photo',
      dataIndex: 'photoPath',
      key:       'photoPath',
      width:     60,
      render:    (path) =>
        path
          ? <Avatar src={`${API_BASE}${path}`} size={40} />
          : <Avatar icon={<UserOutlined />} size={40} />,
    },
    {
      title:     'Admission No',
      dataIndex: 'admissionNo',
      key:       'admissionNo',
      width:     120,
    },
    {
      title:     'Student Name',
      dataIndex: 'studentName',
      key:       'studentName',
    },
    {
      title:  'Class / Section',
      key:    'classSection',
      width:  150,
      render: (_, r) => r.sectionName ? `${r.className} - ${r.sectionName}` : r.className,
    },
    {
      title:     'Gender',
      dataIndex: 'gender',
      key:       'gender',
      width:     80,
    },
    {
      title:  'Status',
      key:    'status',
      width:  150,
      render: (_, record) => {
        const { status, blockedReason, isDetained, detainedReason } = record
        const color = status === 'Active' ? 'green' : status === 'Blocked' ? 'red' : status === 'TC' ? 'orange' : 'default'
        return (
          <Space size={4} wrap>
            <Tooltip title={status === 'Blocked' && blockedReason ? `Blocked: ${blockedReason}` : ''}>
              <Tag color={color}>{status}</Tag>
            </Tooltip>
            {isDetained && (
              <Tooltip title={detainedReason ? `Detained: ${detainedReason}` : 'Detained'}>
                <Tag color="volcano" icon={<PauseCircleOutlined />}>Detained</Tag>
              </Tooltip>
            )}
          </Space>
        )
      },
    },
    {
      title:  'Actions',
      key:    'actions',
      width:  230,
      render: (_, record) => (
        <Space size={4} wrap>
          <Button size="small" icon={<EditOutlined />} onClick={() => navigate(`/students/${record.studentId}`)}>
            Edit
          </Button>
          <Button size="small" icon={<SwapOutlined />} onClick={() => openSectionModal(record)}>
            Section
          </Button>
          {record.status === 'Blocked' ? (
            <Popconfirm title="Unblock this student?" onConfirm={() => handleUnblock(record)} okText="Yes">
              <Button size="small" icon={<CheckCircleOutlined />} style={{ color: '#52c41a', borderColor: '#52c41a' }}>
                Unblock
              </Button>
            </Popconfirm>
          ) : (
            <Button size="small" danger icon={<StopOutlined />} onClick={() => openBlockModal(record)}>
              Block
            </Button>
          )}
          {record.isDetained ? (
            <Popconfirm title="Clear detention for this student?" onConfirm={() => handleRelease(record)} okText="Yes">
              <Button size="small" icon={<PlayCircleOutlined />} style={{ color: '#fa8c16', borderColor: '#fa8c16' }}>
                Release
              </Button>
            </Popconfirm>
          ) : (
            <Button size="small" icon={<PauseCircleOutlined />} onClick={() => openDetainModal(record)}
              style={{ color: '#ad4e00', borderColor: '#ad4e00' }}>
              Detain
            </Button>
          )}
        </Space>
      ),
    },
  ]

  return (
    <Card
      title="Students"
      extra={
        <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/students/new')}>
          Register Student
        </Button>
      }
    >
      {/* Filters */}
      <Row gutter={12} style={{ marginBottom: 16 }}>
        <Col flex="auto">
          <Input
            placeholder="Search by name or admission no"
            prefix={<SearchOutlined />}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onPressEnter={handleSearch}
            allowClear
            onClear={() => { setSearch(''); load({ classId, academicYearId, status }) }}
          />
        </Col>
        <Col>
          <Select
            style={{ width: 160 }}
            value={academicYearId}
            options={academicYearOptions}
            onChange={(v) => { setAcademicYearId(v); load({ search, classId, academicYearId: v, status }) }}
          />
        </Col>
        <Col>
          <Select
            style={{ width: 150 }}
            value={classId}
            options={classOptions}
            onChange={onClassFilterChange}
          />
        </Col>
        <Col>
          <Select
            style={{ width: 140 }}
            value={sectionId}
            options={sectionOptions}
            disabled={sections.length === 0}
            onChange={(v) => { setSectionId(v); load({ search, classId, sectionId: v, academicYearId, status }) }}
          />
        </Col>
        <Col>
          <Select
            style={{ width: 120 }}
            value={status}
            options={STATUS_OPTIONS}
            onChange={(v) => { setStatus(v); load({ search, classId, sectionId, academicYearId, status: v }) }}
          />
        </Col>
        <Col>
          <Button icon={<SearchOutlined />} onClick={handleSearch}>Search</Button>
        </Col>
      </Row>

      <Table
        rowKey="studentId"
        dataSource={students}
        columns={columns}
        loading={loading}
        size="small"
        pagination={{ pageSize: 20, showTotal: (total) => `${total} students` }}
      />

      {/* Block Student Modal */}
      <Modal
        title={<><StopOutlined style={{ color: '#ff4d4f', marginRight: 8 }} />Block Student</>}
        open={blockModal.open}
        onOk={handleBlock}
        onCancel={() => setBlockModal({ open: false, student: null })}
        okText="Block Student"
        okButtonProps={{ danger: true, loading: savingBlock }}
        width={400}
      >
        {blockModal.student && (
          <div style={{ marginBottom: 12 }}>
            <Text strong>{blockModal.student.studentName}</Text>
            <Text type="secondary" style={{ marginLeft: 8 }}>({blockModal.student.admissionNo})</Text>
          </div>
        )}
        <div style={{ marginBottom: 6 }}>
          <Text strong>Reason <span style={{ color: 'red' }}>*</span></Text>
        </div>
        <Input.TextArea
          rows={3}
          maxLength={200}
          showCount
          placeholder="e.g. Fee defaulter — Term 2 pending, Disciplinary action, etc."
          value={blockReason}
          onChange={e => setBlockReason(e.target.value)}
        />
      </Modal>

      {/* Detain Student Modal */}
      <Modal
        title={<><PauseCircleOutlined style={{ color: '#ad4e00', marginRight: 8 }} />Detain Student</>}
        open={detainModal.open}
        onOk={handleDetain}
        onCancel={() => setDetainModal({ open: false, student: null })}
        okText="Mark as Detained"
        okButtonProps={{ style: { background: '#ad4e00', borderColor: '#ad4e00' }, loading: savingDetain }}
        width={400}
      >
        {detainModal.student && (
          <div style={{ marginBottom: 12 }}>
            <Text strong>{detainModal.student.studentName}</Text>
            <Text type="secondary" style={{ marginLeft: 8 }}>({detainModal.student.admissionNo})</Text>
          </div>
        )}
        <div style={{ marginBottom: 6 }}>
          <Text strong>Reason <span style={{ color: 'red' }}>*</span></Text>
        </div>
        <Input.TextArea
          rows={3}
          maxLength={200}
          showCount
          placeholder="e.g. Failed in 3 subjects, Low attendance (below 75%), etc."
          value={detainReason}
          onChange={e => setDetainReason(e.target.value)}
        />
      </Modal>

      <Modal
        title="Change Section"
        open={sectionModal.open}
        onOk={handleSaveSection}
        onCancel={() => setSectionModal({ open: false, student: null })}
        confirmLoading={savingSection}
        width={380}
      >
        {sectionModal.student && (
          <div style={{ marginBottom: 12 }}>
            <Text strong>{sectionModal.student.studentName}</Text>
            <Text type="secondary" style={{ marginLeft: 8 }}>
              ({sectionModal.student.admissionNo})
            </Text>
          </div>
        )}
        <Form form={sectionForm} layout="vertical">
          <Form.Item label="Class">
            <Select
              value={modalClassId}
              onChange={handleSectionModalClassChange}
              options={classes.map(c => ({ label: c.className, value: c.classId }))}
            />
          </Form.Item>
          <Form.Item name="sectionId" label="New Section" rules={[{ required: true, message: 'Select a section' }]}>
            <Select
              placeholder="Select section"
              options={modalSections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
              disabled={modalSections.length === 0}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  )
}
