import { useEffect, useState } from 'react'
import {
  Table, Button, Select, InputNumber, Checkbox, Space, Alert, message, Popconfirm, Tag,
} from 'antd'
import { PlusOutlined, DeleteOutlined, SaveOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

// One editable row in the working set. `key` is stable for the table; class_subject_id
// is not used client-side because Save replaces the whole set for the class+year.
let rowSeq = 0
const newRow = (over = {}) => ({
  key: `r${rowSeq++}`,
  subjectId: null,
  displayOrder: null,
  isOptional: false,
  ...over,
})

export default function ClassSubjectsTab() {
  const [years,    setYears]    = useState([])
  const [yearId,   setYearId]   = useState(null)
  const [classes,  setClasses]  = useState([])
  const [classId,  setClassId]  = useState(null)
  const [subjects, setSubjects] = useState([])   // available subjects for the year
  const [rows,     setRows]     = useState([])   // working set (editable)
  const [loading,  setLoading]  = useState(false)
  const [saving,   setSaving]   = useState(false)

  const classOptions = classes.map((c) => ({ value: c.classId, label: c.className }))

  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true')
       .then((r) => {
         const list = r.data?.data || []
         setYears(list)
         const current = list.find((y) => y.isCurrent)
         if (current) {
           setYearId(current.academicYearId)
           loadSubjects(current.academicYearId)
         }
       })
       .catch(() => {})
    api.get('/school/master/classes')
       .then((r) => setClasses(r.data?.data || []))
       .catch(() => {})
  }, [])

  async function loadSubjects(yId) {
    if (!yId) { setSubjects([]); return }
    const { data } = await api.get(`/school/class-subjects/available-subjects?academicYearId=${yId}`)
    setSubjects(data.data || [])
  }

  async function loadMapping(yId, cId) {
    if (!yId || !cId) { setRows([]); return }
    setLoading(true)
    try {
      const { data } = await api.get(`/school/class-subjects?academicYearId=${yId}&classId=${cId}`)
      const mapped = data.data || []
      if (mapped.length > 0) {
        setRows(mapped.map((m) => newRow({
          subjectId:    m.subjectId,
          displayOrder: m.displayOrder,
          isOptional:   m.isOptional,
        })))
      } else {
        // No subjects assigned yet → prefill EVERY subject as a row so the user can
        // just delete the ones this class doesn't take, then Save.
        let avail = subjects
        if (avail.length === 0) {
          const r = await api.get(`/school/class-subjects/available-subjects?academicYearId=${yId}`)
          avail = r.data?.data || []
          setSubjects(avail)
        }
        setRows(avail.map((s, i) => newRow({ subjectId: s.subjectId, displayOrder: i + 1, isOptional: false })))
      }
    } finally {
      setLoading(false)
    }
  }

  function onYearChange(val) {
    setYearId(val)
    setRows([])
    loadSubjects(val)
    if (classId) loadMapping(val, classId)
  }

  function onClassChange(val) {
    setClassId(val)
    loadMapping(yearId, val)
  }

  function updateRow(key, patch) {
    setRows((rs) => rs.map((r) => (r.key === key ? { ...r, ...patch } : r)))
  }

  function removeRow(key) {
    setRows((rs) => rs.filter((r) => r.key !== key))
  }

  function addRow() {
    // Default order = next multiple after the current max, for convenience.
    const maxOrder = rows.reduce((m, r) => Math.max(m, r.displayOrder || 0), 0)
    setRows((rs) => [...rs, newRow({ displayOrder: maxOrder + 1 })])
  }

  async function save() {
    if (!yearId || !classId) return
    // Guard: every row needs a subject, and no duplicates.
    const chosen = rows.map((r) => r.subjectId).filter((s) => s != null)
    if (chosen.length !== rows.length) {
      message.error('Every row must have a subject selected.')
      return
    }
    if (new Set(chosen).size !== chosen.length) {
      message.error('The same subject is added more than once.')
      return
    }
    setSaving(true)
    try {
      await api.post('/school/class-subjects', {
        academicYearId: yearId,
        classId,
        subjects: rows.map((r) => ({
          subjectId:    r.subjectId,
          displayOrder: r.displayOrder,
          isOptional:   r.isOptional,
        })),
      })
      message.success('Class subjects saved.')
      loadMapping(yearId, classId)
    } finally {
      setSaving(false)
    }
  }

  // A subject can only be picked once — hide subjects already chosen in other rows.
  const subjectOptionsFor = (rowKey) => {
    const takenElsewhere = new Set(
      rows.filter((r) => r.key !== rowKey && r.subjectId != null).map((r) => r.subjectId),
    )
    return subjects
      .filter((s) => !takenElsewhere.has(s.subjectId))
      .map((s) => ({ value: s.subjectId, label: s.shortName ? `${s.subjectName} (${s.shortName})` : s.subjectName }))
  }

  const columns = [
    {
      title: 'Subject', key: 'subject', width: 260,
      render: (_, r) => (
        <Select
          placeholder="Select subject"
          style={{ width: '100%' }}
          value={r.subjectId}
          options={subjectOptionsFor(r.key)}
          showSearch
          optionFilterProp="label"
          onChange={(v) => updateRow(r.key, { subjectId: v })}
        />
      ),
    },
    {
      title: 'Order', key: 'order', width: 90,
      render: (_, r) => (
        <InputNumber
          min={0}
          style={{ width: '100%' }}
          value={r.displayOrder}
          onChange={(v) => updateRow(r.key, { displayOrder: v })}
        />
      ),
    },
    {
      title: 'Elective', key: 'isOptional', width: 90, align: 'center',
      render: (_, r) => (
        <Checkbox
          checked={r.isOptional}
          onChange={(e) => updateRow(r.key, { isOptional: e.target.checked })}
        />
      ),
    },
    {
      title: '', key: 'actions', width: 50,
      render: (_, r) => (
        <Button size="small" danger icon={<DeleteOutlined />} onClick={() => removeRow(r.key)} />
      ),
    },
  ]

  const ready = yearId && classId

  return (
    <div>
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Map the subjects each class studies this year."
        description="Only the subjects added here appear in that class's marks entry and report cards. A class with none assigned is pre-filled with every subject — just delete the ones it doesn't take, then Save. Saving replaces the whole set for the selected class and year."
      />

      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'center', flexWrap: 'wrap' }}>
        <Select
          placeholder="Academic year"
          options={years.map((y) => ({ value: y.academicYearId, label: y.academicYear }))}
          style={{ width: 180 }}
          value={yearId}
          onChange={onYearChange}
        />
        <Select
          placeholder="Select a class"
          options={classOptions}
          style={{ width: 220 }}
          value={classId}
          onChange={onClassChange}
          showSearch
          optionFilterProp="label"
        />
        {ready && (
          <Tag color="blue">{rows.length} subject{rows.length === 1 ? '' : 's'}</Tag>
        )}
      </div>

      {ready ? (
        <>
          <Table
            rowKey="key"
            dataSource={rows}
            columns={columns}
            loading={loading}
            pagination={false}
            size="small"
            locale={{ emptyText: 'No subjects mapped yet. Click "Add Subject" to start.' }}
          />
          <Space style={{ marginTop: 16 }}>
            <Button icon={<PlusOutlined />} onClick={addRow} disabled={subjects.length === 0}>
              Add Subject
            </Button>
            <Popconfirm
              title="Save class subjects?"
              description="This replaces the whole subject list for this class and year."
              okText="Save"
              onConfirm={save}
            >
              <Button type="primary" icon={<SaveOutlined />} loading={saving}>Save</Button>
            </Popconfirm>
          </Space>
          {subjects.length === 0 && (
            <Alert
              type="warning"
              showIcon
              style={{ marginTop: 12 }}
              message="No subjects found. Add subjects under Master Data → Subjects first."
            />
          )}
        </>
      ) : (
        <Alert type="warning" showIcon message="Select an academic year and a class to map subjects." />
      )}
    </div>
  )
}
