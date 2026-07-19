import { useEffect, useState } from 'react'
import { Table, Button, Modal, Form, Select, Tag, Popconfirm, Alert } from 'antd'
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const WHOLE_CLASS = 0   // section dropdown value meaning "all sections"

export default function ClassTeachersTab() {
  const [years,     setYears]     = useState([])
  const [yearId,    setYearId]    = useState(null)
  const [classes,   setClasses]   = useState([])
  const [classId,   setClassId]   = useState(null)
  const [teachers,  setTeachers]  = useState([])
  const [rows,      setRows]      = useState([])
  const [loading,   setLoading]   = useState(false)
  const [open,      setOpen]      = useState(false)
  const [saving,    setSaving]    = useState(false)
  const [formSections, setFormSections] = useState([])
  const [form] = Form.useForm()

  const classOptions = classes.map((c) => ({ value: c.classId, label: c.className }))
  const teacherOptions = teachers.map((t) => ({
    value: t.userId,
    label: t.designation ? `${t.fullName} — ${t.designation}` : t.fullName,
  }))

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true')
       .then((r) => {
         const list = r.data?.data || []
         setYears(list)
         const current = list.find((y) => y.isCurrent)
         if (current) {
           setYearId(current.academicYearId)
           load(current.academicYearId, null)
         }
       })
       .catch(() => {})
    api.get('/school/master/classes')
       .then((r) => setClasses(r.data?.data || []))
       .catch(() => {})
    api.get('/school/class-teachers/teachers')
       .then((r) => setTeachers(r.data?.data || []))
       .catch(() => {})
  }, [])

  async function load(yId, cId) {
    if (!yId) { setRows([]); return }
    setLoading(true)
    try {
      const qs = `academicYearId=${yId}` + (cId ? `&classId=${cId}` : '')
      const { data } = await api.get(`/school/class-teachers?${qs}`)
      setRows(data.data || [])
    } finally {
      setLoading(false)
    }
  }

  function onYearChange(val) {
    setYearId(val)
    load(val, classId)
  }

  function onClassChange(val) {
    setClassId(val)
    load(yearId, val)
  }

  // Sections for the modal's class picker — loaded per class, like SectionsTab.
  async function onFormClassChange(cId) {
    form.setFieldsValue({ sectionId: WHOLE_CLASS })
    if (!cId) { setFormSections([]); return }
    const { data } = await api.get(`/school/master/sections?classId=${cId}`)
    setFormSections(data.data || [])
  }

  function openCreate() {
    form.resetFields()
    form.setFieldsValue({ sectionId: WHOLE_CLASS })
    setFormSections([])
    setOpen(true)
  }

  async function onSave() {
    const vals = await form.validateFields()
    setSaving(true)
    try {
      await api.post('/school/class-teachers', {
        academicYearId: yearId,
        classId:        vals.classId,
        sectionId:      vals.sectionId || null,   // 0 -> null = whole class
        userId:         vals.userId,
      })
      setOpen(false)
      load(yearId, classId)
    } finally {
      setSaving(false)
    }
  }

  async function onDelete(record) {
    await api.delete(`/school/class-teachers/${record.assignmentId}`)
    load(yearId, classId)
  }

  const columns = [
    { title: 'Class', dataIndex: 'className', key: 'className' },
    {
      title: 'Section', dataIndex: 'sectionName', key: 'sectionName',
      render: (v) => (v ? <Tag>{v}</Tag> : <Tag color="blue">All sections</Tag>),
    },
    { title: 'Teacher',     dataIndex: 'fullName',    key: 'fullName' },
    { title: 'Designation', dataIndex: 'designation', key: 'designation' },
    { title: 'Assigned by', dataIndex: 'createdBy',   key: 'createdBy' },
    {
      title: '', key: 'actions', width: 60,
      render: (_, record) => (
        <Popconfirm
          title="Unassign this teacher?"
          description="Parents of this class will no longer be able to message them."
          okText="Unassign"
          okButtonProps={{ danger: true }}
          onConfirm={() => onDelete(record)}
        >
          <Button size="small" danger icon={<DeleteOutlined />} />
        </Popconfirm>
      ),
    },
  ]

  return (
    <div>
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Class teachers receive messages from the parents of their class."
        description="A teacher assigned with 'All sections' covers every section of the class. Several teachers can share a class — any of them can reply. Parents of a class with no teacher assigned cannot start a message."
      />

      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'center' }}>
        <Select
          placeholder="Academic year"
          options={years.map((y) => ({ value: y.academicYearId, label: y.academicYear }))}
          style={{ width: 180 }}
          value={yearId}
          onChange={onYearChange}
        />
        <Select
          placeholder="All classes"
          options={classOptions}
          style={{ width: 220 }}
          value={classId}
          onChange={onClassChange}
          allowClear
        />
        <Button type="primary" icon={<PlusOutlined />} disabled={!yearId} onClick={openCreate}>
          Assign Teacher
        </Button>
      </div>

      <Table
        rowKey="assignmentId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        pagination={false}
        locale={{ emptyText: yearId ? 'No class teachers assigned yet.' : 'Select an academic year.' }}
      />

      <Modal
        title="Assign Class Teacher"
        open={open}
        onOk={onSave}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        okText="Assign"
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="classId" label="Class" rules={[{ required: true, message: 'Class is required.' }]}>
            <Select placeholder="Select a class" options={classOptions} onChange={onFormClassChange} />
          </Form.Item>
          <Form.Item name="sectionId" label="Section">
            <Select
              options={[
                { value: WHOLE_CLASS, label: 'All sections (whole class)' },
                ...formSections.map((s) => ({ value: s.sectionId, label: s.sectionName })),
              ]}
            />
          </Form.Item>
          <Form.Item name="userId" label="Teacher" rules={[{ required: true, message: 'Teacher is required.' }]}>
            <Select
              placeholder="Select a teacher"
              options={teacherOptions}
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
