import { useEffect, useState } from 'react'
import {
  Table, Button, Modal, Form, Input, InputNumber, Select, Space, Alert, Popconfirm,
  Tag, Card, Tooltip, App as AntApp,
} from 'antd'
import { PlusOutlined, DeleteOutlined, SaveOutlined, EditOutlined } from '@ant-design/icons'
import api, { apiError } from '../../api/axiosInstance'

const STATUS_OPTIONS = [
  { value: 'Active',   label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
]

let rowSeq = 0
const newRow = (over = {}) => ({
  key: `g${rowSeq++}`,
  minMarks: null,
  maxMarks: null,
  grade: '',
  gradePoint: '',
  description: '',
  ...over,
})

const toRow = (b) => newRow({
  minMarks:    b.minMarks,
  maxMarks:    b.maxMarks,
  grade:       b.grade || '',
  gradePoint:  b.gradePoint || '',
  description: b.description || '',
})

/** Same rules as the server — returns an error message or null. */
function validate(rows) {
  for (let i = 0; i < rows.length; i++) {
    const r = rows[i]
    const n = `Row ${i + 1}`
    if (!r.grade.trim()) return `${n}: grade is required.`
    if (r.minMarks == null || r.maxMarks == null) return `${n}: min and max marks are required.`
    if (r.minMarks > r.maxMarks) return `${n}: min marks cannot exceed max marks.`
  }
  const grades = rows.map((r) => r.grade.trim().toUpperCase())
  const dup = grades.find((g, i) => grades.indexOf(g) !== i)
  if (dup) return `Grade '${dup}' is used more than once.`
  const sorted = [...rows].sort((a, b) => a.minMarks - b.minMarks)
  for (let i = 1; i < sorted.length; i++) {
    if (sorted[i].minMarks <= sorted[i - 1].maxMarks) {
      const a = sorted[i - 1], b = sorted[i]
      return `Grades '${a.grade}' (${a.minMarks}–${a.maxMarks}) and '${b.grade}' (${b.minMarks}–${b.maxMarks}) overlap.`
    }
  }
  return null
}

export default function MarksGradeMasterTab() {
  const { message } = AntApp.useApp()

  const [scales,   setScales]   = useState([])
  const [selected, setSelected] = useState(null)   // the scale whose bands are open
  const [rows,     setRows]     = useState([])     // working band set (editable)
  const [loading,  setLoading]  = useState(false)
  const [saving,   setSaving]   = useState(false)

  const [open,    setOpen]    = useState(false)    // add/edit scale modal
  const [editing, setEditing] = useState(null)
  const [form]    = Form.useForm()

  useEffect(() => { loadScales() }, [])

  async function loadScales(keepId) {
    setLoading(true)
    try {
      const { data } = await api.get('/school/marks-grades')
      const list = data.data || []
      setScales(list)
      const keep = keepId ?? selected?.id
      const still = list.find((s) => s.id === keep)
      setSelected(still || null)
      if (!still) setRows([])
    } catch (e) {
      message.error(apiError(e, 'Failed to load grade scales.'))
    } finally {
      setLoading(false)
    }
  }

  async function openBands(scale) {
    setSelected(scale)
    setLoading(true)
    try {
      const { data } = await api.get(`/school/marks-grades/${scale.id}/bands`)
      setRows((data.data || []).map(toRow))
    } catch (e) {
      message.error(apiError(e, 'Failed to load grade bands.'))
      setRows([])
    } finally {
      setLoading(false)
    }
  }

  function openCreate() {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ status: 'Active' })
    setOpen(true)
  }

  function openEdit(scale) {
    setEditing(scale)
    form.setFieldsValue({
      scaleName:   scale.scaleName,
      description: scale.description,
      status:      scale.status || 'Active',
    })
    setOpen(true)
  }

  async function saveScale() {
    const v = await form.validateFields()
    setSaving(true)
    try {
      if (editing) {
        await api.put(`/school/marks-grades/${editing.id}`, v)
        message.success('Scale saved.')
        setOpen(false)
        loadScales(editing.id)
      } else {
        const { data } = await api.post('/school/marks-grades', v)
        message.success('Scale created.')
        setOpen(false)
        const id = data?.data?.id
        await loadScales(id)
        if (id) openBands({ id, scaleName: v.scaleName })
      }
    } catch (e) {
      message.error(apiError(e, 'Failed to save the scale.'))
    } finally {
      setSaving(false)
    }
  }

  async function deleteScale(scale) {
    try {
      await api.delete(`/school/marks-grades/${scale.id}`)
      message.success('Scale deleted.')
      if (selected?.id === scale.id) { setSelected(null); setRows([]) }
      loadScales()
    } catch (e) {
      message.error(apiError(e, 'Failed to delete the scale.'))
    }
  }

  const updateRow = (key, patch) => setRows((rs) => rs.map((r) => (r.key === key ? { ...r, ...patch } : r)))
  const removeRow = (key) => setRows((rs) => rs.filter((r) => r.key !== key))
  const addRow    = () => setRows((rs) => [...rs, newRow()])

  async function saveBands() {
    const err = validate(rows)
    if (err) { message.error(err); return }
    setSaving(true)
    try {
      const { data } = await api.put(`/school/marks-grades/${selected.id}/bands`, {
        bands: rows.map((r) => ({
          minMarks:    r.minMarks,
          maxMarks:    r.maxMarks,
          grade:       r.grade.trim(),
          gradePoint:  r.gradePoint.trim() || null,
          description: r.description.trim() || null,
        })),
      })
      setRows((data.data || []).map(toRow))
      message.success('Grade bands saved.')
      loadScales(selected.id)
    } catch (e) {
      message.error(apiError(e, 'Failed to save grade bands.'))
    } finally {
      setSaving(false)
    }
  }

  const scaleColumns = [
    { title: 'Scale Name', dataIndex: 'scaleName' },
    { title: 'Description', dataIndex: 'description', render: (v) => v || '—' },
    { title: 'Bands', dataIndex: 'bandCount', width: 90, align: 'center',
      render: (v) => (v ? v : <Tag color="warning">none</Tag>) },
    { title: 'Used by exams', dataIndex: 'examCount', width: 130, align: 'center',
      render: (v) => v || '—' },
    { title: 'Status', dataIndex: 'status', width: 100,
      render: (v) => <Tag color={v === 'Active' ? 'green' : 'default'}>{v}</Tag> },
    {
      title: '', key: 'actions', width: 150,
      render: (_, r) => (
        <Space>
          <Button size="small" onClick={() => openBands(r)}>Bands</Button>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Tooltip title={r.examCount > 0 ? 'Used by exams — change those exams first, or set it Inactive.' : ''}>
            <span>
              <Popconfirm title={`Delete "${r.scaleName}" and its bands?`} okText="Delete"
                okButtonProps={{ danger: true }} onConfirm={() => deleteScale(r)} disabled={r.examCount > 0}>
                <Button size="small" danger icon={<DeleteOutlined />} disabled={r.examCount > 0} />
              </Popconfirm>
            </span>
          </Tooltip>
        </Space>
      ),
    },
  ]

  const bandColumns = [
    {
      title: 'Min Marks', key: 'min', width: 120,
      render: (_, r) => (
        <InputNumber min={0} precision={2} style={{ width: '100%' }} value={r.minMarks}
                     onChange={(v) => updateRow(r.key, { minMarks: v })} />
      ),
    },
    {
      title: 'Max Marks', key: 'max', width: 120,
      render: (_, r) => (
        <InputNumber min={0} precision={2} style={{ width: '100%' }} value={r.maxMarks}
                     onChange={(v) => updateRow(r.key, { maxMarks: v })} />
      ),
    },
    {
      title: 'Grade', key: 'grade', width: 110,
      render: (_, r) => (
        <Input maxLength={10} placeholder="A1" value={r.grade}
               onChange={(e) => updateRow(r.key, { grade: e.target.value })} />
      ),
    },
    {
      title: 'Grade Point', key: 'gradePoint', width: 120,
      render: (_, r) => (
        <Input maxLength={10} placeholder="A" value={r.gradePoint}
               onChange={(e) => updateRow(r.key, { gradePoint: e.target.value })} />
      ),
    },
    {
      title: 'Description', key: 'description',
      render: (_, r) => (
        <Input maxLength={100} placeholder="e.g. Outstanding" value={r.description}
               onChange={(e) => updateRow(r.key, { description: e.target.value })} />
      ),
    },
    {
      title: '', key: 'actions', width: 50,
      render: (_, r) => <Button size="small" danger icon={<DeleteOutlined />} onClick={() => removeRow(r.key)} />,
    },
  ]

  return (
    <div>
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Grading scales for a student's TOTAL marks (raw marks, not %)."
        description="A scale is a reusable set of bands, e.g. 'Out of 300'. It is not tied to a class, section or year — each exam picks its scale under Master Data → Exam Master. Per-subject bands are set under Subjects Grade Master."
      />

      <Space style={{ marginBottom: 12 }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Add Scale</Button>
      </Space>

      <Table
        rowKey="id"
        dataSource={scales}
        columns={scaleColumns}
        loading={loading}
        size="small"
        pagination={false}
        onRow={(r) => ({ onClick: () => openBands(r), style: { cursor: 'pointer' } })}
        rowClassName={(r) => (selected?.id === r.id ? 'ant-table-row-selected' : '')}
        locale={{ emptyText: 'No grading scales yet. Click "Add Scale" to create one.' }}
      />

      {selected && (
        <Card
          size="small"
          style={{ marginTop: 16 }}
          title={`Bands — ${selected.scaleName}`}
          extra={
            <Space>
              <Button size="small" icon={<PlusOutlined />} onClick={addRow}>Add Band</Button>
              <Popconfirm
                title="Save grade bands?"
                description={`This replaces all bands of "${selected.scaleName}".`}
                okText="Save"
                onConfirm={saveBands}
              >
                <Button size="small" type="primary" icon={<SaveOutlined />} loading={saving}>Save</Button>
              </Popconfirm>
            </Space>
          }
        >
          <Table
            rowKey="key"
            dataSource={rows}
            columns={bandColumns}
            pagination={false}
            size="small"
            scroll={{ x: 'max-content' }}
            locale={{ emptyText: 'No bands yet. Click "Add Band" to start.' }}
          />
        </Card>
      )}

      <Modal
        title={editing ? 'Edit Scale' : 'Add Scale'}
        open={open}
        onOk={saveScale}
        onCancel={() => setOpen(false)}
        confirmLoading={saving}
        okText={editing ? 'Save' : 'Add'}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="scaleName" label="Scale Name"
            rules={[{ required: true, message: 'Scale name is required.' }]}>
            <Input maxLength={100} placeholder="e.g. Out of 300" />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input maxLength={200} placeholder="Optional — e.g. FA exams, total 300" />
          </Form.Item>
          <Form.Item name="status" label="Status">
            <Select options={STATUS_OPTIONS} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
